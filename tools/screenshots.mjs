// Captura de telas via Chrome DevTools Protocol (sem dependências; node --experimental-websocket)
// Uso: node --experimental-websocket tools/screenshots.mjs <pasta> [nomes,separados,por,vírgula]
// Variáveis: BASE (padrão http://localhost:5299), PENDING_EMAIL (conta com e-mail não confirmado),
//            MFA_EMAIL/MFA_PASSWORD (conta com MFA ativo para o print da 2ª etapa do login; opcional)
import fs from 'node:fs';
import path from 'node:path';

const BASE = process.env.BASE ?? 'http://localhost:5299';
const CDP = 'http://127.0.0.1:9222';
const OUT = process.argv[2];
const ONLY = process.argv[3] ? new Set(process.argv[3].split(',')) : null;
fs.mkdirSync(OUT, { recursive: true });

const sleep = ms => new Promise(r => setTimeout(r, ms));

// ---------------------------------------------------------------- Conexão CDP
const connect = async wsUrl => {
    const ws = new WebSocket(wsUrl);
    await new Promise(r => ws.addEventListener('open', r, { once: true }));
    let seq = 0;
    const pending = new Map();
    ws.addEventListener('message', e => {
        const msg = JSON.parse(e.data);
        if (msg.id && pending.has(msg.id)) {
            const { resolve, reject } = pending.get(msg.id);
            pending.delete(msg.id);
            msg.error ? reject(new Error(JSON.stringify(msg.error))) : resolve(msg.result);
        }
    });
    const send = (method, params = {}) => new Promise((resolve, reject) => {
        const id = ++seq;
        pending.set(id, { resolve, reject });
        ws.send(JSON.stringify({ id, method, params }));
    });
    return { ws, send };
};

// Helpers de uma aba (página)
const pageApi = async (wsUrl, { userAgent, width = 1440, height = 900 } = {}) => {
    const { ws, send } = await connect(wsUrl);
    const evaluate = async expr => {
        const r = await send('Runtime.evaluate', { expression: expr, awaitPromise: true, returnByValue: true });
        if (r.exceptionDetails) throw new Error(r.exceptionDetails.exception?.description ?? r.exceptionDetails.text);
        return r.result.value;
    };
    const waitFor = async (expr, timeout = 15000) => {
        const end = Date.now() + timeout;
        while (Date.now() < end) {
            try { if (await evaluate(expr)) return true; } catch { }
            await sleep(150);
        }
        throw new Error('timeout: ' + expr);
    };
    // Página pronta: shell montado, sem skeletons, sem overlay de loading
    const settle = async (extra = 500) => {
        await waitFor(`!!document.querySelector('.app-shell, .auth-shell')`);
        await waitFor(`!document.querySelector('.skeleton, .sk, .table-skeleton, .is-refreshing')`).catch(() => { });
        await sleep(extra);
    };
    const go = async url => {
        await send('Page.navigate', { url: BASE + url });
        await sleep(400);
        await settle(900);
    };
    // Clica no 1º elemento que casa com o seletor (e opcionalmente com o texto)
    const click = async (selector, text) => {
        const ok = await evaluate(`(() => {
            const els = [...document.querySelectorAll(${JSON.stringify(selector)})];
            const el = ${text ? `els.find(e => e.textContent.trim().includes(${JSON.stringify(text)}))` : 'els[0]'};
            if (!el) return false;
            el.scrollIntoView({ block: 'center' });
            el.click();
            return true;
        })()`);
        if (!ok) throw new Error(`não encontrado: ${selector} ${text ?? ''}`);
        await sleep(500);
    };
    const hover = async selector => {
        const r = await evaluate(`(() => { const r = document.querySelector(${JSON.stringify(selector)}).getBoundingClientRect(); return { x: r.x + r.width / 2, y: r.y + r.height / 2 }; })()`);
        await send('Input.dispatchMouseEvent', { type: 'mouseMoved', x: r.x, y: r.y });
        await sleep(400);
    };
    const type = async (selector, value) => {
        await evaluate(`(() => { const el = document.querySelector(${JSON.stringify(selector)}); el.focus(); el.value = ${JSON.stringify(value)}; el.dispatchEvent(new Event('input', { bubbles: true })); el.dispatchEvent(new Event('change', { bubbles: true })); })()`);
    };
    const capture = async (name, moveMouse = true) => {
        if (ONLY && !ONLY.has(name)) return;
        if (moveMouse) await send('Input.dispatchMouseEvent', { type: 'mouseMoved', x: 1, y: 1 }).catch(() => { });
        const { data } = await send('Page.captureScreenshot', { format: 'png' });
        fs.writeFileSync(path.join(OUT, name + '.png'), Buffer.from(data, 'base64'));
        console.log('ok', name);
    };
    const shot = name => capture(name, true);
    const shotHover = name => capture(name, false); // mantém o hover atual
    // Login pela tela (SSR)
    const login = async (email, password) => {
        await go('/Account/Login');
        await evaluate(`appFx.fillLogin(${JSON.stringify(email)}, ${JSON.stringify(password)})`);
        await evaluate(`document.querySelector('form[action="/Account/Login"]').requestSubmit()`);
        await sleep(1500);
        await settle(1200).catch(() => { });
    };

    await send('Page.enable');
    await send('Runtime.enable');
    await send('Emulation.setLocaleOverride', { locale: 'pt-BR' }).catch(() => { });
    // O app escolhe o idioma pelo cookie ou pelo Accept-Language: os seletores por texto esperam português
    await send('Network.enable');
    await send('Network.setExtraHTTPHeaders', { headers: { 'Accept-Language': 'pt-BR,pt;q=0.9' } });
    await send('Emulation.setDeviceMetricsOverride', { width, height, deviceScaleFactor: 1, mobile: false });
    if (userAgent) await send('Emulation.setUserAgentOverride', { userAgent });
    return { ws, send, evaluate, waitFor, settle, go, click, hover, type, shot, shotHover, login };
};

// Aba no contexto padrão
const target = await (await fetch(`${CDP}/json/new?about:blank`, { method: 'PUT' })).json();
const P = await pageApi(target.webSocketDebuggerUrl);
const { evaluate, waitFor, settle, go, click, hover, type, shot, shotHover } = P;

// Aba em contexto isolado (cookies próprios = "outro navegador/dispositivo")
const browser = await connect((await (await fetch(`${CDP}/json/version`)).json()).webSocketDebuggerUrl);
const contexts = [];
const isolatedPage = async options => {
    const { browserContextId } = await browser.send('Target.createBrowserContext');
    const { targetId } = await browser.send('Target.createTarget', { url: 'about:blank', browserContextId });
    contexts.push(browserContextId);
    const info = (await (await fetch(`${CDP}/json/list`)).json()).find(t => t.id === targetId);
    return pageApi(info.webSocketDebuggerUrl, options);
};
const IPHONE = 'Mozilla/5.0 (iPhone; CPU iPhone OS 17_5 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.5 Mobile/15E148 Safari/604.1';

const step = async (name, fn) => {
    try { await fn(); } catch (e) { console.log('FALHOU', name, e.message); }
};

// ---------- Telas públicas (antes do login)
await step('publicas', async () => {
    await go('/Account/Register');
    await shot('25-cadastro');
    if (process.env.PENDING_EMAIL) {
        await go('/Account/ConfirmEmail?email=' + encodeURIComponent(process.env.PENDING_EMAIL));
        await shot('26-confirmar-email');
    }
});

// ---------- Login
await step('login', async () => {
    await go('/Account/Login');
    await shot('01-login');
    await evaluate(`appFx.fillLogin('admin@demo.com', 'Admin@123')`);
    await evaluate(`document.querySelector('form[action="/Account/Login"]').requestSubmit()`);
    await sleep(1500);
    await settle(1200);
});

await step('dashboard', async () => { await go('/'); await shot('02-dashboard'); });

await step('users', async () => {
    await go('/users');
    await shot('03-usuarios');
    await go('/users?team=1');
    await click('button.btn', 'Período');
    await shot('04-usuarios-multifiltros');
    await go('/users');
    await click('tbody .icon-btn[title="Editar"]');
    await waitFor(`!!document.querySelector('.overlay-root.is-open .drawer-panel form')`);
    await sleep(500);
    await shot('05-editor-usuario-lateral');
    await go('/users');
    await click('tbody .icon-btn[title="Detalhes"]');
    await sleep(500);
    await shot('06-detalhes-usuario-central');
    await go('/users');
    await click('tbody .icon-btn[title="Excluir"]');
    await sleep(600);
    await shot('07-confirmacao-exclusao');
    await go('/users');
    await evaluate(`[...document.querySelectorAll('tbody input[type=checkbox]')].slice(0, 3).forEach(c => c.click())`);
    await sleep(500);
    await shot('08-acoes-em-lote');
});

await step('roles', async () => {
    await go('/roles');
    await shot('09-papeis');
    await evaluate(`[...document.querySelectorAll('tbody tr')].find(r => r.textContent.includes('Gestor'))?.querySelector('.icon-btn')?.click()`);
    await waitFor(`!!document.querySelector('.overlay-root.is-open .perm-groups')`);
    await sleep(500);
    await shot('10-editor-papel-permissoes');
    await go('/permissions');
    await shot('11-permissoes');
});

await step('teams', async () => {
    await go('/teams');
    await shot('12-times');
    await click('.team-card .icon-btn[title="Editar"]');
    await waitFor(`!!document.querySelector('.overlay-root.is-open .drawer-panel form')`);
    await sleep(500);
    await shot('13-editor-time-branding');
    await go('/teams/1');
    await type('.add-member .ac input', 'an');
    await waitFor(`!!document.querySelector('.ac-panel .ac-item')`);
    await sleep(500);
    await shot('14-detalhe-time-busca-lazy');
});

await step('branding', async () => { await go('/settings/branding'); await shot('15-whitelabel'); });

await step('menus', async () => {
    await go('/');
    await click('.user-menu:not(.lang-menu) .user-menu-btn'); // o seletor de idioma também usa .user-menu
    await waitFor(`!!document.querySelector('.user-menu:not(.lang-menu) .tag-more-btn')`); // permissões carregam ao abrir
    await hover('.user-menu:not(.lang-menu) .tag-more-btn');
    await shotHover('16-menu-usuario-permissoes');
    await go('/');
    await click('.team-switcher-btn');
    await shot('17-menu-meus-times');
    // seleciona um time com branding próprio e mostra o tema aplicado
    await click('.team-switcher .dropdown-panel-item', 'branding próprio');
    await sleep(1200);
    await go('/users');
    await shot('18-branding-do-time-aplicado');
    await click('.team-switcher-btn');
    await click('.team-switcher .dropdown-panel-item', 'Data & Analytics'); // estado original do admin
    await sleep(800);
});

await step('components', async () => {
    await go('/components');
    await click('button.btn-success', 'Sucesso');
    await click('button.btn-warning', 'Aviso');
    await click('button.btn-info', 'Info');
    await sleep(300);
    await shot('19-componentes-toasts');
    await evaluate(`[...document.querySelectorAll('button')].find(b => b.textContent.includes('Overlay global'))?.click()`);
    await sleep(500);
    await shot('20-loading-global');
    await sleep(2200);
    await evaluate(`[...document.querySelectorAll('button')].find(b => b.textContent.includes('Excluir crítico'))?.click()`);
    await sleep(600);
    await shot('21-exclusao-critica');
});

// ---------- Usuários: dropdown de times dentro do drawer, bloqueio e relatório
await step('users-extra', async () => {
    await go('/users');
    await click('tbody .icon-btn[title="Editar"]');
    await waitFor(`!!document.querySelector('.overlay-root.is-open .drawer-panel form')`);
    await sleep(400);
    await click('.drawer-panel .ms-toggle');
    await waitFor(`!!document.querySelector('.drawer-panel .ms-panel')`);
    await sleep(900); // o painel rola sozinho para ficar visível
    await shot('27-dropdown-times-no-drawer');

    await go('/users');
    await click('tbody .icon-btn[title="Bloquear"]');
    await sleep(600);
    await shot('28-bloquear-usuario');

    await go('/users');
    await click('.page-header button, button.btn-outline-secondary', 'Relatório');
    await waitFor(`!!document.querySelector('.report-formats')`);
    await click('.report-format', 'PDF');
    await evaluate(`(() => { const s = [...document.querySelectorAll('.dialog-panel select')][0]; s.value = 'department'; s.dispatchEvent(new Event('change', { bubbles: true })); })()`);
    await click('.report-column label', 'MFA');
    await sleep(500);
    await shot('29-relatorio-dialog');
});

// ---------- Configurações do sistema e notificações
await step('settings', async () => {
    await go('/settings/system');
    await shot('30-configuracoes-sistema');
    await click('button', 'Testar notificação');
    await sleep(2500); // Hangfire entrega + SignalR
    await click('.notif-btn');
    await sleep(700);
    await shot('31-notificacoes');
});

// ---------- Sessões: outro dispositivo entra, alerta em tempo real, tela de sessões, MFA, reautenticação
await step('sessions', async () => {
    await go('/');
    const phone = await isolatedPage({ userAgent: IPHONE, width: 430, height: 900 });
    await phone.login('admin@demo.com', 'Admin@123');
    await waitFor(`!!document.querySelector('.overlay-root.is-open .session-card.is-alert')`, 20000);
    await sleep(600);
    await shot('33-alerta-novo-acesso');
    await click('.overlay-root.is-open .btn', 'Fui eu');

    await go('/account/security');
    await shot('32-seguranca-conta');

    await click('.session-card .btn-outline-danger', 'Encerrar');
    await waitFor(`!!document.querySelector('.overlay-root.is-open input[type=password]')`);
    await sleep(400);
    await shot('35-reautenticacao');
    // confirma de verdade: o "celular" é desconectado na hora
    await type('.overlay-root.is-open input[type=password]', 'Admin@123');
    await sleep(300);
    await click('.overlay-root.is-open .dialog-footer .btn-danger');
    await phone.waitFor(`location.pathname.includes('SessionEnded')`, 15000);
    await phone.settle(600);
    await phone.shot('36-sessao-encerrada');

    await go('/account/security');
    await click('button', 'Configurar app autenticador');
    await waitFor(`!!document.querySelector('img.mfa-qr')`);
    await sleep(500);
    await shot('34-mfa-configuracao');
    await click('button.btn-link', 'Cancelar');
});

// ---------- 2ª etapa do login (precisa de uma conta com MFA ativo)
await step('mfa-login', async () => {
    if (!process.env.MFA_EMAIL) return;
    const other = await isolatedPage();
    await other.login(process.env.MFA_EMAIL, process.env.MFA_PASSWORD);
    await other.waitFor(`location.pathname.includes('LoginWith2fa')`);
    await other.settle(500);
    await other.shot('37-login-mfa');
});

// ---------- Idiomas (ao final: troca o cookie de cultura e volta para pt-BR)
await step('idiomas', async () => {
    await go('/culture/set?culture=en-US&redirectUri=/users');
    await shot('22-idioma-ingles');
    await go('/culture/set?culture=es-ES&redirectUri=/teams');
    await shot('23-idioma-espanhol');
    await go('/culture/set?culture=pt-BR&redirectUri=/');
});

P.ws.close();
for (const id of contexts) await browser.send('Target.disposeBrowserContext', { browserContextId: id }).catch(() => { });
browser.ws.close();
await fetch(`${CDP}/json/close/${target.id}`);
process.exit(0);

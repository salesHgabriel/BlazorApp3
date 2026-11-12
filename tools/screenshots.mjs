// Captura de telas via Chrome DevTools Protocol (sem dependências; node --experimental-websocket)
import fs from 'node:fs';
import path from 'node:path';

const BASE = process.env.BASE ?? 'http://localhost:5299';
const OUT = process.argv[2];
const ONLY = process.argv[3] ? new Set(process.argv[3].split(',')) : null;
fs.mkdirSync(OUT, { recursive: true });

const sleep = ms => new Promise(r => setTimeout(r, ms));
const target = await (await fetch('http://127.0.0.1:9222/json/new?about:blank', { method: 'PUT' })).json();
const ws = new WebSocket(target.webSocketDebuggerUrl);
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
    await evaluate(`(() => { const el = document.querySelector(${JSON.stringify(selector)}); el.focus(); el.value = ${JSON.stringify(value)}; el.dispatchEvent(new Event('input', { bubbles: true })); })()`);
};
const shot = async name => {
    if (ONLY && !ONLY.has(name)) return;
    await send('Input.dispatchMouseEvent', { type: 'mouseMoved', x: 1, y: 1 }).catch(() => { });
    const { data } = await send('Page.captureScreenshot', { format: 'png' });
    fs.writeFileSync(path.join(OUT, name + '.png'), Buffer.from(data, 'base64'));
    console.log('ok', name);
};
const shotHover = async name => { // mantém o hover atual
    const { data } = await send('Page.captureScreenshot', { format: 'png' });
    fs.writeFileSync(path.join(OUT, name + '.png'), Buffer.from(data, 'base64'));
    console.log('ok', name);
};
const step = async (name, fn) => {
    try { await fn(); } catch (e) { console.log('FALHOU', name, e.message); }
};

await send('Page.enable');
await send('Runtime.enable');
await send('Emulation.setLocaleOverride', { locale: 'pt-BR' }).catch(() => { });
await send('Emulation.setDeviceMetricsOverride', { width: 1440, height: 900, deviceScaleFactor: 1, mobile: false });

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
    await click('.user-menu-btn');
    await hover('.user-menu .tag-more-btn');
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

ws.close();
await fetch(`http://127.0.0.1:9222/json/close/${target.id}`);
process.exit(0);

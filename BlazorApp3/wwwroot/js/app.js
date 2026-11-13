// Efeitos de página e utilidades de UI
window.appFx = {
    // Reinicia a animação de entrada da página + barra de progresso no topo
    pageEnter(effect) {
        const root = document.getElementById('page-root');
        const bar = document.getElementById('nav-progress');
        if (bar) {
            bar.classList.remove('run');
            void bar.offsetWidth;
            bar.classList.add('run');
        }
        if (!root || !effect || effect === 'none') return;
        root.className = 'page-root';
        void root.offsetWidth; // força reflow para reiniciar a animação
        root.classList.add('fx-' + effect);
        // ao terminar, remove a classe: transform/filter residual prende overlays "fixed" dentro do conteúdo
        const done = e => { if (e.target !== root) return; root.classList.remove('fx-' + effect); root.removeEventListener('animationend', done); };
        root.addEventListener('animationend', done);
        window.scrollTo({ top: 0 });
    },

    // Preenche o login com uma conta demo (página SSR estática)
    fillLogin(email, password) {
        const e = document.getElementById('email');
        const p = document.getElementById('password');
        if (e) e.value = email;
        if (p) { p.value = password; p.focus(); }
    }
};

// Dropdowns (MultiSelect, Autocomplete): fecha ao clicar fora sem cobrir a tela com um scrim,
// assim a rolagem do mouse continua funcionando (ex.: dropdown aberto dentro de um Drawer)
window.appDropdown = (() => {
    let seq = 0;
    const watchers = new Map();
    return {
        watch(el, dotnetRef, reveal) {
            const id = ++seq;
            const onDown = e => {
                if (!el || !el.isConnected || el.contains(e.target)) return;
                dotnetRef.invokeMethodAsync('OnOutside').catch(() => { });
            };
            document.addEventListener('pointerdown', onDown, true);
            watchers.set(id, () => document.removeEventListener('pointerdown', onDown, true));
            if (reveal) {
                requestAnimationFrame(() => {
                    const panel = el.querySelector('.ms-panel, .ac-panel');
                    if (panel) panel.scrollIntoView({ block: 'nearest', behavior: 'smooth' });
                });
            }
            return id;
        },
        unwatch(id) {
            const off = watchers.get(id);
            if (off) off();
            watchers.delete(id);
        }
    };
})();

// Popover das listas de tags ("…"): posicionado em fixed para não ser cortado por containers com scroll (tabelas, menus)
(() => {
    const place = e => {
        const host = e.target.closest && e.target.closest('.tag-more');
        if (!host) return;
        const pop = host.querySelector('.tag-more-pop');
        if (!pop) return;
        const r = host.getBoundingClientRect(), gap = 6, margin = 8;
        const w = pop.offsetWidth, h = pop.offsetHeight;
        let left = Math.min(Math.max(margin, r.left + r.width / 2 - w / 2), window.innerWidth - w - margin);
        let top = r.bottom + gap;
        if (top + h > window.innerHeight - margin) top = Math.max(margin, r.top - gap - h); // sem espaço embaixo: abre para cima
        pop.style.left = left + 'px';
        pop.style.top = top + 'px';
    };
    document.addEventListener('mouseover', place);
    document.addEventListener('focusin', place);
})();

// ---------------- Telas de conta (SSR estático): código de confirmação, contagem para reenvio e força da senha
(() => {
    // 6 caixas para o código; o input real (enviado no formulário) fica escondido e é sincronizado
    const initOtp = root => {
        if (root.dataset.ready) return;
        root.dataset.ready = '1';
        const real = document.getElementById(root.dataset.otp);
        const len = parseInt(root.dataset.otpLength || '6', 10);
        if (!real) return;
        const boxes = [];
        const wrap = document.createElement('div');
        wrap.className = 'otp-boxes';
        const sync = () => { real.value = boxes.map(b => b.value).join(''); root.classList.toggle('is-complete', real.value.length === len); };
        const fill = (digits, from) => {
            digits.split('').forEach((d, i) => { if (boxes[from + i]) boxes[from + i].value = d; });
            boxes[Math.min(from + digits.length, len - 1)].focus();
            sync();
        };
        for (let i = 0; i < len; i++) {
            const b = document.createElement('input');
            b.type = 'text'; b.inputMode = 'numeric'; b.maxLength = 1; b.className = 'otp-box';
            b.autocomplete = i === 0 ? 'one-time-code' : 'off';
            b.setAttribute('aria-label', `Dígito ${i + 1}`);
            b.value = real.value[i] || '';
            b.addEventListener('input', () => {
                const digits = b.value.replace(/\D/g, '');
                if (digits.length > 1) { fill(digits.slice(0, len - i), i); return; } // autofill do SO
                b.value = digits;
                sync();
                if (digits && i < len - 1) boxes[i + 1].focus();
            });
            b.addEventListener('keydown', e => {
                if (e.key === 'Backspace' && !b.value && i > 0) { boxes[i - 1].focus(); boxes[i - 1].value = ''; sync(); e.preventDefault(); }
                if (e.key === 'ArrowLeft' && i > 0) boxes[i - 1].focus();
                if (e.key === 'ArrowRight' && i < len - 1) boxes[i + 1].focus();
            });
            b.addEventListener('paste', e => {
                const digits = (e.clipboardData.getData('text') || '').replace(/\D/g, '').slice(0, len);
                if (!digits) return;
                e.preventDefault();
                boxes.forEach(x => x.value = '');
                fill(digits, 0);
            });
            boxes.push(b);
            wrap.appendChild(b);
        }
        root.appendChild(wrap);
        root.classList.add('otp-ready');
        sync();
        (boxes.find(b => !b.value) || boxes[len - 1]).focus();
    };

    // Botão desabilitado com contagem regressiva (reenvio de código)
    const initCountdown = btn => {
        if (btn.dataset.ready) return;
        btn.dataset.ready = '1';
        let left = parseInt(btn.dataset.countdown || '0', 10);
        if (!left) return;
        const label = btn.textContent, fmt = btn.dataset.countdownText || '{0}s';
        btn.disabled = true;
        const tick = () => {
            if (left <= 0) { btn.disabled = false; btn.textContent = label; return; }
            btn.textContent = fmt.replace('{0}', left--);
            setTimeout(tick, 1000);
        };
        tick();
    };

    // Medidor simples de força da senha
    const initMeter = input => {
        if (input.dataset.ready) return;
        input.dataset.ready = '1';
        const meter = document.getElementById(input.dataset.passwordMeter);
        if (!meter) return;
        const score = v => [v.length >= 6, /[A-Z]/.test(v) && /[a-z]/.test(v), /\d/.test(v), v.length >= 10 || /[^A-Za-z0-9]/.test(v)].filter(Boolean).length;
        input.addEventListener('input', () => meter.dataset.score = input.value ? score(input.value) : 0);
    };

    const init = () => {
        document.querySelectorAll('[data-otp]').forEach(initOtp);
        document.querySelectorAll('[data-countdown]').forEach(initCountdown);
        document.querySelectorAll('[data-password-meter]').forEach(initMeter);
    };
    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', init); else init();
    document.addEventListener('DOMContentLoaded', () => window.Blazor?.addEventListener?.('enhancedload', init));
})();

// ---------------- Notificações em tempo real (SignalR) + sessões (encerramento e aviso de novo acesso)
window.appNotify = {
    _conn: null,
    _sessionRef: null,

    // Componente .NET que exibe o aviso "novo acesso à sua conta" (SessionAlertHost)
    onNewSession(dotnetRef) { this._sessionRef = dotnetRef; },

    // Conecta ao hub; repassa notificações ao componente do sino (.NET) e trata encerramento de sessão
    async start(dotnetRef, sessionId) {
        if (!window.signalR) { console.warn('SignalR client não carregado'); return; }
        await this.stop();
        const conn = new signalR.HubConnectionBuilder()
            .withUrl('/hubs/notifications')
            .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
            .configureLogging(signalR.LogLevel.Warning)
            .build();
        conn.on('notification', n => dotnetRef.invokeMethodAsync('OnNotification', n).catch(() => { }));
        // Esta sessão foi encerrada (sessão única, encerrada de outro dispositivo, bloqueio, logout em outra aba)
        conn.on('sessionRevoked', reason => {
            this.stop();
            window.location.href = reason === 'logout' ? '/Account/Login' : '/Account/SessionEnded?reason=' + encodeURIComponent(reason || '');
        });
        conn.on('newSession', info => { if (this._sessionRef) this._sessionRef.invokeMethodAsync('OnNewSession', info).catch(() => { }); });
        this._conn = conn;
        try { await conn.start(); } catch (e) { console.warn('Hub de notificações indisponível', e); }
    },

    async stop() {
        const c = this._conn;
        this._conn = null;
        if (c) { try { await c.stop(); } catch { } }
    },

    // Efeito do sino: balanço + pulso no badge
    ring(el) {
        if (!el) return;
        el.classList.remove('ringing');
        void el.offsetWidth;
        el.classList.add('ringing');
        setTimeout(() => el.classList.remove('ringing'), 1300);
    }
};

// ---------------- Arquivos: download de conteúdo gerado no servidor (relatórios, códigos de recuperação) e cópia
window.appFiles = {
    async download(fileName, contentType, streamRef) {
        const buffer = await streamRef.arrayBuffer();
        const url = URL.createObjectURL(new Blob([buffer], { type: contentType }));
        const a = document.createElement('a');
        a.href = url;
        a.download = fileName;
        document.body.appendChild(a);
        a.click();
        a.remove();
        setTimeout(() => URL.revokeObjectURL(url), 2000);
    },
    async copy(text) {
        try { await navigator.clipboard.writeText(text); return true; } catch { return false; }
    }
};

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

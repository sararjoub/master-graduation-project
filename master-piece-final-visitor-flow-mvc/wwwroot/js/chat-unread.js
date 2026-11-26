(() => {
    const link = document.querySelector('[data-learning-chat-unread]');
    if (!link || link.dataset.unreadInitialized) return;
    link.dataset.unreadInitialized = 'true';

    const badge = link.querySelector('.notification-badge');
    let timer;
    let pending = false;
    let refreshRequested = false;
    let delay = 15000;

    function display(count) {
        badge.textContent = count;
        badge.hidden = count === 0;
        link.setAttribute('aria-label', count ? 'Messages (' + count + ' unread)' : 'Messages');
    }

    function schedule(wait = delay) {
        clearTimeout(timer);
        if (!document.hidden) timer = setTimeout(refresh, wait);
    }

    async function refresh() {
        clearTimeout(timer);
        if (document.hidden) return;
        if (pending) {
            refreshRequested = true;
            return;
        }

        pending = true;
        try {
            const response = await fetch('/chat/unread', {
                headers: { Accept: 'application/json' },
                redirect: 'manual',
                cache: 'no-store',
                signal: AbortSignal.timeout(10000)
            });
            if (response.type === 'opaqueredirect' || [401, 403].includes(response.status)) {
                display(0);
                return;
            }
            if (!response.ok || !response.headers.get('content-type')?.includes('application/json')) throw new Error();
            const count = await response.json();
            if (!Number.isSafeInteger(count) || count < 0) throw new Error();
            display(count);
            delay = 15000;
        } catch {
            delay = Math.min(delay * 2, 60000);
        } finally {
            pending = false;
            const wait = refreshRequested ? 0 : delay;
            refreshRequested = false;
            schedule(wait);
        }
    }

    document.addEventListener('learning-chat-read', refresh);
    document.addEventListener('visibilitychange', () => document.hidden ? clearTimeout(timer) : refresh());
    window.addEventListener('pageshow', event => { if (event.persisted) refresh(); });
    refresh();
})();

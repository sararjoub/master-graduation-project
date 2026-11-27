(() => {
    const dates = document.querySelectorAll('[data-membership-date]');
    if (!dates.length) return;
    function render() {
        try {
            const zone = Intl.DateTimeFormat().resolvedOptions().timeZone || 'UTC';
            const locale = document.documentElement.lang === 'ar' ? 'ar' : 'en';
            const format = new Intl.DateTimeFormat(locale, {
                timeZone: zone, year: 'numeric', month: 'short', day: 'numeric',
                hour: '2-digit', minute: '2-digit', timeZoneName: 'short'
            });
            for (const date of dates) {
                const value = new Date(date.dateTime);
                if (!Number.isNaN(value.valueOf())) date.querySelector('bdi').textContent = format.format(value);
            }
        } catch {
            // Keep the server-rendered UTC timestamps if the browser cannot format local dates.
        }
    }
    render();
    new MutationObserver(render).observe(document.documentElement, { attributes: true, attributeFilter: ['lang'] });
})();

document.addEventListener('DOMContentLoaded', () => {
    const overlays = [...document.querySelectorAll('.modal-overlay')];

    function closeAll() {
        overlays.forEach(modal => modal.classList.add('hidden'));
        document.body.style.overflow = '';
    }

    document.querySelectorAll('[data-open-modal]').forEach(button => {
        button.addEventListener('click', () => {
            const modal = document.getElementById(button.dataset.openModal);
            if (!modal) return;
            closeAll();
            modal.classList.remove('hidden');
            document.body.style.overflow = 'hidden';
            modal.querySelector('input:not([type="hidden"]), button')?.focus();
        });
    });

    document.querySelectorAll('[data-close-modal]').forEach(button =>
        button.addEventListener('click', closeAll));

    overlays.forEach(modal => modal.addEventListener('click', event => {
        if (event.target === modal) closeAll();
    }));

    document.addEventListener('keydown', event => {
        if (event.key === 'Escape') closeAll();
    });
});

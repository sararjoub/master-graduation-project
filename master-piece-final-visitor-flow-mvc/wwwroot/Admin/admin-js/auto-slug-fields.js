document.addEventListener('DOMContentLoaded', () => {
    document.querySelectorAll('input[name="Slug"]').forEach(input => {
        input.required = false;
        input.removeAttribute('pattern');
        input.closest('.form-group')?.setAttribute('hidden', 'hidden');
    });
});

document.querySelectorAll('input[name="EmailLanguage"]').forEach(input => {
    input.form?.addEventListener('submit', () => {
        input.value = document.documentElement.lang === 'ar' ? 'ar' : 'en';
    });
});

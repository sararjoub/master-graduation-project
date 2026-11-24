

document.addEventListener('DOMContentLoaded', () => {

    
    // . إظهار/إخفاء كلمة المرور
    let togglePassword = document.getElementById('togglePassword');
    let passwordInput = document.getElementById('password');

    if (togglePassword && passwordInput) {
        togglePassword.addEventListener('click', () => {
            let type = passwordInput.getAttribute('type') === 'password' ? 'text' : 'password';
            passwordInput.setAttribute('type', type);
            togglePassword.textContent = type === 'password' ? '👁️' : '🙈';
        });
    }

    // دالة عرض التنبيهات
    function showNotification(message, type = 'info') {
        // إزالة أي تنبيه قديم
        let oldNotification = document.querySelector('.notification');
        if (oldNotification) oldNotification.remove();

        let notification = document.createElement('div');
        notification.className = `notification notification-${type}`;
        notification.innerHTML = `
            <span class="notification-icon">
                ${type === 'success' ? '✓' : type === 'error' ? '✕' : 'ℹ'}
            </span>
            <span class="notification-message">${message}</span>
        `;

        document.body.appendChild(notification);

        setTimeout(() => notification.classList.add('show'), 10);

        setTimeout(() => {
            notification.classList.remove('show');
            setTimeout(() => notification.remove(), 300);
        }, 3000);
    }

    //  تأثير عند التركيز على الحقول
    let allInputs = document.querySelectorAll('.form-input');
    allInputs.forEach(input => {
        input.addEventListener('focus', () => {
            input.parentElement.style.transform = 'scale(1.01)';
            input.parentElement.style.transition = 'transform 0.2s ease';
        });

        input.addEventListener('blur', () => {
            input.parentElement.style.transform = 'scale(1)';
        });
    });


    // أزرار التواصل الاجتماعي (Placeholder)
    let socialButtons = document.querySelectorAll('.social-btn');
    socialButtons.forEach(btn => {
        btn.addEventListener('click', () => {
            let provider = btn.textContent.trim();
            showNotification(`${provider} login coming soon!`, 'info');
           
        });
    });

});

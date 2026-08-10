// js/app.js
import { showToast } from './utils.js';
import { API_BASE_URL } from './api.js';

const loginBtn = document.getElementById('loginBtn');

if(loginBtn) {
    loginBtn.addEventListener('click', async () => {
        const email = document.getElementById('email').value;
        const password = document.getElementById('password').value;

        if (!email || !password) {
            showToast("Lütfen e-posta ve şifrenizi girin.", "warning");
            return;
        }

        const originalText = loginBtn.textContent;
        loginBtn.textContent = "Giriş Yapılıyor...";
        loginBtn.disabled = true;

        try {
            const response = await fetch(`${API_BASE_URL}/Auth/login`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ email, password })
            });

            if (response.ok) {
                const data = await response.json();
                localStorage.setItem('token', data.token);
                showToast("Giriş başarılı! Yönlendiriliyorsunuz...", "success");
                
                setTimeout(() => {
                    window.location.href = 'tasks.html';
                }, 1000);
            } else {
                showToast("Giriş başarısız. Lütfen bilgilerinizi kontrol edin.", "error");
            }
        } catch (error) {
            console.error('Hata:', error);
            showToast("Sunucuya bağlanılamadı.", "error");
        } finally {
            loginBtn.textContent = originalText;
            loginBtn.disabled = false;
        }
    });
}
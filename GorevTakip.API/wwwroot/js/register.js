// js/register.js
import { showToast } from './utils.js';
import { API_BASE_URL } from './api.js';

const registerBtn = document.getElementById('registerBtn');

if(registerBtn) {
    registerBtn.addEventListener('click', async () => {
        const firstName = document.getElementById('firstName').value;
        const lastName = document.getElementById('lastName').value;
        const email = document.getElementById('email').value;
        const password = document.getElementById('password').value;

        if (!firstName || !lastName || !email || !password) {
            showToast("Lütfen tüm alanları doldurun.", "warning");
            return;
        }

        const originalText = registerBtn.textContent;
        registerBtn.textContent = "Kaydediliyor...";
        registerBtn.disabled = true;

        try {
            const response = await fetch(`${API_BASE_URL}/Auth/register`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ firstName, lastName, email, password })
            });

            if (response.ok) {
                showToast("Kayıt başarılı! Giriş sayfasına yönlendiriliyorsunuz...", "success");
                setTimeout(() => {
                    window.location.href = 'index.html';
                }, 1500);
            } else {
                if (response.status === 400) {
                    const errorData = await response.json();
                    if (errorData.errors) {
                        const firstErrorKey = Object.keys(errorData.errors)[0];
                        showToast(errorData.errors[firstErrorKey][0], "error");
                        return;
                    }
                }
                showToast("Kayıt işlemi başarısız oldu.", "error");
            }
        } catch (error) {
            console.error('Hata:', error);
            showToast("Sunucuya bağlanılamadı.", "error");
        } finally {
            registerBtn.textContent = originalText;
            registerBtn.disabled = false;
        }
    });
}
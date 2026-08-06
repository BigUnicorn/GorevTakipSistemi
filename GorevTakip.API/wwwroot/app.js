const loginBtn = document.getElementById('loginBtn');
// API'nin portu buraya yazılmalı
const API_BASE_URL = '/api';

// Toast Bildirim Fonksiyonu (tasks.js'deki ile aynı)
function showToast(message, type = 'success') {
    const container = document.getElementById('toast-container');
    const toast = document.createElement('div');
    
    const bgColor = type === 'success' ? '#28a745' : (type === 'error' ? '#ef4444' : '#f59e0b');
    const color = type === 'warning' ? 'black' : 'white';

    toast.style.cssText = `
        background-color: ${bgColor};
        color: ${color};
        padding: 12px 20px;
        border-radius: 6px;
        box-shadow: 0 4px 10px rgba(0,0,0,0.1);
        font-size: 14px;
        font-family: 'Inter', sans-serif;
        opacity: 0;
        transform: translateX(100%);
        transition: all 0.3s ease-in-out;
    `;
    toast.textContent = message;

    container.appendChild(toast);

    setTimeout(() => { 
        toast.style.opacity = '1'; 
        toast.style.transform = 'translateX(0)';
    }, 10);

    setTimeout(() => {
        toast.style.opacity = '0';
        toast.style.transform = 'translateX(100%)';
        setTimeout(() => { toast.remove(); }, 300);
    }, 3000);
}

loginBtn.addEventListener('click', async () => {
    const email = document.getElementById('email').value;
    const password = document.getElementById('password').value;

    if (!email || !password) {
        showToast("Lütfen e-posta ve şifrenizi girin.", "warning");
        return;
    }

    // Butonu kilitle ve yazısını değiştir
    const originalText = loginBtn.textContent;
    loginBtn.textContent = "Giriş Yapılıyor...";
    loginBtn.disabled = true;

    try {
        const response = await fetch(`${API_BASE_URL}/Auth/Login`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ email: email, password: password })
        });

        if (response.ok) {
            const data = await response.json();
            localStorage.setItem('token', data.token);
            
            showToast("Giriş başarılı, yönlendiriliyorsunuz...", "success");
            
            // Kullanıcı başarılı bildirimi görsün diye 1 saniye bekleyip yönlendiriyoruz
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
        // İşlem bitince butonu eski haline getir
        loginBtn.textContent = originalText;
        loginBtn.disabled = false;
    }
});
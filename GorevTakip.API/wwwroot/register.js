const API_BASE_URL = '/api';
const registerBtn = document.getElementById('registerBtn');

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
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ 
                firstName: firstName, 
                lastName: lastName,
                email: email, 
                password: password 
            })
        });

        if (response.ok) {
            showToast("Kayıt başarılı! Giriş sayfasına yönlendiriliyorsunuz...", "success");
            setTimeout(() => {
                window.location.href = 'index.html';
            }, 1500);
        } else {
            const errorText = await response.text();
            showToast(errorText || "Kayıt işlemi başarısız oldu.", "error");
        }
    } catch (error) {
        console.error('Hata:', error);
        showToast("Sunucuya bağlanılamadı.", "error");
    } finally {
        registerBtn.textContent = originalText;
        registerBtn.disabled = false;
    }
});
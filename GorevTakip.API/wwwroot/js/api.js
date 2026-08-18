import { showToast, logout } from './utils.js';

export const API_BASE_URL = '/api';

let isRefreshing = false;
let refreshPromise = null;

export async function fetchWithAuth(endpoint, options = {}) {
    let token = localStorage.getItem('token');
    
    const getHeaders = (authToken) => {
        const headers = {
            'Content-Type': 'application/json',
            ...options.headers
        };
        if (authToken) {
            headers['Authorization'] = `Bearer ${authToken}`;
        }
        return headers;
    };

    try {
        let response = await fetch(`${API_BASE_URL}${endpoint}`, {
            ...options,
            headers: getHeaders(token)
        });

        // Yetkisiz erişim kontrolü (Token süresi dolmuşsa)
        if (response.status === 401) {
            const refreshToken = localStorage.getItem('refreshToken');
            
            if (refreshToken && token) {
                if (!isRefreshing) {
                    isRefreshing = true;
                    refreshPromise = fetch(`${API_BASE_URL}/Auth/refresh`, {
                        method: 'POST',
                        headers: { 'Content-Type': 'application/json' },
                        body: JSON.stringify({ accessToken: token, refreshToken: refreshToken })
                    }).then(async (res) => {
                        if (res.ok) {
                            const data = await res.json();
                            localStorage.setItem('token', data.accessToken);
                            localStorage.setItem('refreshToken', data.refreshToken);
                            return data.accessToken;
                        }
                        throw new Error('Refresh failed');
                    }).finally(() => {
                        isRefreshing = false;
                    });
                }

                try {
                    const newToken = await refreshPromise;
                    // Orijinal isteği yeni token ile tekrarla
                    response = await fetch(`${API_BASE_URL}${endpoint}`, {
                        ...options,
                        headers: getHeaders(newToken)
                    });
                    return response;
                } catch (refreshError) {
                    console.error('Token yenileme hatası:', refreshError);
                }
            }

            // Refresh başarısızsa veya refresh token yoksa çıkış yap
            showToast('Oturumunuzun süresi doldu. Lütfen tekrar giriş yapın.', 'error');
            setTimeout(logout, 1500);
            return null; // İşlemi kes
        }

        return response;
    } catch (error) {
        console.error('API İsteği Hatası:', error);
        showToast('Sunucuya bağlanılamadı.', 'error');
        throw error;
    }
}
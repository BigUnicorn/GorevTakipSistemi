import { showToast, logout } from './utils.js';

export const API_BASE_URL = '/api';

export async function fetchWithAuth(endpoint, options = {}) {
    const token = localStorage.getItem('token');
    
    // Default headers ayarları
    const headers = {
        'Content-Type': 'application/json',
        ...options.headers
    };

    if (token) {
        headers['Authorization'] = `Bearer ${token}`;
    }

    try {
        const response = await fetch(`${API_BASE_URL}${endpoint}`, {
            ...options,
            headers
        });

        // Yetkisiz erişim kontrolü (Token süresi dolmuşsa)
        if (response.status === 401) {
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
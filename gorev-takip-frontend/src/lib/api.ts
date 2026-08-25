import axios from 'axios';

// Sunucu (API) ile arayüz (Frontend) aynı domain'de çalıştığı için (wwwroot üzerinden),
// relative path kullanabiliriz.
const API_URL = '/api/v1';

export const api = axios.create({
  baseURL: API_URL,
  withCredentials: true, // Çerezlerin gönderilmesi için eklendi
  headers: {
    'Content-Type': 'application/json',
  },
});

let isRefreshing = false;
let refreshPromise: Promise<boolean> | null = null;

api.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config;

    if (error.response?.status === 401 && !originalRequest._retry) {
      originalRequest._retry = true;

      if (!isRefreshing) {
        isRefreshing = true;
        refreshPromise = (async () => {
          try {
            // Sadece Auth/refresh rotasına istek atıyoruz. Token'lar çerezden gidecek.
            await axios.post(`${API_URL}/Auth/refresh`, {}, { withCredentials: true });
            return true;
          } catch {
            // Token yenilenemediyse çıkış yap.
            // window.location.href kullanmak sonsuz döngüye sebep olur, React Router yönlendirmeyi halledecek.
            if (typeof window !== 'undefined' && window.location.pathname !== '/login' && window.location.pathname !== '/register') {
              // eslint-disable-next-line @next/next/no-location-assign-relative-destination
              window.location.href = '/login';
            }
            return false;
          } finally {
            isRefreshing = false;
          }
        })();
      }

      const success = await refreshPromise;
      if (success) {
        // Çerezler başarılı yenilendiyse, orijinal isteği aynı konfigürasyonla tekrar et.
        return api(originalRequest);
      }
    }
    return Promise.reject(error);
  }
);

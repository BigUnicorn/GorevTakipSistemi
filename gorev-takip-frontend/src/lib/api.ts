import axios from 'axios';

// Sunucu (API) ile arayüz (Frontend) aynı domain'de çalıştığı için (wwwroot üzerinden),
// relative path kullanabiliriz.
const API_URL = '/api/v1';

export const api = axios.create({
  baseURL: API_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

let isRefreshing = false;
let refreshPromise: Promise<string | null> | null = null;

api.interceptors.request.use((config) => {
  const token = sessionStorage.getItem('token');
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
}, (error) => Promise.reject(error));

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
            const refreshToken = sessionStorage.getItem('refreshToken');
            const accessToken = sessionStorage.getItem('token');
            if (!refreshToken || !accessToken) {
              throw new Error("No tokens available");
            }
            const res = await axios.post(`${API_URL}/Auth/refresh`, {
              accessToken: accessToken,
              refreshToken: refreshToken
            });
            const newToken = res.data.accessToken;
            const newRefreshToken = res.data.refreshToken;
            
            sessionStorage.setItem('token', newToken);
            sessionStorage.setItem('refreshToken', newRefreshToken);
            return newToken;
          } catch (refreshError) {
            sessionStorage.removeItem('token');
            sessionStorage.removeItem('refreshToken');
            sessionStorage.removeItem('user'); // Kullanıcıyı da temizle
            window.location.href = '/login';
            return null;
          } finally {
            isRefreshing = false;
          }
        })();
      }

      const newToken = await refreshPromise;
      if (newToken) {
        originalRequest.headers.Authorization = `Bearer ${newToken}`;
        return api(originalRequest);
      }
    }
    return Promise.reject(error);
  }
);

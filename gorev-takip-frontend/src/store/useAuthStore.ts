import { create } from 'zustand';
import { api } from '@/lib/api';

interface User {
  id: number;
  firstName: string;
  lastName: string;
  email: string;
  role: number;
}

interface AuthState {
  user: User | null;
  isAuthenticated: boolean;
  login: (user: User) => void;
  logout: () => Promise<void>;
  checkAuth: () => Promise<void>;
}

export const useAuthStore = create<AuthState>((set) => ({
  user: null,
  isAuthenticated: false,
  login: (user) => {
    set({ user, isAuthenticated: true });
  },
  logout: async () => {
    try {
      await api.post('/Auth/logout');
    } catch (e) {
      console.error('Logout hatası', e);
    }
    set({ user: null, isAuthenticated: false });
    window.location.href = '/login'; // Çıkış yapınca login sayfasına yönlendir
  },
  checkAuth: async () => {
    try {
      const response = await api.get('/Auth/me');
      set({ user: response.data, isAuthenticated: true });
    } catch (error) {
      set({ user: null, isAuthenticated: false });
    }
  }
}));

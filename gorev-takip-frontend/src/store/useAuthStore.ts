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
  login: (token: string, refreshToken: string, user: User) => void;
  logout: () => void;
  checkAuth: () => void;
}

export const useAuthStore = create<AuthState>((set) => ({
  user: null,
  isAuthenticated: false,
  login: (token, refreshToken, user) => {
    sessionStorage.setItem('token', token);
    sessionStorage.setItem('refreshToken', refreshToken);
    sessionStorage.setItem('user', JSON.stringify(user));
    set({ user, isAuthenticated: true });
  },
  logout: () => {
    sessionStorage.removeItem('token');
    sessionStorage.removeItem('refreshToken');
    sessionStorage.removeItem('user');
    set({ user: null, isAuthenticated: false });
  },
  checkAuth: () => {
    const token = sessionStorage.getItem('token');
    const userStr = sessionStorage.getItem('user');
    if (token && userStr) {
      set({ user: JSON.parse(userStr), isAuthenticated: true });
    } else {
      set({ user: null, isAuthenticated: false });
    }
  }
}));

import { create } from 'zustand';
import { api } from '@/lib/api';

export interface User {
  id: number;
  firstName: string;
  lastName: string;
  email: string;
  role: number; // 0: User, 1: Admin, 2: Employee vs.
}

interface UserState {
  users: User[];
  isLoading: boolean;
  fetchUsers: () => Promise<void>;
  updateUserRole: (userId: number, newRole: number) => Promise<void>;
}

export const useUserStore = create<UserState>((set) => ({
  users: [],
  isLoading: false,

  fetchUsers: async () => {
    set({ isLoading: true });
    try {
      const response = await api.get('/Users');
      set({ users: response.data, isLoading: false });
    } catch (error) {
      console.error('Kullanıcılar yüklenirken hata oluştu:', error);
      set({ isLoading: false });
    }
  },

  updateUserRole: async (userId: number, newRole: number) => {
    try {
      await api.put(`/Users/${userId}/role`, { userId, newRole });
      set((state) => ({
        users: state.users.map((u) => (u.id === userId ? { ...u, role: newRole } : u)),
      }));
    } catch (error) {
      console.error('Kullanıcı rolü güncellenirken hata oluştu:', error);
    }
  },
}));

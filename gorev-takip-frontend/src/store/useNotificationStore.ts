import { create } from 'zustand';

export interface Notification {
  id: string;
  message: string;
  read: boolean;
  date: Date;
}

interface NotificationState {
  notifications: Notification[];
  addNotification: (message: string) => void;
  markAsRead: (id: string) => void;
  markAllAsRead: () => void;
  clearNotifications: () => void;
}

export const useNotificationStore = create<NotificationState>((set) => ({
  notifications: [],
  
  addNotification: (message: string) => {
    const newNotif: Notification = {
      id: Math.random().toString(36).substring(7),
      message,
      read: false,
      date: new Date()
    };
    
    set((state) => ({
      notifications: [newNotif, ...state.notifications]
    }));
  },
  
  markAsRead: (id: string) => {
    set((state) => ({
      notifications: state.notifications.map((n) => 
        n.id === id ? { ...n, read: true } : n
      )
    }));
  },
  
  markAllAsRead: () => {
    set((state) => ({
      notifications: state.notifications.map((n) => ({ ...n, read: true }))
    }));
  },
  
  clearNotifications: () => {
    set({ notifications: [] });
  }
}));

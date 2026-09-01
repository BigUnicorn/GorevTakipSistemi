import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { api } from '@/lib/api';

export interface Notification {
  id: number;
  message: string;
  isRead: boolean;
  createdAt: string;
  relatedTaskId?: number;
}

export const useNotifications = () => {
  const queryClient = useQueryClient();

  const { data: notifications = [], isLoading, error } = useQuery<Notification[]>({
    queryKey: ['notifications'],
    queryFn: async () => {
      const response = await api.get('/Notifications');
      return response.data;
    },
    refetchInterval: 30000, // Opsiyonel: 30 saniyede bir otomatik yenile (SignalR'a ek olarak garanti olması için)
  });

  const markAsReadMutation = useMutation({
    mutationFn: async (id: number) => {
      await api.put(`/Notifications/${id}/read`);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['notifications'] });
    },
  });

  const markAllAsReadMutation = useMutation({
    mutationFn: async () => {
      await api.put('/Notifications/read-all');
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['notifications'] });
    },
  });

  return {
    notifications,
    isLoading,
    error,
    markAsRead: (id: number) => markAsReadMutation.mutate(id),
    markAllAsRead: () => markAllAsReadMutation.mutate(),
    unreadCount: notifications.filter(n => !n.isRead).length
  };
};

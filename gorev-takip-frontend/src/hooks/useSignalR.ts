import { useEffect, useState } from 'react';
import * as signalR from '@microsoft/signalr';
import { useAuthStore } from '@/store/useAuthStore';
import { useNotificationStore } from '@/store/useNotificationStore';
import { useQueryClient } from '@tanstack/react-query';
import { Task } from '@/hooks/useTasks';

export const useSignalR = () => {
  const [connection, setConnection] = useState<signalR.HubConnection | null>(null);
  const { isAuthenticated } = useAuthStore();
  const queryClient = useQueryClient();

  useEffect(() => {
    if (!isAuthenticated) {
      return;
    }

    const newConnection = new signalR.HubConnectionBuilder()
      .withUrl('/taskhub')
      .withAutomaticReconnect()
      .build();

    // eslint-disable-next-line react-hooks/set-state-in-effect
    setConnection(newConnection);

    return () => {
      newConnection.stop();
    };
  }, [isAuthenticated]);

  const { user } = useAuthStore();
  const { addNotification } = useNotificationStore();

  useEffect(() => {
    if (connection) {
      connection.start()
        .then(() => {
          console.log('SignalR Connected!');

          connection.on('ReceiveTaskUpdate', (data: { action: string; task: Task }) => {
            console.log('Task Update Received:', data);

            const isRelevant = user?.role === 1 || data.task?.assignedUserId === user?.id;

            if (isRelevant) {
              if (data.action === 'Create') {
                addNotification(`Yeni görev oluşturuldu: ${data.task?.title || 'Bilinmiyor'}`);
              } else if (data.action === 'Update') {
                addNotification(`Görev güncellendi: ${data.task?.title || 'Bilinmiyor'}`);
              } else if (data.action === 'Delete') {
                addNotification('Bir görev silindi.');
              }
            }

            // Invalidate queries to trigger a refetch
            queryClient.invalidateQueries({ queryKey: ['tasks'] });
          });

          connection.on('ReceiveNewComment', (taskId: number) => {
            const tasks = queryClient.getQueryData<Task[]>(['tasks']) || [];
            const task = tasks.find((t: Task) => t.id === taskId);
            const isRelevant = user?.role === 1 || task?.assignedUserId === user?.id;

            if (isRelevant) {
              addNotification(`#${taskId} numaralı göreve yeni bir yorum yapıldı.`);
            }
          });
        })
        .catch(e => console.log('SignalR Connection Error: ', e));
    }
  }, [connection, queryClient, addNotification, user]);

  return connection;
};

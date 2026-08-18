import { useEffect, useState } from 'react';
import * as signalR from '@microsoft/signalr';
import { useAuthStore } from '@/store/useAuthStore';
import { useTaskStore } from '@/store/useTaskStore';
import { useNotificationStore } from '@/store/useNotificationStore';

export const useSignalR = () => {
  const [connection, setConnection] = useState<signalR.HubConnection | null>(null);
  const { isAuthenticated } = useAuthStore();
  const { fetchTasks } = useTaskStore();

  useEffect(() => {
    if (!isAuthenticated) {
      if (connection) {
        connection.stop();
        setConnection(null);
      }
      return;
    }

    const token = sessionStorage.getItem('token');
    
    const newConnection = new signalR.HubConnectionBuilder()
      .withUrl('/taskhub', {
        accessTokenFactory: () => token || ''
      })
      .withAutomaticReconnect()
      .build();

    setConnection(newConnection);
  }, [isAuthenticated]);

  const { user } = useAuthStore();
  const { tasks } = useTaskStore();
  const { addNotification } = useNotificationStore();

  useEffect(() => {
    if (connection) {
      connection.start()
        .then(() => {
          console.log('SignalR Connected!');
          
          connection.on('ReceiveTaskUpdate', (data: any) => {
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

            // Re-fetch tasks to keep it simple, or update zustand store directly
            fetchTasks();
          });

          connection.on('ReceiveNewComment', (taskId: number) => {
            // Find task to check if it's assigned to current user
            const task = useTaskStore.getState().tasks.find(t => t.id === taskId);
            const isRelevant = user?.role === 1 || task?.assignedUserId === user?.id;
            
            if (isRelevant) {
              addNotification(`#${taskId} numaralı göreve yeni bir yorum yapıldı.`);
            }
          });
        })
        .catch(e => console.log('SignalR Connection Error: ', e));
    }
  }, [connection, fetchTasks, addNotification, user]);

  return connection;
};

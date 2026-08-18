import { useEffect, useState } from 'react';
import * as signalR from '@microsoft/signalr';
import { useAuthStore } from '@/store/useAuthStore';
import { useTaskStore } from '@/store/useTaskStore';

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

    const token = localStorage.getItem('token');
    
    const newConnection = new signalR.HubConnectionBuilder()
      .withUrl('/taskhub', {
        accessTokenFactory: () => token || ''
      })
      .withAutomaticReconnect()
      .build();

    setConnection(newConnection);
  }, [isAuthenticated]);

  useEffect(() => {
    if (connection) {
      connection.start()
        .then(() => {
          console.log('SignalR Connected!');
          
          connection.on('ReceiveTaskUpdate', (data: any) => {
            console.log('Task Update Received:', data);
            // Re-fetch tasks to keep it simple, or update zustand store directly
            fetchTasks();
          });
        })
        .catch(e => console.log('SignalR Connection Error: ', e));
    }
  }, [connection, fetchTasks]);

  return connection;
};

import { useEffect, useState } from 'react';
import * as signalR from '@microsoft/signalr';
import { useAuthStore } from '@/store/useAuthStore';
import { useQueryClient } from '@tanstack/react-query';
import { Task } from '@/hooks/useTasks';

export const useSignalR = () => {
  const [connection, setConnection] = useState<signalR.HubConnection | null>(null);
  const { isAuthenticated } = useAuthStore();
  const queryClient = useQueryClient();

  useEffect(() => {
    if (!isAuthenticated) return;

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

  useEffect(() => {
    if (connection) {
      connection.start()
        .then(() => {
          console.log('SignalR Connected!');

          connection.on('ReceiveTaskUpdate', (data: { action: string; task: Task }) => {
            console.log('Task Update Received:', data);
            
            // Invalidate tasks query to update lists
            queryClient.invalidateQueries({ queryKey: ['tasks'] });
            
            // Invalidate notifications query to fetch new DB notifications
            queryClient.invalidateQueries({ queryKey: ['notifications'] });
          });

          connection.on('ReceiveNewComment', () => {
            // Invalidate task specific comments or tasks
            queryClient.invalidateQueries({ queryKey: ['tasks'] });
            
            // Invalidate notifications query
            queryClient.invalidateQueries({ queryKey: ['notifications'] });
          });
        })
        .catch(e => console.log('SignalR Connection Error: ', e));
    }
  }, [connection, queryClient]);

  return connection;
};

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { api } from '@/lib/api';

export interface Task {
  id: number;
  title: string;
  description: string;
  status: number; // 0: Todo, 1: InProgress, 2: Done
  category: number;
  dueDate: string;
  assignedUserId: number;
  assignedUserName: string;
}

export interface TaskComment {
  id: number;
  text: string;
  userName: string;
  createdDate: string;
}

export interface TaskHistory {
  actionMessage: string;
  createdDate: string;
}

export interface TaskAttachment {
  id: number;
  taskId: number;
  fileName: string;
  filePath: string;
  contentType: string;
  fileSize: number;
  uploadedAt: string;
  uploadedByUserName: string;
}

// Queries
export const useTasksQuery = () => {
  return useQuery({
    queryKey: ['tasks'],
    queryFn: async () => {
      const response = await api.get('/Tasks?pageNumber=1&pageSize=100');
      return response.data.data as Task[];
    },
  });
};

export const useTaskCommentsQuery = (taskId: number) => {
  return useQuery({
    queryKey: ['tasks', taskId, 'comments'],
    queryFn: async () => {
      const res = await api.get(`/Tasks/${taskId}/comments`);
      return res.data as TaskComment[];
    },
    enabled: !!taskId,
  });
};

export const useTaskHistoryQuery = (taskId: number) => {
  return useQuery({
    queryKey: ['tasks', taskId, 'history'],
    queryFn: async () => {
      const res = await api.get(`/Tasks/${taskId}/history`);
      return res.data as TaskHistory[];
    },
    enabled: !!taskId,
  });
};

export const useTaskAttachmentsQuery = (taskId: number) => {
  return useQuery({
    queryKey: ['tasks', taskId, 'attachments'],
    queryFn: async () => {
      const res = await api.get(`/Attachments/task/${taskId}`);
      return res.data as TaskAttachment[];
    },
    enabled: !!taskId,
  });
};

// Mutations
export const useCreateTaskMutation = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (data: Partial<Task>) => {
      await api.post('/Tasks', data);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['tasks'] });
    },
  });
};

export const useUpdateTaskStatusMutation = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({ taskId, newStatus }: { taskId: number; newStatus: number }) => {
      await api.patch(`/Tasks/${taskId}/status`, newStatus, {
        headers: { 'Content-Type': 'application/json' },
      });
    },
    // Optimistic Update
    onMutate: async ({ taskId, newStatus }: { taskId: number, newStatus: number }) => {
      await queryClient.cancelQueries({ queryKey: ['tasks'] });
      
      const previousTasks = queryClient.getQueryData<Task[]>(['tasks']);

      if (previousTasks) {
        queryClient.setQueryData<Task[]>(['tasks'], (old: Task[] | undefined) =>
          old?.map((task: Task) =>
            task.id === taskId ? { ...task, status: newStatus } : task
          )
        );
      }

      return { previousTasks };
    },
    onError: (err: any, newTodo: any, context: any) => {
      if (context?.previousTasks) {
        queryClient.setQueryData(['tasks'], context.previousTasks);
      }
    },
    onSettled: () => {
      queryClient.invalidateQueries({ queryKey: ['tasks'] });
    },
  });
};

export const useUpdateTaskDetailsMutation = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({ taskId, data }: { taskId: number; data: Partial<Task> }) => {
      await api.put(`/Tasks/${taskId}`, { id: taskId, ...data });
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['tasks'] });
    },
  });
};

export const useAddCommentMutation = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({ taskId, text }: { taskId: number; text: string }) => {
      await api.post(`/Tasks/${taskId}/comments`, { text });
    },
    onSuccess: (_: any, variables: any) => {
      queryClient.invalidateQueries({ queryKey: ['tasks', variables.taskId, 'comments'] });
      queryClient.invalidateQueries({ queryKey: ['tasks', variables.taskId, 'history'] });
    },
  });
};

export const useUploadAttachmentMutation = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({ taskId, file }: { taskId: number; file: File }) => {
      const formData = new FormData();
      formData.append('file', file);
      const res = await api.post(`/Attachments/task/${taskId}`, formData, {
        headers: { 'Content-Type': 'multipart/form-data' },
      });
      return res.data;
    },
    onSuccess: (_: any, variables: any) => {
      queryClient.invalidateQueries({ queryKey: ['tasks', variables.taskId, 'attachments'] });
      queryClient.invalidateQueries({ queryKey: ['tasks', variables.taskId, 'history'] });
    },
  });
};

export const useDeleteAttachmentMutation = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({ attachmentId }: { attachmentId: number }) => {
      await api.delete(`/Attachments/${attachmentId}`);
    },
    onSuccess: () => {
      // We ideally want the taskId to invalidate specific queries, but global invalidation works too for simplicity here if we don't have taskId
      // Alternatively, we could clear all attachments cache, but let's clear all tasks related query to be safe
      queryClient.invalidateQueries({ queryKey: ['tasks'] });
    },
  });
};

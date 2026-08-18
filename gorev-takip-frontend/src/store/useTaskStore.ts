import { create } from 'zustand';
import { api } from '@/lib/api';

export interface Task {
  id: number;
  title: string;
  description: string;
  status: number; // 0: Todo, 1: InProgress, 2: Done
  category: number;
  dueDate: string;
  assignedUserId: number;
  assignedUserFullName: string;
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

interface TaskState {
  tasks: Task[];
  setTasks: (tasks: Task[]) => void;
  fetchTasks: () => Promise<void>;
  createTask: (data: Partial<Task>) => Promise<void>;
  updateTaskStatus: (taskId: number, newStatus: number) => Promise<void>;
  updateTaskDetails: (taskId: number, data: Partial<Task>) => Promise<void>;
  fetchTaskComments: (taskId: number) => Promise<TaskComment[]>;
  addTaskComment: (taskId: number, text: string) => Promise<void>;
  fetchTaskHistory: (taskId: number) => Promise<TaskHistory[]>;
  fetchTaskAttachments: (taskId: number) => Promise<TaskAttachment[]>;
  uploadTaskAttachment: (taskId: number, file: File) => Promise<TaskAttachment>;
}

export const useTaskStore = create<TaskState>((set, get) => ({
  tasks: [],
  setTasks: (tasks) => set({ tasks }),
  fetchTasks: async () => {
    try {
      const response = await api.get('/Tasks?pageNumber=1&pageSize=100'); // TODO: Pagination support later
      set({ tasks: response.data.data });
    } catch (error) {
      console.error('Error fetching tasks:', error);
    }
  },
  createTask: async (data) => {
    try {
      await api.post('/Tasks', data);
      await get().fetchTasks();
    } catch (error) {
      console.error('Error creating task:', error);
      throw error;
    }
  },
  updateTaskStatus: async (taskId, newStatus) => {
    try {
      const previousTasks = get().tasks;
      set({
        tasks: previousTasks.map(t => t.id === taskId ? { ...t, status: newStatus } : t)
      });
      
      await api.patch(`/Tasks/${taskId}/status`, newStatus, {
        headers: { 'Content-Type': 'application/json' }
      });
    } catch (error) {
      console.error('Error updating task status:', error);
      get().fetchTasks();
    }
  },
  updateTaskDetails: async (taskId, data) => {
    try {
      const previousTasks = get().tasks;
      set({
        tasks: previousTasks.map(t => t.id === taskId ? { ...t, ...data } : t)
      });
      
      await api.put(`/Tasks/${taskId}`, { id: taskId, ...data });
    } catch (error) {
      console.error('Error updating task details:', error);
      get().fetchTasks();
    }
  },
  fetchTaskComments: async (taskId) => {
    const response = await api.get(`/Tasks/${taskId}/comments`);
    return response.data;
  },
  addTaskComment: async (taskId, text) => {
    await api.post(`/Tasks/${taskId}/comments`, { text });
  },
  fetchTaskHistory: async (taskId) => {
    const response = await api.get(`/Tasks/${taskId}/history`);
    return response.data;
  },
  fetchTaskAttachments: async (taskId) => {
    const response = await api.get(`/Attachments/task/${taskId}`);
    return response.data;
  },
  uploadTaskAttachment: async (taskId, file) => {
    const formData = new FormData();
    formData.append('file', file);
    const response = await api.post(`/Attachments/task/${taskId}`, formData, {
      headers: {
        'Content-Type': 'multipart/form-data'
      }
    });
    return response.data;
  }
}));

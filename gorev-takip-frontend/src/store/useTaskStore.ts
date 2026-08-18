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

interface TaskState {
  tasks: Task[];
  setTasks: (tasks: Task[]) => void;
  fetchTasks: () => Promise<void>;
  updateTaskStatus: (taskId: number, newStatus: number) => Promise<void>;
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
  updateTaskStatus: async (taskId, newStatus) => {
    try {
      // Optimistic update
      const previousTasks = get().tasks;
      set({
        tasks: previousTasks.map(t => t.id === taskId ? { ...t, status: newStatus } : t)
      });
      
      await api.patch(`/Tasks/${taskId}/status`, newStatus, {
        headers: { 'Content-Type': 'application/json' }
      });
    } catch (error) {
      console.error('Error updating task status:', error);
      // Revert on error
      get().fetchTasks();
    }
  }
}));

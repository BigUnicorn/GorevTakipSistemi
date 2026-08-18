'use client';

import { useEffect, useState } from 'react';
import KanbanBoard from '@/components/KanbanBoard';
import { useTaskStore } from '@/store/useTaskStore';
import { useAuthStore } from '@/store/useAuthStore';

export default function TasksPage() {
  const { fetchTasks, tasks } = useTaskStore();
  const { user } = useAuthStore();
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const loadTasks = async () => {
      await fetchTasks();
      setLoading(false);
    };
    
    loadTasks();
  }, [fetchTasks]);

  if (loading) {
    return <div className="flex items-center justify-center h-full text-gray-400">Görevler yükleniyor...</div>;
  }

  return (
    <div className="h-full flex flex-col">
      <div className="flex justify-between items-center mb-6">
        <div>
          <h1 className="text-2xl font-bold text-white mb-1">Tüm Görevler</h1>
          <p className="text-gray-400 text-sm">Sürükle bırak ile görev durumlarını güncelleyebilirsiniz.</p>
        </div>
        
        {user?.role === 1 && (
          <button className="bg-purple-600 hover:bg-purple-500 text-white font-medium py-2 px-4 rounded-xl transition-colors shadow-lg shadow-purple-500/20">
            + Yeni Görev
          </button>
        )}
      </div>

      <KanbanBoard />
    </div>
  );
}

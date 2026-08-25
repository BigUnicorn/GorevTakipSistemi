'use client';

import { useState } from 'react';
import KanbanBoard from '@/components/KanbanBoard';
import TaskListView from '@/components/TaskListView';
import CreateTaskModal from '@/components/CreateTaskModal';
import TaskDetailModal from '@/components/TaskDetailModal';
import { Task, useTasksQuery } from '@/hooks/useTasks';
import { useAuthStore } from '@/store/useAuthStore';
import { LayoutGrid, List } from 'lucide-react';

export default function TasksPage() {
  const { data: tasks = [], isLoading: loading } = useTasksQuery();
  const { user } = useAuthStore();
  
  const [viewMode, setViewMode] = useState<'kanban' | 'list'>('kanban');
  const [isCreateModalOpen, setIsCreateModalOpen] = useState(false);
  const [selectedTask, setSelectedTask] = useState<Task | null>(null);

  if (loading) {
    return <div className="flex items-center justify-center h-full text-gray-400">Görevler yükleniyor...</div>;
  }

  return (
    <div className="h-full flex flex-col">
      <div className="flex flex-col md:flex-row justify-between items-start md:items-center mb-6 gap-4">
        <div>
          <h1 className="text-2xl font-bold text-white mb-1">Tüm Görevler</h1>
          <p className="text-gray-400 text-sm">Görevleri görüntüleyin ve yönetin.</p>
        </div>
        
        <div className="flex items-center gap-3 w-full md:w-auto">
          <div className="bg-gray-800 rounded-lg p-1 flex items-center shadow-inner">
            <button 
              onClick={() => setViewMode('kanban')}
              className={`p-2 rounded-md flex items-center transition-all ${viewMode === 'kanban' ? 'bg-gray-700 text-white shadow' : 'text-gray-400 hover:text-gray-300'}`}
              title="Kanban Görünümü"
            >
              <LayoutGrid size={18} />
            </button>
            <button 
              onClick={() => setViewMode('list')}
              className={`p-2 rounded-md flex items-center transition-all ${viewMode === 'list' ? 'bg-gray-700 text-white shadow' : 'text-gray-400 hover:text-gray-300'}`}
              title="Liste Görünümü"
            >
              <List size={18} />
            </button>
          </div>

          {user?.role === 1 && (
            <button 
              onClick={() => setIsCreateModalOpen(true)}
              className="bg-purple-600 hover:bg-purple-500 text-white font-medium py-2 px-4 rounded-xl transition-colors shadow-lg shadow-purple-500/20 whitespace-nowrap ml-auto md:ml-0"
            >
              + Yeni Görev
            </button>
          )}
        </div>
      </div>

      {viewMode === 'kanban' ? (
        <KanbanBoard />
      ) : (
        <TaskListView tasks={tasks} onTaskClick={setSelectedTask} />
      )}

      {/* Modals */}
      <CreateTaskModal 
        isOpen={isCreateModalOpen} 
        onClose={() => setIsCreateModalOpen(false)} 
      />

      {selectedTask && viewMode === 'list' && (
        <TaskDetailModal 
          task={selectedTask} 
          isOpen={true} 
          onClose={() => setSelectedTask(null)} 
        />
      )}
    </div>
  );
}

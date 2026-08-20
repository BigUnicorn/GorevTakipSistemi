'use client';

import React from 'react';
import { Task } from '@/hooks/useTasks';
import { Calendar, User, Layout, Code, Database, Bug, Smartphone, TerminalSquare } from 'lucide-react';

interface Props {
  tasks: Task[];
  onTaskClick: (task: Task) => void;
}

const statusLabels: Record<number, string> = {
  1: 'Yapılacaklar',
  2: 'Devam Edenler',
  3: 'Tamamlananlar'
};

const statusColors: Record<number, string> = {
  1: 'bg-gray-500/20 text-gray-400 border-gray-500/30',
  2: 'bg-blue-500/20 text-blue-400 border-blue-500/30',
  3: 'bg-green-500/20 text-green-400 border-green-500/30'
};

const categoryLabels: Record<number, string> = {
  1: 'Frontend',
  2: 'Backend',
  3: 'Database',
  4: 'Bug Fix',
  5: 'Mobile',
  6: 'DevOps',
};

const getCategoryIcon = (categoryId: number) => {
  switch (categoryId) {
    case 1: return <Layout size={14} className="mr-1" />;
    case 2: return <Code size={14} className="mr-1" />;
    case 3: return <Database size={14} className="mr-1" />;
    case 4: return <Bug size={14} className="mr-1" />;
    case 5: return <Smartphone size={14} className="mr-1" />;
    case 6: return <TerminalSquare size={14} className="mr-1" />;
    default: return <Layout size={14} className="mr-1" />;
  }
};

export default function TaskListView({ tasks, onTaskClick }: Props) {
  if (tasks.length === 0) {
    return (
      <div className="flex items-center justify-center h-64 text-gray-400 border border-dashed border-gray-700 rounded-xl">
        Gösterilecek görev bulunamadı.
      </div>
    );
  }

  return (
    <div className="bg-gray-900/50 backdrop-blur-sm border border-gray-800 rounded-2xl overflow-hidden">
      <div className="overflow-x-auto">
        <table className="w-full text-left border-collapse min-w-[800px]">
          <thead>
            <tr className="bg-gray-800/80 text-gray-300 text-sm border-b border-gray-700">
              <th className="p-4 font-semibold">Görev Başlığı</th>
              <th className="p-4 font-semibold">Durum</th>
              <th className="p-4 font-semibold">Kategori</th>
              <th className="p-4 font-semibold">Atanan Kişi</th>
              <th className="p-4 font-semibold">Bitiş Tarihi</th>
            </tr>
          </thead>
          <tbody>
            {tasks.map(task => (
              <tr 
                key={task.id} 
                onClick={() => onTaskClick(task)}
                className="border-b border-gray-800 hover:bg-gray-800/50 cursor-pointer transition-colors group"
              >
                <td className="p-4">
                  <p className="text-white font-medium group-hover:text-purple-400 transition-colors">{task.title}</p>
                  <p className="text-sm text-gray-500 line-clamp-1 mt-1">{task.description}</p>
                </td>
                <td className="p-4">
                  <span className={`inline-flex items-center px-2.5 py-1 rounded-md text-xs font-medium border ${statusColors[task.status]}`}>
                    {statusLabels[task.status]}
                  </span>
                </td>
                <td className="p-4">
                  <span className="inline-flex items-center text-gray-400 text-sm">
                    {getCategoryIcon(task.category)}
                    {categoryLabels[task.category] || 'Diğer'}
                  </span>
                </td>
                <td className="p-4">
                  <div className="flex items-center gap-2 text-gray-300 text-sm">
                    <div className="w-6 h-6 rounded-full bg-gray-700 flex items-center justify-center text-xs">
                      {task.assignedUserName?.charAt(0) || '?'}
                    </div>
                    {task.assignedUserName || 'Atanmadı'}
                  </div>
                </td>
                <td className="p-4">
                  <div className="flex items-center gap-2 text-gray-400 text-sm">
                    <Calendar size={14} />
                    {task.dueDate ? new Date(task.dueDate).toLocaleDateString('tr-TR') : '-'}
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}

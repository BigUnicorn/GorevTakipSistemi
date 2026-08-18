'use client';

import { useSortable } from '@dnd-kit/sortable';
import { CSS } from '@dnd-kit/utilities';
import { Task } from '@/store/useTaskStore';
import { Calendar, Tag, User } from 'lucide-react';

interface Props {
  task: Task;
  onClick?: (task: Task) => void;
}

const categoryColors: Record<number, string> = {
  0: 'bg-pink-500/20 text-pink-400 border-pink-500/30',
  1: 'bg-indigo-500/20 text-indigo-400 border-indigo-500/30',
  2: 'bg-emerald-500/20 text-emerald-400 border-emerald-500/30',
  3: 'bg-red-500/20 text-red-400 border-red-500/30',
  4: 'bg-cyan-500/20 text-cyan-400 border-cyan-500/30',
  5: 'bg-orange-500/20 text-orange-400 border-orange-500/30',
};

const categoryLabels: Record<number, string> = {
  0: 'Frontend',
  1: 'Backend',
  2: 'Database',
  3: 'Bug Fix',
  4: 'Mobile',
  5: 'DevOps',
};

export default function TaskCard({ task, onClick }: Props) {
  const {
    attributes,
    listeners,
    setNodeRef,
    transform,
    transition,
    isDragging,
  } = useSortable({ id: task.id.toString(), data: { type: 'Task', task } });

  const style = {
    transform: CSS.Transform.toString(transform),
    transition,
  };

  if (isDragging) {
    return (
      <div
        ref={setNodeRef}
        style={style}
        className="bg-gray-800 border-2 border-purple-500 rounded-xl p-4 min-h-[120px] opacity-50 z-50 cursor-grabbing"
      />
    );
  }

  return (
    <div
      ref={setNodeRef}
      style={style}
      {...attributes}
      {...listeners}
      onClick={() => onClick && onClick(task)}
      className="bg-gray-800/80 hover:bg-gray-800 border border-gray-700 hover:border-gray-600 rounded-xl p-4 shadow-lg cursor-grab active:cursor-grabbing transition-colors group"
    >
      <div className="flex justify-between items-start mb-3">
        <span className={`text-xs font-semibold px-2 py-1 rounded-md border ${categoryColors[task.category] || 'bg-gray-500/20 text-gray-400 border-gray-500/30'}`}>
          {categoryLabels[task.category] || 'Diğer'}
        </span>
        {/* Optional: priority indicator or options menu */}
      </div>
      
      <h4 className="text-white font-medium mb-2 group-hover:text-purple-400 transition-colors">{task.title}</h4>
      <p className="text-gray-400 text-sm line-clamp-2 mb-4">{task.description}</p>
      
      <div className="flex items-center justify-between text-xs text-gray-500 border-t border-gray-700/50 pt-3 mt-auto">
        <div className="flex items-center gap-1 bg-gray-900/50 px-2 py-1 rounded-md">
          <Calendar size={12} />
          <span>{new Date(task.dueDate).toLocaleDateString('tr-TR')}</span>
        </div>
        <div className="flex items-center gap-1 bg-gray-900/50 px-2 py-1 rounded-md" title={task.assignedUserName}>
          <User size={12} />
          <span className="truncate max-w-[80px]">{task.assignedUserName?.split(' ')[0] || 'Atanmadı'}</span>
        </div>
      </div>
    </div>
  );
}

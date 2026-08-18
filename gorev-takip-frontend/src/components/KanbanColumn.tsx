'use client';

import { useDroppable } from '@dnd-kit/core';
import { SortableContext, verticalListSortingStrategy } from '@dnd-kit/sortable';
import { Task } from '@/store/useTaskStore';
import TaskCard from './TaskCard';

interface Props {
  status: number;
  title: string;
  tasks: Task[];
  onTaskClick?: (task: Task) => void;
}

export default function KanbanColumn({ status, title, tasks, onTaskClick }: Props) {
  const { setNodeRef, isOver } = useDroppable({
    id: status.toString(),
    data: {
      type: 'Column',
      status
    }
  });

  return (
    <div className="flex flex-col bg-gray-900/50 backdrop-blur-sm border border-gray-800 rounded-2xl p-4 w-full md:w-1/3 h-full max-h-full">
      <div className="flex items-center justify-between mb-4 px-2">
        <h3 className="font-bold text-lg text-white">{title}</h3>
        <span className="bg-gray-800 text-gray-400 text-xs font-bold px-2.5 py-1 rounded-full">
          {tasks.length}
        </span>
      </div>
      
      <div 
        ref={setNodeRef}
        className={`flex-1 overflow-y-auto space-y-3 p-2 rounded-xl transition-colors ${
          isOver ? 'bg-gray-800/50 border-2 border-dashed border-purple-500/50' : 'border-2 border-transparent'
        }`}
      >
        <SortableContext items={tasks.map(t => t.id.toString())} strategy={verticalListSortingStrategy}>
          {tasks.map(task => (
            <TaskCard key={task.id} task={task} onClick={onTaskClick} />
          ))}
        </SortableContext>
        
        {tasks.length === 0 && !isOver && (
          <div className="h-full flex items-center justify-center text-gray-500 text-sm border-2 border-dashed border-gray-800 rounded-xl">
            Görev bulunmuyor
          </div>
        )}
      </div>
    </div>
  );
}

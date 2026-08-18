'use client';

import { useState } from 'react';
import { DndContext, DragEndEvent, DragOverlay, DragStartEvent, PointerSensor, useSensor, useSensors, closestCorners } from '@dnd-kit/core';
import { useTaskStore, Task } from '@/store/useTaskStore';
import KanbanColumn from './KanbanColumn';
import TaskCard from './TaskCard';

export default function KanbanBoard() {
  const { tasks, updateTaskStatus } = useTaskStore();
  const [activeTask, setActiveTask] = useState<Task | null>(null);

  const sensors = useSensors(
    useSensor(PointerSensor, {
      activationConstraint: {
        distance: 5,
      },
    })
  );

  const todoTasks = tasks.filter(t => t.status === 0);
  const inProgressTasks = tasks.filter(t => t.status === 1);
  const doneTasks = tasks.filter(t => t.status === 2);

  const handleDragStart = (event: DragStartEvent) => {
    const { active } = event;
    if (active.data.current?.type === 'Task') {
      setActiveTask(active.data.current.task);
    }
  };

  const handleDragEnd = (event: DragEndEvent) => {
    setActiveTask(null);
    const { active, over } = event;

    if (!over) return;

    const activeId = active.id;
    const overId = over.id;

    if (activeId === overId) return;

    const isActiveTask = active.data.current?.type === 'Task';
    const isOverColumn = over.data.current?.type === 'Column';
    const isOverTask = over.data.current?.type === 'Task';

    if (!isActiveTask) return;

    // Dropping a task over a column
    if (isOverColumn) {
      const newStatusId = over.data.current?.status;
      const activeTaskStatus = active.data.current?.task.status;
      
      if (newStatusId !== activeTaskStatus) {
        updateTaskStatus(Number(activeId), newStatusId);
      }
      return;
    }

    // Dropping a task over another task (in the same or different column)
    if (isOverTask) {
      const newStatusId = over.data.current?.task.status;
      const activeTaskStatus = active.data.current?.task.status;

      if (newStatusId !== activeTaskStatus) {
        updateTaskStatus(Number(activeId), newStatusId);
      }
    }
  };

  return (
    <DndContext
      sensors={sensors}
      collisionDetection={closestCorners}
      onDragStart={handleDragStart}
      onDragEnd={handleDragEnd}
    >
      <div className="flex flex-col md:flex-row gap-6 h-[calc(100vh-140px)]">
        <KanbanColumn status={0} title="Yapılacaklar" tasks={todoTasks} />
        <KanbanColumn status={1} title="Devam Edenler" tasks={inProgressTasks} />
        <KanbanColumn status={2} title="Tamamlananlar" tasks={doneTasks} />
      </div>

      <DragOverlay>
        {activeTask ? <TaskCard task={activeTask} /> : null}
      </DragOverlay>
    </DndContext>
  );
}

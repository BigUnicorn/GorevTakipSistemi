'use client';

import React, { useState, useEffect } from 'react';
import { X } from 'lucide-react';
import { Task, useCreateTaskMutation } from '@/hooks/useTasks';
import { useUserStore } from '@/store/useUserStore';

interface CreateTaskModalProps {
  isOpen: boolean;
  onClose: () => void;
}

export default function CreateTaskModal({ isOpen, onClose }: CreateTaskModalProps) {
  const { mutateAsync: createTask } = useCreateTaskMutation();
  const { users, fetchUsers } = useUserStore();
  
  const [formData, setFormData] = useState<Partial<Task>>({
    title: '',
    description: '',
    status: 1, // Default: Yapılacaklar (Todo)
    category: 1, // Default: Frontend
    dueDate: '',
    assignedUserId: 0
  });

  const [isSubmitting, setIsSubmitting] = useState(false);

  useEffect(() => {
    if (isOpen) {
      fetchUsers();
    }
  }, [isOpen, fetchUsers]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!formData.title || !formData.dueDate || !formData.assignedUserId) {
      alert("Lütfen zorunlu alanları doldurun.");
      return;
    }
    
    setIsSubmitting(true);
    try {
      const formattedData = {
        ...formData,
        dueDate: formData.dueDate ? new Date(formData.dueDate).toISOString() : undefined
      };
      await createTask(formattedData);
      onClose();
      // Reset form
      setFormData({
        title: '',
        description: '',
        status: 1,
        category: 1,
        dueDate: '',
        assignedUserId: 0
      });
    } catch {
      alert("Görev oluşturulurken bir hata oluştu. Lütfen tekrar deneyin.");
    } finally {
      setIsSubmitting(false);
    }
  };

  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black bg-opacity-50 p-4">
      <div className="bg-white dark:bg-gray-800 rounded-lg shadow-xl w-full max-w-2xl flex flex-col">
        {/* Header */}
        <div className="flex justify-between items-center p-4 border-b dark:border-gray-700">
          <h2 className="text-xl font-bold dark:text-white">Yeni Görev Ekle</h2>
          <button onClick={onClose} className="p-1 hover:bg-gray-200 dark:hover:bg-gray-700 rounded-full transition-colors">
            <X size={24} className="text-gray-500 dark:text-gray-400" />
          </button>
        </div>

        {/* Form */}
        <form onSubmit={handleSubmit} className="p-6 space-y-4">
          <div>
            <label className="block text-sm font-medium mb-1 text-gray-700 dark:text-gray-300">Başlık *</label>
            <input 
              type="text" 
              value={formData.title} 
              onChange={e => setFormData({...formData, title: e.target.value})} 
              className="w-full p-2 border rounded-lg dark:bg-gray-700 dark:border-gray-600 focus:ring-2 focus:ring-purple-500 outline-none" 
              placeholder="Görev başlığını girin"
              required 
            />
          </div>
          
          <div>
            <label className="block text-sm font-medium mb-1 text-gray-700 dark:text-gray-300">Açıklama</label>
            <textarea 
              value={formData.description} 
              onChange={e => setFormData({...formData, description: e.target.value})} 
              className="w-full p-2 border rounded-lg dark:bg-gray-700 dark:border-gray-600 focus:ring-2 focus:ring-purple-500 outline-none" 
              rows={4} 
              placeholder="Görevin detaylarını yazın..."
            />
          </div>
          
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div>
              <label className="block text-sm font-medium mb-1 text-gray-700 dark:text-gray-300">Kategori</label>
              <select 
                value={formData.category} 
                onChange={e => setFormData({...formData, category: Number(e.target.value)})} 
                className="w-full p-2 border rounded-lg dark:bg-gray-700 dark:border-gray-600 focus:ring-2 focus:ring-purple-500 outline-none"
              >
                <option value={1}>Frontend</option>
                <option value={2}>Backend</option>
                <option value={3}>Veritabanı</option>
                <option value={4}>Bug Fix</option>
                <option value={5}>Mobil</option>
                <option value={6}>DevOps</option>
              </select>
            </div>
            
            <div>
              <label className="block text-sm font-medium mb-1 text-gray-700 dark:text-gray-300">Durum</label>
              <select 
                value={formData.status} 
                onChange={e => setFormData({...formData, status: Number(e.target.value)})} 
                className="w-full p-2 border rounded-lg dark:bg-gray-700 dark:border-gray-600 focus:ring-2 focus:ring-purple-500 outline-none"
              >
                <option value={1}>Yapılacaklar</option>
                <option value={2}>Devam Edenler</option>
                <option value={3}>Tamamlananlar</option>
              </select>
            </div>
            
            <div>
              <label className="block text-sm font-medium mb-1 text-gray-700 dark:text-gray-300">Bitiş Tarihi *</label>
              <input 
                type="date" 
                value={formData.dueDate || ''} 
                onChange={e => setFormData({...formData, dueDate: e.target.value})} 
                className="w-full p-2 border rounded-lg dark:bg-gray-700 dark:border-gray-600 focus:ring-2 focus:ring-purple-500 outline-none" 
                required 
              />
            </div>
            
            <div>
              <label className="block text-sm font-medium mb-1 text-gray-700 dark:text-gray-300">Atanan Kişi *</label>
              <select 
                value={formData.assignedUserId} 
                onChange={e => setFormData({...formData, assignedUserId: Number(e.target.value)})} 
                className="w-full p-2 border rounded-lg dark:bg-gray-700 dark:border-gray-600 focus:ring-2 focus:ring-purple-500 outline-none"
                required
              >
                <option value={0} disabled>Lütfen seçin</option>
                {users.map(u => (
                  <option key={u.id} value={u.id}>{u.firstName} {u.lastName}</option>
                ))}
              </select>
            </div>
          </div>
          
          <div className="flex justify-end gap-3 mt-6">
            <button 
              type="button" 
              onClick={onClose}
              className="px-4 py-2 text-gray-600 bg-gray-100 hover:bg-gray-200 dark:bg-gray-700 dark:text-gray-300 dark:hover:bg-gray-600 rounded-lg transition-colors"
            >
              İptal
            </button>
            <button 
              type="submit" 
              disabled={isSubmitting}
              className="px-4 py-2 bg-purple-600 text-white hover:bg-purple-700 rounded-lg transition-colors shadow-lg shadow-purple-500/30 disabled:opacity-50"
            >
              {isSubmitting ? 'Kaydediliyor...' : 'Görev Oluştur'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}

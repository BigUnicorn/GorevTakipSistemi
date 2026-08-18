'use client';

import React, { useState, useEffect, useRef } from 'react';
import { X, MessageSquare, History, Paperclip, Edit, Download, Send, Trash2 } from 'lucide-react';
import { useTaskStore, Task, TaskComment, TaskHistory, TaskAttachment } from '@/store/useTaskStore';
import { useUserStore } from '@/store/useUserStore';

interface TaskDetailModalProps {
  task: Task;
  isOpen: boolean;
  onClose: () => void;
}

export default function TaskDetailModal({ task, isOpen, onClose }: TaskDetailModalProps) {
  const [activeTab, setActiveTab] = useState<'edit' | 'comments' | 'attachments' | 'history'>('edit');
  
  const { 
    updateTaskDetails, 
    fetchTaskComments, addTaskComment, 
    fetchTaskAttachments, uploadTaskAttachment, 
    fetchTaskHistory 
  } = useTaskStore();
  const { users, fetchUsers } = useUserStore();

  const [comments, setComments] = useState<TaskComment[]>([]);
  const [history, setHistory] = useState<TaskHistory[]>([]);
  const [attachments, setAttachments] = useState<TaskAttachment[]>([]);
  
  // Edit state
  const [editData, setEditData] = useState<Partial<Task>>({});
  
  // Comment state
  const [newComment, setNewComment] = useState('');
  
  // File state
  const fileInputRef = useRef<HTMLInputElement>(null);
  const [uploading, setUploading] = useState(false);

  useEffect(() => {
    if (isOpen) {
      setEditData({
        title: task.title,
        description: task.description,
        status: task.status,
        category: task.category,
        dueDate: task.dueDate ? task.dueDate.split('T')[0] : '',
        assignedUserId: task.assignedUserId
      });
      fetchUsers();
      loadTabData(activeTab);
    }
  }, [isOpen, task, activeTab]);

  const loadTabData = async (tab: string) => {
    if (tab === 'comments') {
      const data = await fetchTaskComments(task.id);
      setComments(data || []);
    } else if (tab === 'history') {
      const data = await fetchTaskHistory(task.id);
      setHistory(data || []);
    } else if (tab === 'attachments') {
      const data = await fetchTaskAttachments(task.id);
      setAttachments(data || []);
    }
  };

  const handleEditSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    const formattedData = {
      ...editData,
      dueDate: editData.dueDate ? new Date(editData.dueDate).toISOString() : undefined
    };
    await updateTaskDetails(task.id, formattedData);
    onClose();
  };

  const handleAddComment = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!newComment.trim()) return;
    await addTaskComment(task.id, newComment);
    setNewComment('');
    loadTabData('comments');
  };

  const handleFileUpload = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;

    if (file.size > 10 * 1024 * 1024) {
      alert("Dosya boyutu 10MB'dan büyük olamaz.");
      return;
    }

    setUploading(true);
    try {
      await uploadTaskAttachment(task.id, file);
      loadTabData('attachments');
    } catch (error) {
      console.error(error);
      alert("Dosya yüklenirken bir hata oluştu.");
    } finally {
      setUploading(false);
      if (fileInputRef.current) fileInputRef.current.value = '';
    }
  };

  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black bg-opacity-50">
      <div className="bg-white dark:bg-gray-800 rounded-lg shadow-xl w-full max-w-4xl max-h-[90vh] flex flex-col m-4">
        {/* Header */}
        <div className="flex justify-between items-center p-4 border-b dark:border-gray-700">
          <h2 className="text-xl font-bold dark:text-white truncate pr-4">Görev Detayı: {task.title}</h2>
          <button onClick={onClose} className="p-1 hover:bg-gray-200 dark:hover:bg-gray-700 rounded-full flex-shrink-0">
            <X size={24} />
          </button>
        </div>

        {/* Tabs */}
        <div className="flex border-b dark:border-gray-700 overflow-x-auto">
          <button onClick={() => setActiveTab('edit')} className={`flex items-center gap-2 p-4 font-semibold whitespace-nowrap ${activeTab === 'edit' ? 'text-blue-600 border-b-2 border-blue-600' : 'text-gray-500 hover:text-gray-700'}`}>
            <Edit size={18} /> Düzenle
          </button>
          <button onClick={() => setActiveTab('comments')} className={`flex items-center gap-2 p-4 font-semibold whitespace-nowrap ${activeTab === 'comments' ? 'text-blue-600 border-b-2 border-blue-600' : 'text-gray-500 hover:text-gray-700'}`}>
            <MessageSquare size={18} /> Sohbet
          </button>
          <button onClick={() => setActiveTab('attachments')} className={`flex items-center gap-2 p-4 font-semibold whitespace-nowrap ${activeTab === 'attachments' ? 'text-blue-600 border-b-2 border-blue-600' : 'text-gray-500 hover:text-gray-700'}`}>
            <Paperclip size={18} /> Dosyalar
          </button>
          <button onClick={() => setActiveTab('history')} className={`flex items-center gap-2 p-4 font-semibold whitespace-nowrap ${activeTab === 'history' ? 'text-blue-600 border-b-2 border-blue-600' : 'text-gray-500 hover:text-gray-700'}`}>
            <History size={18} /> Geçmiş
          </button>
        </div>

        {/* Content */}
        <div className="p-6 overflow-y-auto flex-1">
          {activeTab === 'edit' && (
            <form onSubmit={handleEditSubmit} className="space-y-4">
              <div>
                <label className="block text-sm font-medium mb-1">Başlık</label>
                <input type="text" value={editData.title || ''} onChange={e => setEditData({...editData, title: e.target.value})} className="w-full p-2 border rounded dark:bg-gray-700 dark:border-gray-600" required />
              </div>
              <div>
                <label className="block text-sm font-medium mb-1">Açıklama</label>
                <textarea value={editData.description || ''} onChange={e => setEditData({...editData, description: e.target.value})} className="w-full p-2 border rounded dark:bg-gray-700 dark:border-gray-600" rows={4} />
              </div>
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-medium mb-1">Durum</label>
                  <select value={editData.status ?? task.status} onChange={e => setEditData({...editData, status: Number(e.target.value)})} className="w-full p-2 border rounded dark:bg-gray-700 dark:border-gray-600">
                    <option value={1}>Yapılacaklar</option>
                    <option value={2}>Devam Edenler</option>
                    <option value={3}>Tamamlananlar</option>
                  </select>
                </div>
                <div>
                  <label className="block text-sm font-medium mb-1">Kategori</label>
                  <select value={editData.category ?? task.category} onChange={e => setEditData({...editData, category: Number(e.target.value)})} className="w-full p-2 border rounded dark:bg-gray-700 dark:border-gray-600">
                    <option value={1}>Frontend</option>
                    <option value={2}>Backend</option>
                    <option value={3}>Veritabanı</option>
                    <option value={4}>Mobil</option>
                    <option value={5}>DevOps</option>
                    <option value={6}>BugFix</option>
                  </select>
                </div>
                <div>
                  <label className="block text-sm font-medium mb-1">Bitiş Tarihi</label>
                  <input type="date" value={editData.dueDate || ''} onChange={e => setEditData({...editData, dueDate: e.target.value})} className="w-full p-2 border rounded dark:bg-gray-700 dark:border-gray-600" />
                </div>
                <div>
                  <label className="block text-sm font-medium mb-1">Atanan Kişi</label>
                  <select value={editData.assignedUserId ?? task.assignedUserId} onChange={e => setEditData({...editData, assignedUserId: Number(e.target.value)})} className="w-full p-2 border rounded dark:bg-gray-700 dark:border-gray-600">
                    {users.map(u => (
                      <option key={u.id} value={u.id}>{u.firstName} {u.lastName}</option>
                    ))}
                  </select>
                </div>
              </div>
              <button type="submit" className="mt-4 bg-blue-600 text-white px-4 py-2 rounded hover:bg-blue-700">Değişiklikleri Kaydet</button>
            </form>
          )}

          {activeTab === 'comments' && (
            <div className="flex flex-col h-[400px]">
              <div className="flex-1 overflow-y-auto mb-4 space-y-4 pr-2">
                {comments.length === 0 ? (
                  <p className="text-gray-500 text-center py-4">Henüz not eklenmemiş. İlk notu siz ekleyin!</p>
                ) : (
                  comments.map(c => (
                    <div key={c.id} className="bg-blue-50 dark:bg-blue-900/30 p-3 rounded-lg w-max max-w-[85%] border dark:border-blue-800/50">
                      <div className="text-xs font-semibold text-blue-800 dark:text-blue-300 mb-1">
                        {c.userName} - {new Date(c.createdDate).toLocaleString()}
                      </div>
                      <div className="text-sm text-gray-800 dark:text-gray-200">{c.text}</div>
                    </div>
                  ))
                )}
              </div>
              <form onSubmit={handleAddComment} className="flex gap-2">
                <input 
                  type="text" 
                  value={newComment} 
                  onChange={e => setNewComment(e.target.value)} 
                  placeholder="Göreve bir not ekleyin..." 
                  className="flex-1 p-3 border rounded-lg dark:bg-gray-700 dark:border-gray-600 focus:ring-2 focus:ring-blue-500 focus:outline-none"
                />
                <button type="submit" disabled={!newComment.trim()} className="bg-blue-600 text-white p-3 rounded-lg hover:bg-blue-700 disabled:opacity-50">
                  <Send size={20} />
                </button>
              </form>
            </div>
          )}

          {activeTab === 'attachments' && (
            <div className="space-y-6">
              <div className="border-2 border-dashed border-gray-300 dark:border-gray-600 hover:border-blue-500 transition-colors rounded-xl p-8 text-center bg-gray-50 dark:bg-gray-800/50">
                <p className="mb-2 font-medium text-gray-700 dark:text-gray-300">Görsel veya PDF yükleyin</p>
                <p className="mb-4 text-xs text-gray-500">Maksimum dosya boyutu: 10MB</p>
                <input 
                  type="file" 
                  accept="image/*,application/pdf"
                  ref={fileInputRef}
                  onChange={handleFileUpload}
                  className="hidden"
                />
                <button 
                  onClick={() => fileInputRef.current?.click()} 
                  disabled={uploading}
                  className="bg-white border dark:bg-gray-700 dark:border-gray-600 shadow-sm px-6 py-2 rounded-lg hover:bg-gray-50 dark:hover:bg-gray-600 disabled:opacity-50 flex items-center gap-2 mx-auto"
                >
                  <Paperclip size={18} />
                  {uploading ? 'Yükleniyor...' : 'Dosya Seç'}
                </button>
              </div>
              <div className="space-y-3">
                {attachments.map(a => (
                  <div key={a.id} className="flex justify-between items-center p-4 bg-white dark:bg-gray-800 rounded-lg border dark:border-gray-700 shadow-sm">
                    <div className="flex items-center gap-4">
                      <div className="p-3 bg-blue-100 dark:bg-blue-900/40 text-blue-600 rounded-full">
                        <Paperclip size={20} />
                      </div>
                      <div>
                        <p className="text-sm font-medium">{a.fileName}</p>
                        <p className="text-xs text-gray-500 mt-1">{(a.fileSize / 1024 / 1024).toFixed(2)} MB • {a.uploadedByUserName} • {new Date(a.uploadedAt).toLocaleDateString()}</p>
                      </div>
                    </div>
                    <div className="flex gap-2">
                      <a href={`/api/Attachments/${a.id}/download`} target="_blank" rel="noopener noreferrer" className="p-2 text-gray-500 hover:text-blue-600 hover:bg-blue-50 dark:hover:bg-gray-700 rounded-full transition-colors" title="İndir">
                        <Download size={20} />
                      </a>
                      <button 
                        onClick={() => {
                          if (window.confirm("Bu dosyayı silmek istediğinize emin misiniz?")) {
                            useTaskStore.getState().deleteTaskAttachment(a.id)
                              .then(() => loadTabData('attachments'))
                              .catch(() => alert("Dosya silinirken bir hata oluştu."));
                          }
                        }}
                        className="p-2 text-red-500 hover:text-red-700 hover:bg-red-50 dark:hover:bg-red-900/30 rounded-full transition-colors"
                        title="Dosyayı Sil"
                      >
                        <Trash2 size={20} />
                      </button>
                    </div>
                  </div>
                ))}
                {attachments.length === 0 && (
                  <p className="text-center text-gray-500 py-4">Bu göreve ait bir dosya bulunmuyor.</p>
                )}
              </div>
            </div>
          )}

          {activeTab === 'history' && (
            <div className="space-y-4">
              {history.length === 0 ? (
                <p className="text-gray-500 text-center py-4">Geçmiş kaydı bulunamadı.</p>
              ) : (
                <div className="relative border-l-2 border-gray-200 dark:border-gray-700 ml-4 space-y-8 py-4">
                  {history.map((h, i) => (
                    <div key={i} className="pl-6 relative">
                      <div className="absolute w-4 h-4 bg-white border-2 border-blue-500 rounded-full -left-[9px] top-1 dark:bg-gray-800"></div>
                      <p className="text-sm font-medium text-gray-800 dark:text-gray-200">{h.actionMessage}</p>
                      <p className="text-xs text-gray-500 mt-1">{new Date(h.createdDate).toLocaleString()}</p>
                    </div>
                  ))}
                </div>
              )}
            </div>
          )}
        </div>
      </div>
    </div>
  );
}

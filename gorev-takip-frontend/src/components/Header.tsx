'use client';

import { useState } from 'react';
import { usePathname } from 'next/navigation';
import { Bell, Search, CheckCircle2, Menu } from 'lucide-react';
import { useNotifications } from '@/hooks/useNotifications';
import { useSidebarStore } from '@/store/useSidebarStore';

export default function Header() {
  const pathname = usePathname();
  const [showNotifications, setShowNotifications] = useState(false);
  const { notifications, markAsRead, markAllAsRead, unreadCount, isLoading } = useNotifications();
  const { toggle: toggleSidebar } = useSidebarStore();
  
  const getPageTitle = () => {
    if (pathname.startsWith('/dashboard')) return 'Kontrol Paneli';
    if (pathname.startsWith('/tasks')) return 'Görevler';
    if (pathname.startsWith('/users')) return 'Kullanıcı Yönetimi';
    return 'Görev Takip';
  };

  return (
    <header className="h-20 bg-gray-900/80 backdrop-blur-md border-b border-gray-800 flex items-center justify-between px-4 md:px-8 sticky top-0 z-50">
      <div className="flex items-center gap-4">
        <button 
          onClick={toggleSidebar}
          className="md:hidden text-gray-400 hover:text-white transition-colors"
        >
          <Menu size={24} />
        </button>
        <h2 className="text-xl font-bold text-white">{getPageTitle()}</h2>
      </div>

      <div className="flex items-center gap-6 relative">
        <div className="relative hidden md:block">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-500" size={18} />
          <input 
            type="text" 
            placeholder="Görevlerde ara..." 
            className="bg-gray-800 border border-gray-700 text-sm rounded-full pl-10 pr-4 py-2 text-gray-200 focus:outline-none focus:ring-2 focus:ring-purple-500 w-64 transition-all"
          />
        </div>
        
        <div className="relative">
          <button 
            className="relative text-gray-400 hover:text-white transition-colors"
            onClick={() => setShowNotifications(!showNotifications)}
          >
            <Bell size={20} />
            {unreadCount > 0 && (
              <span className="absolute -top-1 -right-1 w-3.5 h-3.5 bg-purple-500 rounded-full border-2 border-gray-900 text-[8px] flex items-center justify-center text-white font-bold">
                {unreadCount > 9 ? '9+' : unreadCount}
              </span>
            )}
          </button>

          {/* Notifications Popover */}
          {showNotifications && (
            <div className="absolute right-0 mt-4 w-80 bg-gray-800 border border-gray-700 rounded-2xl shadow-2xl overflow-hidden animate-in fade-in slide-in-from-top-2">
              <div className="p-4 border-b border-gray-700 flex justify-between items-center bg-gray-800/50">
                <h3 className="font-semibold text-white">Bildirimler</h3>
                {unreadCount > 0 && (
                  <button 
                    onClick={() => markAllAsRead()}
                    className="text-xs text-purple-400 hover:text-purple-300 transition-colors"
                  >
                    Tümünü Okundu İşaretle
                  </button>
                )}
              </div>
              <div className="max-h-80 overflow-y-auto">
                {isLoading ? (
                  <div className="p-6 text-center text-sm text-gray-500">
                    Yükleniyor...
                  </div>
                ) : notifications.length === 0 ? (
                  <div className="p-6 text-center text-sm text-gray-500">
                    Henüz bildiriminiz yok.
                  </div>
                ) : (
                  notifications.map(notif => (
                    <div 
                      key={notif.id} 
                      className={`p-4 border-b border-gray-700/50 hover:bg-gray-700/30 transition-colors cursor-pointer flex gap-3 ${!notif.isRead ? 'bg-purple-900/10' : ''}`}
                      onClick={() => !notif.isRead && markAsRead(notif.id)}
                    >
                      <div className="mt-0.5">
                        <CheckCircle2 size={16} className={notif.isRead ? 'text-gray-600' : 'text-purple-500'} />
                      </div>
                      <div>
                        <p className={`text-sm ${notif.isRead ? 'text-gray-400' : 'text-gray-200 font-medium'}`}>
                          {notif.message}
                        </p>
                        <p className="text-xs text-gray-500 mt-1">
                          {new Date(notif.createdAt).toLocaleTimeString('tr-TR', { hour: '2-digit', minute: '2-digit' })}
                        </p>
                      </div>
                    </div>
                  ))
                )}
              </div>
            </div>
          )}
        </div>
      </div>
    </header>
  );
}

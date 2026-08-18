'use client';

import { useAuthStore } from '@/store/useAuthStore';
import { useNotificationStore } from '@/store/useNotificationStore';
import { useRouter, usePathname } from 'next/navigation';
import Link from 'next/link';
import { LayoutDashboard, CheckSquare, LogOut, Settings, User } from 'lucide-react';

export default function Sidebar() {
  const { user, logout } = useAuthStore();
  const { clearNotifications } = useNotificationStore();
  const router = useRouter();
  const pathname = usePathname();

  const handleLogout = () => {
    logout();
    clearNotifications();
    router.push('/login');
  };

  const navItems = [
    { name: 'Kontrol Paneli', path: '/dashboard', icon: LayoutDashboard },
    { name: 'Görevler', path: '/tasks', icon: CheckSquare },
  ];

  return (
    <aside className="w-64 bg-gray-900 border-r border-gray-800 flex flex-col h-screen fixed left-0 top-0 z-20">
      <div className="p-6">
        <h1 className="text-2xl font-bold bg-clip-text text-transparent bg-gradient-to-r from-purple-400 to-blue-500 tracking-tight">
          Görev Takip
        </h1>
      </div>

      <div className="px-6 py-4 border-b border-gray-800 mb-6">
        <div className="flex items-center gap-3">
          <div className="w-10 h-10 rounded-full bg-gradient-to-br from-purple-500 to-blue-500 flex items-center justify-center text-white font-bold text-lg shadow-lg">
            {user?.firstName?.charAt(0) || 'U'}
          </div>
          <div className="overflow-hidden">
            <p className="text-sm font-medium text-gray-200 truncate">{user?.firstName} {user?.lastName}</p>
            <p className="text-xs text-gray-500 truncate">{user?.role === 1 ? 'Admin' : 'Kullanıcı'}</p>
          </div>
        </div>
      </div>

      <nav className="flex-1 px-4 space-y-1">
        {navItems.map((item) => {
          const isActive = pathname === item.path || pathname.startsWith(`${item.path}/`);
          return (
            <Link
              key={item.path}
              href={item.path}
              className={`flex items-center gap-3 px-4 py-3 rounded-xl transition-all ${
                isActive 
                  ? 'bg-purple-500/10 text-purple-400 font-medium' 
                  : 'text-gray-400 hover:bg-gray-800/50 hover:text-gray-200'
              }`}
            >
              <item.icon size={20} className={isActive ? 'text-purple-400' : 'text-gray-500'} />
              {item.name}
            </Link>
          );
        })}
        {user?.role === 1 && (
          <Link
            href="/users"
            className={`flex items-center gap-3 px-4 py-3 rounded-xl transition-all ${
              pathname === '/users' 
                ? 'bg-purple-500/10 text-purple-400 font-medium' 
                : 'text-gray-400 hover:bg-gray-800/50 hover:text-gray-200'
            }`}
          >
            <User size={20} className={pathname === '/users' ? 'text-purple-400' : 'text-gray-500'} />
            Kullanıcı Yönetimi
          </Link>
        )}
      </nav>

      <div className="p-4 border-t border-gray-800">
        <button
          onClick={handleLogout}
          className="flex items-center gap-3 px-4 py-3 w-full rounded-xl text-gray-400 hover:bg-red-500/10 hover:text-red-400 transition-all text-left"
        >
          <LogOut size={20} />
          <span>Çıkış Yap</span>
        </button>
      </div>
    </aside>
  );
}

'use client';

import { usePathname } from 'next/navigation';
import { Bell, Search } from 'lucide-react';

export default function Header() {
  const pathname = usePathname();
  
  const getPageTitle = () => {
    if (pathname.startsWith('/dashboard')) return 'Kontrol Paneli';
    if (pathname.startsWith('/tasks')) return 'Görevler';
    return 'Görev Takip';
  };

  return (
    <header className="h-20 bg-gray-900/80 backdrop-blur-md border-b border-gray-800 flex items-center justify-between px-8 sticky top-0 z-10">
      <div>
        <h2 className="text-xl font-bold text-white">{getPageTitle()}</h2>
      </div>

      <div className="flex items-center gap-6">
        <div className="relative hidden md:block">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-500" size={18} />
          <input 
            type="text" 
            placeholder="Görevlerde ara..." 
            className="bg-gray-800 border border-gray-700 text-sm rounded-full pl-10 pr-4 py-2 text-gray-200 focus:outline-none focus:ring-2 focus:ring-purple-500 w-64 transition-all"
          />
        </div>
        
        <button className="relative text-gray-400 hover:text-white transition-colors">
          <Bell size={20} />
          <span className="absolute -top-1 -right-1 w-2.5 h-2.5 bg-purple-500 rounded-full border-2 border-gray-900"></span>
        </button>
      </div>
    </header>
  );
}

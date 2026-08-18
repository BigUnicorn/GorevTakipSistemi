'use client';

import { useEffect, useState } from 'react';
import { api } from '@/lib/api';
import { useAuthStore } from '@/store/useAuthStore';
import { CheckCircle2, CircleDashed, Clock, Layout, Code, Database, Bug, Smartphone, TerminalSquare } from 'lucide-react';

interface TaskStats {
  totalTasks: number;
  todoTasks: number;
  inProgressTasks: number;
  completedTasks: number;
  frontendTasks: number;
  backendTasks: number;
  databaseTasks: number;
  bugFixTasks: number;
  mobileTasks: number;
  devOpsTasks: number;
}

export default function DashboardPage() {
  const [stats, setStats] = useState<TaskStats | null>(null);
  const [loading, setLoading] = useState(true);
  const { user } = useAuthStore();

  useEffect(() => {
    const fetchStats = async () => {
      try {
        const url = user?.role === 1 ? '/Tasks/statistics' : `/Tasks/statistics?userId=${user?.id}`;
        const res = await api.get(url);
        setStats(res.data);
      } catch (error) {
        console.error('İstatistikler yüklenemedi', error);
      } finally {
        setLoading(false);
      }
    };
    
    if (user) {
      fetchStats();
    }
  }, [user]);

  if (loading) {
    return <div className="flex items-center justify-center h-full text-gray-400">Yükleniyor...</div>;
  }

  const StatCard = ({ title, value, icon: Icon, colorClass, borderClass }: any) => (
    <div className={`bg-gray-900/60 p-6 rounded-2xl border ${borderClass} backdrop-blur-sm relative overflow-hidden group hover:scale-[1.02] transition-transform`}>
      <div className={`absolute -right-6 -top-6 w-24 h-24 ${colorClass} rounded-full opacity-10 group-hover:opacity-20 transition-opacity blur-2xl`}></div>
      <div className="flex justify-between items-start relative z-10">
        <div>
          <p className="text-gray-400 text-sm font-medium mb-1">{title}</p>
          <h3 className="text-3xl font-bold text-white">{value || 0}</h3>
        </div>
        <div className={`p-3 rounded-xl ${colorClass} bg-opacity-10 text-white`}>
          <Icon size={24} />
        </div>
      </div>
    </div>
  );

  return (
    <div className="space-y-8 max-w-7xl mx-auto">
      <div>
        <h1 className="text-3xl font-bold text-white mb-2">Hoş Geldiniz, {user?.firstName} 👋</h1>
        <p className="text-gray-400">İşte {user?.role === 1 ? 'sistemin' : 'senin'} güncel görev özeti.</p>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
        <StatCard title="Toplam Görev" value={stats?.totalTasks} icon={Layout} colorClass="bg-purple-500" borderClass="border-purple-500/20" />
        <StatCard title="Yapılacaklar" value={stats?.todoTasks} icon={CircleDashed} colorClass="bg-gray-500" borderClass="border-gray-500/20" />
        <StatCard title="Devam Edenler" value={stats?.inProgressTasks} icon={Clock} colorClass="bg-blue-500" borderClass="border-blue-500/20" />
        <StatCard title="Tamamlananlar" value={stats?.completedTasks} icon={CheckCircle2} colorClass="bg-green-500" borderClass="border-green-500/20" />
      </div>

      <h2 className="text-xl font-semibold text-white mt-8 mb-4">Kategorilere Göre Dağılım</h2>
      <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-6 gap-4">
        <StatCard title="Frontend" value={stats?.frontendTasks} icon={Layout} colorClass="bg-pink-500" borderClass="border-gray-800" />
        <StatCard title="Backend" value={stats?.backendTasks} icon={Code} colorClass="bg-indigo-500" borderClass="border-gray-800" />
        <StatCard title="Database" value={stats?.databaseTasks} icon={Database} colorClass="bg-emerald-500" borderClass="border-gray-800" />
        <StatCard title="Bug Fix" value={stats?.bugFixTasks} icon={Bug} colorClass="bg-red-500" borderClass="border-gray-800" />
        <StatCard title="Mobile" value={stats?.mobileTasks} icon={Smartphone} colorClass="bg-cyan-500" borderClass="border-gray-800" />
        <StatCard title="DevOps" value={stats?.devOpsTasks} icon={TerminalSquare} colorClass="bg-orange-500" borderClass="border-gray-800" />
      </div>
    </div>
  );
}

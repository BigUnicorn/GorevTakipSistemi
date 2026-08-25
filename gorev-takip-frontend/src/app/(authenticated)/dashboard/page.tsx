'use client';

import { useEffect, useState } from 'react';
import { api } from '@/lib/api';
import { useAuthStore } from '@/store/useAuthStore';
import { useUserStore } from '@/store/useUserStore';
import { CheckCircle2, CircleDashed, Clock, Layout, Filter } from 'lucide-react';
import { PieChart, Pie, Cell, BarChart, Bar, XAxis, YAxis, Tooltip, Legend, ResponsiveContainer } from 'recharts';

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

const StatCard = ({ title, value, icon: Icon, colorClass, borderClass }: { title: string; value?: number; icon: React.ElementType; colorClass: string; borderClass: string }) => (
  <div className={`bg-gray-900/60 p-6 rounded-2xl border ${borderClass} backdrop-blur-sm relative overflow-hidden group hover:scale-[1.02] transition-transform`}>
    <div className={`absolute -right-6 -top-6 w-24 h-24 ${colorClass} rounded-full opacity-10 group-hover:opacity-20 transition-opacity blur-2xl`}></div>
    <div className="flex justify-between items-start relative z-10">
      <div>
        <p className="text-gray-400 text-sm font-medium mb-1">{title}</p>
        <h3 className="text-3xl font-bold text-white">{value === undefined ? '...' : value}</h3>
      </div>
      <div className={`p-3 rounded-xl ${colorClass} bg-opacity-10 text-white`}>
        <Icon size={24} />
      </div>
    </div>
  </div>
);

export default function DashboardPage() {
  const [stats, setStats] = useState<TaskStats | null>(null);
  const { user } = useAuthStore();
  const { users, fetchUsers } = useUserStore();

  const [selectedUserId, setSelectedUserId] = useState<number | ''>('');
  const [selectedCategoryId, setSelectedCategoryId] = useState<number | ''>('');

  useEffect(() => {
    if (user?.role === 1) {
      fetchUsers();
    }
  }, [user, fetchUsers]);

  useEffect(() => {
    const fetchStats = async () => {
      try {
        let url = '/Tasks/statistics?';
        
        // Admin can filter by user, normal users only see their own tasks
        if (user?.role === 1) {
          if (selectedUserId !== '') url += `userId=${selectedUserId}&`;
        } else {
          url += `userId=${user?.id}&`;
        }

        if (selectedCategoryId !== '') {
          url += `categoryId=${selectedCategoryId}&`;
        }

        const res = await api.get(url);
        setStats(res.data);
      } catch (error) {
        console.error('İstatistikler yüklenemedi', error);
      }
    };
    
    if (user) {
      fetchStats();
    }
  }, [user, selectedUserId, selectedCategoryId]);


  const statusData = stats ? [
    { name: 'Yapılacaklar', value: stats.todoTasks, color: '#9CA3AF' },
    { name: 'Devam Edenler', value: stats.inProgressTasks, color: '#3B82F6' },
    { name: 'Tamamlananlar', value: stats.completedTasks, color: '#10B981' },
  ] : [];

  const categoryData = stats ? [
    { name: 'Frontend', value: stats.frontendTasks },
    { name: 'Backend', value: stats.backendTasks },
    { name: 'Database', value: stats.databaseTasks },
    { name: 'Bug Fix', value: stats.bugFixTasks },
    { name: 'Mobile', value: stats.mobileTasks },
    { name: 'DevOps', value: stats.devOpsTasks },
  ] : [];

  return (
    <div className="space-y-8 max-w-7xl mx-auto h-full overflow-y-auto pb-10">
      <div className="flex flex-col md:flex-row justify-between items-start md:items-center gap-4">
        <div>
          <h1 className="text-3xl font-bold text-white mb-2">Hoş Geldiniz, {user?.firstName} 👋</h1>
          <p className="text-gray-400">İşte {user?.role === 1 ? 'sistemin' : 'senin'} güncel görev özeti ve istatistikleri.</p>
        </div>

        {/* Filters */}
        <div className="flex flex-wrap items-center gap-3 bg-gray-900/50 p-3 rounded-xl border border-gray-800">
          <div className="flex items-center text-gray-400 mr-2">
            <Filter size={18} className="mr-2" />
            <span className="text-sm font-medium">Filtrele:</span>
          </div>
          
          <select 
            value={selectedCategoryId} 
            onChange={e => setSelectedCategoryId(e.target.value ? Number(e.target.value) : '')}
            className="bg-gray-800 border border-gray-700 text-white text-sm rounded-lg p-2 outline-none focus:ring-1 focus:ring-purple-500 min-w-[120px]"
          >
            <option value="">Tüm Kategoriler</option>
            <option value="1">Frontend</option>
            <option value="2">Backend</option>
            <option value="3">Veritabanı</option>
            <option value="4">Bug Fix</option>
            <option value="5">Mobil</option>
            <option value="6">DevOps</option>
          </select>

          {user?.role === 1 && (
            <select 
              value={selectedUserId} 
              onChange={e => setSelectedUserId(e.target.value ? Number(e.target.value) : '')}
              className="bg-gray-800 border border-gray-700 text-white text-sm rounded-lg p-2 outline-none focus:ring-1 focus:ring-purple-500 min-w-[150px]"
            >
              <option value="">Tüm Kullanıcılar</option>
              {users.map(u => (
                <option key={u.id} value={u.id}>{u.firstName} {u.lastName}</option>
              ))}
            </select>
          )}
        </div>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
        <StatCard title="Toplam Görev" value={stats?.totalTasks} icon={Layout} colorClass="bg-purple-500" borderClass="border-purple-500/20" />
        <StatCard title="Yapılacaklar" value={stats?.todoTasks} icon={CircleDashed} colorClass="bg-gray-500" borderClass="border-gray-500/20" />
        <StatCard title="Devam Edenler" value={stats?.inProgressTasks} icon={Clock} colorClass="bg-blue-500" borderClass="border-blue-500/20" />
        <StatCard title="Tamamlananlar" value={stats?.completedTasks} icon={CheckCircle2} colorClass="bg-green-500" borderClass="border-green-500/20" />
      </div>

      {/* Charts Section */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6 mt-8">
        <div className="bg-gray-900/60 p-6 rounded-2xl border border-gray-800 backdrop-blur-sm">
          <h2 className="text-lg font-semibold text-white mb-6">Görev Durumu Dağılımı</h2>
          <div className="h-[300px] w-full">
            <ResponsiveContainer width="100%" height="100%">
              <PieChart>
                <Pie
                  data={statusData}
                  cx="50%"
                  cy="50%"
                  innerRadius={70}
                  outerRadius={100}
                  paddingAngle={5}
                  dataKey="value"
                >
                  {statusData.map((entry, index) => (
                    <Cell key={`cell-${index}`} fill={entry.color} />
                  ))}
                </Pie>
                <Tooltip 
                  contentStyle={{ backgroundColor: '#1F2937', borderColor: '#374151', borderRadius: '0.5rem', color: '#fff' }}
                  itemStyle={{ color: '#fff' }}
                  // eslint-disable-next-line @typescript-eslint/no-explicit-any
                  formatter={(value: any) => [value, 'Görev Sayısı']}
                />
                <Legend verticalAlign="bottom" height={36} />
              </PieChart>
            </ResponsiveContainer>
          </div>
        </div>

        <div className="bg-gray-900/60 p-6 rounded-2xl border border-gray-800 backdrop-blur-sm">
          <h2 className="text-lg font-semibold text-white mb-6">Kategorilere Göre Görevler</h2>
          <div className="h-[300px] w-full">
            <ResponsiveContainer width="100%" height="100%">
              <BarChart data={categoryData} margin={{ top: 20, right: 30, left: 0, bottom: 5 }}>
                <XAxis dataKey="name" stroke="#9CA3AF" fontSize={12} tickLine={false} axisLine={false} />
                <YAxis stroke="#9CA3AF" fontSize={12} tickLine={false} axisLine={false} allowDecimals={false} />
                <Tooltip 
                  cursor={{ fill: '#374151', opacity: 0.4 }}
                  contentStyle={{ backgroundColor: '#1F2937', borderColor: '#374151', borderRadius: '0.5rem', color: '#fff' }}
                  // eslint-disable-next-line @typescript-eslint/no-explicit-any
                  formatter={(value: any) => [value, 'Görev Sayısı']}
                />
                <Bar dataKey="value" name="Görev Sayısı" fill="#8B5CF6" radius={[4, 4, 0, 0]} maxBarSize={50} />
              </BarChart>
            </ResponsiveContainer>
          </div>
        </div>
      </div>
    </div>
  );
}

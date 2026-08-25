'use client';

import { useEffect, useState } from 'react';
import { useUserStore } from '@/store/useUserStore';
import { useAuthStore } from '@/store/useAuthStore';
import { ShieldAlert, User as UserIcon, Mail } from 'lucide-react';
import { useRouter } from 'next/navigation';

export default function UsersPage() {
  const { users, isLoading, fetchUsers, updateUserRole } = useUserStore();
  const { user: currentUser } = useAuthStore();
  const router = useRouter();
  
  const [editingRole, setEditingRole] = useState<number | null>(null);

  useEffect(() => {
    // Sadece Admin (1) görebilir
    if (currentUser && currentUser.role !== 1) {
      router.push('/dashboard');
    } else if (currentUser?.role === 1) {
      fetchUsers();
    }
  }, [currentUser, fetchUsers, router]);

  const handleRoleChange = async (userId: number, newRole: number) => {
    await updateUserRole(userId, newRole);
    setEditingRole(null);
  };

  if (isLoading || !currentUser) {
    return <div className="flex items-center justify-center h-full text-gray-400">Yükleniyor...</div>;
  }

  return (
    <div className="max-w-6xl mx-auto py-8">
      <div className="flex justify-between items-center mb-8">
        <div>
          <h1 className="text-3xl font-bold text-white mb-2">Kullanıcı Yönetimi</h1>
          <p className="text-gray-400">Sistemdeki personelleri görüntüleyin ve yetkilerini yönetin.</p>
        </div>
      </div>

      <div className="bg-gray-900/50 border border-gray-800 rounded-2xl overflow-x-auto shadow-xl backdrop-blur-sm">
        <table className="w-full text-left border-collapse min-w-[600px]">
          <thead>
            <tr className="bg-gray-800/80 border-b border-gray-700 text-gray-300 text-sm uppercase tracking-wider">
              <th className="p-5 font-semibold">Personel</th>
              <th className="p-5 font-semibold hidden md:table-cell">E-posta</th>
              <th className="p-5 font-semibold">Yetki / Rol</th>
              <th className="p-5 font-semibold text-right">İşlem</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-gray-800">
            {users.map((u) => (
              <tr key={u.id} className="hover:bg-gray-800/30 transition-colors">
                <td className="p-5">
                  <div className="flex items-center gap-4">
                    <div className="w-10 h-10 rounded-full bg-gradient-to-tr from-purple-600 to-blue-600 flex items-center justify-center text-white font-bold shadow-lg">
                      {u.firstName[0]}{u.lastName[0]}
                    </div>
                    <div>
                      <div className="font-medium text-white">{u.firstName} {u.lastName}</div>
                      <div className="text-xs text-gray-500 md:hidden">{u.email}</div>
                    </div>
                  </div>
                </td>
                <td className="p-5 hidden md:table-cell text-gray-400">
                  <div className="flex items-center gap-2">
                    <Mail size={14} className="text-gray-500" />
                    {u.email}
                  </div>
                </td>
                <td className="p-5">
                  {editingRole === u.id ? (
                    <select
                      className="bg-gray-800 border border-gray-600 text-white rounded-lg px-3 py-1.5 focus:ring-2 focus:ring-purple-500 outline-none text-sm"
                      defaultValue={u.role}
                      onChange={(e) => handleRoleChange(u.id, Number(e.target.value))}
                      onBlur={() => setEditingRole(null)}
                      autoFocus
                    >
                      <option value={1}>Admin (Yönetici)</option>
                      <option value={2}>Personel (Çalışan)</option>
                    </select>
                  ) : (
                    <div className="flex items-center gap-2">
                      {u.role === 1 ? (
                        <span className="inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-medium bg-purple-500/20 text-purple-400 border border-purple-500/30">
                          <ShieldAlert size={12} />
                          Admin
                        </span>
                      ) : (
                        <span className="inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-medium bg-blue-500/20 text-blue-400 border border-blue-500/30">
                          <UserIcon size={12} />
                          Personel
                        </span>
                      )}
                    </div>
                  )}
                </td>
                <td className="p-5 text-right">
                  {u.id !== currentUser.id && (
                    <button
                      onClick={() => setEditingRole(u.id)}
                      className="text-sm font-medium text-purple-400 hover:text-purple-300 hover:underline transition-all"
                    >
                      Rolü Değiştir
                    </button>
                  )}
                  {u.id === currentUser.id && (
                    <span className="text-sm text-gray-500 italic">Siz</span>
                  )}
                </td>
              </tr>
            ))}
            
            {users.length === 0 && !isLoading && (
              <tr>
                <td colSpan={4} className="p-8 text-center text-gray-400">
                  Hiç kullanıcı bulunamadı.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
}

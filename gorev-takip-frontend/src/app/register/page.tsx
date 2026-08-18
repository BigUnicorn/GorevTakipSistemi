'use client';

import { useState } from 'react';
import { api } from '@/lib/api';
import { useRouter } from 'next/navigation';
import { AlertCircle, CheckCircle2, Loader2 } from 'lucide-react';
import Link from 'next/link';

export default function RegisterPage() {
  const [formData, setFormData] = useState({
    firstName: '',
    lastName: '',
    email: '',
    password: ''
  });
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const router = useRouter();

  const handleRegister = async (e: React.FormEvent) => {
    e.preventDefault();
    setIsLoading(true);
    setError('');
    setSuccess('');

    try {
      await api.post('/Auth/register', formData);
      setSuccess('Kayıt başarılı! Giriş sayfasına yönlendiriliyorsunuz...');
      setTimeout(() => {
        router.push('/login');
      }, 2000);
    } catch (err: any) {
      setError(err.response?.data || 'Kayıt işlemi başarısız oldu.');
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="min-h-screen flex text-gray-100 font-sans relative overflow-hidden bg-gray-950">
      {/* Background Mesh */}
      <div className="absolute inset-0 pointer-events-none z-0">
        <div className="absolute w-[800px] h-[800px] bg-purple-600/20 rounded-full blur-[120px] -top-64 -right-64 mix-blend-screen animate-pulse"></div>
        <div className="absolute w-[600px] h-[600px] bg-blue-500/20 rounded-full blur-[100px] bottom-0 left-0 mix-blend-screen animate-pulse" style={{ animationDelay: '2s' }}></div>
      </div>

      <div className="w-full flex items-center justify-center p-8 z-10 relative">
        <div className="w-full max-w-md bg-gray-900/50 p-10 rounded-3xl shadow-2xl backdrop-blur-xl border border-gray-800/50">
          <div className="text-center mb-8">
            <h2 className="text-4xl font-bold bg-clip-text text-transparent bg-gradient-to-r from-purple-400 to-blue-500 mb-2">Kayıt Ol</h2>
            <p className="text-gray-400">Yeni bir hesap oluşturun</p>
          </div>

          {error && (
            <div className="bg-red-500/10 border border-red-500/50 text-red-400 p-4 rounded-xl flex items-center gap-3 mb-6">
              <AlertCircle size={20} className="shrink-0" />
              <span className="text-sm font-medium">{error}</span>
            </div>
          )}

          {success && (
            <div className="bg-green-500/10 border border-green-500/50 text-green-400 p-4 rounded-xl flex items-center gap-3 mb-6">
              <CheckCircle2 size={20} className="shrink-0" />
              <span className="text-sm font-medium">{success}</span>
            </div>
          )}

          <form onSubmit={handleRegister} className="space-y-5">
            <div className="grid grid-cols-2 gap-4">
              <div>
                <label className="block text-sm font-medium text-gray-300 mb-2">Ad</label>
                <input
                  type="text"
                  required
                  className="w-full bg-gray-800/50 border border-gray-700 text-white rounded-xl px-4 py-3 focus:outline-none focus:ring-2 focus:ring-purple-500 focus:border-transparent transition-all placeholder-gray-500"
                  placeholder="Ahmet"
                  value={formData.firstName}
                  onChange={(e) => setFormData({...formData, firstName: e.target.value})}
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-300 mb-2">Soyad</label>
                <input
                  type="text"
                  required
                  className="w-full bg-gray-800/50 border border-gray-700 text-white rounded-xl px-4 py-3 focus:outline-none focus:ring-2 focus:ring-purple-500 focus:border-transparent transition-all placeholder-gray-500"
                  placeholder="Yılmaz"
                  value={formData.lastName}
                  onChange={(e) => setFormData({...formData, lastName: e.target.value})}
                />
              </div>
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-300 mb-2">E-posta</label>
              <input
                type="email"
                required
                className="w-full bg-gray-800/50 border border-gray-700 text-white rounded-xl px-5 py-3 focus:outline-none focus:ring-2 focus:ring-purple-500 focus:border-transparent transition-all placeholder-gray-500"
                placeholder="ornek@sirket.com"
                value={formData.email}
                onChange={(e) => setFormData({...formData, email: e.target.value})}
              />
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-300 mb-2">Şifre</label>
              <input
                type="password"
                required
                className="w-full bg-gray-800/50 border border-gray-700 text-white rounded-xl px-5 py-3 focus:outline-none focus:ring-2 focus:ring-purple-500 focus:border-transparent transition-all placeholder-gray-500"
                placeholder="••••••••"
                value={formData.password}
                onChange={(e) => setFormData({...formData, password: e.target.value})}
              />
            </div>

            <button
              type="submit"
              disabled={isLoading || !!success}
              className="w-full bg-gradient-to-r from-purple-600 to-blue-600 hover:from-purple-500 hover:to-blue-500 text-white font-bold py-3.5 px-4 rounded-xl transition-all shadow-lg shadow-purple-500/25 flex justify-center items-center gap-2 disabled:opacity-70 mt-4"
            >
              {isLoading ? <Loader2 className="animate-spin" size={20} /> : 'Hesap Oluştur'}
            </button>
          </form>

          <p className="mt-8 text-center text-gray-400 text-sm">
            Zaten hesabınız var mı?{' '}
            <Link href="/login" className="text-purple-400 hover:text-purple-300 font-medium transition-colors">
              Giriş Yapın
            </Link>
          </p>
        </div>
      </div>
    </div>
  );
}

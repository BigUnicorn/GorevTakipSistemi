'use client';

import { useState } from 'react';
import { useAuthStore } from '@/store/useAuthStore';
import { api } from '@/lib/api';
import { useRouter } from 'next/navigation';
import { AlertCircle, Loader2 } from 'lucide-react';
import Link from 'next/link';

export default function LoginPage() {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const login = useAuthStore(state => state.login);
  const router = useRouter();

  const handleLogin = async (e: React.FormEvent) => {
    e.preventDefault();
    setIsLoading(true);
    setError('');

    try {
      const res = await api.post('/Auth/login', { email, password });
      
      const { accessToken, refreshToken, userId, firstName, lastName, email, role } = res.data;
      
      const user = { id: userId, firstName, lastName, email, role };
      
      // Update state and localStorage
      login(accessToken, refreshToken, user);
      
      // Redirect to dashboard
      router.push('/dashboard');
    } catch (err: any) {
      setError(err.response?.data || 'Giriş başarısız. Lütfen bilgilerinizi kontrol edin.');
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="min-h-screen flex text-gray-100 font-sans relative overflow-hidden bg-gray-950">
      {/* Background Mesh (Dark Mesh theme) */}
      <div className="absolute inset-0 pointer-events-none z-0">
        <div className="absolute w-[800px] h-[800px] bg-purple-600/20 rounded-full blur-[120px] -top-64 -left-64 mix-blend-screen animate-pulse"></div>
        <div className="absolute w-[600px] h-[600px] bg-blue-500/20 rounded-full blur-[100px] bottom-0 right-0 mix-blend-screen animate-pulse" style={{ animationDelay: '2s' }}></div>
      </div>

      <div className="w-full lg:w-1/2 flex items-center justify-center p-8 z-10 relative">
        <div className="w-full max-w-md bg-gray-900/50 p-10 rounded-3xl shadow-2xl backdrop-blur-xl border border-gray-800/50">
          <div className="text-center mb-8">
            <h2 className="text-4xl font-bold bg-clip-text text-transparent bg-gradient-to-r from-purple-400 to-blue-500 mb-2">Görev Takip</h2>
            <p className="text-gray-400">Hesabınıza giriş yapın</p>
          </div>

          {error && (
            <div className="bg-red-500/10 border border-red-500/50 text-red-400 p-4 rounded-xl flex items-center gap-3 mb-6">
              <AlertCircle size={20} />
              <span className="text-sm font-medium">{error}</span>
            </div>
          )}

          <form onSubmit={handleLogin} className="space-y-6">
            <div>
              <label className="block text-sm font-medium text-gray-300 mb-2">E-posta</label>
              <input
                type="email"
                required
                className="w-full bg-gray-800/50 border border-gray-700 text-white rounded-xl px-5 py-3 focus:outline-none focus:ring-2 focus:ring-purple-500 focus:border-transparent transition-all placeholder-gray-500"
                placeholder="ornek@sirket.com"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
              />
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-300 mb-2">Şifre</label>
              <input
                type="password"
                required
                className="w-full bg-gray-800/50 border border-gray-700 text-white rounded-xl px-5 py-3 focus:outline-none focus:ring-2 focus:ring-purple-500 focus:border-transparent transition-all placeholder-gray-500"
                placeholder="••••••••"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
              />
            </div>

            <button
              type="submit"
              disabled={isLoading}
              className="w-full bg-gradient-to-r from-purple-600 to-blue-600 hover:from-purple-500 hover:to-blue-500 text-white font-bold py-3.5 px-4 rounded-xl transition-all shadow-lg shadow-purple-500/25 flex justify-center items-center gap-2 disabled:opacity-70"
            >
              {isLoading ? <Loader2 className="animate-spin" size={20} /> : 'Giriş Yap'}
            </button>
          </form>

          <p className="mt-8 text-center text-gray-400 text-sm">
            Hesabınız yok mu?{' '}
            <Link href="/register" className="text-purple-400 hover:text-purple-300 font-medium transition-colors">
              Hemen Kaydolun
            </Link>
          </p>
        </div>
      </div>

      {/* Right side content (Optional features showcase) */}
      <div className="hidden lg:flex w-1/2 bg-gray-900/50 flex-col items-center justify-center p-12 border-l border-gray-800/50 z-10 backdrop-blur-sm">
        <div className="max-w-lg text-center">
          <h3 className="text-3xl font-bold text-white mb-6">Neden Bizi Seçmelisiniz?</h3>
          <div className="space-y-6 text-left">
            <div className="bg-gray-800/50 p-6 rounded-2xl border border-gray-700/50">
              <h4 className="font-semibold text-purple-400 mb-2">⚡ Anlık Senkronizasyon</h4>
              <p className="text-gray-400 text-sm">SignalR altyapısı sayesinde ekibinizin yaptığı tüm değişiklikler sayfa yenilemeden saniyeler içinde ekranınıza yansır.</p>
            </div>
            <div className="bg-gray-800/50 p-6 rounded-2xl border border-gray-700/50">
              <h4 className="font-semibold text-blue-400 mb-2">🚀 Modern Mimari</h4>
              <p className="text-gray-400 text-sm">Backend tarafında S.O.L.I.D prensipleriyle CQRS kullanırken, frontend tarafında Next.js ve Zustand'ın gücünden faydalanıyoruz.</p>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}

import Link from 'next/link';
import { SearchX, Home } from 'lucide-react';

export default function NotFound() {
  return (
    <div className="min-h-[70vh] flex flex-col items-center justify-center p-8 text-center animate-in fade-in zoom-in duration-300">
      <div className="w-24 h-24 bg-blue-500/10 rounded-full flex items-center justify-center mb-6 shadow-lg shadow-blue-500/20">
        <SearchX className="w-12 h-12 text-blue-500" />
      </div>
      <h1 className="text-6xl font-bold text-transparent bg-clip-text bg-gradient-to-r from-blue-400 to-purple-500 mb-4">404</h1>
      <h2 className="text-2xl font-semibold text-white mb-4">Sayfa Bulunamadı</h2>
      <p className="text-gray-400 mb-8 max-w-md">
        Aradığınız sayfa silinmiş, adı değiştirilmiş veya geçici olarak kullanılamıyor olabilir.
      </p>
      
      <Link 
        href="/dashboard"
        className="flex items-center gap-2 bg-gradient-to-r from-purple-600 to-blue-600 hover:from-purple-500 hover:to-blue-500 text-white px-8 py-3.5 rounded-xl font-medium transition-all shadow-lg shadow-purple-500/25"
      >
        <Home className="w-5 h-5" />
        Ana Sayfaya Dön
      </Link>
    </div>
  );
}

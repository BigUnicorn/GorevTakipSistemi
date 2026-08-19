'use client';

import { useEffect } from 'react';
import { AlertTriangle, RefreshCcw } from 'lucide-react';
import Link from 'next/link';

export default function Error({
  error,
  reset,
}: {
  error: Error & { digest?: string };
  reset: () => void;
}) {
  useEffect(() => {
    console.error('Uygulama Hatası (Error Boundary):', error);
  }, [error]);

  return (
    <div className="min-h-[70vh] flex flex-col items-center justify-center p-8 text-center animate-in fade-in zoom-in duration-300">
      <div className="w-24 h-24 bg-red-500/10 rounded-full flex items-center justify-center mb-6 shadow-lg shadow-red-500/20">
        <AlertTriangle className="w-12 h-12 text-red-500" />
      </div>
      <h1 className="text-4xl font-bold text-white mb-4">Bir Şeyler Ters Gitti!</h1>
      <p className="text-gray-400 mb-8 max-w-md">
        Beklenmedik bir hata oluştu. Lütfen sayfayı yenilemeyi deneyin veya daha sonra tekrar dönün.
      </p>
      
      <div className="flex gap-4">
        <button
          onClick={() => reset()}
          className="flex items-center gap-2 bg-gradient-to-r from-purple-600 to-blue-600 hover:from-purple-500 hover:to-blue-500 text-white px-6 py-3 rounded-xl font-medium transition-all shadow-lg shadow-purple-500/25"
        >
          <RefreshCcw className="w-5 h-5" />
          Tekrar Dene
        </button>
        <Link 
          href="/dashboard"
          className="flex items-center gap-2 bg-gray-800 hover:bg-gray-700 text-white px-6 py-3 rounded-xl font-medium transition-all"
        >
          Ana Sayfaya Dön
        </Link>
      </div>
    </div>
  );
}

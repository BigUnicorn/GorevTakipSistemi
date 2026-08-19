'use client';

import { AlertOctagon, RefreshCcw } from 'lucide-react';
import { useEffect } from 'react';

export default function GlobalError({
  error,
  reset,
}: {
  error: Error & { digest?: string };
  reset: () => void;
}) {
  useEffect(() => {
    console.error('Kritik Sistem Hatası (Global Error Boundary):', error);
  }, [error]);

  return (
    <html lang="tr">
      <body className="bg-gray-950 text-gray-100 font-sans antialiased">
        <div className="min-h-screen flex text-gray-100 font-sans relative overflow-hidden bg-gray-950">
          {/* Background Mesh */}
          <div className="absolute inset-0 pointer-events-none z-0">
            <div className="absolute w-[800px] h-[800px] bg-red-600/20 rounded-full blur-[120px] -top-64 -left-64 mix-blend-screen animate-pulse"></div>
            <div className="absolute w-[600px] h-[600px] bg-orange-500/10 rounded-full blur-[100px] bottom-0 right-0 mix-blend-screen animate-pulse" style={{ animationDelay: '2s' }}></div>
          </div>

          <div className="w-full flex items-center justify-center p-8 z-10 relative">
            <div className="w-full max-w-lg bg-gray-900/50 p-10 rounded-3xl shadow-2xl backdrop-blur-xl border border-red-500/20 text-center flex flex-col items-center">
              <div className="w-24 h-24 bg-red-500/20 rounded-full flex items-center justify-center mb-6 shadow-lg shadow-red-500/30">
                <AlertOctagon className="w-12 h-12 text-red-500" />
              </div>
              <h1 className="text-4xl font-bold text-white mb-4">Kritik Sistem Hatası</h1>
              <p className="text-gray-400 mb-8">
                Uygulama çekirdeğinde kritik bir hata meydana geldi. Bu durum genellikle geçicidir.
              </p>
              
              <button
                onClick={() => reset()}
                className="flex items-center justify-center gap-2 w-full bg-gradient-to-r from-red-600 to-orange-600 hover:from-red-500 hover:to-orange-500 text-white px-6 py-4 rounded-xl font-bold transition-all shadow-lg shadow-red-500/25"
              >
                <RefreshCcw className="w-5 h-5" />
                Sistemi Yeniden Başlat
              </button>
            </div>
          </div>
        </div>
      </body>
    </html>
  );
}

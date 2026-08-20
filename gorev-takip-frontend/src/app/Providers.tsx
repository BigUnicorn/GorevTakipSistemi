'use client';

import { useEffect, useState } from 'react';
import { useAuthStore } from '@/store/useAuthStore';
import { useSignalR } from '@/hooks/useSignalR';
import { useRouter, usePathname } from 'next/navigation';


export default function Providers({ children }: { children: React.ReactNode }) {
  const { checkAuth, isAuthenticated } = useAuthStore();
  const [isMounted, setIsMounted] = useState(false);
  const router = useRouter();
  const pathname = usePathname();

  // Initialize auth
  useEffect(() => {
    const initAuth = async () => {
      await checkAuth();
      setIsMounted(true);
    };
    initAuth();
  }, [checkAuth]);

  // Route guarding
  useEffect(() => {
    if (isMounted) {
      if (!isAuthenticated && pathname !== '/login' && pathname !== '/register') {
        router.push('/login');
      } else if (isAuthenticated && (pathname === '/login' || pathname === '/' || pathname === '/register')) {
        router.push('/dashboard');
      }
    }
  }, [isMounted, isAuthenticated, pathname, router]);

  // Initialize SignalR if authenticated
  useSignalR();

  if (!isMounted) return null; // Prevent hydration mismatch

  return <>{children}</>;
}

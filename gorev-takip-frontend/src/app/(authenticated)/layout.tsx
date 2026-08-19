import Sidebar from '@/components/Sidebar';
import Header from '@/components/Header';

export default function AuthenticatedLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <div className="flex h-screen bg-gray-950 overflow-hidden">
      <Sidebar />
      <div className="flex-1 md:ml-64 flex flex-col h-full relative w-full">
        {/* Background Mesh for Authenticated areas */}
        <div className="absolute inset-0 pointer-events-none z-0 overflow-hidden">
          <div className="absolute w-[500px] h-[500px] bg-purple-600/10 rounded-full blur-[120px] -top-32 -right-32 mix-blend-screen"></div>
          <div className="absolute w-[400px] h-[400px] bg-blue-500/10 rounded-full blur-[100px] bottom-0 left-32 mix-blend-screen"></div>
        </div>
        
        <Header />
        <main className="flex-1 overflow-auto p-8 z-10">
          {children}
        </main>
      </div>
    </div>
  );
}

import { Loader2 } from "lucide-react";

export default function Home() {
  return (
    <div className="flex items-center justify-center min-h-screen bg-gray-950">
      <Loader2 className="animate-spin text-purple-500" size={40} />
    </div>
  );
}

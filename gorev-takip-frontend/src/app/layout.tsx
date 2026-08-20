import type { Metadata } from "next";
import { Inter } from "next/font/google";
import "./globals.css";
import Providers from "./Providers";
import ReactQueryProvider from "./ReactQueryProvider";

const inter = Inter({ subsets: ["latin"] });

export const metadata: Metadata = {
  title: "Görev Takip Sistemi",
  description: "Modern Görev Yönetim Platformu",
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="tr">
      <body
        className={`${inter.className} bg-gray-900 text-white min-h-screen`}
      >
        <ReactQueryProvider>
          <Providers>
            {children}
          </Providers>
        </ReactQueryProvider>
      </body>
    </html>
  );
}

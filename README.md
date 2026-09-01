<div align="center">
  <img src="docs/images/banner.jpg" alt="Görev Takip Sistemi Banner" width="100%">

  <br />
  <br />

  <h1>GorevTakipSistemi (TaskFlow)</h1>

  <p>
    <strong>Modern, Gerçek Zamanlı ve Full-Stack Görev Yönetim Sistemi</strong> <br/>
    <em>Modern, Real-Time and Full-Stack Task Management System</em>
  </p>

  <p>
    <a href="#türkçe">🇹🇷 Türkçe</a> • <a href="#english">🇬🇧 English</a>
  </p>

  <p>
    <img src="https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet" alt=".NET 8" />
    <img src="https://img.shields.io/badge/Next.js-black?style=for-the-badge&logo=next.js&logoColor=white" alt="Next.js" />
    <img src="https://img.shields.io/badge/PostgreSQL-316192?style=for-the-badge&logo=postgresql&logoColor=white" alt="PostgreSQL" />
    <img src="https://img.shields.io/badge/Redis-DC382D?style=for-the-badge&logo=redis&logoColor=white" alt="Redis" />
    <img src="https://img.shields.io/badge/Docker-2496ED?style=for-the-badge&logo=docker&logoColor=white" alt="Docker" />
  </p>
</div>

---

<h2 id="türkçe">🇹🇷 Türkçe Dokümantasyon</h2>

Görev Takip Sistemi (GorevTakipSistemi), ekiplerin iş süreçlerini Kanban yaklaşımıyla kolayca yönetebildiği, gelişmiş mimari desenlerle (CQRS, Outbox Pattern) geliştirilmiş, OpenTelemetry destekli ve tam anlamıyla **production-ready** bir web uygulamasıdır.

### 🚀 Temel Özellikler

- **Gerçek Zamanlı İletişim:** SignalR entegrasyonu sayesinde görev oluşturma, durum güncelleme ve bildirimler sayfa yenilenmeden tüm kullanıcılara yansır.
- **Kanban Pano:** Görevlerinizi sürükle-bırak (Drag & Drop) yöntemiyle kolayca farklı durumlara (To Do, In Progress, Done) taşıyın.
- **Modern Arayüz & React Query:** Next.js, Tailwind CSS ve React Query kullanılarak geliştirilmiş *Optimistic UI* tabanlı, süper hızlı ve pürüzsüz bir kullanıcı deneyimi.
- **Sağlam Backend Mimarisi:** .NET Core üzerinde CQRS (MediatR), Repository Pattern ve Outbox Pattern kullanılarak veri tutarlılığı sağlanmıştır.
- **Gözlemlenebilirlik (Observability):** Uygulama OpenTelemetry (Tracing & Metrics) ile donatılmış olup; Grafana, Prometheus ve Jaeger ile anlık izlenebilmektedir.
- **Güvenli Kimlik Doğrulama:** HttpOnly, Secure ve SameSite=Strict konfigürasyonlarına sahip Cookie tabanlı JWT kimlik doğrulama mekanizması.
- **Test ve CI/CD Altyapısı:** Birim (Unit) ve Entegrasyon (Integration - Testcontainers) testleriyle test edilmiş, GitHub Actions ile CI pipeline'ı kurulmuş, %100 sıfır hata/uyarı kod yapısı.

### 🛠️ Teknolojiler ve Mimari

**Frontend:**
- [Next.js](https://nextjs.org/) (App Router, React 18)
- [Tailwind CSS](https://tailwindcss.com/) & Glassmorphism UI
- [TanStack Query (React Query)](https://tanstack.com/query/v5) (Asenkron durum yönetimi)
- [Zustand](https://zustand-demo.pmnd.rs) (Yerel durum yönetimi)

**Backend:**
- .NET 8 / C#
- Entity Framework Core & PostgreSQL
- Redis (Önbellekleme / Distributed Cache)
- SignalR (WebSocket / Real-Time)
- CQRS (MediatR) & FluentValidation
- Outbox Pattern (Sinyal/Mail güvenilirliği)

**DevOps & Gözlemlenebilirlik:**
- Docker & Docker Compose
- Nginx (Reverse Proxy)
- OpenTelemetry, Jaeger (Tracing)
- Prometheus, Grafana (Metrics)
- GitHub Actions (CI/CD)

### 📦 Hızlı Kurulum

Projeyi kendi bilgisayarınızda çalıştırmak için **Docker** ve **Docker Compose** kurulu olması yeterlidir.

1. Depoyu klonlayın:
   ```bash
   git clone https://github.com/KULLANICI_ADINIZ/GorevTakipSistemi.git
   cd GorevTakipSistemi
   ```

2. Docker Compose ile tüm altyapıyı ayağa kaldırın:
   ```bash
   docker-compose up -d --build
   ```

3. Uygulamalara erişin:
   - **Frontend:** http://localhost:5074
   - **Backend API (Swagger):** http://localhost:5074/swagger/
   - **Grafana (Gözlem):** http://localhost:5074/grafana/
   - **Jaeger (İzleme):** http://localhost:5074/jaeger/

---

<h2 id="english">🇬🇧 English Documentation</h2>

Task Tracking System (GorevTakipSistemi) is a **production-ready** web application where teams can easily manage their workflows with a Kanban approach. It is built with advanced architectural patterns (CQRS, Outbox Pattern) and features OpenTelemetry support.

### 🚀 Key Features

- **Real-Time Communication:** With SignalR integration, task creations, status updates, and notifications are reflected to all users without page reloads.
- **Kanban Board:** Easily move your tasks to different statuses (To Do, In Progress, Done) using drag-and-drop.
- **Modern UI & React Query:** Developed using Next.js, Tailwind CSS, and React Query for an *Optimistic UI*-based, super-fast, and smooth user experience.
- **Robust Backend Architecture:** Ensures data consistency using CQRS (MediatR), Repository Pattern, and Outbox Pattern on .NET Core.
- **Observability:** The application is equipped with OpenTelemetry (Tracing & Metrics) and can be monitored instantly with Grafana, Prometheus, and Jaeger.
- **Secure Authentication:** Cookie-based JWT authentication mechanism with HttpOnly, Secure, and SameSite=Strict configurations.
- **Test and CI/CD Infrastructure:** Tested with Unit and Integration (Testcontainers) tests, CI pipeline set up with GitHub Actions, and a 100% zero error/warning codebase.

### 🛠️ Technologies and Architecture

**Frontend:**
- [Next.js](https://nextjs.org/) (App Router, React 18)
- [Tailwind CSS](https://tailwindcss.com/) & Glassmorphism UI
- [TanStack Query (React Query)](https://tanstack.com/query/v5) (Asynchronous state management)
- [Zustand](https://zustand-demo.pmnd.rs) (Local state management)

**Backend:**
- .NET 8 / C#
- Entity Framework Core & PostgreSQL
- Redis (Caching / Distributed Cache)
- SignalR (WebSocket / Real-Time)
- CQRS (MediatR) & FluentValidation
- Outbox Pattern (Signal/Email reliability)

**DevOps & Observability:**
- Docker & Docker Compose
- Nginx (Reverse Proxy)
- OpenTelemetry, Jaeger (Tracing)
- Prometheus, Grafana (Metrics)
- GitHub Actions (CI/CD)

### 📦 Quick Start

To run the project on your local machine, having **Docker** and **Docker Compose** installed is sufficient.

1. Clone the repository:
   ```bash
   git clone https://github.com/YOUR_USERNAME/GorevTakipSistemi.git
   cd GorevTakipSistemi
   ```

2. Spin up the entire infrastructure with Docker Compose:
   ```bash
   docker-compose up -d --build
   ```

3. Access the applications:
   - **Frontend:** http://localhost:5074
   - **Backend API (Swagger):** http://localhost:5074/swagger/
   - **Grafana (Metrics):** http://localhost:5074/grafana/
   - **Jaeger (Tracing):** http://localhost:5074/jaeger/

---
*Developed with modern best practices. / Modern yazılım prensipleriyle geliştirilmiştir.*

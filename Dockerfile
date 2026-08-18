# 1. Aşama: Frontend Build (Node.js)
FROM node:20-alpine AS frontend-build
WORKDIR /frontend
COPY ["gorev-takip-frontend/package.json", "gorev-takip-frontend/package-lock.json*", "./"]
RUN npm install
COPY ["gorev-takip-frontend/", "./"]
# Statik HTML olarak export edecek (next.config.ts'deki output: 'export' sayesinde)
RUN npm run build

# 2. Aşama: Backend Build (Derleme) ortamı
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Sadece proje (.csproj) dosyalarını kopyalayıp bağımlılıkları indiriyoruz (Cache optimizasyonu)
COPY ["GorevTakip.API/GorevTakip.API.csproj", "GorevTakip.API/"]
COPY ["GorevTakip.Business/GorevTakip.Business.csproj", "GorevTakip.Business/"]
COPY ["GorevTakip.DataAccess/GorevTakip.DataAccess.csproj", "GorevTakip.DataAccess/"]
COPY ["GorevTakip.Entities/GorevTakip.Entities.csproj", "GorevTakip.Entities/"]

RUN dotnet restore "GorevTakip.API/GorevTakip.API.csproj"

# Kalan tüm kaynak kodları kopyala
COPY . .
WORKDIR "/src/GorevTakip.API"
RUN dotnet build "GorevTakip.API.csproj" -c Release -o /app/build

# 3. Aşama: Publish (Yayınlama)
FROM build AS publish
RUN dotnet publish "GorevTakip.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# 4. Aşama: Çalışma Zamanı (Runtime) ortamı
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Frontend çıktılarını wwwroot içine kopyala
COPY --from=frontend-build /frontend/out ./wwwroot

# Port ayarı (API 8080 portundan çalışacak)
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "GorevTakip.API.dll"]
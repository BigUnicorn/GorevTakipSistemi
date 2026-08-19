# 1. Aşama: Build (Derleme) ortamı
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

# 2. Aşama: Publish (Yayınlama)
FROM build AS publish
RUN dotnet publish "GorevTakip.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# 3. Aşama: Çalışma Zamanı (Runtime) ortamı
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=publish /app/publish .

# Port ayarı (API 8080 portundan çalışacak)
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "GorevTakip.API.dll"]
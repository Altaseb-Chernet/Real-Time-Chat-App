# ── Stage 1: build ──────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy ALL project files first for layer-cached restore
COPY ["src/ChatApplication.API/ChatApplication.API.csproj",                       "src/ChatApplication.API/"]
COPY ["src/ChatApplication.Core/ChatApplication.Core.csproj",                     "src/ChatApplication.Core/"]
COPY ["src/ChatApplication.Infrastructure/ChatApplication.Infrastructure.csproj", "src/ChatApplication.Infrastructure/"]
COPY ["src/ChatApplication.Shared/ChatApplication.Shared.csproj",                 "src/ChatApplication.Shared/"]
COPY ["src/ChatApplication.Client/ChatApplication.Client.csproj",                 "src/ChatApplication.Client/"]

RUN dotnet restore "src/ChatApplication.API/ChatApplication.API.csproj"

# Copy all source (preserving the full src/ tree)
COPY src/ src/

# Publish — this also builds the Blazor WASM client and bundles it into wwwroot
RUN dotnet publish "src/ChatApplication.API/ChatApplication.API.csproj" \
    -c Release \
    -o /app/publish \
    --no-restore

# ── Stage 2: runtime ─────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Create uploads directory so local-fallback media works
RUN mkdir -p /app/wwwroot/uploads

COPY --from=build /app/publish .

EXPOSE 5000
ENV ASPNETCORE_URLS=http://+:5000

ENTRYPOINT ["dotnet", "ChatApplication.API.dll"]

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/ChatApplication.API/ChatApplication.API.csproj", "ChatApplication.API/"]
COPY ["src/ChatApplication.Core/ChatApplication.Core.csproj", "ChatApplication.Core/"]
COPY ["src/ChatApplication.Infrastructure/ChatApplication.Infrastructure.csproj", "ChatApplication.Infrastructure/"]
COPY ["src/ChatApplication.Shared/ChatApplication.Shared.csproj", "ChatApplication.Shared/"]
RUN dotnet restore "ChatApplication.API/ChatApplication.API.csproj"
COPY src/ .
RUN dotnet build "ChatApplication.API/ChatApplication.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ChatApplication.API/ChatApplication.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ChatApplication.API.dll"]

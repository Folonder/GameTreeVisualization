FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5002

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["GameTreeVisualization.sln", "./"]
COPY ["GameTreeVisualization.Core/GameTreeVisualization.Core.csproj", "GameTreeVisualization.Core/"]
COPY ["GameTreeVisualization.Infrastructure/GameTreeVisualization.Infrastructure.csproj", "GameTreeVisualization.Infrastructure/"]
COPY ["GameTreeVisualization.Web/GameTreeVisualization.Web.csproj", "GameTreeVisualization.Web/"]

RUN dotnet restore

COPY . .

WORKDIR "/src/GameTreeVisualization.Web"
RUN dotnet build "GameTreeVisualization.Web.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "GameTreeVisualization.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "GameTreeVisualization.Web.dll"]
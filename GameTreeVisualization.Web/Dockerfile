FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["GameTreeVisualization.Web/GameTreeVisualization.Web.csproj", "GameTreeVisualization.Web/"]
RUN dotnet restore "GameTreeVisualization.Web/GameTreeVisualization.Web.csproj"
COPY . .
WORKDIR "/src/GameTreeVisualization.Web"
RUN dotnet build "GameTreeVisualization.Web.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "GameTreeVisualization.Web.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "GameTreeVisualization.Web.dll"]

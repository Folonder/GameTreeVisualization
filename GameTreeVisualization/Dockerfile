FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["GameTreeVisualization/GameTreeVisualization.csproj", "GameTreeVisualization/"]
RUN dotnet restore "GameTreeVisualization/GameTreeVisualization.csproj"
COPY . .
WORKDIR "/src/GameTreeVisualization"
RUN dotnet build "GameTreeVisualization.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "GameTreeVisualization.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "GameTreeVisualization.dll"]

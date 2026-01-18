FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["AutomatedMarketIntelligenceTool.sln", "./"]
COPY ["src/Microservices/ReportingService/ReportingService.csproj", "src/Microservices/ReportingService/"]
COPY ["src/Microservices/Protos/Protos.csproj", "src/Microservices/Protos/"]
COPY ["src/AutomatedMarketIntelligenceTool.Core/AutomatedMarketIntelligenceTool.Core.csproj", "src/AutomatedMarketIntelligenceTool.Core/"]
COPY ["src/AutomatedMarketIntelligenceTool.Infrastructure/AutomatedMarketIntelligenceTool.Infrastructure.csproj", "src/AutomatedMarketIntelligenceTool.Infrastructure/"]

RUN dotnet restore "src/Microservices/ReportingService/ReportingService.csproj"

# Copy everything else
COPY . .

WORKDIR "/src/src/Microservices/ReportingService"
RUN dotnet build "ReportingService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ReportingService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Install curl for health checks and fonts for PDF generation
RUN apt-get update && apt-get install -y curl fonts-liberation && rm -rf /var/lib/apt/lists/*

ENTRYPOINT ["dotnet", "ReportingService.dll"]

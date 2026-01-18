FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Install Playwright dependencies
RUN apt-get update && apt-get install -y \
    libnss3 \
    libnspr4 \
    libatk1.0-0 \
    libatk-bridge2.0-0 \
    libcups2 \
    libdrm2 \
    libxkbcommon0 \
    libxcomposite1 \
    libxdamage1 \
    libxfixes3 \
    libxrandr2 \
    libgbm1 \
    libasound2 \
    curl \
    && rm -rf /var/lib/apt/lists/*

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["AutomatedMarketIntelligenceTool.sln", "./"]
COPY ["src/Microservices/ScrapingService/ScrapingService.csproj", "src/Microservices/ScrapingService/"]
COPY ["src/Microservices/Protos/Protos.csproj", "src/Microservices/Protos/"]
COPY ["src/AutomatedMarketIntelligenceTool.Core/AutomatedMarketIntelligenceTool.Core.csproj", "src/AutomatedMarketIntelligenceTool.Core/"]
COPY ["src/AutomatedMarketIntelligenceTool.Infrastructure/AutomatedMarketIntelligenceTool.Infrastructure.csproj", "src/AutomatedMarketIntelligenceTool.Infrastructure/"]

RUN dotnet restore "src/Microservices/ScrapingService/ScrapingService.csproj"

# Copy everything else
COPY . .

WORKDIR "/src/src/Microservices/ScrapingService"
RUN dotnet build "ScrapingService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ScrapingService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Install Playwright browsers
RUN dotnet tool install --global Microsoft.Playwright.CLI || true
ENV PATH="${PATH}:/root/.dotnet/tools"

ENTRYPOINT ["dotnet", "ScrapingService.dll"]

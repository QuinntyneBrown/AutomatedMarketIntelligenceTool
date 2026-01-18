FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["AutomatedMarketIntelligenceTool.sln", "./"]
COPY ["src/Microservices/ApiGateway/ApiGateway.csproj", "src/Microservices/ApiGateway/"]
COPY ["src/AutomatedMarketIntelligenceTool.Core/AutomatedMarketIntelligenceTool.Core.csproj", "src/AutomatedMarketIntelligenceTool.Core/"]
COPY ["src/AutomatedMarketIntelligenceTool.Infrastructure/AutomatedMarketIntelligenceTool.Infrastructure.csproj", "src/AutomatedMarketIntelligenceTool.Infrastructure/"]

RUN dotnet restore "src/Microservices/ApiGateway/ApiGateway.csproj"

# Copy everything else
COPY . .

WORKDIR "/src/src/Microservices/ApiGateway"
RUN dotnet build "ApiGateway.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ApiGateway.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

ENTRYPOINT ["dotnet", "ApiGateway.dll"]

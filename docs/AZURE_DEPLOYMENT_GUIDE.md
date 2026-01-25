# Azure Deployment Guide: .NET Aspire Microservices with Angular Frontend

This comprehensive guide covers deploying a .NET Aspire microservices application with an Angular frontend to Azure Container Apps using Azure Developer CLI (azd).

## Table of Contents

1. [Architecture Overview](#architecture-overview)
2. [Prerequisites](#prerequisites)
3. [Project Structure](#project-structure)
4. [Quick Start (TL;DR)](#quick-start-tldr)
5. [Detailed Setup](#detailed-setup)
6. [Infrastructure Configuration](#infrastructure-configuration)
7. [Angular Frontend Configuration](#angular-frontend-configuration)
8. [Common Issues and Solutions](#common-issues-and-solutions)
9. [Post-Deployment Verification](#post-deployment-verification)
10. [Troubleshooting Commands](#troubleshooting-commands)

---

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           Azure Container Apps                               │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│   ┌──────────────┐                      ┌──────────────┐                    │
│   │   Angular    │◄────────────────────►│  API Gateway │                    │
│   │     UI       │       HTTPS          │    (YARP)    │                    │
│   │  (external)  │                      │  (external)  │                    │
│   └──────────────┘                      └──────┬───────┘                    │
│                                                │                             │
│                     ┌──────────────────────────┼────────────────────────┐   │
│                     │                          │                        │   │
│              ┌──────▼──────┐  ┌───────────────▼┐  ┌──────────────────┐ │   │
│              │  Identity   │  │     User       │  │     Client       │ │   │
│              │    API      │  │     API        │  │      API         │ │   │
│              │ (internal)  │  │  (internal)    │  │   (internal)     │ │   │
│              └──────┬──────┘  └───────┬────────┘  └────────┬─────────┘ │   │
│                     │                 │                    │           │   │
│              ┌──────▼──────┐  ┌───────▼────────┐  ┌────────▼─────────┐ │   │
│              │  TimeEntry  │  │    Invoice     │  │   Reporting      │ │   │
│              │    API      │  │     API        │  │      API         │ │   │
│              │ (internal)  │  │  (internal)    │  │   (internal)     │ │   │
│              └─────────────┘  └────────────────┘  └──────────────────┘ │   │
│                                                                        │   │
└────────────────────────────────────────────────────────────────────────┼───┘
                                                                         │
                    ┌────────────────────────────────────────────────────┘
                    │
         ┌──────────▼──────────┐     ┌─────────────────┐
         │   Azure SQL Server  │     │   Azure Redis   │
         │   (6 databases)     │     │     Cache       │
         └─────────────────────┘     └─────────────────┘
```

### Components

| Component | Type | Exposure | Port |
|-----------|------|----------|------|
| Angular UI | Container App | External (public) | 80 |
| API Gateway | Container App | External (public) | 8080 |
| Identity API | Container App | Internal | 8080 |
| User API | Container App | Internal | 8080 |
| Client API | Container App | Internal | 8080 |
| TimeEntry API | Container App | Internal | 8080 |
| Invoice API | Container App | Internal | 8080 |
| Reporting API | Container App | Internal | 8080 |
| SQL Server | Azure SQL | Internal | 1433 |
| Redis | Azure Cache | Internal | 6379 |

---

## Prerequisites

### Required Tools

```bash
# Azure CLI (2.50+)
winget install Microsoft.AzureCLI
# or
brew install azure-cli

# Azure Developer CLI (1.5+)
winget install Microsoft.Azd
# or
brew install azd

# .NET SDK (8.0+ or 9.0+)
winget install Microsoft.DotNet.SDK.8
# or
brew install dotnet

# Node.js (20 LTS)
winget install OpenJS.NodeJS.LTS
# or
brew install node@20

# Docker Desktop
winget install Docker.DockerDesktop
# or
brew install --cask docker
```

### Azure Account Setup

```bash
# Login to Azure CLI
az login

# Login to Azure Developer CLI
azd auth login

# Verify subscription
az account show
```

---

## Project Structure

```
project-root/
├── azure.yaml                    # azd configuration
├── infra/                        # Infrastructure as Code
│   ├── main.bicep               # Main deployment template
│   ├── main.parameters.json     # Parameters file
│   └── core/
│       ├── host/
│       │   ├── container-apps.bicep    # Container Apps environment
│       │   └── container-app.bicep     # Individual app module
│       ├── database/
│       │   └── sql-server.bicep        # Azure SQL Server
│       └── cache/
│           └── redis.bicep             # Azure Redis Cache
├── src/
│   ├── Books.AppHost/           # .NET Aspire orchestrator
│   │   ├── Program.cs
│   │   └── Books.AppHost.csproj
│   ├── ApiGateway/              # YARP API Gateway
│   ├── Identity/                # Identity microservice
│   ├── UserService/             # User microservice
│   ├── ClientService/           # Client microservice
│   ├── TimeEntryService/        # Time entry microservice
│   ├── InvoiceService/          # Invoice microservice
│   ├── ReportingService/        # Reporting microservice
│   └── Ui/                      # Angular frontend
│       ├── Dockerfile
│       ├── nginx.conf
│       └── projects/books/      # Angular app
```

---

## Quick Start (TL;DR)

For experienced users who want to deploy quickly:

```bash
# 1. Clone and navigate to project
cd your-project

# 2. Initialize azd (first time only)
azd init

# 3. Set environment name and location
azd env new books-dev
azd env set AZURE_LOCATION westus2

# 4. Deploy everything
azd up

# 5. If UI shows placeholder, rebuild and update manually
az acr build --registry <your-acr-name> --image books/ui:latest --platform linux/amd64 --file src/Ui/Dockerfile src/Ui
az containerapp update --name ca-ui --resource-group rg-books-dev --image <your-acr>.azurecr.io/books/ui:latest --revision-suffix v2
```

---

## Detailed Setup

### Step 1: Configure azure.yaml

Create `azure.yaml` in your project root:

```yaml
name: books
metadata:
  template: aspire-starter
infra:
  provider: bicep
  path: infra
services:
  identity-api:
    language: dotnet
    project: ./src/Identity/Identity.Api/Identity.Api.csproj
    host: containerapp
  user-api:
    language: dotnet
    project: ./src/UserService/UserService.Api/UserService.Api.csproj
    host: containerapp
  client-api:
    language: dotnet
    project: ./src/ClientService/ClientService.Api/ClientService.Api.csproj
    host: containerapp
  timeentry-api:
    language: dotnet
    project: ./src/TimeEntryService/TimeEntryService.Api/TimeEntryService.Api.csproj
    host: containerapp
  invoice-api:
    language: dotnet
    project: ./src/InvoiceService/InvoiceService.Api/InvoiceService.Api.csproj
    host: containerapp
  reporting-api:
    language: dotnet
    project: ./src/ReportingService/ReportingService.Api/ReportingService.Api.csproj
    host: containerapp
  api-gateway:
    language: dotnet
    project: ./src/ApiGateway/ApiGateway.csproj
    host: containerapp
  ui:
    language: docker
    project: ./src/Ui
    host: containerapp
    docker:
      path: Dockerfile
```

### Step 2: Configure .NET Aspire AppHost

Update `src/Books.AppHost/Books.AppHost.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <!-- Core Aspire packages -->
    <PackageReference Include="Aspire.Hosting.AppHost" Version="9.3.0" />

    <!-- Azure deployment packages (REQUIRED for azd) -->
    <PackageReference Include="Aspire.Hosting.Azure" Version="9.3.0" />
    <PackageReference Include="Aspire.Hosting.Azure.AppContainers" Version="9.3.0" />
    <PackageReference Include="Aspire.Hosting.Azure.Sql" Version="9.3.0" />
    <PackageReference Include="Aspire.Hosting.Azure.Redis" Version="9.3.0" />

    <!-- Resource-specific packages -->
    <PackageReference Include="Aspire.Hosting.Redis" Version="9.3.0" />
    <PackageReference Include="Aspire.Hosting.SqlServer" Version="9.3.0" />
    <PackageReference Include="Aspire.Hosting.NodeJs" Version="9.3.0" />
  </ItemGroup>

  <ItemGroup>
    <!-- Project references to all microservices -->
    <ProjectReference Include="..\Identity\Identity.Api\Identity.Api.csproj" />
    <ProjectReference Include="..\UserService\UserService.Api\UserService.Api.csproj" />
    <!-- ... other services -->
  </ItemGroup>
</Project>
```

Update `src/Books.AppHost/Program.cs`:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// IMPORTANT: Add Azure Container Apps infrastructure for deployment
builder.AddAzureContainerAppEnvironment("cae");

// SQL Server - uses Azure SQL Edge locally (ARM64 compatible), Azure SQL in production
var sqlServer = builder.AddAzureSqlServer("sqlserver")
    .RunAsContainer(c => c
        .WithImage("azure-sql-edge")
        .WithImageTag("latest")
        .WithLifetime(ContainerLifetime.Persistent));

// Create databases
var identityDb = sqlServer.AddDatabase("identitydb", "IdentityDb");
var userDb = sqlServer.AddDatabase("userdb", "UserDb");
var clientDb = sqlServer.AddDatabase("clientdb", "ClientDb");
var timeEntryDb = sqlServer.AddDatabase("timeentrydb", "TimeEntryDb");
var invoiceDb = sqlServer.AddDatabase("invoicedb", "InvoiceDb");
var reportingDb = sqlServer.AddDatabase("reportingdb", "ReportingDb");

// Redis for pub/sub messaging
var redis = builder.AddAzureRedis("redis")
    .RunAsContainer(c => c.WithLifetime(ContainerLifetime.Persistent));

// Microservices
var identityService = builder.AddProject("identity-api", "../Identity/Identity.Api/Identity.Api.csproj")
    .WithReference(identityDb)
    .WithReference(redis)
    .WaitFor(identityDb);

var userService = builder.AddProject("user-api", "../UserService/UserService.Api/UserService.Api.csproj")
    .WithReference(userDb)
    .WithReference(redis)
    .WaitFor(userDb);

// ... other services ...

// API Gateway (YARP) - MUST be external for public access
var apiGateway = builder.AddProject("api-gateway", "../ApiGateway/ApiGateway.csproj")
    .WithReference(identityService)
    .WithReference(userService)
    // ... other references ...
    .WithExternalHttpEndpoints();  // Makes it publicly accessible

// Angular UI
var ui = builder.AddNpmApp("ui", "../Ui")
    .WithReference(apiGateway)
    .WithExternalHttpEndpoints()
    .PublishAsDockerFile();

// IMPORTANT: Different ports for local vs Azure
if (builder.ExecutionContext.IsRunMode)
{
    // Local development - Angular dev server on port 4200
    ui.WithHttpEndpoint(port: 4200, targetPort: 4200, isProxied: false);
}
else
{
    // Azure deployment - nginx on port 80
    ui.WithHttpEndpoint(targetPort: 80);
}

builder.Build().Run();
```

---

## Infrastructure Configuration

### Main Bicep Template (infra/main.bicep)

```bicep
targetScope = 'subscription'

@minLength(1)
@maxLength(64)
@description('Name of the environment')
param environmentName string

@minLength(1)
@description('Primary location for all resources')
param location string

@description('Id of the user or app to assign application roles')
param principalId string = ''

var tags = {
  'azd-env-name': environmentName
}

var abbrs = {
  containerApp: 'ca-'
}

// Resource Group
resource rg 'Microsoft.Resources/resourceGroups@2021-04-01' = {
  name: 'rg-${environmentName}'
  location: location
  tags: tags
}

// Container Apps environment and ACR
module containerApps 'core/host/container-apps.bicep' = {
  name: 'container-apps'
  scope: rg
  params: {
    name: 'cae'
    location: location
    tags: tags
    containerAppsEnvironmentName: 'cae-${environmentName}'
    containerRegistryName: 'cr${replace(environmentName, '-', '')}'
    logAnalyticsWorkspaceName: 'log-${environmentName}'
  }
}

// Azure SQL Server with multiple databases
module sqlServer 'core/database/sql-server.bicep' = {
  name: 'sql-server'
  scope: rg
  params: {
    name: 'sql-${environmentName}'
    location: location
    tags: tags
    administratorLogin: 'sqladmin'
    databases: [
      { name: 'IdentityDb' }
      { name: 'UserDb' }
      { name: 'ClientDb' }
      { name: 'TimeEntryDb' }
      { name: 'InvoiceDb' }
      { name: 'ReportingDb' }
    ]
  }
}

// Azure Redis Cache
module redis 'core/cache/redis.bicep' = {
  name: 'redis'
  scope: rg
  params: {
    name: 'redis-${environmentName}'
    location: location
    tags: tags
  }
}

// Internal microservices (external: false)
module identityApi 'core/host/container-app.bicep' = {
  name: 'identity-api'
  scope: rg
  params: {
    name: '${abbrs.containerApp}identity-api'
    location: location
    tags: tags
    containerAppsEnvironmentId: containerApps.outputs.containerAppsEnvironmentId
    containerRegistryName: containerApps.outputs.registryName
    serviceName: 'identity-api'
    external: false      // Internal only
    targetPort: 8080     // .NET default port
  }
}

// ... other internal services ...

// API Gateway (external: true)
module apiGateway 'core/host/container-app.bicep' = {
  name: 'api-gateway'
  scope: rg
  params: {
    name: '${abbrs.containerApp}api-gateway'
    location: location
    tags: tags
    containerAppsEnvironmentId: containerApps.outputs.containerAppsEnvironmentId
    containerRegistryName: containerApps.outputs.registryName
    serviceName: 'api-gateway'
    external: true       // Public access
    targetPort: 8080
  }
}

// Angular UI (external: true, port 80)
module ui 'core/host/container-app.bicep' = {
  name: 'ui'
  scope: rg
  params: {
    name: '${abbrs.containerApp}ui'
    location: location
    tags: tags
    containerAppsEnvironmentId: containerApps.outputs.containerAppsEnvironmentId
    containerRegistryName: containerApps.outputs.registryName
    serviceName: 'ui'
    external: true       // Public access
    targetPort: 80       // nginx serves on port 80
  }
}

// Outputs for azd
output AZURE_LOCATION string = location
output AZURE_TENANT_ID string = tenant().tenantId
output AZURE_CONTAINER_REGISTRY_ENDPOINT string = containerApps.outputs.registryLoginServer
output AZURE_CONTAINER_REGISTRY_NAME string = containerApps.outputs.registryName
output AZURE_CONTAINER_APPS_ENVIRONMENT_NAME string = containerApps.outputs.containerAppsEnvironmentName
output AZURE_CONTAINER_APPS_ENVIRONMENT_ID string = containerApps.outputs.containerAppsEnvironmentId
output AZURE_RESOURCE_GROUP string = rg.name
output UI_URL string = ui.outputs.uri
output API_GATEWAY_URL string = apiGateway.outputs.uri
```

### Container App Module (infra/core/host/container-app.bicep)

```bicep
param name string
param location string = resourceGroup().location
param tags object = {}

param containerAppsEnvironmentId string
param containerRegistryName string
param serviceName string
param external bool = false
param targetPort int = 8080

@description('Environment variables for the container')
param env array = []

resource containerRegistry 'Microsoft.ContainerRegistry/registries@2023-01-01-preview' existing = {
  name: containerRegistryName
}

resource containerApp 'Microsoft.App/containerApps@2023-05-01' = {
  name: name
  location: location
  tags: union(tags, { 'azd-service-name': serviceName })
  properties: {
    managedEnvironmentId: containerAppsEnvironmentId
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        external: external
        targetPort: targetPort
        transport: 'auto'
        allowInsecure: false
      }
      registries: [
        {
          server: containerRegistry.properties.loginServer
          username: containerRegistry.listCredentials().username
          passwordSecretRef: 'registry-password'
        }
      ]
      secrets: [
        {
          name: 'registry-password'
          value: containerRegistry.listCredentials().passwords[0].value
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'main'
          // Placeholder image - azd replaces with actual image
          image: 'mcr.microsoft.com/azuredocs/containerapps-helloworld:latest'
          env: env
          resources: {
            cpu: json('0.5')
            memory: '1Gi'
          }
        }
      ]
      scale: {
        minReplicas: 0
        maxReplicas: 3
      }
    }
  }
}

output name string = containerApp.name
output fqdn string = containerApp.properties.configuration.ingress.fqdn
output uri string = 'https://${containerApp.properties.configuration.ingress.fqdn}'
```

### Parameters File (infra/main.parameters.json)

```json
{
  "$schema": "https://schema.management.azure.com/schemas/2019-04-01/deploymentParameters.json#",
  "contentVersion": "1.0.0.0",
  "parameters": {
    "environmentName": {
      "value": "${AZURE_ENV_NAME}"
    },
    "location": {
      "value": "${AZURE_LOCATION}"
    },
    "principalId": {
      "value": "${AZURE_PRINCIPAL_ID}"
    }
  }
}
```

---

## Angular Frontend Configuration

### Dockerfile (src/Ui/Dockerfile)

```dockerfile
# Stage 1: Build the Angular application
FROM node:20-alpine AS build

WORKDIR /app

# Copy package files
COPY package*.json ./

# Install dependencies (use npm install, not npm ci for better compatibility)
RUN npm install

# Copy source code
COPY . .

# Build for production
# IMPORTANT: Use node to run ng directly to avoid permission issues
# IMPORTANT: Specify the project name if using multi-project workspace
RUN node ./node_modules/@angular/cli/bin/ng.js build books --configuration production

# Stage 2: Serve with nginx
FROM nginx:alpine

# Copy nginx configuration
COPY nginx.conf /etc/nginx/nginx.conf

# Copy built application from build stage
# IMPORTANT: Path must match your angular.json outputPath + /browser
COPY --from=build /app/dist/books/browser /usr/share/nginx/html

# Expose port 80
EXPOSE 80

# Start nginx
CMD ["nginx", "-g", "daemon off;"]
```

### Nginx Configuration (src/Ui/nginx.conf)

```nginx
worker_processes auto;

events {
    worker_connections 1024;
}

http {
    include /etc/nginx/mime.types;
    default_type application/octet-stream;

    sendfile on;
    keepalive_timeout 65;

    # Gzip compression for performance
    gzip on;
    gzip_vary on;
    gzip_min_length 1024;
    gzip_proxied any;
    gzip_types text/plain text/css text/xml text/javascript application/javascript application/json application/xml;

    server {
        listen 80;
        server_name localhost;
        root /usr/share/nginx/html;
        index index.html;

        # SPA routing - serve index.html for all routes
        location / {
            try_files $uri $uri/ /index.html;
        }

        # Cache static assets
        location ~* \.(js|css|png|jpg|jpeg|gif|ico|svg|woff|woff2|ttf|eot)$ {
            expires 1y;
            add_header Cache-Control "public, immutable";
        }

        # Health check endpoint for Azure
        location /health {
            access_log off;
            return 200 "healthy\n";
            add_header Content-Type text/plain;
        }
    }
}
```

**IMPORTANT:** Do NOT include an API proxy in nginx.conf for Azure deployment. The nginx server cannot resolve internal Container Apps hostnames at startup, causing the container to crash.

### Dynamic API URL Configuration (src/app/app.config.ts)

```typescript
import { ApplicationConfig } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withFetch, withInterceptors } from '@angular/common/http';

import { routes } from './app.routes';
import { API_BASE_URL } from './shared';
import { authInterceptor } from './core';

function getApiBaseUrl(): string {
  const hostname = window.location.hostname;

  // Azure Container Apps deployment
  if (hostname.includes('azurecontainerapps.io')) {
    // IMPORTANT: Update this URL after deployment with your actual API Gateway URL
    return 'https://ca-api-gateway.<your-environment>.westus2.azurecontainerapps.io';
  }

  // Local development
  return 'http://localhost:5080';
}

export const appConfig: ApplicationConfig = {
  providers: [
    provideRouter(routes),
    provideHttpClient(
      withFetch(),
      withInterceptors([authInterceptor])
    ),
    {
      provide: API_BASE_URL,
      useFactory: getApiBaseUrl
    }
  ]
};
```

### Angular.json Configuration

Ensure your `angular.json` has proper output path and budget settings:

```json
{
  "projects": {
    "books": {
      "architect": {
        "build": {
          "options": {
            "outputPath": "dist/books"
          },
          "configurations": {
            "production": {
              "budgets": [
                {
                  "type": "initial",
                  "maximumWarning": "1MB",
                  "maximumError": "2MB"
                },
                {
                  "type": "anyComponentStyle",
                  "maximumWarning": "16kB",
                  "maximumError": "32kB"
                }
              ]
            }
          }
        }
      }
    }
  }
}
```

---

## Common Issues and Solutions

### Issue 1: SQL Server Provisioning Fails in Certain Regions

**Error:**
```
The subscription has the following Quotas limiting provisioning of new SQL Server instances:
RegionCountLimitForLogicalServer for the region: "eastus".
```

**Solution:**
Use a different region. West US 2 typically works well:

```bash
azd env set AZURE_LOCATION westus2
```

**Recommended Regions:**
- `westus2` - Good availability
- `centralus` - Good availability
- `northeurope` - For EU deployments

### Issue 2: Deprecated Aspire APIs

**Error:**
```
error CS1061: 'IResourceBuilder<SqlServerServerResource>' does not contain a definition for 'PublishAsAzureSqlDatabase'
```

**Solution:**
Update from deprecated syntax to new Aspire 9.x APIs:

```csharp
// OLD (deprecated)
var sqlServer = builder.AddSqlServer("sqlserver")
    .PublishAsAzureSqlDatabase();

// NEW (Aspire 9.x)
var sqlServer = builder.AddAzureSqlServer("sqlserver")
    .RunAsContainer(c => c.WithImage("azure-sql-edge").WithImageTag("latest"));
```

### Issue 3: Angular Build Fails with TypeScript Errors

**Common Errors:**
- Duplicate interface exports
- Missing properties on interfaces
- Wrong parameter names

**Solution:**
1. Check for duplicate exports in `index.ts` files
2. Ensure interface properties match API responses
3. Run `ng build` locally first to catch errors before deployment

### Issue 4: Docker Build - npm ci Fails

**Error:**
```
npm ERR! `npm ci` can only install packages when your package-lock.json and package.json are in sync
```

**Solution:**
Use `npm install` instead of `npm ci` in Dockerfile:

```dockerfile
# Instead of: RUN npm ci
RUN npm install
```

### Issue 5: Angular CLI Permission Denied in Docker

**Error:**
```
/bin/sh: ./node_modules/.bin/ng: Permission denied
```

**Solution:**
Call ng.js directly through Node:

```dockerfile
# Instead of: RUN npx ng build
RUN node ./node_modules/@angular/cli/bin/ng.js build books --configuration production
```

### Issue 6: CSS Budget Exceeded

**Error:**
```
Error: bundle initial exceeded maximum budget. Budget 500.00 kB was not met by 234.56 kB
```

**Solution:**
Increase budgets in `angular.json`:

```json
"budgets": [
  {
    "type": "initial",
    "maximumWarning": "1MB",
    "maximumError": "2MB"
  },
  {
    "type": "anyComponentStyle",
    "maximumWarning": "16kB",
    "maximumError": "32kB"
  }
]
```

### Issue 7: Wrong Angular Output Path in Dockerfile

**Error:**
```
COPY failed: file not found in build context: /app/dist/ui/browser
```

**Solution:**
Check your `angular.json` for the actual output path and project name:

```dockerfile
# Match the path to your angular.json outputPath + /browser
COPY --from=build /app/dist/books/browser /usr/share/nginx/html
```

### Issue 8: Nginx Fails to Start - Upstream Host Not Found

**Error:**
```
[emerg] host not found in upstream "api-gateway" in /etc/nginx/nginx.conf:34
```

**Cause:**
Nginx cannot resolve Container Apps internal DNS at startup time.

**Solution:**
Remove API proxy from nginx.conf and configure Angular to call the API Gateway directly:

```nginx
# DO NOT include this in nginx.conf for Azure:
# location /api/ {
#     proxy_pass http://api-gateway/;
# }
```

Instead, use the dynamic API URL in Angular's app.config.ts.

### Issue 9: Azure CLI Encoding Error

**Error:**
```
UnicodeEncodeError: 'charmap' codec can't encode character '\u276f'
```

**Cause:**
Azure CLI on Windows has encoding issues with certain Unicode characters in log output.

**Solution:**
This is a client-side display issue. The build usually succeeds. Check ACR for the image:

```bash
az acr repository show-tags --name <acr-name> --repository books/ui
```

### Issue 10: Container App Shows Placeholder Page

**Cause:**
The Container App was created with a placeholder image and hasn't been updated with the actual image.

**Solution:**
Manually update the Container App with the correct image:

```bash
# Check if image exists
az acr repository show-tags --name <acr-name> --repository books/ui

# Update container app
az containerapp update \
  --name ca-ui \
  --resource-group rg-<env-name> \
  --image <acr-name>.azurecr.io/books/ui:latest \
  --revision-suffix v2
```

### Issue 11: ARM64 vs AMD64 Architecture Mismatch

**Error:**
```
exec format error
```

**Cause:**
Building on ARM64 Mac (M1/M2) but Azure needs AMD64 images.

**Solution:**
ACR builds run on AMD64 by default. If building locally, use:

```bash
docker buildx build --platform linux/amd64 -t <image> .
```

### Issue 12: CORS Errors in Browser

**Error:**
```
Access to XMLHttpRequest blocked by CORS policy
```

**Solution:**
Ensure your API Gateway has CORS configured:

```csharp
// In API Gateway Program.cs
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// In middleware
app.UseCors();
```

---

## Post-Deployment Verification

### 1. Verify All Container Apps Are Running

```bash
# List all container apps
az containerapp list --resource-group rg-<env-name> --query '[].{name:name,status:properties.runningStatus}' --output table

# Expected output:
# Name              Status
# ----------------  --------
# ca-identity-api   Running
# ca-user-api       Running
# ca-client-api     Running
# ca-timeentry-api  Running
# ca-invoice-api    Running
# ca-reporting-api  Running
# ca-api-gateway    Running
# ca-ui             Running
```

### 2. Check Container App Revisions

```bash
az containerapp revision list --name ca-ui --resource-group rg-<env-name> --query '[].{name:name,active:properties.active,status:properties.runningState}' --output table
```

### 3. Verify API Gateway Health

```bash
curl https://ca-api-gateway.<env>.westus2.azurecontainerapps.io/health
# Expected: "Healthy"
```

### 4. Verify UI Is Accessible

```bash
curl -I https://ca-ui.<env>.westus2.azurecontainerapps.io
# Expected: HTTP/2 200
```

### 5. Check Container Logs

```bash
# Check UI logs
az containerapp logs show --name ca-ui --resource-group rg-<env-name> --tail 50

# Check API Gateway logs
az containerapp logs show --name ca-api-gateway --resource-group rg-<env-name> --tail 50
```

---

## Troubleshooting Commands

### Azure CLI Quick Reference

```bash
# Register required providers
az provider register -n Microsoft.App --wait
az provider register -n Microsoft.ContainerRegistry --wait

# List ACR repositories
az acr repository list --name <acr-name> --output table

# Show ACR image tags
az acr repository show-tags --name <acr-name> --repository books/ui

# Get container app details
az containerapp show --name ca-ui --resource-group rg-<env-name>

# View container logs (real-time)
az containerapp logs show --name ca-ui --resource-group rg-<env-name> --follow

# Restart a container app revision
az containerapp revision restart --name ca-ui --resource-group rg-<env-name> --revision <revision-name>

# Force new deployment with revision suffix
az containerapp update --name ca-ui --resource-group rg-<env-name> --image <image> --revision-suffix v3

# Get container app URLs
az containerapp list --resource-group rg-<env-name> --query '[].{name:name,url:properties.configuration.ingress.fqdn}' --output table
```

### Build Image Manually

```bash
# Build in ACR (recommended)
az acr build --registry <acr-name> --image books/ui:latest --platform linux/amd64 --file src/Ui/Dockerfile src/Ui

# Build locally and push
docker buildx build --platform linux/amd64 -t <acr-name>.azurecr.io/books/ui:latest -f src/Ui/Dockerfile src/Ui
az acr login --name <acr-name>
docker push <acr-name>.azurecr.io/books/ui:latest
```

### Common azd Commands

```bash
# Initialize new environment
azd init
azd env new <env-name>

# Set environment variables
azd env set AZURE_LOCATION westus2

# Provision infrastructure only
azd provision

# Deploy services only
azd deploy

# Provision and deploy
azd up

# View environment details
azd env list
azd env get-values

# Tear down everything
azd down --purge
```

---

## Cost Optimization Tips

1. **Use Basic SKUs for development:**
   - SQL Server: Basic tier ($5/month per database)
   - Redis: Basic C0 ($16/month)
   - Container Apps: Pay-per-use (can scale to zero)

2. **Set minReplicas to 0:**
   ```bicep
   scale: {
     minReplicas: 0  // Scale to zero when not in use
     maxReplicas: 3
   }
   ```

3. **Use appropriate regions:**
   - Some regions are cheaper than others
   - Choose regions close to your users

4. **Clean up after testing:**
   ```bash
   azd down --purge
   ```

---

## Security Considerations

1. **Internal services:** Keep microservices internal (external: false)
2. **HTTPS only:** Container Apps enforce HTTPS by default
3. **Secrets management:** Use Azure Key Vault for production secrets
4. **Firewall rules:** SQL Server allows Azure IPs only by default
5. **Container Registry:** Use managed identity for production

---

## Next Steps

After successful deployment:

1. **Configure custom domains** for production
2. **Set up CI/CD pipelines** with GitHub Actions or Azure DevOps
3. **Implement monitoring** with Application Insights
4. **Configure autoscaling rules** based on traffic patterns
5. **Set up backup and disaster recovery** for databases

---

## Quick Reference Card

| Task | Command |
|------|---------|
| Initialize | `azd init && azd env new <name>` |
| Set region | `azd env set AZURE_LOCATION westus2` |
| Deploy all | `azd up` |
| Deploy only | `azd deploy` |
| Check status | `az containerapp list -g rg-<env> --query '[].{name:name,status:properties.runningStatus}' -o table` |
| View logs | `az containerapp logs show --name <app> -g rg-<env> --tail 50` |
| Rebuild UI | `az acr build --registry <acr> --image books/ui:latest -f src/Ui/Dockerfile src/Ui` |
| Update app | `az containerapp update --name <app> -g rg-<env> --image <image> --revision-suffix v2` |
| Tear down | `azd down --purge` |

---

*Last updated: January 2026*
*Tested with: .NET Aspire 9.3, Angular 19+, Azure CLI 2.81, azd 1.5+*

# Multi-stage Dockerfile for ASP.NET Core 8 Web Application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files for caching layer
COPY src/Ecommerce.Domain/Ecommerce.Domain.csproj src/Ecommerce.Domain/
COPY src/Ecommerce.Application/Ecommerce.Application.csproj src/Ecommerce.Application/
COPY src/Ecommerce.Infrastructure/Ecommerce.Infrastructure.csproj src/Ecommerce.Infrastructure/
COPY src/Ecommerce.Web/Ecommerce.Web.csproj src/Ecommerce.Web/

# Restore dependencies
RUN dotnet restore src/Ecommerce.Web/Ecommerce.Web.csproj

# Copy all source code
COPY src/ src/

# Build and Publish in Release mode
WORKDIR /src/src/Ecommerce.Web
RUN dotnet publish Ecommerce.Web.csproj -c Release -o /app/publish /p:UseAppHost=false

# Runtime Image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Copy published artifacts
COPY --from=build /app/publish .

# Railway dynamic PORT configuration
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

ENTRYPOINT ["dotnet", "Ecommerce.Web.dll"]

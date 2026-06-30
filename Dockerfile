# Use .NET 9 SDK as build environment
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files first for better layer caching
COPY ["PRN232.Notification.Api/PRN232.Notification.Api.csproj", "PRN232.Notification.Api/"]
COPY ["PRN232.Notification.Application/PRN232.Notification.Application.csproj", "PRN232.Notification.Application/"]
COPY ["PRN232.Notification.Infrastructure/PRN232.Notification.Infrastructure.csproj", "PRN232.Notification.Infrastructure/"]
COPY ["PRN232.Notification.Domain/PRN232.Notification.Domain.csproj", "PRN232.Notification.Domain/"]

# Restore dependencies
RUN dotnet restore "PRN232.Notification.Api/PRN232.Notification.Api.csproj"

# Copy the rest of the source code
COPY . .

# Build and publish the API project
WORKDIR "/src/PRN232.Notification.Api"
RUN dotnet publish "PRN232.Notification.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Use .NET 9 Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Expose HTTP API port
EXPOSE 8080

ENTRYPOINT ["dotnet", "PRN232.Notification.Api.dll"]



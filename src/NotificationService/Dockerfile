# Stage 1: Base - Defines the runtime environment
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Stage 2: Build - Compiles and restores dependencies
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy all project files for dependency restoration
COPY ["src/NotificationService/NotificationService.csproj", "src/NotificationService/"]
COPY ["src/Common/Common.csproj", "src/Common/"]

# Restore dependencies
RUN dotnet restore "src/NotificationService/NotificationService.csproj"

# Copy remaining source code
COPY src/. src/.

# Build the project
RUN dotnet build "src/NotificationService/NotificationService.csproj" -c Release -o /app/build

# Stage 3: Publish - Creates the final runnable artifact
FROM build AS publish
RUN dotnet publish "src/NotificationService/NotificationService.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 4: Final - The smallest image for deployment
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Entrypoint
ENTRYPOINT ["dotnet", "NotificationService.dll"]
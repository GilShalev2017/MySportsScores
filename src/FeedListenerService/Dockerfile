# Stage 1: Base - Defines the runtime environment
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Stage 2: Build - Compiles and restores dependencies
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
# Set WORKDIR to the root of where all source code will live
WORKDIR /src 

# Copy all project files for dependency restoration
# This assumes your project files are located relative to the Dockerfile context:
# - src/FeedListenerService/FeedListenerService.csproj
# - src/Common/Common.csproj
COPY ["src/FeedListenerService/FeedListenerService.csproj", "src/FeedListenerService/"]
COPY ["src/Common/Common.csproj", "src/Common/"]

# Restore dependencies
# Run restore from the /src root, referencing the projects relative to /src
RUN dotnet restore "src/FeedListenerService/FeedListenerService.csproj"

# Copy remaining source code (including Program.cs and Services)
# This copies everything from the host's src/ directory into the container's /src/ directory
COPY src/. src/.

# --- IMPORTANT FIX ---
# Build the project from the root WORKDIR (/src), referencing the full path.
# This is more robust against entry point detection issues than changing WORKDIR.
RUN dotnet build "src/FeedListenerService/FeedListenerService.csproj" -c Release -o /app/build

# Stage 3: Publish - Creates the final runnable artifact
FROM build AS publish
# Run publish from the root WORKDIR (/src), referencing the full path.
RUN dotnet publish "src/FeedListenerService/FeedListenerService.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 4: Final - The smallest image for deployment
FROM base AS final
WORKDIR /app
# Copy only the published output from the publish stage
COPY --from=publish /app/publish .
# Set the entry point to run the application
ENTRYPOINT ["dotnet", "FeedListenerService.dll"]

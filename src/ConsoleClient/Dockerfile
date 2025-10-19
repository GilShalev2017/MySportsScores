# Claude
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["ConsoleClient/ConsoleClient.csproj", "ConsoleClient/"]
RUN dotnet restore "ConsoleClient/ConsoleClient.csproj"
COPY . .
WORKDIR "/src/ConsoleClient"
RUN dotnet build "ConsoleClient.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ConsoleClient.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ConsoleClient.dll"]

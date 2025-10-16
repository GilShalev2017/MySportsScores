FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["IngestService/IngestService.csproj", "IngestService/"]
COPY ["Common/Common.csproj", "Common/"]
RUN dotnet restore "IngestService/IngestService.csproj"
COPY . .
WORKDIR "/src/IngestService"
RUN dotnet build "IngestService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "IngestService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "IngestService.dll"]

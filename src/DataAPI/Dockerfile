
# Claude
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["DataAPI/DataAPI.csproj", "DataAPI/"]
COPY ["IngestService/IngestService.csproj", "IngestService/"]
COPY ["Common/Common.csproj", "Common/"]
RUN dotnet restore "DataAPI/DataAPI.csproj"
COPY . .
WORKDIR "/src/DataAPI"
RUN dotnet build "DataAPI.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "DataAPI.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DataAPI.dll"]
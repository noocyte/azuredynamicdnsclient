#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:7.0-alpine AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["src/DynamicDnsClient/DynamicDnsClient.csproj", "DynamicDnsClient/"]
RUN dotnet restore "DynamicDnsClient/DynamicDnsClient.csproj"
COPY /src/ .
WORKDIR "/src/DynamicDnsClient"
RUN dotnet build "DynamicDnsClient.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "DynamicDnsClient.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY "src/DynamicDnsClient/appsettings.*json" .
ENTRYPOINT ["dotnet", "DynamicDnsClient.dll"]
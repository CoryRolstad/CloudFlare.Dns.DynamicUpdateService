#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["CloudFlare.Dns.DynamicUpdateService/CloudFlare.Dns.DynamicUpdateService.csproj", "CloudFlare.Dns.DynamicUpdateService/"]
RUN dotnet restore "CloudFlare.Dns.DynamicUpdateService/CloudFlare.Dns.DynamicUpdateService.csproj"
COPY . .
WORKDIR "/src/CloudFlare.Dns.DynamicUpdateService"
RUN dotnet build "CloudFlare.Dns.DynamicUpdateService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CloudFlare.Dns.DynamicUpdateService.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CloudFlare.Dns.DynamicUpdateService.dll"]
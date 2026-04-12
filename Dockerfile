# 1. Build Stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source

# បន្ថែម Environment variable ដើម្បីរំលងការឆែក global.json
ENV DOTNET_SKIP_FIRST_TIME_EXPERIENCE=true
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1

COPY . .

# រុករក និង Restore
RUN dotnet restore "src/POS.API/POS.API.csproj"
RUN dotnet publish "src/POS.API/POS.API.csproj" -c Release -o /app

# 2. Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .

ENV ASPNETCORE_URLS=http://+:80
EXPOSE 80

ENTRYPOINT ["dotnet", "POS.API.dll"]
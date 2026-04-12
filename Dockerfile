# 1. ដំណាក់កាល Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source

# Copy រាល់ file ទាំងអស់ចូលទៅក្នុង Docker
COPY . .

# Restore dependencies ដោយប្រើ file .csproj
RUN dotnet restore "./src/POS.API/POS.API.csproj"

# Build និង Publish ចេញជា file សម្រេច
RUN dotnet publish "./src/POS.API/POS.API.csproj" -c Release -o /app

# 2. ដំណាក់កាល Runtime (សម្រាប់ Run កូដ)
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .

# កំណត់ Port ឱ្យត្រូវជាមួយ Render (Default គឺ 80)
ENV ASPNETCORE_URLS=http://+:80
EXPOSE 80

# ត្រូវប្រាកដថាឈ្មោះ DLL គឺ POS.API.dll
ENTRYPOINT ["dotnet", "POS.API.dll"]
# 1. Build Stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR "/app/source"

# Copy the csproj file first to cache the layer (speeds up future builds)
COPY ["Prezentownik.WebApi/Prezentownik.WebApi.csproj", "Prezentownik.WebApi/"]
RUN dotnet restore Prezentownik.WebApi/Prezentownik.WebApi.csproj

# Copy the rest of the source code and build
COPY ["Prezentownik.WebApi/", "Prezentownik.WebApi/"]
WORKDIR "/app/source/Prezentownik.WebApi"
RUN dotnet build "Prezentownik.WebApi.csproj" -c Release --no-restore

# 2. Publish Stage
FROM build AS publish
WORKDIR "/app/source/Prezentownik.WebApi"
RUN dotnet publish "Prezentownik.WebApi.csproj" -c Release --no-build -o "/app/publish" /p:UseAppHost=false

# 3. Final Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR "/app"
EXPOSE 8080

COPY --from=publish "/app/publish" .
ENTRYPOINT ["dotnet", "Prezentownik.WebApi.dll"]

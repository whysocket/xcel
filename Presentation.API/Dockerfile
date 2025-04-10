FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER 1001 # Or USER aspnet
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY "./Presentation.API.csproj" .
RUN set -e && dotnet restore "./Presentation.API.csproj"
COPY . .
WORKDIR "/src/Presentation.API"
RUN set -e && dotnet build "./Presentation.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN set -e && dotnet publish "./Presentation.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish /app
ENTRYPOINT ["dotnet", "Presentation.API.dll"]
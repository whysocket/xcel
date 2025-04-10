FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Presentation.API/Presentation.API.csproj", "Presentation.API/"]
COPY ["Application/Application.csproj", "Application/"]
COPY ["Domain/Domain.csproj", "Domain/"]
COPY ["Xcel.Services.Auth/Xcel.Services.Auth.csproj", "Xcel.Services.Auth/"]
COPY ["Xcel.Config/Xcel.Config.csproj", "Xcel.Config/"]
COPY ["Xcel.Services.Email/Xcel.Services.Email.csproj", "Xcel.Services.Email/"]
COPY ["Infra/Infra.csproj", "Infra/"]
RUN dotnet restore "Presentation.API/Presentation.API.csproj"
COPY . .
WORKDIR "/src/Presentation.API"
RUN dotnet build "Presentation.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Presentation.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
#install envsubst
RUN apt-get update && apt-get install -y gettext-base
#verify installation
RUN envsubst --version
#list files in the app directory.
RUN ls /app
WORKDIR /app
COPY --from=publish /app/publish .
#replace the environment variables values inside of the appsettings.Production.json file.
RUN envsubst < appsettings.Production.json > appsettings.json
COPY appsettings.json ./
ENTRYPOINT ["dotnet", "Presentation.API.dll"]
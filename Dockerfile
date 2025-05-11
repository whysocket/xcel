# Stage 1: Base image for the final runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base

USER $APP_UID

WORKDIR /app
EXPOSE 8080

# Stage 2: Build stage using the .NET SDK image
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

# Stage 3: Publish stage - based on the build stage
FROM build AS publish

ARG BUILD_CONFIGURATION=Release

WORKDIR "/src/Presentation.API"

RUN dotnet publish "Presentation.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Stage 4: Final runtime stage - based on the smaller base image
FROM base AS final

WORKDIR /app

COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "Presentation.API.dll"]
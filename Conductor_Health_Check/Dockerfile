FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Conductor_Health_Check/Conductor_Health_Check.csproj", "Conductor_Health_Check/"]
RUN dotnet restore "Conductor_Health_Check/Conductor_Health_Check.csproj"
COPY . .
WORKDIR "/src/Conductor_Health_Check"
RUN dotnet build "./Conductor_Health_Check.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Conductor_Health_Check.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Conductor_Health_Check.dll"]

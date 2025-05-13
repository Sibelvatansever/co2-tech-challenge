FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY measurements-api/src/TechChallenge.Measurements.Api/TechChallenge.Measurements.Api.csproj measurements-api/src/TechChallenge.Measurements.Api/
RUN dotnet restore "measurements-api/src/TechChallenge.Measurements.Api/TechChallenge.Measurements.Api.csproj"
COPY . .
WORKDIR "/src/measurements-api/src/TechChallenge.Measurements.Api"
RUN dotnet build "TechChallenge.Measurements.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "TechChallenge.Measurements.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TechChallenge.Measurements.Api.dll"]
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY emissions-api/src/TechChallenge.Emissions.Api/TechChallenge.Emissions.Api.csproj emissions-api/src/TechChallenge.Emissions.Api/
RUN dotnet restore "emissions-api/src/TechChallenge.Emissions.Api/TechChallenge.Emissions.Api.csproj"
COPY . .
WORKDIR "/src/emissions-api/src/TechChallenge.Emissions.Api"
RUN dotnet build "TechChallenge.Emissions.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "TechChallenge.Emissions.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TechChallenge.Emissions.Api.dll"]
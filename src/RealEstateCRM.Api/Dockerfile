# Build context must be the repo root (docker build -f src/RealEstateCRM.Api/Dockerfile .)
# so this stage can restore all four projects, not just the Api project.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/RealEstateCRM.Domain/RealEstateCRM.Domain.csproj src/RealEstateCRM.Domain/
COPY src/RealEstateCRM.Application/RealEstateCRM.Application.csproj src/RealEstateCRM.Application/
COPY src/RealEstateCRM.Infrastructure/RealEstateCRM.Infrastructure.csproj src/RealEstateCRM.Infrastructure/
COPY src/RealEstateCRM.Api/RealEstateCRM.Api.csproj src/RealEstateCRM.Api/
RUN dotnet restore src/RealEstateCRM.Api/RealEstateCRM.Api.csproj

COPY src/ src/
RUN dotnet publish src/RealEstateCRM.Api/RealEstateCRM.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Runs as non-root — never run the API as the container's default root user.
RUN useradd --uid 5678 --user-group appuser
USER appuser

COPY --from=build /app .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "RealEstateCRM.Api.dll"]

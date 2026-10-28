# syntax=docker/dockerfile:1

# ---- Build stage: full SDK, compiles and publishes the API ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy only the project file first so the restore layer is cached until packages change.
COPY DocIntake.Api/DocIntake.Api.csproj DocIntake.Api/
RUN dotnet restore DocIntake.Api/DocIntake.Api.csproj

COPY DocIntake.Api/ DocIntake.Api/
RUN dotnet publish DocIntake.Api/DocIntake.Api.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

# ---- Runtime stage: small ASP.NET runtime image, no SDK, no source ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Listen on 8080 (the default since .NET 8) and run as the image's built-in non-root user.
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER $APP_UID

ENTRYPOINT ["dotnet", "DocIntake.Api.dll"]

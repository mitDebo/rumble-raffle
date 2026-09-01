# ---- Build stage ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy only the csproj files first so Docker can cache the restore step
# (the slowest part) as long as neither csproj has changed. Api references
# Core via ProjectReference, so both are needed before restore can resolve
# the graph — restoring just Api.csproj walks that reference automatically.
COPY src/RumbleRaffle.Api/RumbleRaffle.Api.csproj src/RumbleRaffle.Api/
COPY src/RumbleRaffle.Core/RumbleRaffle.Core.csproj src/RumbleRaffle.Core/
RUN dotnet restore src/RumbleRaffle.Api/RumbleRaffle.Api.csproj

# Now copy the rest of the source and publish a Release build.
COPY src/RumbleRaffle.Api/ src/RumbleRaffle.Api/
COPY src/RumbleRaffle.Core/ src/RumbleRaffle.Core/
RUN dotnet publish src/RumbleRaffle.Api/RumbleRaffle.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# ---- Runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# The official ASP.NET Core runtime image already listens on port 8080
# and runs as a non-root user by default — nothing extra to configure.
EXPOSE 8080

ENTRYPOINT ["dotnet", "RumbleRaffle.Api.dll"]

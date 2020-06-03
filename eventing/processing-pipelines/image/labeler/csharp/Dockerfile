# Use Microsoft's official build .NET image.
# https://hub.docker.com/_/microsoft-dotnet-core-sdk/
FROM mcr.microsoft.com/dotnet/core/sdk:3.1-alpine AS build
WORKDIR /app

# Copy local code to the container image.
COPY image/labeler ./image/labeler

# Copy common library
COPY common ./common

# Install production dependencies
# Restore as distinct layers
RUN dotnet restore common/csharp
RUN dotnet restore image/labeler/csharp

# Build a release artifact.
RUN dotnet publish image/labeler/csharp -c Release -o out

# Use Microsoft's official runtime .NET image.
# https://hub.docker.com/_/microsoft-dotnet-core-aspnet/
FROM mcr.microsoft.com/dotnet/core/aspnet:3.1 AS runtime
WORKDIR /app
COPY --from=build /app/out ./

# Run the web service on container startup.
ENTRYPOINT ["dotnet", "Labeler.dll"]
# ./WebApplication1/Dockerfile
# Multi-stage build for a .NET web application used for local development and CI.
# - Builds with the .NET SDK and publishes a self-contained app folder.
# - Runs using the lighter ASP.NET runtime image.
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy the project file and restore dependencies
COPY ["WebApplication1.csproj", "./"]
RUN dotnet restore "./WebApplication1.csproj"

# Copy the rest of the project and publish
COPY . .
RUN dotnet publish -c Release -o /app/publish

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS="http://+:8080"
ENTRYPOINT ["dotnet", "WebApplication1.dll"]

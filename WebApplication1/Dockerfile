# build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# copy csproj and restore
COPY ["WebApplication1.csproj", "./"]
RUN dotnet restore "./WebApplication1.csproj"

# copy everything else and build
COPY . .
RUN dotnet publish "WebApplication1.csproj" -c Release -o /app/publish

# runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
EXPOSE 80
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "WebApplication1.dll"]

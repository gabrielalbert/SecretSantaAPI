# Build and publish the ASP.NET Core API
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY TaskGame.API/TaskGame.API.csproj TaskGame.API/
RUN dotnet restore "TaskGame.API/TaskGame.API.csproj"

COPY TaskGame.API/. ./TaskGame.API/
WORKDIR /src/TaskGame.API
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

EXPOSE 8080

COPY --from=build /app/publish ./
# Create uploads directory
RUN mkdir -p /app/uploads

ENTRYPOINT ["dotnet", "TaskGame.API.dll"]

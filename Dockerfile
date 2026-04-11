# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files for restore (layer cache optimisation)
COPY src/API/WhereToStayInJapan.API.csproj             API/
COPY src/Application/WhereToStayInJapan.Application.csproj Application/
COPY src/Domain/WhereToStayInJapan.Domain.csproj       Domain/
COPY src/Infrastructure/WhereToStayInJapan.Infrastructure.csproj Infrastructure/
COPY src/Shared/WhereToStayInJapan.Shared.csproj       Shared/

RUN dotnet restore API/WhereToStayInJapan.API.csproj

# Copy source and publish
COPY src/ .
WORKDIR /src/API
RUN dotnet publish WhereToStayInJapan.API.csproj -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "WhereToStayInJapan.API.dll"]

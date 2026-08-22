# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files first to cache dependencies
COPY ["Bolcko.Web.sln", "./"]
COPY ["Bolcko.Web.App/Bolcko.Web.App.csproj", "Bolcko.Web.App/"]
COPY ["Blocko.Services/Blocko.Services.csproj", "Blocko.Services/"]
COPY ["Blocko.Persistence/Blocko.Persistence.csproj", "Blocko.Persistence/"]
COPY ["Bolcko.Domain/Bolcko.Domain.csproj", "Bolcko.Domain/"]

RUN dotnet restore "Bolcko.Web.sln"

# Copy all source code
COPY . .
WORKDIR "/src/Bolcko.Web.App"
RUN dotnet build "Bolcko.Web.App.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "Bolcko.Web.App.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final stage / Production Image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Render's free tier routing requires apps to listen on port 8080
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "Bolcko.Web.App.dll"]

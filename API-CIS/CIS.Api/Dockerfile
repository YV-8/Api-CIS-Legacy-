# Use the official .NET 10 SDK image to build the app
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY ["CIS.Api/CIS.Api.csproj", "CIS.Api/"]
COPY ["CIS.BusinessLogic/CIS.BusinessLogic.csproj", "CIS.BusinessLogic/"]
COPY ["CIS.DataAcces/CIS.DataAcces.csproj", "CIS.DataAcces/"]
RUN dotnet restore "CIS.Api/CIS.Api.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/CIS.Api"
RUN dotnet build "CIS.Api.csproj" -c Release -o /app/build

# Publish the app
FROM build AS publish
RUN dotnet publish "CIS.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Use the official ASP.NET Core runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Expose port 80
EXPOSE 80

# Set the entry point
ENTRYPOINT ["dotnet", "CIS.Api.dll"]
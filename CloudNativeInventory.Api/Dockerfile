# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Restore dependencies
COPY ["CloudNativeInventory.Api/CloudNativeInventory.Api.csproj", "CloudNativeInventory.Api/"]
RUN dotnet restore "CloudNativeInventory.Api/CloudNativeInventory.Api.csproj"

# Build and publish the application
COPY . .
WORKDIR "/src/CloudNativeInventory.Api"
RUN dotnet build "CloudNativeInventory.Api.csproj" -c Release -o /app/build
RUN dotnet publish "CloudNativeInventory.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Copy published artifacts from the build stage
COPY --from=build /app/publish .

# Expose port and configure ASP.NET Core to use it
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080

# Run as non-root user for enhanced security
USER app

ENTRYPOINT ["dotnet", "CloudNativeInventory.Api.dll"]
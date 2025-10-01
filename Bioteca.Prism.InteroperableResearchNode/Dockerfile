# Multi-stage Dockerfile for IRN Node
# Supports both NodeA and NodeB configurations via ASPNETCORE_ENVIRONMENT

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Debug
WORKDIR /src

# Copy all project files (to handle dependencies)
COPY ["Bioteca.Prism.InteroperableResearchNode/Bioteca.Prism.InteroperableResearchNode.csproj", "Bioteca.Prism.InteroperableResearchNode/"]
COPY ["Bioteca.Prism.Service/Bioteca.Prism.Service.csproj", "Bioteca.Prism.Service/"]
COPY ["Bioteca.Prism.Domain/Bioteca.Prism.Domain.csproj", "Bioteca.Prism.Domain/"]
COPY ["Bioteca.DotNet/Bioteca.DotNet.csproj", "Bioteca.DotNet/"]

# Restore dependencies
RUN dotnet restore "Bioteca.Prism.InteroperableResearchNode/Bioteca.Prism.InteroperableResearchNode.csproj"

# Copy all source files
COPY . .

# Build the project
WORKDIR "/src/Bioteca.Prism.InteroperableResearchNode"
RUN dotnet build "Bioteca.Prism.InteroperableResearchNode.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Debug
RUN dotnet publish "Bioteca.Prism.InteroperableResearchNode.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Final stage
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Bioteca.Prism.InteroperableResearchNode.dll"]
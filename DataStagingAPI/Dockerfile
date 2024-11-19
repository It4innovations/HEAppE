#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["DataStagingAPI/DataStagingAPI.csproj", "DataStagingAPI/"]
COPY ["ExtModels/ExtModels.csproj", "ExtModels/"]
COPY ["BusinessLogicTier/BusinessLogicTier.csproj", "BusinessLogicTier/"]
COPY ["CertificateGenerator/CertificateGenerator.csproj", "CertificateGenerator/"]
COPY ["DomainObjects/DomainObjects.csproj", "DomainObjects/"]
COPY ["DataAccessTier/DataAccessTier.csproj", "DataAccessTier/"]
COPY ["Utils/Utils.csproj", "Utils/"]
COPY ["ExternalAuthentication/ExternalAuthentication.csproj", "ExternalAuthentication/"]
COPY ["RestUtils/RestUtils.csproj", "RestUtils/"]
COPY ["FileTransferFramework/FileTransferFramework.csproj", "FileTransferFramework/"]
COPY ["ConnectionPool/ConnectionPool.csproj", "ConnectionPool/"]
COPY ["HpcConnectionFramework/HpcConnectionFramework.csproj", "HpcConnectionFramework/"]
COPY ["OpenStackAPI/OpenStackAPI.csproj", "OpenStackAPI/"]
COPY ["RestApiModels/RestApiModels.csproj", "RestApiModels/"]
COPY ["ServiceTier/ServiceTier.csproj", "ServiceTier/"]

RUN dotnet restore "DataStagingAPI/DataStagingAPI.csproj"
COPY . .
WORKDIR "/src/DataStagingAPI"
RUN dotnet build "DataStagingAPI.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "DataStagingAPI.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DataStagingAPI.dll"]

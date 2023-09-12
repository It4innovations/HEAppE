#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:7.0-bullseye-slim AS base
WORKDIR /app
RUN mkdir /sftp
RUN chmod 700 /sftp

RUN apt-get update 
RUN apt-get install -y --no-install-recommends curl 
RUN apt-get install -y --no-install-recommends openssh-client

EXPOSE 80


FROM mcr.microsoft.com/dotnet/sdk:7.0-bullseye-slim AS build
WORKDIR /src

COPY ["RestApi/RestApi.csproj", "RestApi/"]
COPY ["ServiceTier/ServiceTier.csproj", "ServiceTier/"]
COPY ["DataAccessTier/DataAccessTier.csproj", "DataAccessTier/"]
COPY ["DomainObjects/DomainObjects.csproj", "DomainObjects/"]
COPY ["ExtModels/ExtModels.csproj", "ExtModels/"]
COPY ["BusinessLogicTier/BusinessLogicTier.csproj", "BusinessLogicTier/"]
COPY ["CertificateGenerator/CertificateGenerator.csproj", "CertificateGenerator/"]
COPY ["HpcConnectionFramework/HpcConnectionFramework.csproj", "HpcConnectionFramework/"]
COPY ["ConnectionPool/ConnectionPool.csproj", "ConnectionPool/"]
COPY ["FileTransferFramework/FileTransferFramework.csproj", "FileTransferFramework/"]
COPY ["RestApiModels/RestApiModels.csproj", "RestApiModels/"]
COPY ["BackgroundThread/BackgroundThread.csproj", "BackgroundThread/"]
COPY ["Utils/Utils.csproj", "Utils/"]
COPY ["ExternalAuthentication/ExternalAuthentication.csproj", "ExternalAuthentication/"]
COPY ["OpenStackAPI/OpenStackAPI.csproj", "OpenStackAPI/"]
COPY ["RestUtils/RestUtils.csproj", "RestUtils/"]

RUN dotnet restore "RestApi/RestApi.csproj"
COPY . .

WORKDIR "/src/RestApi"
RUN dotnet build "RestApi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "RestApi.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "RestApi.dll"]
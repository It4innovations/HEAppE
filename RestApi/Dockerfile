#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
RUN mkdir /sftp
RUN chmod 700 /sftp

RUN apt-get update 
RUN apt-get install -y --no-install-recommends curl 
RUN apt-get install -y --no-install-recommends openssh-client

EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
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

# install OpenTelemetry .NET Automatic Instrumentation
ARG OTEL_VERSION=1.14.1
ENV OTEL_DOTNET_AUTO_HOME=/otel-dotnet-auto
ADD https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/releases/download/v${OTEL_VERSION}/otel-dotnet-auto-install.sh otel-dotnet-auto-install.sh
RUN apt-get update && apt-get install -y unzip && \
    sh otel-dotnet-auto-install.sh
RUN chmod +x /otel-dotnet-auto/instrument.sh

# set OTEL variables
ENV OTEL_SERVICE_NAME=heappe-42
ENV OTEL_EXPORTER_OTLP_ENDPOINT=http://otel-collector:4317
ENV OTEL_EXPORTER_OTLP_PROTOCOL=grpc
ENV OTEL_TRACES_EXPORTER=otlp
ENV OTEL_METRICS_EXPORTER=otlp
ENV OTEL_LOGS_EXPORTER=otlp

# nosemgrep: dockerfile.security.missing-user-entrypoint.missing-user-entrypoint
ENTRYPOINT ["/otel-dotnet-auto/instrument.sh", "dotnet", "RestApi.dll"]
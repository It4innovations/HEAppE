#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
RUN mkdir /sftp
RUN chmod 700 /sftp

RUN apt-get update && apt-get install -y --no-install-recommends \
    curl \
    openssh-client \
    git \
    && rm -rf /var/lib/apt/lists/* \
    && rm -f /etc/krb5.conf

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
COPY ["CHANGELOG.md", "CHANGELOG.md"]

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
ENV OTEL_EXPORTER_OTLP_PROTOCOL=grpc
ENV OTEL_TRACES_EXPORTER=otlp
# enable Head Sampling on trace signals
#ENV OTEL_TRACES_SAMPLER=traceidratio
#ENV OTEL_TRACES_SAMPLER_ARG=0.5

# disable metrics
ENV OTEL_DOTNET_AUTO_METRICS_INSTRUMENTATION_ENABLED=false
# disable logs
ENV OTEL_DOTNET_AUTO_LOGS_INSTRUMENTATION_ENABLED=false

# nosemgrep: dockerfile.security.missing-user-entrypoint.missing-user-entrypoint
ENTRYPOINT ["/otel-dotnet-auto/instrument.sh", "dotnet", "RestApi.dll"]
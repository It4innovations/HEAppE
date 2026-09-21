FROM mcr.microsoft.com/dotnet/sdk:9.0

# Install required utilities
RUN apt-get update && apt-get install -y --no-install-recommends \
    openssh-client \
    curl \
    git \
    procps \
    && rm -rf /var/lib/apt/lists/*

# Install dotnet-reportgenerator-globaltool for HTML coverage generation
RUN dotnet tool install -g dotnet-reportgenerator-globaltool
ENV PATH="${PATH}:/root/.dotnet/tools"

WORKDIR /workspace
COPY . /workspace

# Set default entrypoint
CMD ["bash"]

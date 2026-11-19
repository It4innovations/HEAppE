FROM alpine:3.21 AS base
WORKDIR /app

RUN mkdir shared
RUN mkdir scripts;

COPY ["SshAgent/scripts/init.sh", "./scripts/"]
COPY ["SshAgent/scripts/adding_keys.sh", "./scripts/"]
RUN chmod -R 700 scripts

RUN apk update && apk upgrade --no-cache && \
    apk add --no-cache openssh-client bash

HEALTHCHECK --interval=10s --timeout=3s --retries=3 \
  CMD test -S /app/shared/agentsocket || exit 1

# nosemgrep: dockerfile.security.missing-user-entrypoint.missing-user-entrypoint
ENTRYPOINT ["bash", "./scripts/init.sh"] # nosemgrep: dockerfile.security.missing-user-entrypoint.missing-user-entrypoint


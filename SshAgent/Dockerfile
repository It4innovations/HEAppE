FROM alpine AS base
WORKDIR /app

RUN mkdir shared
RUN mkdir scripts;

COPY ["SshAgent/scripts/init.sh", "./scripts/"]
COPY ["SshAgent/scripts/adding_keys.sh", "./scripts/"]
RUN chmod -R 700 scripts

RUN apk update 
RUN apk add --no-cache openssh-client 
RUN apk add --no-cache bash

# nosemgrep: dockerfile.security.missing-user-entrypoint.missing-user-entrypoint
ENTRYPOINT ["bash", "./scripts/init.sh"]
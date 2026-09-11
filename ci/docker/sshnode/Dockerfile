FROM alpine:3.19

RUN apk add --no-cache \
    openssh-server \
    openssh-client \
    bash \
    shadow \
    curl \
    sudo \
    procps

# Create users
RUN useradd -m -s /bin/bash testuser && \
    useradd -m -s /bin/bash passuser && \
    useradd -m -s /bin/bash adminuser && \
    echo "testuser:TestPass123!" | chpasswd && \
    echo "passuser:Passphrase123!" | chpasswd && \
    echo "adminuser:AdminPass123!" | chpasswd && \
    echo "testuser ALL=(ALL) NOPASSWD:ALL" >> /etc/sudoers.d/testuser

# Configure SSH
RUN ssh-keygen -A && \
    sed -i 's/#PasswordAuthentication yes/PasswordAuthentication yes/' /etc/ssh/sshd_config && \
    sed -i 's/#PubkeyAuthentication yes/PubkeyAuthentication yes/' /etc/ssh/sshd_config && \
    mkdir -p /shared_keys /var/run/sshd

COPY ssh-init.sh /usr/local/bin/ssh-init.sh
RUN chmod +x /usr/local/bin/ssh-init.sh

EXPOSE 22

HEALTHCHECK --interval=5s --timeout=3s --retries=10 \
  CMD nc -z localhost 22 || exit 1

ENTRYPOINT ["/usr/local/bin/ssh-init.sh"]

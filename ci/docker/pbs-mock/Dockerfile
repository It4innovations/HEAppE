FROM alpine:3.19

RUN apk add --no-cache \
    bash \
    openssh-server \
    openssh-client \
    shadow \
    sudo \
    curl \
    python3

# Setup users
RUN useradd -m -s /bin/bash testuser && \
    useradd -m -s /bin/bash adminuser && \
    echo "testuser:TestPass123!" | chpasswd && \
    echo "adminuser:AdminPass123!" | chpasswd && \
    mkdir -p /var/spool/pbs/jobs /shared_keys

# Copy mock PBS binaries
COPY qsub /usr/local/bin/qsub
COPY qstat /usr/local/bin/qstat
COPY qdel /usr/local/bin/qdel

RUN chmod +x /usr/local/bin/qsub /usr/local/bin/qstat /usr/local/bin/qdel && \
    ssh-keygen -A && \
    sed -i 's/#PasswordAuthentication yes/PasswordAuthentication yes/' /etc/ssh/sshd_config && \
    sed -i 's/#PubkeyAuthentication yes/PubkeyAuthentication yes/' /etc/ssh/sshd_config

EXPOSE 22

HEALTHCHECK --interval=5s --timeout=3s --retries=10 \
  CMD qstat || exit 1

CMD ["/bin/bash", "-c", "for u in testuser adminuser root; do uh=$(eval echo ~$u); mkdir -p $uh/.ssh; [ -f /shared_keys/id_ed25519_${u}.pub ] && cat /shared_keys/id_ed25519_${u}.pub >> $uh/.ssh/authorized_keys; [ -f /shared_keys/id_rsa_${u}.pub ] && cat /shared_keys/id_rsa_${u}.pub >> $uh/.ssh/authorized_keys; chown -R $u:$u $uh 2>/dev/null; done; /usr/sbin/sshd -D"]

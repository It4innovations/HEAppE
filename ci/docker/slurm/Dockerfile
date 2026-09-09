FROM rockylinux:9

# Install EPEL and packages
RUN dnf install -y epel-release && \
    dnf install -y \
        munge \
        munge-libs \
        slurm \
        slurm-slurmctld \
        slurm-slurmd \
        slurm-slurmdbd \
        openssh-server \
        openssh-clients \
        sudo \
        procps-ng \
        net-tools \
        which \
        pam \
        ca-certificates \
        diffutils \
    && dnf clean all

# Create slurm system user and test users
RUN (id -u slurm >/dev/null 2>&1 || useradd -r -s /bin/false -u 401 slurm) && \
    useradd -m -s /bin/bash testuser && \
    useradd -m -s /bin/bash adminuser && \
    echo "testuser:TestUserPass123!" | chpasswd && \
    echo "adminuser:AdminUserPass123!" | chpasswd && \
    echo "testuser ALL=(ALL) NOPASSWD:ALL" >> /etc/sudoers.d/testuser && \
    echo "adminuser ALL=(ALL) NOPASSWD:ALL" >> /etc/sudoers.d/adminuser && \
    mkdir -p /var/spool/slurmctld /var/spool/slurmd /var/log/slurm /scratch /shared_keys && \
    chown -R slurm:slurm /var/spool/slurmctld /var/spool/slurmd /var/log/slurm && \
    chmod 755 /var/spool/slurmctld /var/spool/slurmd /scratch

# Copy configuration files and init script
COPY slurm.conf /etc/slurm/slurm.conf
COPY slurmdbd.conf /etc/slurm/slurmdbd.conf
COPY cgroup.conf /etc/slurm/cgroup.conf
COPY slurm-init.sh /usr/local/bin/slurm-init.sh

RUN chmod 644 /etc/slurm/slurm.conf /etc/slurm/cgroup.conf && \
    chmod 600 /etc/slurm/slurmdbd.conf && \
    chown slurm:slurm /etc/slurm/slurm.conf /etc/slurm/slurmdbd.conf /etc/slurm/cgroup.conf && \
    chmod +x /usr/local/bin/slurm-init.sh

# Configure SSH
RUN ssh-keygen -A && \
    sed -i 's/#PermitRootLogin prohibit-password/PermitRootLogin yes/' /etc/ssh/sshd_config && \
    sed -i 's/#PasswordAuthentication yes/PasswordAuthentication yes/' /etc/ssh/sshd_config && \
    sed -i 's/#PubkeyAuthentication yes/PubkeyAuthentication yes/' /etc/ssh/sshd_config

EXPOSE 22 6817 6818 6819

HEALTHCHECK --interval=5s --timeout=3s --retries=12 --start-period=10s \
  CMD sinfo || exit 1

ENTRYPOINT ["/usr/local/bin/slurm-init.sh"]

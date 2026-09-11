#!/bin/bash
set -e

echo "=== Initializing Slurm CI Node ==="

# 1. Setup Munge
if [ ! -f /etc/munge/munge.key ]; then
    echo "Generating Munge key..."
    dd if=/dev/urandom bs=1 count=1024 > /etc/munge/munge.key 2>/dev/null
    chown munge:munge /etc/munge/munge.key
    chmod 400 /etc/munge/munge.key
fi

mkdir -p /run/munge /var/log/munge
chown -R munge:munge /run/munge /var/log/munge
su -s /bin/bash munge -c "/usr/sbin/munged --force"

# 2. Setup SSH Keys for test users
echo "Setting up SSH keys..."
mkdir -p /shared_keys

for user in testuser adminuser root; do
    user_home=$(eval echo "~$user")
    mkdir -p "$user_home/.ssh"
    
    # Generate Ed25519 key if not exists
    if [ ! -f "/shared_keys/id_ed25519_${user}" ]; then
        ssh-keygen -t ed25519 -N "" -f "/shared_keys/id_ed25519_${user}" -C "${user}@heappe-slurm"
    fi
    
    # Generate RSA key if not exists
    if [ ! -f "/shared_keys/id_rsa_${user}" ]; then
        ssh-keygen -t rsa -b 4096 -N "" -f "/shared_keys/id_rsa_${user}" -C "${user}@heappe-slurm"
    fi

    # Authorize keys
    cat "/shared_keys/id_ed25519_${user}.pub" >> "$user_home/.ssh/authorized_keys"
    cat "/shared_keys/id_rsa_${user}.pub" >> "$user_home/.ssh/authorized_keys"
    chmod 700 "$user_home/.ssh"
    chmod 600 "$user_home/.ssh/authorized_keys"
    chown -R "$user:$user" "$user_home/.ssh"
    
    # Setup HEAppE cluster directories
    mkdir -p "$user_home/.HEAppE/.key_scripts" "$user_home/.HEAppE/Executions"
    chown -R "$user:$user" "$user_home/.HEAppE"
done

# Copy private keys permissions in shared volume
chmod 600 /shared_keys/* 2>/dev/null || true
chmod 644 /shared_keys/*.pub 2>/dev/null || true

# 3. Create mock HEAppE scripts
cat << 'EOF' > /home/testuser/.HEAppE/.key_scripts/test.sh
#!/bin/bash
echo "HEAppE Slurm Test Script Executed with args: $@"
exit 0
EOF
chmod +x /home/testuser/.HEAppE/.key_scripts/test.sh
chown testuser:testuser /home/testuser/.HEAppE/.key_scripts/test.sh

cat << 'EOF' > /home/testuser/.HEAppE/.key_scripts/generic.sh
#!/bin/bash
echo "HEAppE Generic Script: executing $1 with params ${@:2}"
if [ -n "$1" ] && [ -f "$1" ]; then
    bash "$@"
else
    echo "No script provided or executed"
fi
exit 0
EOF
chmod +x /home/testuser/.HEAppE/.key_scripts/generic.sh
chown testuser:testuser /home/testuser/.HEAppE/.key_scripts/generic.sh

# 4. Start SSHD
echo "Starting OpenSSH Server..."
/usr/sbin/sshd

# 5. Start Slurm daemons
touch /var/log/slurm/slurmctld.log /var/log/slurm/slurmd.log
chown slurm:slurm /var/log/slurm/slurmctld.log /var/log/slurm/slurmd.log
chmod 664 /var/log/slurm/slurmctld.log /var/log/slurm/slurmd.log

echo "Starting Slurm Controller (slurmctld)..."
su -s /bin/bash slurm -c "/usr/sbin/slurmctld -c -f /etc/slurm/slurm.conf"

echo "Starting Slurm Daemon (slurmd)..."
/usr/sbin/slurmd -c -f /etc/slurm/slurm.conf -N slurm-ci

# 6. Wait for node to be available and register partitions
sleep 2
scontrol update NodeName=slurm-ci State=RESUME || true

echo "=== Slurm CI Node Initialized Successfully ==="
sinfo || true

# Keep container alive and handle exit gracefully
trap "echo 'Stopping daemons...'; killall slurmd slurmctld munged sshd 2>/dev/null || true; exit 0" SIGTERM SIGINT

sleep infinity &
wait $!

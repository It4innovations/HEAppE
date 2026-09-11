#!/bin/bash
set -e

echo "=== Initializing SSH Node ==="

mkdir -p /shared_keys

for user in testuser passuser adminuser root; do
    user_home=$(eval echo "~$user")
    mkdir -p "$user_home/.ssh" "$user_home/.HEAppE/.key_scripts" "$user_home/.HEAppE/Executions"
    
    # Import any public keys from shared volume
    if [ -f "/shared_keys/id_ed25519_${user}.pub" ]; then
        cat "/shared_keys/id_ed25519_${user}.pub" >> "$user_home/.ssh/authorized_keys"
    fi
    if [ -f "/shared_keys/id_rsa_${user}.pub" ]; then
        cat "/shared_keys/id_rsa_${user}.pub" >> "$user_home/.ssh/authorized_keys"
    fi
    
    # Generate passphrased key for passuser if not existing
    if [ "$user" == "passuser" ] && [ ! -f "/shared_keys/id_rsa_passuser" ]; then
        ssh-keygen -t rsa -b 2048 -N "SecretPassphrase123!" -f "/shared_keys/id_rsa_passuser" -C "passuser@heappe-sshnode"
        cat "/shared_keys/id_rsa_passuser.pub" >> "$user_home/.ssh/authorized_keys"
    fi

    # Create mock test scripts
    cat << 'EOF' > "$user_home/.HEAppE/.key_scripts/test.sh"
#!/bin/bash
echo "HEAppE SSH Test Script with args: $@"
exit 0
EOF
    cat << 'EOF' > "$user_home/.HEAppE/.key_scripts/generic.sh"
#!/bin/bash
echo "Generic job script execution: $@"
if [ -n "$1" ] && [ -f "$1" ]; then
    bash "$@"
fi
exit 0
EOF
    chmod +x "$user_home/.HEAppE/.key_scripts/"*.sh
    chmod 700 "$user_home/.ssh"
    chmod 600 "$user_home/.ssh/authorized_keys" 2>/dev/null || true
    chown -R "$user:$user" "$user_home"
done

echo "Starting SSH daemon..."
/usr/sbin/sshd -D

#!/bin/bash
SSH_AGENT_PATH=/app/shared/agentsocket
HOST_KEYS_PATH="/opt/heappe/sshagent_keys/*"
CONTAINER_KEYS_PATH=/opt/sshagent_keys
CLUSTER_ACCOUNTS_COUNT=$1
KEYS_PREFIX="$2"

echo "Starting adding keys to ssh agent ..."
echo "Parameters: cluster keys count: ${CLUSTER_ACCOUNTS_COUNT}, cluster keys prefix: ${KEYS_PREFIX} ..."

export SSH_AUTH_SOCK=${SSH_AGENT_PATH}
if [ ! -d "${CONTAINER_KEYS_PATH}" ]; then
	mkdir ${CONTAINER_KEYS_PATH}
	cp -R ${HOST_KEYS_PATH} ${CONTAINER_KEYS_PATH}
	chmod -R 500 ${CONTAINER_KEYS_PATH}
fi

files_count=$(find "${CONTAINER_KEYS_PATH}" 2> /dev/null | wc -l)
if [ "$files_count" == "0" ];then
	echo "SSH Agent docker container does not have clusters keys ..."
	exit 255
elif [ -z "$SSH_AUTH_SOCK" ];then
	echo "SSH Agent variable is empty..."
	exit 255
else
	for i in $(eval echo "{01..${CLUSTER_ACCOUNTS_COUNT}}"); do ssh-add "/opt/sshagent_keys/${KEYS_PREFIX}${i}"; done
	echo "Keys added to ssh agent ..."
	ssh-add -l
	exit 1
fi
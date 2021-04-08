#!/bin/bash
rm -Rf /app/shared/*
echo "Starting ssh agent"
eval "$(ssh-agent -s -a /app/shared/agentsocket)"

chmod -R 500 /app/shared
tail -f /dev/null
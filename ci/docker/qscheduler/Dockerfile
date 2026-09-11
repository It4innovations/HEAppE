FROM python:3.11-alpine

WORKDIR /app
COPY server.py /app/server.py
COPY entrypoint.sh /app/entrypoint.sh

RUN chmod +x /app/entrypoint.sh /app/server.py && \
    apk add --no-cache curl

EXPOSE 4300

HEALTHCHECK --interval=5s --timeout=3s --retries=10 \
  CMD curl -f http://localhost:4300/health || exit 1

ENTRYPOINT ["/app/entrypoint.sh"]

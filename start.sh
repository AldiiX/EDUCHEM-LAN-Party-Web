#!/bin/sh

# pouziti env souboru
log() {
  echo "[init] $*"
}

if [ -f "/app/.env" ]; then
  log "/app/.env already exists, skipping generation"
elif b64="$(read_b64)"; then
  log "writing /app/.env from base64"
  umask 077
  printf '%s' "$b64" | base64 -d > /app/.env
  chmod 600 /app/.env
else
  log "no base64 provided and /app/.env not found; continuing without it"
fi

# Start the backend in the background
dotnet EduchemLP.Server.dll &

# Start the frontend (vite preview) in the background
cd /app/client
npm run preview -- --port 3154 --host 0.0.0.0 &

# Start Nginx
nginx -g 'daemon off;'
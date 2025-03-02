#!/bin/sh

# Start the backend in the background
dotnet 4tense.Server.dll &

# Start the frontend (vite preview) in the background
cd /app/client
npm run preview -- --port 3154 --host 0.0.0.0 &

# Start Nginx
nginx -g 'daemon off;'
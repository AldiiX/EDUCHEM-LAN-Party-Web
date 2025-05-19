# Define the user ID (default value 1000)
ARG APP_UID=1000

# Base stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app

# Stage to install Node.js
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS with-node
RUN apt-get update && apt-get install -y curl
RUN curl -sL https://deb.nodesource.com/setup_22.x | bash && apt-get install -y nodejs

# Stage to build the backend
FROM with-node AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["EduchemLP.Server/EduchemLP.Server.csproj", "EduchemLP.Server/"]
COPY ["educhemlp.client/educhemlp.client.esproj", "educhemlp.client/"]
RUN dotnet restore "./EduchemLP.Server/EduchemLP.Server.csproj"
COPY . .
WORKDIR "/src/EduchemLP.Server"

# vytvoreni prazdnyho .env souboru v tomto direktory pokud .env neexistuje
RUN if [ ! -f ".env" ]; then touch .env; fi

# buildnuti backendu
RUN dotnet build "./EduchemLP.Server.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Stage to publish the backend
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./EduchemLP.Server.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Final stage
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Switch to root to install packages
USER root
RUN apt-get update && apt-get install -y curl nginx
RUN curl -sL https://deb.nodesource.com/setup_22.x | bash && apt-get install -y nodejs

# Copy frontend files and fix permissions
COPY ["educhemlp.client/", "/app/client/"]
RUN chown -R $APP_UID:$APP_UID /app/client
WORKDIR /app/client

# Set npm cache and install dependencies
RUN npm config set cache /app/.npm
RUN npm install --unsafe-perm

# Build the frontend
RUN npm run build

# Copy Nginx configuration
COPY nginx.conf /etc/nginx/nginx.conf

# Switch back to non-privileged user
#USER $APP_UID

# Prepare the start script
WORKDIR /app
EXPOSE 80
COPY --chmod=0755 start.sh .
CMD ["./start.sh"]
# define the user ID (default value 1000)
#ARG APP_UID=1000

# base stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app

# stage to install node.js (will be the parent for build)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS with-node
RUN apt-get update && apt-get install -y curl
RUN curl -sL https://deb.nodesource.com/setup_24.x | bash && apt-get install -y nodejs

# turn off npm lifecycle scripts everywhere downstream (prevents postinstall => patch-package)
ENV npm_config_ignore_scripts=true
ENV NPM_CONFIG_IGNORE_SCRIPTS=true

# stage to build the backend
FROM with-node AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# copy csproj and esproj first to leverage layer cache
COPY ["EduchemLP.Server/EduchemLP.Server.csproj", "EduchemLP.Server/"]
COPY ["educhemlp.client/educhemlp.client.esproj", "educhemlp.client/"]

# restore backend (ms javascript sdk will run npm install for client during build; ignore-scripts is already set)
RUN dotnet restore "./EduchemLP.Server/EduchemLP.Server.csproj"

# copy the rest of the source
COPY . .

# switch to server project
WORKDIR "/src/EduchemLP.Server"

# create empty .env if not present
RUN if [ ! -f ".env" ]; then touch .env; fi

# build backend (npm install invoked by msbuild will not run postinstall due to ignore-scripts)
RUN dotnet build "./EduchemLP.Server.csproj" -c $BUILD_CONFIGURATION -o /app/build

# stage to publish the backend
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./EduchemLP.Server.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# final stage
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# keep npm scripts disabled here as well (for explicit frontend install below)
ENV npm_config_ignore_scripts=true
ENV NPM_CONFIG_IGNORE_SCRIPTS=true

# switch to root to install packages
USER root
RUN apt-get update && apt-get install -y curl nginx
RUN curl -sL https://deb.nodesource.com/setup_24.x | bash && apt-get install -y nodejs

# copy frontend files and fix permissions
COPY ["educhemlp.client/", "/app/client/"]
RUN chown -R $APP_UID:$APP_UID /app/client
WORKDIR /app/client

# set npm cache and install dependencies without running postinstall
RUN npm config set cache /app/.npm
# prefer ci; fallback to install if lockfile is missing
RUN npm ci --ignore-scripts || npm install --ignore-scripts

# build the frontend (explicit script call is allowed; ignore-scripts affects only lifecycle hooks)
RUN npm run build

# copy nginx configuration
WORKDIR /app
COPY nginx.conf /etc/nginx/nginx.conf

# expose http
EXPOSE 80

# copy start script
COPY --chmod=0755 start.sh .

# default command
CMD ["./start.sh"]
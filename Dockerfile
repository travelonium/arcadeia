ARG VERSION=8.0
ARG NODEJS_VERSION=20
ARG DISTRO=bullseye-slim

FROM mcr.microsoft.com/dotnet/sdk:${VERSION} AS builder
ARG NODEJS_VERSION
ARG TARGETARCH
WORKDIR /root/
COPY ./ ./
RUN set -eux; \
    apt-get update; \
    apt-get -y install curl gnupg build-essential python3; \
    curl -sLo /tmp/nsolid_setup_deb.sh https://deb.nodesource.com/nsolid_setup_deb.sh; \
    chmod 500 /tmp/nsolid_setup_deb.sh; \
    /tmp/nsolid_setup_deb.sh ${NODEJS_VERSION}; \
    apt-get -y install nodejs; \
    node --version; \
    npm version; \
    dotnet restore -a $TARGETARCH; \
    dotnet publish -a $TARGETARCH --no-restore --configuration Release -o /app;

FROM mcr.microsoft.com/dotnet/aspnet:${VERSION}
ARG VERSION
ARG TARGETARCH
ENV DEBIAN_FRONTEND=noninteractive
LABEL org.opencontainers.image.architecture=$TARGETARCH
RUN dpkg --print-architecture;
RUN set -eux; \
    apt-get update; \
    apt-get install -y python3-full python3-pip iputils-ping net-tools curl xz-utils sqlite3 cifs-utils nfs-common; \
    curl -SL "https://johnvansickle.com/ffmpeg/releases/ffmpeg-release-$(dpkg --print-architecture)-static.tar.xz" -o /tmp/ffmpeg-release.tar.xz; \
    tar -xvf /tmp/ffmpeg-release.tar.xz -C /tmp/; \
    cd /tmp/$(ls -l /tmp/ | grep ^d | grep 'ffmpeg-' | awk '{print $9}' | head -n 1); \
    cp ffmpeg ffprobe qt-faststart /usr/bin/; \
    pip install --break-system-packages -U "yt-dlp[default]"; \
    rm -rf /var/lib/apt/lists/*; \
    mkdir -p /Network /Uploads; \
    dpkg -l; \
    ffmpeg -version; \
    yt-dlp --version;

COPY --from=builder /app /var/lib/app/
COPY entrypoint.sh /

EXPOSE 8080
ENTRYPOINT ["/entrypoint.sh"]
CMD ["-D", "FOREGROUND"]

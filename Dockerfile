ARG VERSION=6.0
ARG NODEJS_VERSION=20
ARG DISTRO=bullseye-slim

FROM mcr.microsoft.com/dotnet/sdk:${VERSION}-${DISTRO} AS builder
ARG NODEJS_VERSION
WORKDIR /root/
COPY ./ ./
RUN set -eux; \
    apt-get update; \
    apt-get -y install curl gnupg build-essential python; \
    curl -sLo /tmp/nsolid_setup_deb.sh https://deb.nodesource.com/nsolid_setup_deb.sh; \
    chmod 500 /tmp/nsolid_setup_deb.sh; \
    /tmp/nsolid_setup_deb.sh ${NODEJS_VERSION}; \
    apt-get -y install nodejs; \
    node --version; \
    npm version; \
    dotnet restore; \
    dotnet publish --configuration Release;

FROM mcr.microsoft.com/dotnet/aspnet:${VERSION}-${DISTRO}
ENV DEBIAN_FRONTEND noninteractive
ARG VERSION
RUN set -eux; \
    apt-get update; \
    apt-get install -y iputils-ping net-tools ffmpeg sqlite3 cifs-utils nfs-common; \
    rm -rf /var/lib/apt/lists/*; \
    mkdir -p /Network /Uploads; \
    dpkg -l; \
    ffmpeg -version;

COPY --from=builder /root/bin/Release/net${VERSION}/publish /var/lib/app/
COPY entrypoint.sh /

EXPOSE 80 443
ENTRYPOINT ["/entrypoint.sh"]
CMD ["-D", "FOREGROUND"]

ARG VERSION=5.0
ARG DISTRO=bullseye-slim

FROM mcr.microsoft.com/dotnet/sdk:${VERSION}-${DISTRO} AS builder
WORKDIR /root/
COPY ./ ./
RUN set -eux; \
    apt-get update; \
    apt-get -y install curl gnupg build-essential python; \
    curl -fsSL https://deb.nodesource.com/setup_14.x | bash -; \
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
    apt-get install -y iputils-ping net-tools ffmpeg cifs-utils nfs-common; \
    rm -rf /var/lib/apt/lists/*; \
    mkdir -p /Network /Uploads; \
    dpkg -l; \
    ffmpeg -version;

COPY --from=builder /root/bin/Release/net${VERSION}/publish /var/lib/app/
COPY entrypoint.sh /

EXPOSE 80 443
ENTRYPOINT ["/entrypoint.sh"]
CMD ["-D", "FOREGROUND"]

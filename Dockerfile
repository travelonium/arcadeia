FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS builder
WORKDIR /root/
COPY ./ ./
RUN set -eux; \
    find .; \
    apt-get update; \
    apt-get -y install curl gnupg build-essential; \
    curl -sL https://deb.nodesource.com/setup_12.x | bash -; \
    apt-get -y install nodejs; \
    node --version; \
    npm version; \
    dotnet restore; \
    dotnet publish --configuration Release; \
    ls -la;

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1
ENV DEBIAN_FRONTEND noninteractive

RUN set -eux; \
    apt-get update; \
    apt-get install -y apache2 ffmpeg cifs-utils nfs-common; \
    rm -rf /var/lib/apt/lists/*; \
    a2enmod rewrite ssl alias headers deflate proxy proxy_balancer proxy_http proxy_fcgi; \
    mkdir -p /Network /Uploads; \
    dpkg -l; \
    apache2 -v; \
    ffmpeg -version;

COPY --from=builder /root/bin/Release/netcoreapp3.1/publish/ /var/lib/app/
COPY entrypoint.sh /

EXPOSE 80 443
ENTRYPOINT ["/entrypoint.sh"]
CMD ["-D", "FOREGROUND"]

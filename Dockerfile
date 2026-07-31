ARG VERSION=10.0
ARG NODEJS_VERSION=20
ARG FFMPEG_VERSION=8.1.1
ARG WHISPER_VERSION=v1.9.1
ARG WHISPER_MODEL=ggml-small.bin
ARG DISTRO=noble

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:${VERSION}-${DISTRO} AS builder
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
    dotnet restore -a $TARGETARCH ./Arcadeia.csproj; \
    dotnet publish -a $TARGETARCH ./Arcadeia.csproj --no-restore --configuration Release -o /app;

FROM mwader/static-ffmpeg:${FFMPEG_VERSION}-${TARGETARCH} AS ffmpeg

FROM ubuntu:${DISTRO} AS whisper
ARG WHISPER_VERSION
RUN set -eux; \
    apt-get update; \
    apt-get -y install --no-install-recommends git build-essential cmake ca-certificates; \
    git clone --depth 1 --branch ${WHISPER_VERSION} https://github.com/ggml-org/whisper.cpp.git /whisper.cpp; \
    cmake -S /whisper.cpp -B /whisper.cpp/build -DCMAKE_BUILD_TYPE=Release -DBUILD_SHARED_LIBS=OFF; \
    cmake --build /whisper.cpp/build --config Release -j"$(nproc)"; \
    rm -rf /var/lib/apt/lists/*;

FROM mcr.microsoft.com/dotnet/aspnet:${VERSION}-${DISTRO}
ARG VERSION
ARG TARGETARCH
ARG WHISPER_MODEL
ENV DEBIAN_FRONTEND=noninteractive
LABEL org.opencontainers.image.architecture=$TARGETARCH
RUN dpkg --print-architecture;
COPY --from=ffmpeg /ffmpeg /ffprobe /usr/bin/
COPY --from=whisper /whisper.cpp/build/bin/whisper-cli /usr/bin/
RUN set -eux; \
    apt-get update; \
    apt-get install -y --no-install-recommends \
                    curl \
                    sqlite3 \
                    net-tools \
                    iputils-ping \
                    cifs-utils \
                    nfs-common \
                    python3-full \
                    python3-pip \
                    ca-certificates \
                    libgomp1 \
                    software-properties-common; \
    pip install --break-system-packages -U "yt-dlp[default,curl-cffi]"; \
    rm -rf /var/lib/apt/lists/*; \
    mkdir -p /Network /Uploads /usr/share/whisper; \
    curl -sLo /usr/share/whisper/${WHISPER_MODEL} https://huggingface.co/ggerganov/whisper.cpp/resolve/main/${WHISPER_MODEL}; \
    dpkg -l; \
    ffmpeg -version; \
    yt-dlp --version; \
    whisper-cli --version; \
    apt-get clean; \
    rm -rf /var/lib/apt/lists/*;
COPY --from=builder /app /var/lib/app/
COPY entrypoint.sh /

EXPOSE 8080
ENTRYPOINT ["/entrypoint.sh"]
CMD ["-D", "FOREGROUND"]

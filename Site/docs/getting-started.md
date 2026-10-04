---
outline: deep
description: Install Arcadeia with Docker Compose, add your first photos and videos, and troubleshoot common setup problems.
---

# Getting Started

[Explore the demo](https://demo.arcadeia.org/Uploads/) first, or run your own library with the Docker package below. The demo uses shared sample media; use your own instance for personal files.

## Before you start

- Install [Docker Engine on Linux](https://docs.docker.com/engine/install/) or [Docker Desktop on macOS](https://www.docker.com/products/docker-desktop/).
- Use **Docker Compose 2.24.4 or later**. The supplied configuration uses [`!override`](https://docs.docker.com/reference/compose-file/merge/#replace-value), which older versions cannot parse.
- Plan for at least 4 GB RAM and 10 GB free space, plus space for your media. Larger libraries and transcoding need more resources.
- Ensure ports **80 and 443** are available on your host. The bundled proxy publishes both; publishing port 443 alone does not configure HTTPS.

Check your installation:

```bash
docker --version
docker compose version
```

Docker must be running before you start Arcadeia. Docker Desktop includes Compose; Linux users can install the [Compose plugin](https://docs.docker.com/compose/install/linux/).

::: warning Deployment status
Arcadeia does not include built-in authentication. Start on a local or trusted network and review the [deployment guidance](/docs/index.html#deployment-status) before making an instance accessible from the internet.
:::

## Install with Docker

### 1. Download the release package

Run these commands in a terminal on your server. `curl -fL` follows the release redirect and reports a failed download rather than saving an error page as an archive.

```bash
mkdir arcadeia
cd arcadeia
curl -fL https://github.com/travelonium/arcadeia/releases/latest/download/arcadeia.tar.gz -o arcadeia.tar.gz
tar -xzf arcadeia.tar.gz
chmod +x start stop restart logs exec
```

You can also download `arcadeia.tar.gz` from the [latest release](https://github.com/travelonium/arcadeia/releases/latest) in your browser and extract it into a new directory.

The package contains Compose files, proxy configuration and helper scripts. Docker downloads the application and its dependencies; you do not need to install .NET or FFmpeg separately for this setup.

### 2. Start the services

```bash
./start production
```

The script downloads the container images, creates `appsettings.Production.json` if needed, and starts the services in the background. The first start can take longer while images are downloaded.

### 3. Open your library

Open [http://localhost/](http://localhost/) when your browser is on the same machine. From another device on your trusted network, use `http://YOUR-SERVER-IP/`.

## Add your first media

1. Open the **Uploads** folder in the library.
2. Drag a few photos or videos into it, or use the upload button to select files.
3. Wait for upload processing and thumbnail generation to finish.
4. Browse the previews, open a video, and try searching for part of its filename.

Use a small sample first so you can check playback and browsing before importing a larger archive. Videos take longer to process than photos, especially when thumbnails or transcodes are being generated.

Uploads, the media index and thumbnail data use Docker volumes. Keep those volumes when upgrading, and maintain a separate backup of your original media. See [Settings](/docs/settings.html) to connect network storage or enable speech transcription.

## Stop and troubleshoot

Stop the services without removing their persistent volumes:

```bash
./stop
```

If startup fails, inspect service status and logs from the installation directory:

```bash
docker compose -f docker-compose.yml -f docker-compose-production.yml ps
docker compose -f docker-compose.yml -f docker-compose-production.yml logs --tail=100 app solr apache
```

| Symptom | What to check |
| --- | --- |
| Compose rejects `!override` | Upgrade to Compose 2.24.4 or later and use `docker compose`, with a space. |
| Cannot connect to Docker | Start Docker Desktop or the Docker daemon; check your user's Docker permissions. |
| A port is already allocated | Check whether another service is using port 80 or 443. |
| The page is not ready | Check whether `app`, `solr` and `apache` are running and inspect their logs. |
| Thumbnails are missing | Allow processing to finish; check application logs for FFmpeg errors. |
| An upload returns HTTP 413 | Check the request-body limit on any additional reverse proxy in front of Arcadeia. |

Still stuck? [Open an issue](https://github.com/travelonium/arcadeia/issues) with your release version, operating system, Compose version, reproduction steps and relevant logs. Remove credentials and private paths from logs before sharing them.

## Running the application outside Docker

The GitHub `arcadeia.tar.gz` package is a Docker deployment package, not a native executable bundle. Repeating the steps above with `./start` starts containers.

For native builds, the current source targets **.NET 10** and uses FFmpeg, yt-dlp, Solr and a web proxy. The repository's `./start local` mode expects a published `Arcadeia.dll` beside the script and a `docker-compose-local.yml` configuration. It still uses Docker for Solr and the proxy.

Use the [source repository](https://github.com/travelonium/arcadeia) for development and the [testing guide](https://github.com/travelonium/arcadeia/blob/master/TESTING.md) for validation. Prefer the Docker installation above unless you are configuring a native build yourself.

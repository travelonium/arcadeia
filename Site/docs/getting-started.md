---
outline: deep
---

<script setup>
    const tag = __LATEST_TAG__;
</script>

# Getting Started
Ensure all prerequisites are fulfilled before proceeding to the Installation step.

## Prerequisites

Before you begin, ensure that **Docker** and **Docker Compose** are installed on your system.

### Installing Docker
1. **Linux**:
Follow the [Docker Installation Guide](https://docs.docker.com/engine/install/) for your distribution.

2. **macOS**:
Download and install **Docker Desktop** from [Docker Desktop](https://www.docker.com/products/docker-desktop/).

1. **Verify Installation**:
   After installation, check if Docker is running:
   ```bash
   docker --version

### Installing Docker Compose
1. **Linux**:
Install the Docker Compose from [Overview of installing Docker Compose](https://docs.docker.com/compose/install/).

1. **macOS**:
Docker Desktop includes Docker Compose, so no additional installation is required.

1. **Verify Installation**:
   After installation, check if Docker Compose is installed:
   ```bash
   docker compose --version

## Installation (Docker)
1. Download and unpack the latest release package from [GitHub](https://github.com/travelonium/arcadeia/releases/latest):

```bash
mkdir arcadeia
cd arcadeia
wget https://github.com/travelonium/arcadeia/releases/latest/download/arcadeia.tar.gz
tar -xvzf arcadeia.tar.gz
rm arcadeia.tar.gz
chmod +x start stop restart logs exec
```

2. Run **Arcadeia**:
```bash
./start
```

3. Open **Arcadeia** in your browser by navigating to: http://localhost/

## Installation (Local)
1. Download and install the [.NET SDK 8.0](https://dotnet.microsoft.com/en-us/download/dotnet) installer for your operating system and processor architecture.
2. Download and install [FFmpeg](https://ffmpeg.org/download.html) for your operating system and architecture either manually or using a package manager of your choice.
3. Download and install [yt-dlp](https://github.com/yt-dlp/yt-dlp?tab=readme-ov-file#installation) for your operating system and architecture either manually or using a package manager of your choice.
4. Download and unpack the latest release package from [GitHub](https://github.com/travelonium/arcadeia/releases/latest):

```bash
mkdir arcadeia
cd arcadeia
wget https://github.com/travelonium/arcadeia/releases/latest/download/arcadeia.tar.gz
tar -xvzf arcadeia.tar.gz
rm arcadeia.tar.gz
chmod +x start stop restart logs exec
```

2. Run **Arcadeia**:
```bash
./start
```

3. Open **Arcadeia** in your browser by navigating to: http://localhost/

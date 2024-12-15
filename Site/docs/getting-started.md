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

## Installation
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

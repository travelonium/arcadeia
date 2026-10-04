---
outline: deep
description: Learn how Arcadeia organizes your photos and videos, which services it needs, and how to deploy it on a trusted network.
---

# Introduction

Arcadeia is an open-source, self-hosted photo and video library. Dynamic thumbnails preview video moments automatically, so you can recognize clips without opening every file. Search, favorites, network storage and browser playback help you revisit your archive.

[Try the sample library](https://demo.arcadeia.org/Uploads/) or [install with Docker](/docs/getting-started.html). Optional [speech transcription](/docs/settings.html#transcription) makes words spoken in videos searchable.

<video src="./assets/introduction-video-preview.mp4" poster="/media-preview.jpg" title="Arcadeia media library preview" style="width: 100%; height: auto; border-radius: 10px;" controls muted playsinline preload="metadata"></video>

## Deployment status

::: warning

**Arcadeia** is not yet fully tested or optimized for deployment on publicly accessible networks. Users are advised to take the following precautions if considering internet-facing deployments:

- **Authentication and Security**: Ensure that adequate authentication mechanisms are in place to restrict unauthorized access. **Arcadeia** does not include built-in security features for public deployment.
- **Network Configuration**: Use a reverse proxy (e.g., Apache or NGINX) with SSL/TLS encryption for secure connections. A reverse proxy is already employed but does not provide security or encryption out of the box.
- **Firewall and IP Restrictions**: Limit access to trusted IP addresses using a firewall or VPN.
- **Data Sensitivity**: Avoid storing sensitive or personally identifiable information in the system until it is deemed secure.
- **Testing Environment**: Use in local or development environments until further testing is completed.

**The use of Arcadeia in general and on publicly accessible environments is at your own risk.** The copyright holders or development team assume no responsibility among other things for data breaches or security vulnerabilities arising from improper use.
:::

## System Requirements
**Arcadeia** is a self-hosted service and runs on your own server. The following describes the minimum and recommended hardware, software and environment.

### Hardware Requirements
- **Processor**: x86_64 or ARM64 architecture
- **Memory**: Minimum 4 GB RAM (8 GB recommended for large libraries)
- **Storage**: Minimum 10 GB free disk space (additional space required based on media library size)

### Software Requirements

- A Linux Docker host or Docker Desktop on macOS compatible with your hardware and current operating system.
- Docker Compose **2.24.4 or later**, required for the supplied `!override` configuration.
- For native builds, the current source targets **.NET 10**; the Docker image bundles its runtime.

### Network Requirements

The default configuration publishes ports **80 and 443** on the host. HTTPS requires additional certificate and proxy configuration; it is not enabled merely by publishing port 443.

Start with a local or trusted network deployment. Follow the [installation guide](/docs/getting-started.html) to download the package and bring up the services.

### Dependencies
- **Arcadeia Application**:
  - .NET Core Runtime: Bundled in Docker container
- **Search Engine**:
  - Solr: Official Docker image (configured via `docker-compose.yml`)
- **Web Server**:
  - Apache HTTP Server: Official Docker image (configured via `docker-compose.yml`)

# Arcadeia

**Find the video you remember, even when you forgot its filename.**

Arcadeia is an open-source, self-hosted media library for browsing, searching and managing your personal photos and videos in a web browser. **Dynamic thumbnails** preview key moments in videos automatically, so you can recognize a clip without opening each file.

[Try the demo](https://demo.arcadeia.org) · [Installation guide](https://www.arcadeia.org/docs/getting-started.html) · [Website](https://www.arcadeia.org) · [Latest release](https://github.com/travelonium/arcadeia/releases/latest)

![Arcadeia media library with dynamic video thumbnails](https://github.com/user-attachments/assets/36b287d7-5c17-4f9c-a5b3-29dedd02edd9)

## What you can do

- **Browse visually:** animated video thumbnails show key moments without interaction.
- **Search your archive:** find media with search and advanced queries.
- **Import media:** upload files, directories or URLs using drag and drop.
- **Keep favorites close:** mark files and filter your library by favorites.
- **Play videos in the browser:** transcode into web-friendly formats on the fly.
- **Connect network storage:** bring media from network locations into your library.

Built for people with personal media archives and self-hosting setups who want to find and revisit their files. Your library runs on your own server. Arcadeia is licensed under [AGPL-3.0](LICENSE).

## Try Arcadeia

Explore the [demo](https://demo.arcadeia.org), then follow the [Docker installation guide](https://www.arcadeia.org/docs/getting-started.html) to run your own instance. You will need Docker and Docker Compose; hardware requirements are listed below.

Start with a local or trusted network deployment. Review the deployment warning before connecting your own media or exposing an instance to the internet.

## Feedback and contributions

Tried Arcadeia? [Open an issue](https://github.com/travelonium/arcadeia/issues) with what worked, what got in your way, and your operating system and deployment method. For bugs, include reproduction steps and logs with private paths and credentials removed.

Contributions to setup documentation, media browsing and thumbnail behavior are welcome. For substantial changes, open an issue to discuss the approach first. See [TESTING.md](TESTING.md) for development testing instructions. If Arcadeia is useful to you, a GitHub star helps others discover it.

## Deployment status

> [!WARNING]
> **Arcadeia** is not yet fully tested or optimized for deployment on publicly accessible networks. Users are advised to take the following precautions if considering internet-facing deployments:
>
> - **Authentication and Security**: Ensure that adequate authentication mechanisms are in place to restrict unauthorized access. **Arcadeia** does not include built-in security features for public deployment.
> - **Network Configuration**: Use a reverse proxy (e.g., Apache or NGINX) with SSL/TLS encryption for secure connections. A reverse proxy is already employed but does not provide security or encryption out of the box.
> - **Firewall and IP Restrictions**: Limit access to trusted IP addresses using a firewall or VPN.
> - **Data Sensitivity**: Avoid storing sensitive or personally identifiable information in the system until it is deemed secure.
> - **Testing Environment**: Use in local or development environments until further testing is completed.
>
> **The use of Arcadeia in general and on publicly accessible environments is at your own risk.** The copyright holders or development team assume no responsibility among other things for data breaches or security vulnerabilities arising from improper use.
>

## System Requirements
**Arcadeia** is a self-hosted service and runs on your own server. The following describes the minimum and recommended hardware, software and environment.

### Hardware Requirements
- **Processor**: x86_64 or ARM64 architecture
- **Memory**: Minimum 4 GB RAM (8 GB recommended for large libraries)
- **Storage**: Minimum 10 GB free disk space (additional space required based on media library size)

### Software Requirements
- **Operating System**:
  - **Linux**: Ubuntu 20.04+, Debian 10+, CentOS 8+, or similar distributions
  - **macOS**: Version 11.0 (Big Sur) or later with Apple Silicon (M1/M2) or Intel processors
- **Docker**:
  - Version 20.10.0 or later (Docker Desktop for macOS)
- **Docker Compose**:
  - Version 1.29.0 or later

### Network Requirements
- **Ports**:
  - **80**: For Apache HTTP server

### Recommended Environment
- **Virtualization**: Compatible with cloud services like AWS, Azure, and Google Cloud
- **Development Tools**: (for contributors and advanced users)
  - Visual Studio Code
  - Docker CLI

### Dependencies
- **Arcadeia Application**:
  - .NET Core Runtime: Bundled in Docker container
- **Search Engine**:
  - Solr: Official Docker image (configured via `docker-compose.yml`)
- **Web Server**:
  - Apache HTTP Server: Official Docker image (configured via `docker-compose.yml`)

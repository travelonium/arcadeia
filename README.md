# ARCADEIA

**Arcadeia** is a self-hosted, web-based media archiving, browsing, searching and management solution designed to bring order and access
to your personal media storage. It allows you to collect all your media files in one place and find, view and manage your media files on any device.

It also features **Dynamic Thumbnails** that brings your library to life by showing a quick preview of the video files without requiring any interaction whatsoever.

![](https://github.com/user-attachments/assets/36b287d7-5c17-4f9c-a5b3-29dedd02edd9)

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

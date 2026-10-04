---
# https://vitepress.dev/reference/default-theme-home-page
layout: home

hero:
  name: false
  text: "Find the video you remember."
  image: /icon.svg
  tagline: "Browse your photos and videos with animated previews. An open-source media library you host on your own server."

  actions:
    - theme: brand
      text: Try the demo
      link: https://demo.arcadeia.org

    - theme: alt
      text: Install Arcadeia
      link: /docs/getting-started.html

    - theme: alt
      text: GitHub
      link: https://github.com/travelonium/arcadeia

features:
  - title: Recognize videos at a glance
    details: Dynamic thumbnails automatically preview key moments, so you can find a familiar clip without opening every file.

  - title: Search your media archive
    details: Find photos and videos with search and advanced queries, then mark favorites for easy access.

  - title: Bring your media together
    details: Upload files, directories or URLs with drag and drop, and connect network storage locations.

  - title: Watch in your browser
    details: Transcode videos into web-friendly formats on the fly and browse your library from your devices.

  - title: Host it yourself
    details: Run your media library on your own server using Docker and Docker Compose.

  - title: Open source
    details: Explore the source, report issues and contribute improvements. Licensed under AGPL-3.0.
---

## See dynamic thumbnails in action

<video src="./docs/assets/introduction-video-preview.mp4" poster="./docs/assets/introduction-preview.png" title="Arcadeia media browsing and dynamic thumbnail preview" style="width: 100%; border-radius: 10px;" controls muted playsinline preload="metadata"></video>

[Try the demo](https://demo.arcadeia.org) or [install your own instance](/docs/getting-started.html).

Arcadeia is intended for local or trusted network use. It does not include built-in authentication for public deployment. Read the [deployment guidance](/docs/index.html) before exposing an instance to the internet.

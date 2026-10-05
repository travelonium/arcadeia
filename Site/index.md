---
# https://vitepress.dev/reference/default-theme-home-page
layout: home
title: Arcadeia — Self-hosted photo and video library
titleTemplate: false
description: Find photos and videos in your own archive with animated thumbnails, search and browser playback. Try the Arcadeia demo or install with Docker.

hero:
  name: "Find the video you remember."
  text: false
  image: /icon.svg
  tagline: "Browse your photos and videos with animated previews. An open-source media library you host on your own server."

  actions:
    - theme: brand
      text: Try The Demo
      link: https://demo.arcadeia.org/Uploads/

    - theme: alt
      text: Get Started
      link: /docs/getting-started.html

    - theme: alt
      text: GitHub
      link: https://github.com/travelonium/arcadeia

features:
  - title: Recognize videos at a glance
    details: Dynamic thumbnails automatically preview key moments, so you can find a familiar clip without opening every file.

  - title: Search your media archive
    details: Find media by filename, description or advanced query. Optional speech transcription makes spoken words searchable too.

  - title: Bring your media together
    details: Upload files, directories or URLs with drag and drop, and connect network storage locations.

  - title: Watch in your browser
    details: Transcode videos into web-friendly formats on the fly and browse your library from your devices.

  - title: Host it yourself
    details: Run your media library on your own server using Docker and Docker Compose.

  - title: Open source
    details: Explore the source, report issues and contribute improvements. Licensed under AGPL-3.0.
---

Arcadeia is intended for local or trusted network use. It does not include built-in authentication for public deployment. Read the [deployment guidance](/docs/index.html) before exposing an instance to the internet.

## A self-hosted photo and video library

Install Arcadeia with Docker, add a few photos or videos to Uploads, and let it generate previews. Browse clips visually, search for a filename or description, and mark favorites to revisit later.

Want to find a video by something someone said? Enable optional speech transcription in [Settings](/docs/settings.html#transcription) to search its spoken words.

## Help shape Arcadeia

[Report an issue](https://github.com/travelonium/arcadeia/issues) or contribute through [GitHub](https://github.com/travelonium/arcadeia). Feedback on setup, browsing and playback helps improve the next release.


## Common questions

### Is Arcadeia free and open source?

Yes. Arcadeia is free software under the AGPL version 3 or later. You can run it on your own server and explore or contribute to the source on GitHub. Read the [license and legal guide](/docs/legal.html) for distribution terms and media rights.

### How do I install Arcadeia?

Use Docker and Docker Compose on a compatible host, then add a few photos or videos to your library. The [getting started guide](/docs/getting-started.html) covers installation, persistent storage and troubleshooting.

### Can I search for words spoken in videos?

Yes. Optional local speech recognition makes transcribed speech searchable. It is disabled by default; enable it in [transcription settings](/docs/settings.html#transcription). Supported embedded text subtitles can also supply searchable text.

### Can I use Arcadeia as a public media server?

Arcadeia is intended for local or trusted network use and has no built-in user authentication. Review the [deployment guidance](/docs/#deployment-status) before making an instance accessible over the internet.

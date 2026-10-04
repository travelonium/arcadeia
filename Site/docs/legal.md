---
outline: deep
description: Arcadeia's AGPL license, third-party software and models, media rights, warranty and deployment responsibilities.
---

# License & Legal

## Arcadeia license

Arcadeia is free software licensed under the **GNU Affero General Public License, version 3 or any later version** (`AGPL-3.0-or-later`). The repository's [LICENSE](https://github.com/travelonium/arcadeia/blob/master/LICENSE) contains the license text; the source headers specify the “or later” option.

You may use, study, modify and redistribute Arcadeia, including for commercial purposes, subject to the license terms. When distributing covered software, preserve required notices and provide the corresponding source as the license requires. Section 13 additionally requires a modified version supporting remote network interaction to offer its interacting users access to its corresponding source. Read the [full AGPL](https://www.gnu.org/licenses/agpl-3.0.html), particularly sections 4–6 and 13, for the exact conditions; this page is a practical overview, not a replacement license.

The software license does not grant rights to media uploaded, downloaded or indexed with Arcadeia. Your media does not become AGPL-licensed simply because you store it in the application.

## Warranty and liability

Arcadeia is provided without warranty, including implied warranties of merchantability or fitness for a particular purpose, to the extent permitted by applicable law. The warranty disclaimer and limitation of liability are governed by sections 15 and 16 of the AGPL. This page does not expand those terms or remove rights that applicable law preserves.

Back up original media, configuration and persistent data before upgrades or maintenance. Thumbnails and transcripts are generated outputs; recognition accuracy, codec compatibility and availability are not guaranteed.

## Media rights and privacy

Use Arcadeia only with media you have permission to access, store, process and share. Importing a URL with yt-dlp does not grant permission to download or redistribute its content. Source-site terms, copyright, privacy and other applicable requirements may still apply. Preserve any attribution or other conditions attached to third-party media.

Libraries can contain filenames, metadata, captions, searchable transcripts and images of people. Limit access appropriately and obtain any permission required for your intended use. Arcadeia's software license is separate from a media license or permission from people depicted in it.

The public demo is shared. Use your own instance for personal or confidential media. Creator credits shown on demo clips identify their sources; those credits alone do not describe every possible usage restriction.

## Local processing and external connections

Thumbnail generation, FFmpeg processing and whisper.cpp speech recognition run in the application environment. Speech recognition does not require uploading your recording to a hosted transcription service.

External connections can still occur: URL imports contact the media source, missing Whisper models are downloaded from the whisper.cpp model repository on Hugging Face, and installation or upgrades retrieve images and packages. Network mounts contact configured storage servers. These are distinct from local processing; consult the relevant service's terms when using it.

Mount credentials may be stored in configuration and returned by the settings API. Logs can contain media paths and operational details. Protect configuration files, API access, backups and logs, and remove sensitive details before sharing diagnostics.

## Deployment responsibilities

Arcadeia is intended for local or trusted network use and does not provide built-in user authentication. Read-only API flags do not protect every service or establish user identity. Publishing ports does not configure TLS or access controls.

Before exposing a deployment, secure the full application and its supporting services. Follow the [deployment guidance](/docs/index.html#deployment-status) and [settings reference](/docs/settings.html#security). Operators are responsible for access controls, backups, updates and their handling of media and personal information.

## Third-party software and models

Dependencies retain their own licenses and notices. The following are principal components, not an exhaustive dependency or redistribution compliance inventory. The application’s **Settings → About** page lists additional libraries and identifies the running build; package manifests, lockfiles and the Dockerfile identify its dependencies.

| Component | Role | License information |
| --- | --- | --- |
| [FFmpeg](https://ffmpeg.org/) | Metadata, previews and playback transcoding | LGPL or GPL depending on build options and included libraries. Review the [FFmpeg legal page](https://ffmpeg.org/legal.html) and the exact binary's license/configuration. The Dockerfile uses `mwader/static-ffmpeg`. |
| [whisper.cpp](https://github.com/ggml-org/whisper.cpp) | Local speech recognition | [MIT](https://github.com/ggml-org/whisper.cpp/blob/master/LICENSE). |
| [OpenAI Whisper models](https://github.com/openai/whisper) | Speech recognition weights | OpenAI releases its Whisper code and model weights under [MIT](https://github.com/openai/whisper#license). Review the license of any alternative model you select. |
| [yt-dlp](https://github.com/yt-dlp/yt-dlp) | URL imports | Its source and PyPI distribution use the Unlicense; dependencies and bundled release executables may have different terms. See [upstream licensing](https://github.com/yt-dlp/yt-dlp#licensing). Arcadeia installs it with dependencies through pip. |
| [Apache Solr](https://solr.apache.org/) | Search index | [Apache License 2.0](https://www.apache.org/licenses/LICENSE-2.0), plus dependencies and notices. |
| [Apache HTTP Server](https://httpd.apache.org/) | Reverse proxy | [Apache License 2.0](https://www.apache.org/licenses/LICENSE-2.0), plus dependencies and notices. |
| [.NET and ASP.NET Core](https://github.com/dotnet/aspnetcore) | Application runtime | See upstream license and third-party notices for the runtime distribution. |
| [Magick.NET](https://github.com/dlemstra/Magick.NET) / [ImageMagick](https://imagemagick.org/) | Image processing | Magick.NET uses Apache-2.0; ImageMagick and its delegates have their own [license information](https://imagemagick.org/script/license.php). |
| [SQLite](https://sqlite.org/) | Thumbnail storage | SQLite is [public domain](https://sqlite.org/copyright.html); its .NET provider has separate terms. |

When redistributing binaries or container images, preserve applicable licenses and notices and satisfy corresponding-source requirements for the versions actually included. Codec patent questions can depend on usage and jurisdiction; FFmpeg's license alone does not settle them. This page does not certify a particular redistribution as compliant.

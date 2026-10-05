/*
 *  Copyright © 2024 Travelonium AB
 *
 *  This file is part of Arcadeia.
 *
 *  Arcadeia is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU Affero General Public License as published
 *  by the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  Arcadeia is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 *  GNU Affero General Public License for more details.
 *
 *  You should have received a copy of the GNU Affero General Public License
 *  along with Arcadeia. If not, see <https://www.gnu.org/licenses/>.
 *
 */

import { defineConfig } from 'vitepress';

const siteUrl = 'https://www.arcadeia.org';

// https://vitepress.dev/reference/site-config

export default defineConfig({
    title: "Arcadeia",
    description: "Arcadeia is an open-source, self-hosted media library for photos and videos. Browse animated video thumbnails, search your archive and watch in your browser.",
    sitemap: {
        hostname: "https://www.arcadeia.org"
    },
    cleanUrls: false,
    lang: 'en-US',
    transformHead({ pageData, siteData }) {
        const pathname = pageData.relativePath === 'index.md'
            ? '/'
            : `/${pageData.relativePath.replace(/index\.md$/, '').replace(/\.md$/, '.html')}`;
        const url = new URL(pathname, siteUrl).href;
        const title = pageData.frontmatter.titleTemplate === false
            ? pageData.title
            : `${pageData.title} | ${siteData.title}`;
        const description = pageData.description || siteData.description;
        const image = `${siteUrl}/media-preview.jpg`;

        const isNotFound = pageData.relativePath === '404.md';
        const graph: Record<string, unknown>[] = [
            {
                '@type': 'WebSite', '@id': `${siteUrl}/#website`,
                url: `${siteUrl}/`, name: 'Arcadeia', inLanguage: 'en-US',
                description: siteData.description,
            },
            {
                '@type': 'WebPage', '@id': `${url}#webpage`, url, name: title,
                description, inLanguage: 'en-US',
                isPartOf: { '@id': `${siteUrl}/#website` },
            },
        ];
        if (pathname === '/') {
            graph.push({
                '@type': 'SoftwareApplication', '@id': `${siteUrl}/#software`,
                name: 'Arcadeia', url: `${siteUrl}/`, description,
                applicationCategory: 'MultimediaApplication',
                operatingSystem: 'Linux; macOS with Docker Desktop',
                isAccessibleForFree: true,
                license: 'https://www.gnu.org/licenses/agpl-3.0.html',
                downloadUrl: 'https://github.com/travelonium/arcadeia/releases',
                screenshot: image,
                offers: { '@type': 'Offer', price: 0, priceCurrency: 'USD', url: `${siteUrl}/docs/getting-started.html` },
                featureList: ['Animated video thumbnails', 'Photo and video search', 'Browser playback', 'Optional local speech transcription'],
            });
        } else if (pathname.startsWith('/docs/')) {
            graph.push({
                '@type': 'BreadcrumbList',
                itemListElement: [
                    { '@type': 'ListItem', position: 1, name: 'Arcadeia', item: `${siteUrl}/` },
                    { '@type': 'ListItem', position: 2, name: 'Documentation', item: `${siteUrl}/docs/` },
                    ...(pathname === '/docs/' ? [] : [{ '@type': 'ListItem', position: 3, name: pageData.title, item: url }]),
                ],
            });
        }

        return [
            ['meta', { name: 'robots', content: isNotFound ? 'noindex, follow' : 'index, follow, max-image-preview:large' }],
            ...(!isNotFound ? [['script', { type: 'application/ld+json' }, JSON.stringify({ '@context': 'https://schema.org', '@graph': graph }).replace(/</g, '\\u003c')]] : []),
            ['link', { rel: 'canonical', href: url }],
            ['meta', { property: 'og:type', content: 'website' }],
            ['meta', { property: 'og:locale', content: 'en_US' }],
            ['meta', { property: 'og:site_name', content: 'Arcadeia' }],
            ['meta', { property: 'og:title', content: title }],
            ['meta', { property: 'og:description', content: description }],
            ['meta', { property: 'og:url', content: url }],
            ['meta', { property: 'og:image', content: image }],
            ['meta', { property: 'og:image:alt', content: 'Arcadeia photo and video library with visual previews' }],
            ['meta', { name: 'twitter:card', content: 'summary_large_image' }],
            ['meta', { name: 'twitter:title', content: title }],
            ['meta', { name: 'twitter:description', content: description }],
            ['meta', { name: 'twitter:image', content: image }],
            ['meta', { name: 'twitter:image:alt', content: 'Arcadeia photo and video library with visual previews' }],
        ];
    },
    themeConfig: {
        siteTitle: false,
        search: { provider: 'local' },
        editLink: {
            pattern: 'https://github.com/travelonium/arcadeia/edit/master/Site/:path',
            text: 'Improve this page on GitHub'
        },
        logo: "/logo-full.svg",

        // https://vitepress.dev/reference/default-theme-config

        nav: [
            {
                text: 'Home',
                link: '/'
            },
            {
                text: 'Try The Demo',
                link: 'https://demo.arcadeia.org/Uploads/'
            },
            {
                text: 'Documentation',
                link: '/docs/',
                activeMatch: '/docs'
            }
        ],

        sidebar: [
            {
                text: 'Documentation',
                items: [
                    {
                        text: 'Introduction',
                        link: '/docs/'
                    },
                    {
                        text: 'Getting Started',
                        link: '/docs/getting-started.html'
                    },
                    {
                        text: 'License & Legal',
                        link: '/docs/legal.html'
                    },
                    {
                        text: 'Settings',
                        link: '/docs/settings.html'
                    },
                ]
            }
        ],

        footer: {
            message: 'Licensed Under <a href="https://www.gnu.org/licenses/agpl-3.0.html" target="_blank" rel="noopener noreferrer">AGPL-3.0</a>',
            copyright: `Copyright © 2024–${new Date().getUTCFullYear()} <a href="https://www.travelonium.com" target="_blank" rel="noopener noreferrer">Travelonium AB</a>`
        },

        socialLinks: [
            { icon: 'github', link: 'https://github.com/travelonium/arcadeia' }
        ]
    },

    head: [
        ['link', { rel: 'manifest', href: '/site.webmanifest' }],
        ['link', { rel: 'apple-touch-icon', sizes: '180x180', href: '/apple-touch-icon.png' }],
        ['link', { rel: 'icon', type: 'image/png', sizes: '32x32', href: '/favicon-32x32.png' }],
        ['link', { rel: 'icon', type: 'image/png', sizes: '16x16', href: '/favicon-16x16.png' }],
    ]
})

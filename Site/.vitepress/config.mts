import { defineConfig } from 'vitepress'

// https://vitepress.dev/reference/site-config

export default defineConfig({
    title: "ARCADEIA",
    description: "A self-hosted and web-based media archiving, browsing, searching and management solution",
    cleanUrls: false,
    lang: 'en-US',

    themeConfig: {
        siteTitle: false,
        logo: "/logo-full.svg",

        // https://vitepress.dev/reference/default-theme-config

        nav: [
            {
                text: 'Home',
                link: '/'
            },
            {
                text: 'Documentation',
                link: '/docs',
                activeMatch: '/docs'
            }
        ],

        sidebar: [
            {
                text: 'Documentation',
                items: [
                    {
                        text: 'Getting Started',
                        link: '/docs/getting-started'
                    },
                    {
                        text: 'Configuration',
                        link: '/docs/configuration'
                    },
                    {
                        text: 'License & Legal',
                        link: '/docs/legal'
                    }
                ]
            }
        ],

        footer: {
            message: 'Licensed Under <a href="https://www.gnu.org/licenses/agpl-3.0.html" target="_blank" rel="noopener noreferrer">AGPL-3.0</a>',
            copyright: 'Copyright © 2024 <a href="https://www.travelonium.com" target="_blank" rel="noopener noreferrer">Travelonium AB</a>'
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

import simpleGit from 'simple-git';
import { defineConfig } from 'vitepress'

async function getLatestTag() {
    const git = simpleGit();
    try {
        const tags = await git.tags();
        return tags.latest || 'latest';
    } catch (err) {
        console.error('Error fetching git tags:', err);
        return 'unknown';
    }
}

// https://vitepress.dev/reference/site-config

export default defineConfig({
    title: "ARCADEIA",
    description: "An open-source, self-hosted and web-based media archiving, browsing, searching and management solution",
    sitemap: {
        hostname: "https://www.arcadeia.org"
    },
    cleanUrls: false,
    lang: 'en-US',
    vite: {
        define: {
            __LATEST_TAG__: JSON.stringify(await getLatestTag()),
        },
        ssr: {
            noExternal: ['simple-git'],
        },
    },
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
                    /*{
                        text: 'Configuration',
                        link: '/docs/configuration'
                    },*/
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

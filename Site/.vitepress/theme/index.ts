// https://vitepress.dev/guide/custom-theme

import { h } from 'vue'
import type { Theme } from 'vitepress'
import DefaultTheme from 'vitepress/theme'
import './style.css'

export default {
    extends: DefaultTheme,
    Layout: () => {
        return h(DefaultTheme.Layout, null, {
            // https://vitepress.dev/guide/extending-default-theme#layout-slots
            'home-hero-info-before': () => {
                return h('a', {
                    href: '/',
                    style: 'display: flex; align-items: center;'
                }, [
                    h('img', {
                        src: '/logo.svg',
                        style: 'height: 50px; margin-bottom: 1rem',
                        alt: 'Logo',
                    })
                ])
            },
        })
    },
    enhanceApp({ app, router, siteData }) {
        // ...
    }
} satisfies Theme

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

// https://vitepress.dev/guide/custom-theme

import { h } from 'vue'
import type { Theme } from 'vitepress'
import DefaultTheme from 'vitepress/theme'
import './style.css'
import previewVideo from '../../docs/assets/introduction-video-preview.mp4'

export default {
    extends: DefaultTheme,
    Layout: () => {
        return h(DefaultTheme.Layout, null, {
            'home-features-before': () => h('section', {
                class: 'home-preview',
                'aria-labelledby': 'preview-title'
            }, [
                h('div', { class: 'home-preview-content' }, [
                    h('h2', { id: 'preview-title' }, 'See dynamic thumbnails in action'),
                    h('video', {
                        src: previewVideo,
                        poster: '/media-preview.jpg',
                        title: 'Arcadeia media browsing and dynamic thumbnail preview',
                        controls: true,
                        muted: true,
                        playsinline: true,
                        preload: 'metadata'
                    }),
                    h('p', [
                        'Explore sample clips featuring wildlife, landscapes and people in the ',
                        h('a', { href: 'https://demo.arcadeia.org/Uploads/' }, 'demo library'),
                        ', or ',
                        h('a', { href: '/docs/getting-started.html' }, 'install your own instance'),
                        '. The demo is shared; use your own instance for personal media.'
                    ])
                ])
            ]),
            // https://vitepress.dev/guide/extending-default-theme#layout-slots
            'home-hero-info-before': () => {
                return h('a', {
                    href: '/',
                    style: 'display: flex; align-items: center;'
                }, [
                    h('img', {
                        src: '/logo.svg',
                        style: 'height: 50px; margin-bottom: 1rem',
                        alt: 'Arcadeia',
                    })
                ])
            },
        })
    },
    enhanceApp({ app, router, siteData }) {
        // ...
    }
} satisfies Theme

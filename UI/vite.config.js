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

import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import { VitePWA } from 'vite-plugin-pwa';
import removeConsole from 'vite-plugin-remove-console';

const { env } = process;

const solr = 'http://localhost';
const target = env.ASPNETCORE_HTTPS_PORT
  ? `https://localhost:${env.ASPNETCORE_HTTPS_PORT}`
  : env.ASPNETCORE_URLS
    ? env.ASPNETCORE_URLS.split(';')[0]
    : 'http://localhost:31735';

export default defineConfig({
  plugins: [
    react(),
    removeConsole({
      includes: ['trace', 'debug', 'group', 'groupCollapsed', 'groupEnd']
    }),
    VitePWA({
      registerType: 'autoUpdate',
      manifest: false,
      outDir: 'build',
      workbox: {
        sourcemap: true,
        globPatterns: ['**/*.{js,css,html,woff,woff2,png,svg}'],
        clientsClaim: true, // makes new SW take control immediately
        skipWaiting: true, // forces SW activation without waiting
      },
      devOptions: {
        enabled: true
      }
    }),
  ],
  base: '/',
  build: {
    outDir: 'build',  // match the output directory .NET Core expects
  },
  server: {
    proxy: {
      '/api': {
        target: target,
        changeOrigin: true,
        secure: false,
        headers: {
          Connection: 'Keep-Alive, Upgrade'
        }
      },
      '/solr': {
        target: solr,
        changeOrigin: true,
        secure: false,
        headers: {
          Connection: 'Keep-Alive, Upgrade'
        }
      },
      '/signalr': {
        target: target,
        changeOrigin: true,
        secure: false,
        ws: true,
        headers: {
          Connection: 'Keep-Alive, Upgrade'
        }
      }
    }
  }
});
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

const target = env.ASPNETCORE_HTTPS_PORT
  ? `https://localhost:${env.ASPNETCORE_HTTPS_PORT}`
  : env.ASPNETCORE_URLS
    ? env.ASPNETCORE_URLS.split(';')[0]
    : 'http://localhost:31735';

export default defineConfig({
  plugins: [
    react(),
    removeConsole({
      includes: ['trace', 'debug', 'log', 'group', 'groupEnd']
    }),
    VitePWA({
      registerType: 'autoUpdate',
      strategies: 'injectManifest',
      srcDir: 'src',
      filename: 'service-worker.js',
      workbox: {
        globPatterns: ['**/*.{js,css,html,woff,woff2,png,svg}']
      }
    }),
  ],
  base: '/',
  build: {
    outDir: 'build',  // Match the output directory .NET Core expects
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
        target: target,
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
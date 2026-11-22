/// <reference types="vitest/config" />
import react from '@vitejs/plugin-react';
import { fileURLToPath } from 'node:url';
import { defineConfig, loadEnv } from 'vite';

// Keep in sync with "paths" in tsconfig.app.json.
const aliases = {
  '@components': 'src/common/components',
  '@ui': 'src/common/components/ui',
  '@hooks': 'src/common/hooks',
  '@models': 'src/common/models',
  '@pages': 'src/common/pages',
  '@services': 'src/common/services',
  '@shared': 'src/common/shared',
  '@states': 'src/common/states',
  '@widgets': 'src/common/widgets',
  '@core': 'src/core',
};

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), '');

  // The API's https launch profile; override for the http profile or a container.
  const target = env.VITE_DEV_PROXY_TARGET || 'https://localhost:7273';

  return {
    plugins: [react()],
    resolve: {
      alias: Object.entries(aliases).map(([find, path]) => ({
        find,
        replacement: fileURLToPath(new URL(path, import.meta.url)),
      })),
    },
    server: {
      port: 3000,
      strictPort: true,
      proxy: {
        '/api': { target, changeOrigin: true, secure: false },
        '/hubs': { target, changeOrigin: true, secure: false, ws: true },
      },
    },
    preview: {
      port: 3000,
      strictPort: true,
    },
    test: {
      environment: 'node',
      include: ['src/**/*.test.ts'],
    },
  };
});

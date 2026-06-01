import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import path from 'path';

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [react()],
  // При сборке (npm run build) файлы попадают прямо в wwwroot бэкенда
  build: {
    outDir: path.resolve(__dirname, '../ServiceCompany.Api/wwwroot'),
    emptyOutDir: true,
  },
  server: {
    port: 3000,
    proxy: {
      '/api': {
        target: 'http://localhost:5265',
        changeOrigin: true,
        secure: false,
      }
    }
  }
});

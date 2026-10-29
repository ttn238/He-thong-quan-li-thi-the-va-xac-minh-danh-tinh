import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import tailwindcss from '@tailwindcss/vite';

export default defineConfig({
    plugins: [react(), tailwindcss()],
    server: {
        port: 5173,
        proxy: {
            '/api': 'http://localhost:5000',
            // Dịch vụ AI (FastAPI, cổng 8000): /ai/v1/... -> http://localhost:8000/v1/...
            '/ai': {
                target: 'http://localhost:8000',
                changeOrigin: true,
                rewrite: (p) => p.replace(/^\/ai/, ''),
            },
        },
    },
});
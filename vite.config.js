import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import { viteSingleFile } from 'vite-plugin-singlefile';

// viteSingleFile inlines all JS/CSS into dist/index.html, so the built game
// runs offline by double-clicking the file — no server needed at the booth.
export default defineConfig({
  plugins: [react(), viteSingleFile()],
  base: './',
});

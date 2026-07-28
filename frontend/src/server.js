const express = require('express');
const { createProxyMiddleware, fixRequestBody } = require('http-proxy-middleware');
const path = require('path');

const app = express();
const PORT = process.env.PORT || 3000;
const BACKEND_URL = process.env.BACKEND_URL || 'http://backend:8080';

app.use(express.json());
app.use(express.urlencoded({ extended: true }));

// 1. PROXY: Apenas para requisições que começam com /api
app.use(
  '/api',
  createProxyMiddleware({
    target: BACKEND_URL,
    changeOrigin: true,
    // Garante que o /api NÃO seja removido ao enviar para o C#
    pathRewrite: (pathStr, req) => `/api${pathStr}`,
    on: {
      proxyReq: (proxyReq, req, res) => {
        console.log(`[Proxy Outbound]: ${req.method} -> ${BACKEND_URL}${proxyReq.path}`);
        fixRequestBody(proxyReq, req);
      },
      error: (err, req, res) => {
        console.error('[Proxy Error]:', err.message);
        res.status(502).json({ message: 'Erro de conexão com o Backend.' });
      }
    }
  })
);

// 2. ESTÁTICOS: Servir arquivos da pasta public (index.html, app.js, style.css)
app.use(express.static(path.join(__dirname, 'public')));

// 3. FALLBACK: Garante que a raiz abra o index.html
app.get('*', (req, res) => {
  res.sendFile(path.join(__dirname, 'public', 'index.html'));
});

app.listen(PORT, () => {
  console.log(`🚀 BFF rodando na porta ${PORT}`);
});
# --- ESTÁGIO 1: Build (Opcional, caso tenha dependências do Node) ---
FROM node:20-alpine AS builder
WORKDIR /app
COPY package*.json ./
# Descomente a linha abaixo se realmente precisar instalar dependências no front
# RUN npm install --omit=dev
COPY . .

# --- ESTÁGIO 2: Servidor de Produção com Nginx ---
FROM nginx:alpine

# Remove o arquivo de configuração padrão do Nginx
RUN rm -rf /etc/nginx/conf.d/default.conf

# Copia o nosso arquivo de configuração customizado (default.conf)
COPY default.conf /etc/nginx/conf.d/

# CORREÇÃO: Copia o conteúdo de src/public diretamente para a pasta pública do Nginx
COPY src/public /usr/share/nginx/html

# O Nginx roda por padrão na porta 80 dentro do container
EXPOSE 80

# Inicia o Nginx
CMD ["nginx", "-g", "daemon off;"]

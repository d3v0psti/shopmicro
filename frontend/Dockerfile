FROM nginx:1.27-alpine

# Remove o site padrão do Nginx
RUN rm -rf /etc/nginx/conf.d/default.conf

# Template processado automaticamente pelo entrypoint oficial do Nginx
# (envsubst nativo, sem precisar de scripts extras) — troca ${BACKEND_UPSTREAM}
# pelo valor real da variável de ambiente no momento em que o container inicia.
COPY templates/default.conf.template /etc/nginx/templates/default.conf.template

# Arquivos estáticos da vitrine (HTML/CSS/JS puro)
COPY src/public /usr/share/nginx/html

# Valor padrão — funciona de imediato em Docker Compose local, onde o
# serviço do backend se chama "backend". Em Kubernetes, sobrescreva essa
# variável de ambiente no Deployment apontando para o Service
# ("http://backend-service:8080").
ENV BACKEND_UPSTREAM=http://backend:8080

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]

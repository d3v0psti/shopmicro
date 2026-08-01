FROM nginx:1.27-alpine

RUN rm -rf /etc/nginx/conf.d/default.conf
COPY templates/default.conf.template /etc/nginx/templates/default.conf.template
COPY src/public /usr/share/nginx/html
ENV BACKEND_UPSTREAM=http://backend:8080
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]

FROM nginx:alpine-slim AS base
COPY nginx.conf /etc/nginx/conf.d/default.conf
WORKDIR /usr/share/nginx/html
COPY dist/frontend .
EXPOSE 8080
ENTRYPOINT ["nginx", "-g", "daemon off;"]

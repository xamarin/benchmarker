FROM alpine:3.3
RUN apk add --update nginx && rm -rf /var/cache/apk/*
EXPOSE 443

COPY nginx.conf /etc/nginx/nginx.conf
COPY server.crt /etc/nginx/certificates/server.crt
COPY server.key /etc/nginx/certificates/server.key

CMD ["nginx", "-g", "daemon off;"]

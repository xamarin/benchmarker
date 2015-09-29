## Nginx proxy server in a box.

This Repository is an example of how to configure a simple nginx proxy server.

It was created specifically to help use nginx reverse proxy when hosting site with my dPanel setup

But you can use it to configure for example a static site or any dynamic site

All you'd need to to is rename and modify the site.conf file for your purposes

the setup_certs.sh file is a helper to create a self-signed ssl certificate.

Edit the Docker file and site.conf if you require sites proxying to 80 port as well

## How to run :

```
git clone https://github.com/paimpozhil/nginx-ssl-reverse-proxy.git 
cd nginx-ssl-reverse-proxy
docker build -t nginxsslproxy .
docker run -d -p 443:443 --link [container name of your actual webserver]:lamp nginxsslproxy 
```

Credits : 

https://github.com/dhrp/docker-nginx-proxy updated/fixed it slightly.

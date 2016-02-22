FROM centurylink/ca-certs
MAINTAINER Mark Probst "mark@xamarin.com"
EXPOSE 8081

WORKDIR /app

# copy binary into image
COPY http-api /app/
COPY benchmarkerCredentials /app/

ENTRYPOINT ["./http-api"]

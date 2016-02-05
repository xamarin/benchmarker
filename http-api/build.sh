#!/bin/bash

env GOOS=linux GOARCH=amd64 go build -o http-api *.go
docker build -t xamarin/performance/http-api .

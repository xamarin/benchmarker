FROM ubuntu:14.04

ENV TZ=America/Los_Angeles
RUN ln -snf /usr/share/zoneinfo/$TZ /etc/localtime && echo $TZ > /etc/timezone

RUN apt-key adv --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys 3FA7E0328081BFF6A14DA29AA6A19B38D3D831EF
RUN echo "deb http://download.mono-project.com/repo/debian wheezy main" > /etc/apt/sources.list.d/mono-xamarin.list
RUN echo "deb http://download.mono-project.com/repo/debian wheezy-apache24-compat main" >> /etc/apt/sources.list.d/mono-xamarin.list
RUN apt-get update
RUN apt-get install -y \
        git \
        build-essential \
        cmake \
        autoconf \
        libtool \
        automake \
        gettext \
        mono-xbuild \
        ccache \
        libmono-microsoft-build-tasks-v4.0-4.0-cil \
        mono-complete \
        nuget \
        python-dev \
        libffi-dev \
        libssl-dev \
        python-cffi \
        python-openssl \
        python-pip \
        gettext

RUN pip install --upgrade cffi
RUN pip install --upgrade PyOpenSSL
RUN pip install pyasn1 characteristic
RUN pip install service_identity
RUN pip install Twisted==16.1.0
RUN pip install buildbot==0.8.12

# web status of buildbot
EXPOSE 8010
# pb interface (used to connect slaves with master)
EXPOSE 9989
# pb changes interface
EXPOSE 9999

CMD (test -d /ebs || (echo "/ebs must be mounted" && exit 1)) && \
    (test -d /ebs/pbot-master || git clone https://github.com/xamarin/benchmarker/ /ebs/pbot-master) && \
    (test -f /ebs/pbot-master/performancebot/buildbot.tac || buildbot create-master /ebs/pbot-master/performancebot) && \
    (buildbot start --nodaemon /ebs/pbot-master/performancebot || echo "WARNING!!! for first deployment 'make ec2-deploy' is required to run on your machine")

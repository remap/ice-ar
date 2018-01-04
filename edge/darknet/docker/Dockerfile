FROM nvidia/cuda:9.0-devel-ubuntu16.04
LABEL maintainer "Peter Gusev <peter@remap.ucla.edu>"

RUN apt-get update
RUN apt-get install -y git libopencv-dev wget

# cmake needed by nanomsg
RUN wget https://cmake.org/files/v3.10/cmake-3.10.1.tar.gz && tar -xzf cmake-3.10.1.tar.gz
RUN cd /cmake-3.10.1 && ./bootstrap && make && make install

RUN cd / && git clone https://github.com/nanomsg/nanomsg.git
RUN cd /nanomsg && ./configure && make && make install

RUN cd /cmake-3.10.1 && make uninstall

RUN git clone --recursive https://github.com/remap/ice-ar
RUN cd /ice-ar/edge/darknet && make

RUN wget https://pjreddie.com/media/files/yolo.weights
RUN cd / && rm -Rf cmake-* nanomsg && apt-get remove -y wget

ENV LD_LIBRARY_PATH=/usr/local/lib

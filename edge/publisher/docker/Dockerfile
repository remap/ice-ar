FROM ubuntu:16.04
LABEL maintainer "Peter Gusev <peter@remap.ucla.edu>"

RUN  apt-get update \
	&& apt-get install -y git libssl-dev build-essential libboost-all-dev cmake libprotobuf-dev protobuf-compiler libsqlite3-dev

RUN git clone https://github.com/named-data/ndn-cpp.git \
	&& cd ndn-cpp \
	&& ./configure \
	&& make && make install \
	&& rm -Rf /ndn-cpp

RUN git clone https://github.com/named-data/cnl-cpp.git \
	&& cd cnl-cpp \
	&& ./configure \
	&& make && make install \
	&& rm -Rf /cnl-cpp

RUN git clone https://github.com/nanomsg/nanomsg.git \
	&& cd /nanomsg && ./configure && make && make install \
	&& rm -Rf /nanomsg

ARG DEBUG_VER=unknown
RUN DEBUG_VER=${DEBUG_VER} git clone --recursive https://github.com/remap/ice-ar
RUN ldconfig && cd /ice-ar/edge/publisher \
	&& g++ main.cpp ipc-shim.c cJSON.c -o ice-publisher -std=c++11 -lboost_thread -lboost_system -lnanomsg -lndn-cpp -lndn-cpp-tools

RUN apt-get remove -y git cmake protobuf-compiler build-essential

ENV LD_LIBRARY_PATH=$LD_LIBRARY_PATH:/usr/local/lib

ENV BASE_PREFIX=/icear/user
ENV USER_ID=peter
ENV SERVICE=object_recognizer
ENV INPUT=/in/ice-annotations
ENV OUTPUT=/out/ice-annotations

CMD /ice-ar/edge/publisher/ice-publisher $BASE_PREFIX $USER_ID $SERVICE $INPUT $OUTPUT

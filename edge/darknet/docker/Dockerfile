FROM nvidia/cuda:9.0-devel-ubuntu16.04
LABEL maintainer "Peter Gusev <peter@remap.ucla.edu>"

RUN apt-get update
RUN apt-get install -y git libopencv-dev wget

# cmake needed by nanomsg
RUN wget https://cmake.org/files/v3.10/cmake-3.10.1.tar.gz && tar -xzf cmake-3.10.1.tar.gz
RUN cd /cmake-3.10.1 && ./bootstrap && make && make install

RUN git clone https://github.com/nanomsg/nanomsg.git
RUN cd /nanomsg && ./configure && make && make install

RUN cd /cmake-3.10.1 && make uninstall

RUN git clone --recursive https://github.com/remap/ice-ar
RUN cd /ice-ar/edge/darknet && make

RUN cd /ice-ar/edge/darknet && wget https://pjreddie.com/media/files/yolo.weights
RUN rm -Rf cmake-* nanomsg && apt-get remove -y wget

ENV LD_LIBRARY_PATH=/usr/local/lib

ENV INPUT=/tmp/mtcamera
ENV FRAME_WIDTH=320
ENV FRAME_HEIGHT=180
ENV OUTPUT=/tmp/ice-annotations
ENV PREVIEW=/tmp/yolo-out

CMD cd /ice-ar/edge/darknet && ./darknet detector ndnrtc cfg/coco.data cfg/yolo.cfg yolo.weights -input $INPUT -w $FRAME_WIDTH -h $FRAME_HEIGHT -output $OUTPUT -preview $PREVIEW
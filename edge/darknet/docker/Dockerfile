FROM peetonn/ice-ar-base:latest
LABEL maintainer "Peter Gusev <peter@remap.ucla.edu>"

RUN git clone --recursive https://github.com/remap/ice-ar
RUN cd /ice-ar/edge/darknet && make

RUN cd /ice-ar/edge/darknet && wget https://pjreddie.com/media/files/yolo.weights

RUN apt-get remove -y git cmake wget lsb-release python-pip

RUN mkdir /in /out /preview /yolo-data
RUN ln -s /yolo-data /ice-ar/edge/darknet/yolo-data

ENV LD_LIBRARY_PATH=/usr/local/lib

ENV INPUT=/in/mtcamera
ENV FRAME_WIDTH=320
ENV FRAME_HEIGHT=180
ENV OUTPUT=/out/ice-annotations
ENV PREVIEW=/preview/yolo-out
ENV THRESHOLD=0.1
ENV DATA_FILE=cfg/coco.data
ENV YOLO_CFG=cfg/yolo.cfg
ENV YOLO_WEIGHTS=yolo.weights
ENV ENGINE=yolo

CMD cd /ice-ar/edge/darknet && ./darknet detector ndnrtc $DATA_FILE $YOLO_CFG $YOLO_WEIGHTS -input $INPUT -w $FRAME_WIDTH -h $FRAME_HEIGHT -output $OUTPUT -preview $PREVIEW -thresh $THRESHOLD
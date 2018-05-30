#!/bin/bash

. ./cleanup.sh

CONSUMER_IMAGE=peetonn/ice-ar:consumer-v3
YOLO_IMAGE=peetonn/ice-ar:yolo-new
OPENFACE_IMAGE=peetonn/ice-ar:openface-new
OPENPOSE_IMAGE=peetonn/ice-ar:openpose
PUBLISHER_IMAGE=peetonn/ice-ar:publisher-new

RAWVIDEO_VOL=rawvideoVol
JSON_VOL=annotationsVol
DB_VOL=$EDGE_ENV_FOLDER

case "$1" in
	"consumer")
        docker rm consumer1 2>/dev/null
        docker run --name=consumer1 --rm -v /var/run:/var/run -v $HOME/.ndn:/root/.ndn \
            -v $RAWVIDEO_VOL:/out -v $EDGE_ENV_FOLDER/logs:/tmp -v $EDGE_ENV_FOLDER/preview:/preview -ti \
            -e SIGNING_IDENTITY=/`whoami` $CONSUMER_IMAGE
    	;;
    "yolo")
        docker rm yolo1 2>/dev/null
        docker run --runtime=nvidia --rm --name=yolo1 -v $RAWVIDEO_VOL:/in -v $JSON_VOL:/out \
            -v $EDGE_ENV_FOLDER/preview:/preview -ti $YOLO_IMAGE
        ;;
    "openface")
        docker rm openface1 2>/dev/null
        HAS_OPENFACE_TRAINED=`docker images | grep openface-trained`
        if [ -z "${HAS_OPENFACE_TRAINED}" ]; then
            docker run --runtime=nvidia --rm --name=openface-trained -v `pwd`/edge-env/faces:/faces $OPENFACE_IMAGE /train.sh /faces
            docker commit openface-trained ice-ar:openface-trained
            docker rm openface-trained
            docker rmi peetonn/ice-ar:openface
        fi
        docker run --runtime=nvidia --rm --name=openface1 -v $RAWVIDEO_VOL:/in -v $JSON_VOL:/out \
            -v $EDGE_ENV_FOLDER/preview:/preview -it \
            ice-ar:openface-trained /run.sh
        ;;
    "openpose")
        docker rm openpose1 2>/dev/null
        docker run --runtime=nvidia --rm --name=openpose1 -v $RAWVIDEO_VOL:/in -v $JSON_VOL:/out \
            -v $EDGE_ENV_FOLDER/preview:/preview -ti \
            $OPENPOSE_IMAGE
        ;;
    "publisher")
        docker rm publisher 2>/dev/null
        docker run --rm --name publisher -v /var/run:/var/run -v $HOME/.ndn:/root/.ndn -v $JSON_VOL:/in -v $DB_VOL:/out -ti \
        $PUBLISHER_IMAGE
        ;;
    *)
        echo "> unknown argument "$1
        ;;
esac

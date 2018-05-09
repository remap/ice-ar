#!/bin/bash

. ./cleanup.sh

case "$1" in
	"consumer")
        docker rm consumer1 2>/dev/null
        docker run --name=consumer1 --rm -v /var/run:/var/run -v $HOME/.ndn:/root/.ndn \
            -v rawvideoVol:/out -v $EDGE_ENV_FOLDER/logs:/tmp -v $EDGE_ENV_FOLDER/preview:/preview -ti \
            -e SIGNING_IDENTITY=/`whoami` peetonn/ice-ar:consumer-test
    	;;
    "yolo")
		docker rm yolo1 2>/dev/null
		docker run --runtime=nvidia --rm --name=yolo1 -v rawvideoVol:/in -v annotationsVol:/out \
			-v $EDGE_ENV_FOLDER/preview:/preview -ti \
			peetonn/ice-ar:yolo
		;;
	"openface")
		docker rm openface1 2>/dev/null
		HAS_OPENFACE_TRAINED=`docker images | grep openface-trained`
		if [ -z "${HAS_OPENFACE_TRAINED}" ]; then
			docker run --runtime=nvidia --rm --name=openface-trained -v `pwd`/edge-env/faces:/faces peetonn/ice-ar:openface /train.sh /faces
			docker commit openface-trained ice-ar:openface-trained
			docker rm openface-trained
			docker rmi peetonn/ice-ar:openface
		fi
		docker run --runtime=nvidia --rm --name=openface1 -v rawvideoVol:/in -v annotationsVol:/out \
			-v $EDGE_ENV_FOLDER/preview:/preview -it \
			ice-ar:openface-trained /run.sh
		;;
	"openpose")
		docker rm openpose1 2>/dev/null
		docker run --runtime=nvidia --rm --name=openpose1 -v rawvideoVol:/in -v annotationsVol:/out \
			-v $EDGE_ENV_FOLDER/preview:/preview -ti \
			peetonn/ice-ar:openpose
		;;
	"publisher")
		docker rm publisher 2>/dev/null
		docker run --rm --name publisher -v /var/run:/var/run -v $HOME/.ndn:/root/.ndn -v annotationsVol:/in -ti \
			peetonn/ice-ar:publisher
		;;
*)
		echo "> unknown argument "$1
		;;
esac

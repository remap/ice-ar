#!/bin/bash

DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
EDGE_ENV_FOLDER=$DIR/edge-env

if [ -z $2 ]; then
	FRAME_WIDTH=$2
else
	FRAME_WIDTH=320
fi

if [ -z $3 ]; then
	FRAME_HEIGHT=$3
else
	FRAME_HEIGHT=180
fi

case "$1" in
	"consumer")
    	ffplay -f rawvideo -vcodec rawvideo -video_size ${FRAME_WIDTH}x${FRAME_HEIGHT} -pixel_format argb -i $EDGE_ENV_FOLDER/preview/mt-out
    	;;
	"yolo")
    	ffplay -f rawvideo -vcodec rawvideo -video_size ${FRAME_WIDTH}x${FRAME_HEIGHT} -pixel_format bgra -i $EDGE_ENV_FOLDER/preview/yolo-out
    	;;
	"openface")
    	ffplay -f rawvideo -vcodec rawvideo -video_size ${FRAME_WIDTH}x${FRAME_HEIGHT} -pixel_format bgr24 -i $EDGE_ENV_FOLDER/preview/openface-out
    	;;
	"openpose")
    	ffplay -f rawvideo -vcodec rawvideo -video_size ${FRAME_WIDTH}x${FRAME_HEIGHT} -pixel_format bgr24 -i $EDGE_ENV_FOLDER/preview/openpose-out
    	;;
*)
    echo "> unknown argument "$1
    ;;
esac
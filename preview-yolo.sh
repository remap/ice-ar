#!/bin/sh
ffplay -f rawvideo -vcodec rawvideo -s 320x180 -pix_fmt bgra -i /tmp/yolo-out

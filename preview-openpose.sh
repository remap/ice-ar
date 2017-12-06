#!/bin/sh
ffplay -f rawvideo -vcodec rawvideo -s 320x180 -pix_fmt bgr24 -i /tmp/openpose-out

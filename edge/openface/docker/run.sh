#!/bin/bash

# activate torch
. /distro/install/bin/torch-activate

# run recognition
python /ice-ar/edge/openface/main.py \
	--reps=$REPS --labels=$LABELS \
	--torchmodel=$TORCH_MODEL --dlibmodel=$DLIB_MODEL \
	--input=$INPUT --output=$OUTPUT --preview=$PREVIEW \
	--frame-width=$FRAME_WIDTH --frame-height=$FRAME_HEIGHT

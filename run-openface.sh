#!/bin/sh

cd edge/openface
#python main.py --frame-width=480 --frame-height=270 --reps=/home/peter/ice-ar/edge/openface/train/reps/reps.csv --labels=/home/peter/ice-ar/edge/openface/train/reps/labels.csv
python main.py --reps=/home/peter/ice-ar/edge/openface/train/reps/reps.csv \
	--labels=/home/peter/ice-ar/edge/openface/train/reps/labels.csv \
	--torchmodel=/home/peter/openface/models/openface/nn4.small2.v1.t7 \
	--dlibmodel=/home/peter/openface/models/dlib/shape_predictor_68_face_landmarks.dat \
	--input=/tmp/mtcamera \
	--output=/tmp/ice-annotations \
	--preview=/tmp/openface-out

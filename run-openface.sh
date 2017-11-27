#!/bin/sh

cd edge/openface
#python main.py --frame-width=480 --frame-height=270 --reps=/home/peter/ice-ar/edge/openface/train/reps/reps.csv --labels=/home/peter/ice-ar/edge/openface/train/reps/labels.csv
python main.py --reps=/home/peter/ice-ar/edge/openface/train/reps/reps.csv --labels=/home/peter/ice-ar/edge/openface/train/reps/labels.csv

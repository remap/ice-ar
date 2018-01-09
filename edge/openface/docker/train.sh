#!/bin/bash

# 1st argument - folder with subfolders with face images
# each subfolder name - person's name (or ID) which will be used as a label
# for example, this is a possible folder structure for training:
# 	- faces
#		- john
#			pic1.jpg
#			pic2.jpg
#			...
#		- sam
#			pic1.jpg
#			pic2.jpg
#			...
#		- nick
#			pic1.jpg
#			pic2.jpg
#			...

FACES_FOLDER=$1

if [ -z "$FACES_FOLDER" ]
then
	echo "usage: $0 <faces folder>"
	exit 1
else

	echo "> processing faces from ${FACES_FOLDER}..."
	. /distro/install/bin/torch-activate
	/openface/util/align-dlib.py $FACES_FOLDER align outerEyesAndNose /aligned --verbose
	/openface/batch-represent/main.lua -outDir /reps -data /aligned
 	mv /reps/labels.csv /reps/labels.csv.original
 	cat /reps/labels.csv.original | gawk -F',' 'match($2, /\/aligned\/(.*)\/.*/, a) { print a[1] "," $2}' > /reps/labels.csv
fi

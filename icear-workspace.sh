#!/bin/bash

TERM_OPEN="gnome-terminal -e"

function openTerm(){
    x=$1
    y=$2
    w=$3
    h=$4
    cmd1="$5"
    cmd2="$6"

    if [ -z "$cmd2" ]; then
        #--window-with-profile='ice-ar'
        gnome-terminal --geometry="${w}x${h}+${x}+${y}"  -e "/bin/bash -c '${cmd1}'"
    else
        gnome-terminal --geometry="${w}x${h}+${x}+${y}" --tab -e "/bin/bash -c '${cmd1}'" \
            --tab -e "/bin/bash -c '${cmd2}'"
    fi
}

row1y=0
previewRow=350
row2y=700
termW=51
termH=20

# open publisher
openTerm 0 $row2y $termW $termH "while true; do ./run.sh publisher; sleep 1; done;" ""

# open DB
# openTerm 600 $row2y $termW $termH "./run.sh db"
openTerm 600 $row2y $termW $termH "python edge/semantic-db/tornado/run.py" "python edge/semantic-db/nanoreader.py /tmp/dbingest/ice-annotations"

# open yolo and preview
colX=540
openTerm $colX $row1y $termW $termH "./run.sh yolo" "while true; do ./preview.sh yolo; sleep 1; done;"

# open openface and preview
colX=1000
openTerm $colX $row1y $termW $termH "./run.sh openface" "while true; do ./preview.sh openface; sleep 1; done;"

# open openpose and preview
# colX=1500
# openTerm $colX $row1y $termW $termH "./run.sh openpose" "while true; do ./preview.sh openpose ${colX} ${previewRow}; sleep 1; done;"

# open consumer and preview
colX=0
openTerm $colX $row1y $termW $termH "while true; ./run.sh consumer; sleep 1; done;" "while true; do ./preview.sh consumer; sleep 1; done;"

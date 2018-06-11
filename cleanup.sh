#!/bin/bash

DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
EDGE_ENV_FOLDER=$DIR/edge-env

[[ -d $EDGE_ENV_FOLDER ]] || (mkdir -p $EDGE_ENV_FOLDER/preview $EDGE_ENV_FOLDER/logs $EDGE_ENV_FOLDER/faces)

#sudo rm $EDGE_ENV_FOLDER/preview/* 2> /dev/null
sudo rm $EDGE_ENV_FOLDER/logs/* 2> /dev/null
#!/bin/bash


while true; do
	while [ -z "$SINK" ]; do
		SINK=`cat $CONSUMER_CONFIG | gawk ' match($0, /sink.?=.*"(.*)";/, a) { print a[1] }'`
		sleep 0.2
	done;

	while [ -z "$SINKFILE" ]; do
		SINKFILE=`ls ${SINK}* 2>/dev/null`
		sleep 0.5
	done; 

	/ice-ar/edge/misc/nanopipe-adaptor/nanopipe $SINKFILE /preview/mt-out
done;


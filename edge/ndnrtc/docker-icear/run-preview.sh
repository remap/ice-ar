#!/bin/bash


while true; do
	while [ -z "$SINK" ]; do
                SINK=`cat icear-consumer.cfg | awk '/sink\s?=\s?{/,/.*}$/ { print }' | gawk ' match($0, /name.?=.*"(.*)";/, a) { print a[1] }'`
		sleep 0.2
	done;

	while [ -z "$SINKFILE" ]; do
		SINKFILE=`ls ${SINK}* 2>/dev/null`
		sleep 0.5
	done; 

	/ice-ar/edge/ndnrtc/cpp/nanopipe-adaptor $SINKFILE /preview/mt-out
done;


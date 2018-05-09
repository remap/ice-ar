#!/bin/bash

/run-preview.sh 2>&1 > /dev/null &
/ice-ar/edge/ndnrtc/cpp/ndnrtc-client -c $CONSUMER_CONFIG -s $SIGNING_IDENTITY -p $POLICY_FILE -t $RUNTIME -v

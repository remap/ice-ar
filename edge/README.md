# Edge Node Subsystem

The edge node includes the communication plane (using `ndnrtc` and `nfd`) and the computation engine (currently `YOLO`). The computation engine consumes the real-time video frames from the communication plane, runs computations (object recognizations), and publishes context features to the communication plane. 

## Pre-requisite
1. Install `Ubuntu-64bit 14.04 (amd64)`;
2. Install the following packages:

		sudo apt-get install build-essential cmake g++ python-dev autotools-dev libicu-dev build-essential libbz2-dev libboost-all-dev
		sudo apt-get install software-properties-common
		sudo apt-get install libopencv-dev python-opencv
		sudo apt-get install openssl libssl-dev
		sudo apt-get install sqlite3 libsqlite3-dev libprotobuf-dev liblog4cxx10-dev doxygen libboost-all-dev
		sudo apt-get install autotools-dev automake byacc flex binutils
		sudo add-apt-repository ppa:named-data/ppa
		sudo apt-get update
		sudo apt-get install nfd
		sudo apt-get install libconfig-dev libconfig++-dev # For building ndnrtc-client

3. In the `ndnrtc` folder, compile `ndnrtc` by following the instructions here: <https://github.com/remap/ndnrtc/blob/dev/cpp/INSTALL.md>

4. In the `darknet` folder, compile `YOLO`:

		cd darknet
		make

5. Now it's ready to run the edge. 

	(1) Run `YOLO` first:
	
		cd darknet
		./darknet detector ndnrtc cfg/coco.data cfg.yolo.cfg yolo.weights /tmp/frame_fifo -w 320 -h 240
		
	**NOTE:** Please change the video frame width and height accordingly using the `-w` and `-h` parameters. Please do not  change the parameter `/tmp/frame_fifo`
	
	(2) Run `ndnrtc-client` consumers and producers:
	
		cd ndnrtc/cpp
		./ndnrtc-client -c ./sample-producer.cfg -p ./rule.conf -t 300 -s /yuanjie
		./ndnrtc-client -c ./sample-consumer.cfg -p ./rule.conf -t 300 -s /yuanjie
		
	A window will popup to display the recognized frames. Alternatively, you can check `darknet/ndnrtc.png`, which stores the latest frame with recognized objects. 

## Known Issues

- **Performance issue in `YOLO`**: Without GPU acceleration, it takes ~10s for YOLO to process each video frame. 

- **Synchronization between `ndnrtc` and `YOLO`**: For real-time computation, ideally `YOLO` should catch up with frame fetching by `ndnrtc-client`. But without CUDA, it turns out to be impossible. Currently, `ndnrtc` and `YOLO` use a named pipe `/tmp/frame_fifo` for synchronization. But due to the limited size of pipe, if `YOLO` cannot catch up with `ndnrtc`, frame loss would be observed. 



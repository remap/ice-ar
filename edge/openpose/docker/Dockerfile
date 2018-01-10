FROM nvidia/cuda:8.0-devel-ubuntu16.04
LABEL maintainer "Peter Gusev <peter@remap.ucla.edu>"
ARG DEBUG_VER=unknown

RUN apt-get update \
	&& apt-get install -y git cmake sudo wget lsb-release python-pip libopenblas-dev liblapack-dev libboost-all-dev \
	libopencv-dev libatlas-base-dev libgoogle-glog-dev protobuf-compiler libprotobuf-dev libhdf5-serial-dev \
	libleveldb-dev libsnappy-dev liblmdb-dev \
	&& pip2 install --upgrade pip

RUN git clone https://github.com/nanomsg/nanomsg.git \
	&& cd /nanomsg && ./configure && make && make install \
	&& rm -Rf /nanomsg

RUN git clone https://github.com/tonysimpson/nanomsg-python.git \
	&& cd /nanomsg-python \
	&& wget https://raw.githubusercontent.com/remap/ice-ar/master/edge/openface/nn_recvmsg.patch \
	&& git apply --whitespace=nowarn nn_recvmsg.patch \
	&& python setup.py install \
	&& rm -Rf /nanomsg-python

# not using install_cmake.sh as it installs over #3.5GB of stuff and it seems that it screws up installation
# necessary dependencies were installed manually during first step
# && cd /openpose && bash ./ubuntu/install_cmake.sh \

RUN git clone --recursive https://github.com/CMU-Perceptual-Computing-Lab/openpose.git \
	&& wget http://developer.download.nvidia.com/compute/redist/cudnn/v5.1/cudnn-8.0-linux-x64-v5.1.tgz \
	&& tar -xzf cudnn-8.0-linux-x64-v5.1.tgz -C /usr/local \
	&& rm cudnn-8.0-linux-x64-v5.1.tgz && ldconfig \
	&& mkdir -p /openpose/build \
	&& cd /openpose/build \
	&& cmake .. \
	&& make -j$(nproc) \
	&& make install

RUN DEBUG_VER=${DEBUG_VER} git clone --recursive https://github.com/remap/ice-ar \
	&& cd /ice-ar/edge/openpose \
	&& g++ -std=c++11 ice-openpose.cpp ipc-shim.c pipe-utils.c jsonOsstream.cpp \
	  -o ice-openpose \
	  -L/usr/local/lib  -lopenpose -lpthread -lopencv_highgui \
	  -lopencv_objdetect -lopencv_imgproc \
	  -lopencv_core -lgflags -lnanomsg

RUN apt-get remove -y git cmake wget lsb-release python-pip

ENV LD_LIBRARY_PATH=$LD_LIBRARY_PATH:/usr/local/lib

ENV INPUT=/in/mtcamera
ENV FRAME_WIDTH=320
ENV FRAME_HEIGHT=180
ENV OUTPUT=/out/ice-annotations
ENV PREVIEW=/preview/openpose-out

CMD /ice-ar/edge/openpose/ice-openpose -model_folder=/openpose/models -keypoint_scale=3 \
  -frame_width=$FRAME_WIDTH -frame_height=$FRAME_HEIGHT \
  -frame_source=$INPUT -preview=$PREVIEW -annotations_dest=$OUTPUT

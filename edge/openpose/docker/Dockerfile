FROM peetonn/ice-ar-base:latest
LABEL maintainer "Peter Gusev <peter@remap.ucla.edu>"
ARG DEBUG_VER=unknown

RUN git clone --recursive https://github.com/CMU-Perceptual-Computing-Lab/openpose.git
RUN cd /openpose && bash ./ubuntu/install_cmake.sh
RUN mkdir -p /openpose/build && cd /openpose/build && cmake .. && make -j$(nproc) && make install

RUN DEBUG_VER=${DEBUG_VER} git clone --recursive https://github.com/remap/ice-ar
RUN cd /ice-ar/edge/openpose && g++ -std=c++11 ice-openpose.cpp ipc-shim.c pipe-utils.c jsonOsstream.cpp -o ice-openpose -L/usr/local/lib  -lopenpose -lpthread -lopencv_highgui -lopencv_objdetect -lopencv_imgcodecs -lopencv_imgproc -lopencv_core -lgflags -lnanomsg

RUN apt-get remove -y git cmake wget lsb-release python-pip

FROM ubuntu:16.04
LABEL maintainer "Peter Gusev <peter@remap.ucla.edu>"
ARG DEBUG_VER=unknown

RUN  apt-get update \
	&& apt-get install -y libssl-dev libboost-all-dev libprotobuf-dev libsqlite3-dev sudo libconfig++9v5 libconfig++-dev \
	  wget autoconf automake libtool cmake git build-essential lsb-release protobuf-compiler gawk

RUN git clone https://github.com/nanomsg/nanomsg.git \
	&& cd /nanomsg && ./configure && make && make install \
	&& rm -Rf /nanomsg

# NDN-CPP
RUN git clone https://github.com/named-data/ndn-cpp \
	&& cd ndn-cpp \
	&& ./configure --with-std-shared-ptr=no --with-std-function=no \
	&& make && make install \
	&& rm -Rf /ndn-cpp

# OpenFEC
RUN wget http://openfec.org/files/openfec_v1_4_2.tgz \
	&& tar -xvf openfec_v1_4_2.tgz && rm openfec_v1_4_2.tgz \
	&& mkdir -p openfec_v1.4.2/build && cd openfec_v1.4.2/ \
	&& wget https://raw.githubusercontent.com/remap/ndnrtc/master/cpp/resources/ndnrtc-openfec.patch \
	&& patch src/CMakeLists.txt ndnrtc-openfec.patch \
	&& cd build && cmake .. -DDEBUG:STRING=OFF \
	&& make

# This line accepts the Microsoft End User License Agreement allowing use of
#   the MS True Type core fonts 
RUN echo ttf-mscorefonts-installer msttcorefonts/accepted-mscorefonts-eula select true | debconf-set-selections

# WebRTC & NDN-RTC (doing in one step to avoid large image size)
RUN git clone https://chromium.googlesource.com/chromium/tools/depot_tools.git \
	&& export PATH=$PATH:`pwd`/depot_tools \
	&& mkdir webrtc-checkout && cd webrtc-checkout \
	&& fetch --nohooks webrtc \
	&& cd src && ./build/install-build-deps.sh --no-syms --no-prompt \
	&& git checkout -b branch-heads-59 refs/remotes/branch-heads/59 \
	&& gclient sync \
	&& gn gen out/Default --args='is_debug=false' \
	&& ninja -C out/Default \
	&& cd / \
	&& git clone --recursive https://github.com/remap/ice-ar \
	&& cd /ice-ar/edge/ndnrtc/cpp && mkdir -p build/share \
	&& echo 'CPPFLAGS="-DWEBRTC_POSIX" CXXFLAGS="-DWEBRTC_POSIX"' >  build/share/config.site \
	&& echo OPENFECDIR=/openfec_v1.4.2 >> build/share/config.site \
	&& echo WEBRTCDIR=/webrtc-checkout/src >> build/share/config.site \
	&& ./configure --prefix=$(pwd)/build \
	&& make && make install && make ndnrtc-client && make nanopipe-adaptor \
	&& cd /webrtc-checkout/src \
	&& rm -Rf tools-webrtc/ tools/ testing/ resources/ infra/ data/ buildtools/ build_overrides/ build/ base/ .git webrtc/ third_party/

# cleanup
RUN apt-get remove -y wget autoconf automake libtool cmake git build-essential libconfig9 lsb-release protobuf-compiler
RUN mkdir /out /preview

COPY icear-consumer.cfg /
COPY rule.conf /
COPY run-preview.sh /
COPY run.sh /

ENV RUNTIME=10000
ENV SIGNING_IDENTITY=/icear-client
ENV CONSUMER_CONFIG=/icear-consumer.cfg
ENV POLICY_FILE=/rule.conf

CMD /run.sh

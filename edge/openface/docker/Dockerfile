FROM peetonn/ice-ar-base:latest
LABEL maintainer "Peter Gusev <peter@remap.ucla.edu>"

RUN apt-get install -y gawk \
	&& pip2 install numpy dlib pandas scipy scikit-learn scikit-image opencv-python

RUN git clone https://github.com/torch/distro.git --recursive \
	&& export TORCH_NVCC_FLAGS="-D__CUDA_NO_HALF_OPERATORS__"  \
	&& cd /distro \
	&& bash install-deps \
	&&  yes yes | ./install.sh \
	&& . /distro/install/bin/torch-activate \
	&& for NAME in dpnn nn optim optnet csvigo cutorch cunn fblualib torchx tds; do luarocks install $NAME; done

RUN git clone --recursive https://github.com/cmusatyalab/openface.git \
	&& cd /openface \
	&& python setup.py install \
	&& cd /openface/models \
	&& ./get-models.sh

ARG DEBUG_VER=unknown
RUN DEBUG_VER=${DEBUG_VER} git clone --recursive https://github.com/remap/ice-ar
RUN apt-get remove -y git cmake wget lsb-release python-pip

ENV LD_LIBRARY_PATH=$LD_LIBRARY_PATH:/usr/local/lib

ENV INPUT=/in/mtcamera
ENV FRAME_WIDTH=320
ENV FRAME_HEIGHT=180
ENV OUTPUT=/out/ice-annotations
ENV PREVIEW=/preview/openface-out
ENV TORCH_MODEL=/openface/models/openface/nn4.small2.v1.t7
ENV DLIB_MODEL=/openface/models/dlib/shape_predictor_68_face_landmarks.dat
ENV LABELS=/reps/labels.csv
ENV REPS=/reps/reps.csv
ENV TRAIN_FOLDER=/faces

COPY run.sh /
COPY train.sh /
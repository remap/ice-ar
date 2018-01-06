FROM nvidia/cuda:9.0-devel-ubuntu16.04
LABEL maintainer "Peter Gusev <peter@remap.ucla.edu>"

RUN apt-get update
RUN apt-get install -y git python-pip cmake libopenblas-dev liblapack-dev libboost-all-dev sudo wget

RUN pip2 install --upgrade pip
RUN pip2 install numpy dlib pandas scipy scikit-learn scikit-image opencv-python

RUN git clone https://github.com/torch/distro.git --recursive
RUN export TORCH_NVCC_FLAGS="-D__CUDA_NO_HALF_OPERATORS__"
RUN cd /distro && bash install-deps &&  yes yes | ./install.sh && source ~/.bashrc
RUN for NAME in dpnn nn optim optnet csvigo cutorch cunn fblualib torchx tds; do luarocks install $NAME; done

RUN git clone https://github.com/nanomsg/nanomsg.git
RUN cd /nanomsg && ./configure && make && make install
RUN rm -Rf /nanomsg

RUN git clone https://github.com/tonysimpson/nanomsg-python.git
RUN cd /nanomsg-python && python setup.py install
RUN rm -Rf /nanomsg-python

RUN git clone --recursive https://github.com/cmusatyalab/openface.git
RUN cd /openface && python setup.py install
RUN cd /openface/models && ./get-models.sh


RUN git clone --recursive https://github.com/remap/ice-ar

RUN apt-get remove -y cmake wget git

RUN export LD_LIBRARY_PATH=$LD_LIBRARY_PATH:/usr/local/lib

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

CMD /ice-ar/edge/openface/train.sh $TRAIN_FOLDER
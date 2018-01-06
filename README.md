# ICE AR Repository

Current prototype architecture draft:

[prototype architecture draft 07/25](doc/proto-draft-0725.pdf)

## Mobile Terminal

## Edge Node

### Docker
In order to provide easy and quick edge-node deployment, edge node modules were containerized. These modules can (and should) interact with each other in order to provide full edge node functionality.
There are three types of edge node modules:

- **Fetching module**

    Fetching modules implement *input functionality* of an edge node. I.e. fetching module communicates with the outside world (for example, over the network, NDN) and retrieves data from clients who are interested in providing their data for edge node processing. Fetching modules usually provide some kind of data channel (for example, file pipe) for other modules to consume. Thus,  one shall create [volumes](https://docs.docker.com/engine/admin/volumes/volumes/) that can be shared between fetching modules and other modules. Data channels created by fetching modules generally must allow **1-to-Many** data dissemination in order to allow plug-n-play scalability for processing modules. An example fetching module would be a video consumer, that fetches video from a client, decodes it and writes decoded frames into a data channel for processing modules to consume. 
- **Processing module**

    Processing modules implement *business logic* functionality of an edge node. These modules have inputs and outputs: they consume data received by fetching module(s), use/process it according to their logic and output results into some data channel for other modules to use. For example, an example processing module would be a module that detects cats on ARGB images and writes bounding boxes for detected cats to some data channel (file pipe, for example). It is useful, in general, if output data channels support **Many-to-Many** (or at least **Many-to-1**) model, so that other modules are able to use processed data.
- **Publishing module**

    Publishing modules consume data from other (processing) modules and make it available for clients. Publishing modules usually have one input data channel (file pipe) which supports **Many-to-1** synchronization model (in order to allow multiple processing modules to write to it), and have no output data channels, as they provide data for clients by other means (network).

Currently, the following modules are implemented (those in **bold** are currently containerized):

- Mobile Terminal Video fetching module

    *This module fetches video (over NDN) from a *Mobile Terminal* (mobile video producer), decodes and writes it frame by frame (ARGB format) into a unix socket (powered by [nanomsg](http://nanomsg.org/)).*
- **YOLO processing module**

    *This module consumes raw video frame by frame from a unix socket and processes it by GPU-accelerated object recognition software [YOLO](https://pjreddie.com/darknet/yolo/). Resulting information is formatted as JSON dictionary and written into another unix socket.* 
- OpenFace processing module

    *This module consumes raw video frame by frame from a unix socket and processes it by GPU-accelerated face recognition software [OpenFace](https://cmusatyalab.github.io/openface/). Resulting information is formatted as JSON dictionary and written into another unix socket.*
- OpenPose processing module

    *This module consumes raw video frame by frame from a unix socket and processes it by GPU-accelerated pose recognition software [OpenPose](https://github.com/CMU-Perceptual-Computing-Lab/openpose). Resulting information is formatted as JSON dictionary and written into another unix socket.* 
- Annotations publishing module

   This module consumer JSON arrays from a unix socket and makes this information available over the network (NDN) for clients.

The diagram below shows how these modules interoperate:

![edge node containerization](doc/containerization.png)

#### Mobile Terminal Video fetching
*TBD*

#### Yolo processing

```
 docker run --runtime=nvidia -it --name yolo -a stdout -v /tmp:/in -v /tmp:/out -v /tmp:/preview peetonn/ice-ar:yolo
```

#### OpenFace processing
*TBD*

#### OpenPose processing
*TBD*

#### Annotations publishing
*TBD*

## Content Provider

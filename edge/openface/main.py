#!/usr/bin/python

# 
# main.py
# This script uses openface for detecting faces on ARGB frames, delivered through a nanomsg pipe
#

import sys
import getopt
import os
import openface
import cv2
import numpy as np
from nanomsg import *
import _nanomsg_ctypes as nnc
import struct
import datetime
import json
import pandas
import sklearn
from sklearn import preprocessing
from sklearn import svm

dlibModelPath = '/home/peter/openface/models/dlib/shape_predictor_68_face_landmarks.dat'
torchModelPath = '/home/peter/openface/models/openface/nn4.small2.v1.t7'

dlibObject = None
dumpSocket = None
labelEncoder = None
svc = None
torchNn = None
imagePipeName = '/tmp/openface-out'
imagePipe = None

def dumpImage(img):
    global imagePipe, imagePipeName
    if not imagePipe:
        if not os.path.exists(imagePipeName):
            os.mkfifo(imagePipeName, 0644)
        imagePipe = os.open(imagePipeName, os.O_WRONLY)
    else:
        os.write(imagePipe, img.tobytes())

def drawBox(img, rect, label):
    cv2.rectangle(img, (rect.left(), rect.top()), (rect.right(), rect.bottom()), (0,0,255), 2)
    cv2.putText(img,label,(rect.left(),rect.top()),cv2.FONT_HERSHEY_SIMPLEX,0.5,(255,255,0),1)

def initClassifier(labelsFile, repsFile):
    global svc, labelEncoder
    reps = pandas.read_csv(repsFile).as_matrix()
    labelsCsv = pandas.read_csv(labelsFile).as_matrix()
    labels = labelsCsv[:,0]
    # labels = [x for i,x in enumerate(tmp) if x not in tmp[0:i]]
    labelEncoder = preprocessing.LabelEncoder().fit(labels)
    transformedLabels = labelEncoder.transform(labels)
    svc = svm.SVC(C=1, kernel="rbf", probability=True, gamma=2)
    svc.fit(reps, transformedLabels)

# reads a frame from nanomsg socket
def readFrame(socket, frameSize):
    timestamp = None
    frameNo = None
    frameName = None
    headerSize = 512
    strLen = headerSize - 8 - 4
    headerBuf, frame, err = nnc.nn_recvmsg(socket, [headerSize, frameSize])
    if not err:
        headerStruct = struct.Struct('Q I %ds' % strLen)
        timestamp, frameNo, frameName = headerStruct.unpack(headerBuf)
    return timestamp, frameNo, frameName, err

def getAnnotationFromRect(r, w, h):
    annotation = {}
    annotation['xleft'] = float(r.left())/float(w)
    annotation['xright'] = float(r.right())/float(w)
    annotation['ytop'] = float(h-r.top())/float(h)        # since we flipped the image
    annotation['ybottom'] = float(h-r.bottom())/float(h)    # since we flipped the image
    annotation['prob'] = 1.
    annotation['label'] = 'a face'

    return annotation

def dumpAnnotations(timestamp, frameNo, frameName, annotations):
    global dumpSocket
    jsonDict = {'playbackNo':frameNo, 'timestamp':timestamp, 'frameName':frameName, 'engine':'openface', 'annotations':annotations}
    s = json.dumps(jsonDict)
    print(" > will send this "+s)
    dumpSocket.send(s)

def runClassifier(img, annotation, rect):
    global dlibObject, torchNn, svc, labelEncoder
    alignedImg = dlibObject.align(96, img, bb=rect, landmarkIndices=openface.AlignDlib.OUTER_EYES_AND_NOSE)
    features = torchNn.forward(alignedImg)
    # need to reshape features, as svc object expects 2d array, while features is 1d array
    predictions = svc.predict_proba(features.reshape(1,-1)).ravel()
    print "predictions "+str(predictions)
    idx = np.argmax(predictions)
    maxLikelihood = predictions[idx]
    label = labelEncoder.inverse_transform(idx)
    if maxLikelihood > 0.5:
        annotation['prob'] = maxLikelihood
        annotation['label'] = label
    return annotation

counter = 0
def processFrames(socket, frameWidth, frameHeight):
    global dlibObject, counter, svc
    frameSize = frameWidth*frameHeight*4 # assuming ARGB

    while True:
        timestamp, frameNumber, frameName, frame, err = readFrame(socket, frameSize)
        if not err:
            print(" > read frame "+str(frameNumber)+": " + frameName + " ("+str(len(frame))+" bytes)")
            imgARGB = np.frombuffer(frame, 'uint8').reshape(frameHeight, frameWidth, 4)
            imgAlpha = np.zeros((frameHeight, frameWidth, 1), 'uint8')
            imgBGR = np.zeros((frameHeight, frameWidth, 3), 'uint8')
            cv2.mixChannels([imgARGB], [imgBGR, imgAlpha], [1,2, 2,1, 3,0, 0,3])
            # cv2.imwrite('test-frame'+str(counter)+'.jpg', imgBGR)
            # counter += 1
            # continue
            # sys.exit(0)
            p1 = datetime.datetime.now()
            # we need to flip (vertically&horizontally) the image so that OpenFace can work properly
            flippedImg = cv2.flip(imgBGR,-1)
            rects = dlibObject.getAllFaceBoundingBoxes(flippedImg)

            if len(rects) > 0:
                print(" > DETECTED "+str(len(rects))+" faces")
                facesArray = []
                for r in rects:
                    faceAnnotation = getAnnotationFromRect(r, frameWidth, frameHeight)
                    # run classifier, if probabilirt less than 50%, ignore classifier's data
                    faceAnnotation = runClassifier(flippedImg, faceAnnotation, r)
                    p2 = datetime.datetime.now()
                    delta = p2-p1
                    processingMs = int(delta.total_seconds() * 1000)
                    print(" > open face processing took " + str(processingMs)+" ms")
                    facesArray.append(faceAnnotation)
                    drawBox(flippedImg, r, str(faceAnnotation['label']))
                dumpAnnotations(timestamp, frameNumber, frameName, facesArray)
            else:
                print(" > no faces detected")
            dumpImage(flippedImg)

            # handy code to slice image into separate channels
            # imgR = np.zeros((frameHeight, frameWidth, 1), 'uint8')
            # imgG = np.zeros((frameHeight, frameWidth, 1), 'uint8')
            # imgB = np.zeros((frameHeight, frameWidth, 1), 'uint8')
            # imgA = np.zeros((frameHeight, frameWidth, 1), 'uint8')
            # cv2.mixChannels([imgARGB], [imgA, imgR, imgG, imgB], [0,0, 1,1, 2,2, 3,3])
            # cv2.imwrite('test-frame-A.jpg', imgA)
            # cv2.imwrite('test-frame-R.jpg', imgR)
            # cv2.imwrite('test-frame-G.jpg', imgG)
            # cv2.imwrite('test-frame-B.jpg', imgB)
        else:
            print(" > error reading frame: "+str(err))

#******************************************************************************
def usage():
    print "\t"+sys.argv[0]+" [--frame-width=, --frame-height=, --input=, --output=, --preview=] --dlibmodel=<dlib model path> --torchmodel=<torch model path> --labels=<labels file>, --reps=<reps file> [pipe file]"
    print "usage: "

def main():
    global dlibObject, dumpSocket, torchNn, torchModelPath, imagePipeName, dlibModelPath
    try:
        opts, args = getopt.getopt(sys.argv[1:], "w:h:", ["frame-width=", "frame-height=", "labels=", "reps=", "input=", "output=", "preview=", "dlibmodel=", "torchmodel="])
    except getopt.GetoptError as err:
        print str(err)
        usage()
        exit(2)
    frameWidth=320
    frameHeight=180
    pipeName = "/tmp/mtcamera"
    outputName = "/tmp/ice-annotations"
    repsFile = None
    labelsFile = None

    for o,a in opts:
        if o in ("-w", "--frame-width"):
            frameWidth = int(a)
        elif o in ("-h", "--frame-height"):
            frameHeight = int(a)
        elif o in ("--labels"):
            labelsFile = a
        elif o in ("--reps"):
            repsFile = a
        elif o in ("--input"):
            pipeName = a
        elif o in ("--output"):
            outputName = a
        elif o in ("--preview"):
            imagePipeName = a
        elif o in ("--dlibmodel"):
            dlibModelPath = a
        elif o in ("--torchmodel"):
            torchModelPath = a
        else:
            assert False, "unhandled option "+ o

    if not repsFile or not labelsFile:
        print "please provide reps and labels file"
        usage()
        exit(2)

    if len(args) == 1:
        pipeName = args[0]+"."+str(frameWidth)+"x"+str(frameHeight)
    else:
        pipeName += "."+str(frameWidth)+"x"+str(frameHeight)

    s = Socket(SUB)
    s.connect("ipc://"+pipeName)
    s.set_string_option(SUB, SUB_SUBSCRIBE, '')

    dumpSocket = Socket(PUB)
    dumpSocket.connect("ipc://"+outputName)

    print " > reading frames " + str(frameWidth) + "x" + str(frameHeight) + " from "+pipeName
    print " > writing annotations to "+outputName
    print " > preview at " + imagePipeName + " (ffplay -f rawvideo -vcodec rawvideo -s " + str(frameWidth) + "x" + str(frameHeight) + " -pix_fmt bgr24 -i " + imagePipeName + ")"
    print " > loading dlib model from " + dlibModelPath
    print " > loading torch model from " + torchModelPath
    print " > reps file " + repsFile
    print " > labels file " + labelsFile
    print ""

    print(" > initializing OpenFace...")
    dlibObject=openface.AlignDlib(dlibModelPath)
    print(" > ...done.")
    
    print(" > initializing classifier...")
    initClassifier(labelsFile, repsFile)
    print(" > ...done.")

    print(" > initializing torch feature extractor...")
    torchNn = openface.TorchNeuralNet(torchModelPath, imgDim=96, cuda=True)
    print(" > ...done.")

    print(" > processing frames of size "+str(frameWidth)+"x"+str(frameHeight)+" from "+pipeName)
    processFrames(s, frameWidth, frameHeight)

if __name__ == '__main__':
    main()

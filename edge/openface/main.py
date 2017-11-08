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
	frameNo = None
	frameNoBuf, frame, err = nnc.nn_recvmsg(socket, [4, frameSize])
	if not err:
		frameNo, = struct.unpack('<L', frameNoBuf)
	return frameNo, frame, err

def getAnnotationFromRect(r, w, h):
	annotation = {}
	annotation['xleft'] = float(r.left())/float(w)
	annotation['xright'] = float(r.right())/float(w)
	annotation['ytop'] = float(r.top())/float(h)
	annotation['ybottom'] = float(r.bottom())/float(h)
	annotation['prob'] = 1.
	annotation['label'] = 'a face'
	return annotation

def dumpAnnotations(frameNo, annotations):
	global dumpSocket
	jsonDict = {'frameNo':frameNo, 'engine':'openface', 'annotations':annotations}
	s = json.dumps(jsonDict)
	# print(" > will send this "+s)
	dumpSocket.send(s)

def runClassifier(img, annotation, rect):
	global dlibObject, torchNn, svc, labelEncoder
	alignedImg = dlibObject.align(96, img, bb=rect, landmarkIndices=openface.AlignDlib.OUTER_EYES_AND_NOSE)
	features = torchNn.forward(alignedImg)
	# need to reshape features, as svc object expects 2d array, while features is 1d array
	predictions = svc.predict_proba(features.reshape(1,-1)).ravel()
	maxLikelihood = np.argmax(predictions)
	label = labelEncoder.inverse_transform(maxLikelihood)
	if maxLikelihood > 0.5:
		annotation['prob'] = maxLikelihood
		annotation['label'] = label
	return annotation

counter = 0
def processFrames(socket, frameWidth, frameHeight):
	global dlibObject, counter, svc
	frameSize = frameWidth*frameHeight*4 # assuming ARGB

	while True:
		frameNumber, frame, err = readFrame(socket, frameSize)
		if not err:
			print(" > read frame "+str(frameNumber)+" ("+str(len(frame))+" bytes)")
			imgARGB = np.frombuffer(frame, 'uint8').reshape(frameHeight, frameWidth, 4)
			imgBGR = np.zeros((frameHeight, frameWidth, 3), 'uint8')
			cv2.mixChannels([imgARGB], [imgBGR], [1,2, 2,1, 3,0])
			# cv2.imwrite('test-frame'+str(counter)+'.jpg', imgBGR)
			# counter += 1
			# continue
			# sys.exit(0)
			p1 = datetime.datetime.now()
			# we need to flip the image so that OpenFace can work properly
			rects = dlibObject.getAllFaceBoundingBoxes(cv2.flip(imgBGR,0))
			p2 = datetime.datetime.now()
			delta = p2-p1
			processingMs = int(delta.total_seconds() * 1000)
			print(" > open face processing took " + str(processingMs)+" ms")

			if len(rects) > 0:
				print(" > DETECTED "+str(len(rects))+" faces")
				facesArray = []
				for r in rects:
					faceAnnotation = getAnnotationFromRect(r, frameWidth, frameHeight)
					# run classifier, if probabilirt less than 50%, ignore classifier's data
					faceAnnotation = runClassifier(imgBGR, faceAnnotation, r)
					facesArray.append(faceAnnotation)
				dumpAnnotations(frameNumber, facesArray)
			else:
				print(" > no faces detected")

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
	print "\t"+sys.argv[0]+" [--frame-width=, --frame-height=] --labels=<labels file>, --reps=<reps file> [pipe file]"
	print "usage: "

def main():
	global dlibObject, dumpSocket, torchNn, torchModelPath
	try:
		opts, args = getopt.getopt(sys.argv[1:], "w:h:", ["frame-width=", "frame-height=", "labels=", "reps="])
	except getopt.GetoptError as err:
		print str(err)
		usage()
		exit(2)
	frameWidth=320
	frameHeight=240
	pipeName = "/tmp/mtcamera.320x240"
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
		else:
			assert False, "unhandled option "+ o

	if not repsFile or not labelsFile:
		print "please provide reps and labels file"
		usage()
		exit(2)

	if len(args) == 1:
		pipeName = args[0]+"."+str(frameWidth)+"x"+str(frameHeight)

	s = Socket(SUB)
	s.connect("ipc://"+pipeName)
	s.set_string_option(SUB, SUB_SUBSCRIBE, '')

	dumpSocket = Socket(PUB)
	dumpSocket.connect("ipc:///tmp/ice-annotations")

	print(" > initializing OpenFace...")
	dlibObject=openface.AlignDlib(dlibModelPath)
	print(" > ...done.")
	
	print(" > initializing classifier...")
	initClassifier(labelsFile, repsFile)
	print(" > ...done.")

	print(" > initializing torch feature extractor...")
	torchNn = openface.TorchNeuralNet(torchModelPath, imgDim=96)
	print(" > ...done.")

	print(" > processing frames of size "+str(frameWidth)+"x"+str(frameHeight)+" from "+pipeName)
	processFrames(s, frameWidth, frameHeight)

if __name__ == '__main__':
	main()
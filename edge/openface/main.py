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

dlibModelPath = '/home/peter/openface/models/dlib/shape_predictor_68_face_landmarks.dat'
dlibObject = None
dumpSocket = None

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

counter = 0
def processFrames(socket, frameWidth, frameHeight):
	global dlibObject, counter
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
	print ""
	print "usage: "

def main():
	global dlibObject, dumpSocket
	try:
		opts, args = getopt.getopt(sys.argv[1:], "w:h:", ["frame-width=", "frame-height="])
	except getopt.GetoptError as err:
		print str(err)
		usage()
		exit(2)
	frameWidth=320
	frameHeight=240
	pipeName = "/tmp/mtcamera.320x240"

	for o,a in opts:
		if o in ("-w", "--frame-width"):
			frameWidth = int(a)
		elif o in ("-h", "--frame-height"):
			frameHeight = int(a)
		else:
			assert False, "unhandled option "+ o

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
	print(" > processing frames of size "+str(frameWidth)+"x"+str(frameHeight)+" from "+pipeName)
	processFrames(s, frameWidth, frameHeight)

if __name__ == '__main__':
	main()
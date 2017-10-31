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
from nanomsg import *
import _nanomsg_ctypes as nnc
import struct

# reads a frame from nanomsg socket
def readFrame(socket, frameSize):
	frameNo = None
	frameNoBuf, frame, err = nnc.nn_recvmsg(socket, [4, frameSize])
	if not err:
		frameNo, = struct.unpack('<L', frameNoBuf)
	return frameNo, frame, err

def processFrames(socket, frameWidth, frameHeight):
	# openFaceObject = new openface.AlignDlib()
	frameSize = frameWidth*frameHeight*4 # assuming ARGB

	while True:
		frameNumber, frame, err = readFrame(socket, frameSize)
		if not err:
			print(" > read frame "+str(frameNumber)+" ("+str(len(frame))+" bytes)")
		else:
			print(" > error reading frame: "+str(err))

#******************************************************************************
def usage():
	print ""
	print "usage: "

def main():
	try:
		opts, args = getopt.getopt(sys.argv[1:], "w:h:", ["frame-width=", "frame-height="])
	except getopt.GetoptError as err:
		print str(err)
		usage()
		exit(2)
	frameWidth=320
	frameHeight=240
	pipeName = "/tmp/mtcamera.320x240"

	if len(args) == 1:
		pipeName = args[0]

	for o,a in opts:
		if o in ("-w", "--frame-width"):
			frameWidth = int(a)
		elif o in ("-h", "--frame-height"):
			frameHeight = int(a)
		else:
			assert False, "unhandled option "+ o

	s = Socket(SUB)
	s.connect("ipc://"+pipeName)
	s.set_string_option(SUB, SUB_SUBSCRIBE, '')

	print(" > processing frames of size "+str(frameWidth)+"x"+str(frameHeight)+" from "+pipeName)
	processFrames(s, frameWidth, frameHeight)

if __name__ == '__main__':
	main()
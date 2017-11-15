import openface
import cv2
import getopt
import sys
import os

try:
	opts, args = getopt.getopt(sys.argv[1:], "", [])	
except getopt.GetoptError as err:
	print str(err)
	exit(2)

if len(args) == 1:
	folder = args[0]
else:
	print "usage: specify folder"
	exit(2)

dlibModelPath = '/home/peter/openface/models/dlib/shape_predictor_68_face_landmarks.dat'
dlibObject=openface.AlignDlib(dlibModelPath)

jpegs = [img for img in os.listdir(folder) if img.endswith('.jpg')]

print "found "+str(len(jpegs))+" images in "+folder

if len(jpegs):
	totalFound = 0
	for jpeg in jpegs:
		path = folder+"/"+jpeg
		img = cv2.imread(path)
		if len(img):
			# img = cv2.cvtColor(img, cv2.COLOR_BGR2RGB)
			# rects = dlibObject.getAllFaceBoundingBoxes(cv2.flip(img,0))
			rects = dlibObject.getAllFaceBoundingBoxes(img)
			print jpeg+" -> "+str(len(rects))+" faces"
			totalFound += len(rects)

	pct = round(float(totalFound)/float(len(jpegs))*100, 2)
	print ""
	print "total found "+str(totalFound) + " faces on "+str(len(jpegs))+" images ("+str(pct)+"%)"

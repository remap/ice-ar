import openface
import cv2
import getopt
import sys
import os
import numpy as np
import pandas
import sklearn
from sklearn import preprocessing
from sklearn import svm
import datetime

dlibModelPath = '/home/peter/openface/models/dlib/shape_predictor_68_face_landmarks.dat'
torchModelPath = '/home/peter/openface/models/openface/nn4.small2.v1.t7'
dlibObject = None
svc = None
labelEncoder = None
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

def runClassifier(img, rect):
	global dlibObject, torchNn, svc, labelEncoder
	alignedImg = dlibObject.align(96, img, bb=rect, landmarkIndices=openface.AlignDlib.OUTER_EYES_AND_NOSE)
	features = torchNn.forward(alignedImg)
	# need to reshape features, as svc object expects 2d array, while features is 1d array
	predictions = svc.predict_proba(features.reshape(1,-1)).ravel()
	print "predictions "+str(predictions)
	idx = np.argmax(predictions)
	maxLikelihood = predictions[idx]
	label = labelEncoder.inverse_transform(idx)
	return (maxLikelihood, label)

#******************************************************************************
def usage():
	print "usage: "
	print "\t"+sys.argv[0]+" --labels=<labels file> --reps=<reps file> [pipe file] --test=<test images folder>"

def main():
	global dlibObject, torchNn, torchModelPath
	try:
		opts, args = getopt.getopt(sys.argv[1:], "", ["labels=","reps=","test="])
	except getopt.GetoptError as err:
		print str(err)
		exit(2)

	labelsFile=None
	repsFile=None
	testImagesFolder=None

	for o,a in opts:
		if o in ("--labels"):
			labelsFile = a
		elif o in ("--reps"):
			repsFile = a
		elif o in ("--test"):
			testImagesFolder = a
		else:
			assert False, "unhandled option " + o
			usage()
			exit(2)

	if not (labelsFile and repsFile and testImagesFolder):
		usage()
		exit(2)

	dlibObject=openface.AlignDlib(dlibModelPath)
	initClassifier(labelsFile, repsFile)
	torchNn = openface.TorchNeuralNet(torchModelPath, imgDim=96)
	jpegs = [img for img in os.listdir(testImagesFolder) if img.endswith('.jpg')]

	print "found "+str(len(jpegs))+" test images in "+testImagesFolder

	if len(jpegs):
		totalFound = 0
		totalConfidence = 0
		avgProcessing = 0
		for jpeg in jpegs:
			path = testImagesFolder+"/"+jpeg
			img = cv2.imread(path)
			if len(img):
				# img = cv2.cvtColor(img, cv2.COLOR_BGR2RGB)
				# rects = dlibObject.getAllFaceBoundingBoxes(cv2.flip(img,0))
				rects = dlibObject.getAllFaceBoundingBoxes(img)
				for r in rects:
					p1 = datetime.datetime.now()
					prob, label = runClassifier(img, r)
					p2 = datetime.datetime.now()
					delta = p2-p1
					processingMs = int(delta.total_seconds() * 1000)
					avgProcessing += processingMs
					totalConfidence += prob
					print path + " (" + str(img.shape[0])+"x"+str(img.shape[1])+ ") "+ " -> " + str(label) + " (" + str(prob) + " confidence), took " + str(processingMs) + "ms"
				totalFound += len(rects)

		pct = round(float(totalFound)/float(len(jpegs))*100, 2)
		print ""
		print "total found "+str(totalFound) + " faces on "+str(len(jpegs))+" images ("+str(pct)+"%)"
		pct = round(float(totalConfidence)/float(len(jpegs))*100, 2)
		print "average recognition confidence - "+str(pct)+"%, "+str(avgProcessing/len(jpegs))+"ms per image"

if __name__ == '__main__':
	main()
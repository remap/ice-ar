#!/usr/bin/python


import sys
import getopt
import os
from nanomsg import *
import _nanomsg_ctypes as nnc
import struct
import datetime
import json
import time

def usage():
    print "usage: "
    print "\t"+sys.argv[0]+" <source file> <rate> <nanomsg pipe file>"

def main():
    if len(sys.argv) == 4:
        sourceFile = sys.argv[1]
        rate = float(sys.argv[2])
        pipeName = sys.argv[3]
    else:
        usage()
        exit(1)

    s = Socket(PUB)
    s.connect("ipc://"+pipeName)

    if s:
        sleepDelay = 1./rate
        with open(sourceFile, 'r') as f:
            print(" > will source text from " + sourceFile)
            for line in f.readlines():
                s.send(line.strip())
                print("sent "+line)
                time.sleep(sleepDelay)
    else:
        print(" > failed to open nanomsg pipe: ipc://"+pipeName)

if __name__ == '__main__':
    main()

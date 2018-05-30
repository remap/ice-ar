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
    print "\t"+sys.argv[0]+" <nanomsg pipe file>"

def main():
    if len(sys.argv) == 2:
        pipeName = sys.argv[1]
    else:
        usage()
        exit(1)

    print(" > will read from " + pipeName)
    s = Socket(SUB)
    s.bind("ipc://"+pipeName)
    s.set_string_option(SUB, SUB_SUBSCRIBE, '')
    if s:
        while True:
            print(s.recv())
    else:
        print(" > failed to open nanomsg pipe: ipc://"+pipeName)

if __name__ == '__main__':
    main()

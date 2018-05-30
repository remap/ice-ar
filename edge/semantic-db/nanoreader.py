#!/usr/bin/python

import sys
import getopt
import os
from nanomsg import *
import _nanomsg_ctypes as nnc
import struct
import datetime
import json
import operator

from pymongo import MongoClient
from copy import deepcopy
from bson.objectid import ObjectId
from pprint import pprint

def timestampMs():
    return float((datetime.datetime.utcnow() - datetime.datetime(1970,1,1)).total_seconds() * 1000)

def usage():
    print "usage: "
    print "\t"+sys.argv[0]+" <nanomsg pipe file>"

def main():
    #needs to have MongoDB daemon running on server -- mongod in terminal
    client = MongoClient()
    db = client.db
    #db.entries.drop()
    entr = db.entries

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
            try:
                jsonString = s.recv()
                curr = json.loads(jsonString)

                hist = entr.find_one({"oid": curr["timestamp"]})
                entry = {"oid": curr["timestamp"], "name": curr["frameName"], "objects": [], "labels": []}
            except:
                print("json parsing failed: "+str(sys.exc_info()[0]))
                print("failed json string: "+jsonString)
                continue

            temp = []
            seen = []

            numbered = []
            #numprobs = {}

            if hist != None:
                for j in hist["objects"]:
                    j["label"] = ''.join([i for i in j["label"] if not i.isdigit()])
                    temp.append(j)

            for k in curr["annotations"]:
                temp.append({"label": ''.join([i for i in k["label"] if not i.isdigit()]), "ytop":k["ytop"], "ybottom":k["ybottom"], "xleft":k["xleft"], "xright":k["xright"], "prob":k["prob"]})

            for item in temp:
                if item["label"] in seen:
                    continue

                seen.append(item["label"])
                objs = deepcopy([k for k in temp if item["label"] in k.itervalues()])

                srtd = sorted(objs, key = lambda k: k["prob"]) 

                for ind, obj in enumerate(srtd):
                    obj["label"] = obj["label"] + str(ind)
                    numbered.append(obj["label"])
                    #numprobs[obj["label"]] = obj["prob"]
                    entry["objects"].append(obj)

            entry["labels"] = numbered

            if hist != None:
                entr.replace_one({"oid": curr["timestamp"]}, entry)
            else:
                entr.insert_one(entry)

            """
            query = {"$in": []}
            for elem in numbered:
                query["$in"].append(elem.encode("utf-8"))
            query = {"labels": query}

            #t1 = timestampMs()
            cursor = entr.aggregate(
                [{"$match": query},
                {"$unwind": "$labels"},
                {"$match": query},
                {"$group": {
                    "_id":"$_id",
                    "matches": {"$sum":1}
                }},
                {"$sort": {"matches": -1}}]
            )
            #t2 = timestampMs()
            #print(t2-t1)

            count = 0
            pairs = {}
            for document in cursor: 
                if count > 15:
                    continue

                curs = entr.find_one({"_id": document["_id"]})
                currobjs = curs["objects"]
                probs = []
                for key, value in numprobs.iteritems():
                    for currobj in currobjs:
                        if currobj["label"] == key:
                            probs.append(float(currobj["prob"]) * float(value))
                if len(probs) > 0:
                    pairs[curs["name"]] = sum(probs)
                count = count + 1

            sortedpairs = sorted(pairs.items(), key=operator.itemgetter(1))
            sortedpairs.reverse()
            top3 = sortedpairs[:3]
            top3 = [i[0] for i in top3]

            print(top3)
            """

    else:
        print(" > failed to open nanomsg pipe: ipc://"+pipeName)

if __name__ == '__main__':
    main()

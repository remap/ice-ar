from __future__ import print_function
import os, sys, signal
sys.path.insert(0, 'src')

# adding import path for the directory above this sctip (for deeplab modules)
myPath = os.path.dirname(sys.argv[0])
rootPath = os.path.join(myPath,'..')

# uploadPath =  os.path.join(rootPath, "upload")
# resultsPath = os.path.join(rootPath, "results")
# modelsDir = os.path.join(rootPath, 'ce-models');

sys.path.append(rootPath)

import tornado.httpserver, tornado.ioloop, tornado.options, tornado.web, os.path, random, string
import uuid
from tornado.options import define, options
from Queue import Queue
from threading import Thread
from datetime import datetime
import re
import time
import datetime
import numpy as np
from collections import defaultdict
import operator
import time
import json
import subprocess
import numpy
import glob
import urllib
from pymongo import MongoClient
from copy import deepcopy
from bson.objectid import ObjectId
from pprint import pprint

BATCH_SIZE = 4

port = 8888
ipaddress = "131.179.142.7"
hostUrl = "http://"+ipaddress+":"+str(port)
define("port", default=port, help="run on the given port", type=int)

quit = False
debug = False

#******************************************************************************
def timestampMs():
    return int((datetime.datetime.utcnow() - datetime.datetime(1970, 1, 1)).total_seconds() * 1000)

class Application(tornado.web.Application):
    def __init__(self):
        handlers = [
            (r"/", IndexHandler),
            (r"/query", QueryHandler)
        ]
        tornado.web.Application.__init__(self, handlers)
        
class IndexHandler(tornado.web.RequestHandler):
    def get(self):
        self.render("upload_form.html")
        
class QueryHandler(tornado.web.RequestHandler):
    def post(self):
        client = MongoClient()
        db = client.db
        entr = db.entries

        global workerQueues, debug
        print("New query request "+str(self.request))
	intermediate = str(self.request.body.decode('utf-8'))
        data = json.loads(urllib.unquote(intermediate).decode('utf8')) 
        print("Got these annotations: "+str(data)+" and will make query now...")
        
        temp = []
        seen = []
        nbrd = []
        numprobs = {}

        for k in data["annotations"]:
            temp.append({"label": ''.join([i for i in k["label"] if not i.isdigit()]), "prob":k["prob"]})

        for item in temp:
            if item["label"] in seen:
                    continue

            seen.append(item["label"])
            objs = deepcopy([k for k in temp if item["label"] in k.itervalues()])

            srtd = sorted(objs, key = lambda k: k["prob"]) 

            for ind, obj in enumerate(srtd):
                obj["label"] = obj["label"] + str(ind)
                nbrd.append(obj["label"])
                numprobs[obj["label"]] = obj["prob"]
        
        query = {"$in": []}
        for elem in nbrd:
            query["$in"].append(elem.encode("utf-8"))
        query = {"labels": query}

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

        self.set_status(200)
        self.finish(json.dumps(top3))

####
def signal_handler(signum, frame):
    global is_closing
    print('Received stop signal, exiting...')
    tornado.ioloop.IOLoop.instance().stop()
    quit = True

def main():
    global port, debug
    signal.signal(signal.SIGINT, signal_handler)
    if len(sys.argv) > 1 and sys.argv[1] == 'debug':
        debug = True
        port = 8890
        print("**********DEBUG MODE************************************************************")
        print("Portnumber: "+str(port))
    
    print("Started Tornado on "+str(port)+" port.")
    http_server = tornado.httpserver.HTTPServer(Application())
    http_server.listen(port)
    tornado.ioloop.IOLoop.instance().start()
    
    print("Will terminate now...")

if __name__ == "__main__":
    main()

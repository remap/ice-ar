/* -*- Mode:C++; c-file-style:"gnu"; indent-tabs-mode:nil -*- */
/**
 * Copyright (C) 2017 Regents of the University of California.
 * @author: Jeff Thompson <jefft0@remap.ucla.edu>
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version, with the additional exemption that
 * compiling, linking, and/or using OpenSSL is allowed.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 * A copy of the GNU Lesser General Public License is in the file COPYING.
 */

/* This tests the GeneralizedContent which fetches a _meta info object and segmented
 * content with a single segment. This requires a local running NFD.
 */

// Only compile if ndn-cpp-config.h defines NDN_CPP_HAVE_PROTOBUF = 1.
#include <ndn-cpp/ndn-cpp-config.h>
#if NDN_CPP_HAVE_PROTOBUF

#include <cstdlib>
#include <unistd.h>
#include <time.h>
#include <stdlib.h>
#include <ndn-cpp-tools/usersync/generalized-content.hpp>
#include <ndn-cpp/threadsafe-face.hpp>
#include <execinfo.h>
#include <set>
#include <ctime>
#include <signal.h>
#include <cassert>
#include <sys/types.h>
#include <sys/stat.h>
#include <fcntl.h>
#include <boost/asio.hpp>
#include <boost/thread/mutex.hpp>
#include <boost/thread.hpp>

#include "ipc-shim.h"
#include "cJSON.h"

#define USE_NANOMSG
#define RUN_FOREVER

using namespace std;
using namespace ndn;
using namespace ndn::func_lib;
using namespace ndntools;

static void
onRegisterFailed(const ptr_lib::shared_ptr<const Name>& prefix, bool* enabled);

static void
onRegisterSuccess
  (const ptr_lib::shared_ptr<const Name>& registeredPrefix,
   uint64_t registeredPrefixId, bool* result);

static void
printMetaInfoAndContent
  (const ptr_lib::shared_ptr<ContentMetaInfo>& metaInfo,
   const Blob& content, bool* enabled);

static void
onError
  (GeneralizedContent::ErrorCode errorCode, const string& message, bool* enabled);

//******************************************************************************
class AnnotationArray {
public: 
  AnnotationArray(string jsonString):jsonString_(jsonString){}
  AnnotationArray(const Blob& b):
    jsonString_(b.toRawStr()) {}
  AnnotationArray(const AnnotationArray& aa) : jsonString_(aa.jsonString_){}

  ~AnnotationArray(){}

  string get() const { return jsonString_; }

  friend ostream& operator << (ostream &s, const AnnotationArray& a)
  { return s << a.get(); }

private:
  string jsonString_;
};

typedef func_lib::function<void(unsigned int frameNo, const AnnotationArray&)> OnAnnotation;
typedef func_lib::function<void(unsigned int frameNo, GeneralizedContent::ErrorCode errorCode, const string& message)> OnFetchFailure;

class AnnotationConsumer {
public:
  typedef struct _FetcherListEntry {
    OnAnnotation onAnnotation_;
    OnFetchFailure onFetchFailure_;
    unsigned int frameNo_;
    unsigned int retriesLeft_;
  } FetcherListEntry;

  AnnotationConsumer(const Name& servicePrefix, std::string instance, Face* f):
    basePrefix_(servicePrefix), instance_(instance), face_(f){}
  ~AnnotationConsumer(){  }

  void fetch(unsigned int frameNo, OnAnnotation onAnnotation, OnFetchFailure onFetchFailure)
  {
    Name prefix(basePrefix_);
    prefix.appendSequenceNumber(frameNo).append(instance_);

    try {
      // if it's a new fetch - set 3 retry attempts
      if (fetchers_.find(prefix) == fetchers_.end())
        fetchers_[prefix] = {onAnnotation, onFetchFailure, frameNo, 3};
      else
      { // if it's a repeated attempt to fetch - just update 
        // callbacks, leave retry counter unchanged
        fetchers_[prefix].onAnnotation_ = onAnnotation;
        fetchers_[prefix].onFetchFailure_  = onFetchFailure;
      }

      cout << " -  spawned fetching for " << prefix << ", total " << fetchers_.size() << std::endl;

      GeneralizedContent::fetch
      (*face_, prefix, 0, 
       bind(&AnnotationConsumer::onComplete, this, _1, _2, prefix),
       bind(&AnnotationConsumer::onError, this, _1, _2, prefix));
    } 
    catch (std::exception& e) {
      cout << "exception: " << e.what() << endl;
    }
  }

private:
  Name basePrefix_;
  std::string instance_;
  Face *face_;
  map<Name, FetcherListEntry> fetchers_;

  void onComplete(const ptr_lib::shared_ptr<ContentMetaInfo>& metaInfo,
    const Blob& content, const Name objectName)
  {
    map<Name, FetcherListEntry>::iterator it = fetchers_.find(objectName);
    if (it != fetchers_.end())
    {
      FetcherListEntry e = it->second;
      if (metaInfo->getHasSegments())
        e.onAnnotation_(e.frameNo_, AnnotationArray(content));
      else
        e.onAnnotation_(e.frameNo_, AnnotationArray(metaInfo->getOther()));
      fetchers_.erase(it);

      cout << "  * received " << objectName 
      << ", content-type: " << metaInfo->getContentType()
      << " (has segments: " << (metaInfo->getHasSegments() ? "YES)" : "NO)")
      << " size: " << (metaInfo->getHasSegments() ? content.size() : metaInfo->getOther().toRawStr().size())
      << ", remaining " << fetchers_.size()
      << endl;
    }
    else
      throw std::runtime_error("fetcher entry not found");
  }

  void onError(GeneralizedContent::ErrorCode errorCode, const string& message, 
    const Name objectName)
  {
    cout << "error fetching " << objectName << ": " << message << endl;

    map<Name, FetcherListEntry>::iterator it = fetchers_.find(objectName);
    if (it != fetchers_.end())
    {
      FetcherListEntry& e = fetchers_[objectName];
      if (e.retriesLeft_ > 0)
      {
        e.retriesLeft_--;
        fetch(e.frameNo_, e.onAnnotation_, e.onFetchFailure_);
      }
      else
      {
        e.onFetchFailure_(e.frameNo_, errorCode, message);
        fetchers_.erase(it);
      }
    }
    else
      throw std::runtime_error("fetcher entry not found");
  }
};

class AnnotationPublisher {
public:
  /**
   * @param userPrefix According to namespace design, smth. like "/icear/user/<user-id>"
   * @param serviceName According to namespace design, smth. like "object_recognizer"
   * @param cache Memory content cache for storing published data
   * @param keyChain Key chain for signing published packets
   * @param certificateName Certificate name to use for signing
   */
  AnnotationPublisher(const Name& servicePrefix, MemoryContentCache& cache, 
    KeyChain *keyChain, const Name& certificateName):
    baseName_(servicePrefix), cache_(cache), keyChain_(keyChain), certName_(certificateName){}
  ~AnnotationPublisher() {  }

  void publish(unsigned int frameNo, const AnnotationArray& a, const string& instance){
    size_t contentSegmentSize = 1000;
    string content = a.get();
    ContentMetaInfo metaInfo;
    bool hasSegments = content.size() > contentSegmentSize;

    metaInfo.setContentType("application/json")
      .setTimestamp(1477681379)
      .setHasSegments(hasSegments);

    if (!hasSegments)
      metaInfo.setOther(Blob((const uint8_t*)&content[0], content.size()));
    
    // <user-prefix>/<service-name>/<frame-no>/<service-instance>
    Name annotationName(baseName_);
    annotationName.appendSequenceNumber(frameNo).append(instance);

    GeneralizedContent::publish
      (cache_, annotationName, 10000, keyChain_, certName_, metaInfo,
       Blob((const uint8_t*)&content[0], content.size()), contentSegmentSize);

    std::cout << "*   published " << annotationName << std::endl;
    //   << " (has segments: " << (metaInfo.getHasSegments() ? "YES, content size: " : "NO, content size: ") 
    //   << content.size()  << ")" << std::endl;
  }

private:
  MemoryContentCache cache_;
  KeyChain *keyChain_;
  Name certName_;
  Name baseName_;
};

//******************************************************************************
void handler(int sig) {
  void *array[10];
  size_t size;

  // get void*'s for all entries on the stack
  size = backtrace(array, 10);

  // print out all the frames to stderr
  fprintf(stderr, "Error: signal %d:\n", sig);
  backtrace_symbols_fd(array, size, STDERR_FILENO);
  exit(1);
}

char *labels[] = {"aeroplane", "bicycle", "bird", "boat", "bottle", "bus", "car", "cat", "chair", "cow", "diningtable", "dog", "horse", "motorbike", "person", "pottedplant", "sheep", "sofa", "train", "tvmonitor"};
std::string generateRandomAnnotation(){
  double xleft = std::rand()%70;
  double xright = (std::rand()%int(100-xleft-1)+xleft+1);
  double ytop = std::rand()%70;
  double ybottom = (std::rand()%int(100-ytop-1)+ytop+1);
  int labelIdx = std::rand()%20;
  double prob = double(std::rand()%20 + 80)/100.;

  assert(xleft >= 0 && xright > 0.);
  assert(ytop >= 0. && ybottom > 0);
  assert(xright <= 100. && xleft <= 100.);
  assert(ytop <= 100. && ybottom <= 100.);
  assert(xright > xleft);
  assert(ybottom > ytop);
  assert(labelIdx < 20);
  assert(prob >= 0.8);
  assert(prob <= 1.);

  std::stringstream ss;
  ss << "{\"xleft\":" << xleft/100. 
      << ", \"xright\":" << xright/100.
      << ", \"ytop\":" << ytop/100.
      << ", \"ybottom\":" << ybottom/100.
      << ", \"label\":\"" << labels[labelIdx] 
      << "\", \"prob\":" << prob << " }";

  // std::cout << "generated " << ss.str() << std::endl;

  return ss.str();
}

AnnotationArray generateAnnotationArray(int nAnnotations)
{
  std::stringstream ss;
  
  ss << "[";
  for (int i = 0; i < nAnnotations; ++i)
  {
    ss << generateRandomAnnotation();
    if (i+1 != nAnnotations)
      ss << ",";
  }
  ss << "]";

  return AnnotationArray(ss.str());
}

//******************************************************************************
static const char *pipeName = "/tmp/ice-annotations";
static int feature_pipe = -1;

int create_pipe(const char* fname)
{
  int res = 0;
  do {
    res = mkfifo(fname, 0644);
    if (res < 0 && errno != EEXIST)
    {
      printf("error creating pipe (%d): %s\n", errno, strerror(errno));
      sleep(1);
    }
    else res = 0;
  } while (res < 0);

  return 0;
}

void reopen_readpipe(const char* fname, int* pipe)
{
    do {
        if (*pipe > 0) 
            close(*pipe);

        *pipe = open(fname, O_RDONLY);

        if (*pipe < 0)
            printf("> error opening pipe: %s (%d)\n",
                strerror(errno), errno);
    } while (*pipe < 0);
}

std::string readAnnotations(int pipe, unsigned int &frameNo, std::string &engine)
{
#ifndef USE_NANOMSG
  bool hasFrameNo = false;
  bool completeRead = false;
  int c = 0, nIter = 0; 
  int bufSize = 16*1024;
  static char buf[16*1024];
  memset(buf,0,bufSize);
  int nBraces = 0;

  // cout << "> reading annotations..." << std::endl;

  do {
    if (!hasFrameNo)
    {
      int r = read(pipe, (uint8_t*)&frameNo, sizeof(unsigned int));
      if (r < 0)
        std::cout << "> error reading frameNo from pipe: " 
          << strerror(errno) << std::endl;
      else if (r == 0)
        reopen_readpipe(pipeName, &feature_pipe);
      else
      {
        if (r != sizeof(unsigned int))
          std::cout << "frameNo read " << r << " bytes ONLY. will crash =(" << std::endl;
        hasFrameNo = true;
      }
    } // !hasFrameNo

    int r = read(pipe, buf+c, 1);

    if (buf[c] == '[') nBraces++;
    if (buf[c] == ']') nBraces--;

    if (r > 0)
      c += r;
    else if (r < 0)
      std::cout << "> error reading from pipe: " 
        << strerror(errno) << std::endl;
    else
      reopen_readpipe(pipeName, &feature_pipe);

    completeRead = (nBraces == 0);

    assert(nBraces >= 0);
    assert(c < bufSize-1);

  } while(!completeRead);

  buf[c+1] = '\0';

  cout << "> read annotaitons (frame " << frameNo << "): " << buf << std::endl;

  engine = "yolo";
  return std::string(buf);
#else
  char *annotations;
  int len = ipc_readData(feature_pipe, (void**)&annotations);
  
  if (len > 0)
  {
    cJSON *item = cJSON_Parse(annotations);
    
    if (item)
    {
      cJSON *fNo = cJSON_GetObjectItem(item, "frameNo");
      cJSON *eng = cJSON_GetObjectItem(item, "engine");
      cJSON *array = cJSON_GetObjectItem(item, "annotations");

      if (cJSON_IsArray(array) && cJSON_IsNumber(fNo))
      {
        char *annStr = cJSON_Print(array);
        engine = std::string(eng->valuestring);
        frameNo = fNo->valueint;
        string s(annStr);
        free(annStr);

        cout << "> read annotations (frame " << frameNo << ", engine " << engine << "): " << s << std::endl;

        return s;
      }
      else
        cout << "JSON is poorely formatted" << endl;
    }
    else
      cout << "> error parsing JSON: " << annotations << endl;
  }

  return "";
#endif
}
//******************************************************************************
//#define DEBUG
int main(int argc, char** argv)
{
  signal(SIGABRT, handler);
  std::srand(std::time(0));

  try {
    boost::asio::io_service io;
    boost::shared_ptr<boost::asio::io_service::work> work(boost::make_shared<boost::asio::io_service::work>(io));
    boost::thread t([&io](){ 
      try {
        io.run();
      }
      catch (std::exception &e)
      {
        std::cout << "caught exception on main thread: " << e.what() << std::endl;
      }
    });

    KeyChain keyChain;
    Name certificateName = keyChain.getDefaultCertificateName();
    // The default Face will connect using a Unix socket, or to "localhost".
    ThreadsafeFace producerFace(io);
    producerFace.setCommandSigningInfo(keyChain, certificateName);
    MemoryContentCache contentCache(&producerFace);
    std::string userId = "peter";
    std::string service = "object_recognizer";
    std::string serviceInstance = "yolo-mock";
    Name servicePrefix("/icear/user");
    servicePrefix.append(userId).append(service);
    
    bool enabled = true;
    bool registrationResultSuccess = false;
    unsigned int frameNo;

    std::map<unsigned int, AnnotationArray> acquiredAnnotations;

    AnnotationPublisher apub(servicePrefix, contentCache, &keyChain, certificateName);
    contentCache.registerPrefix(servicePrefix, 
      bind(&onRegisterFailed, _1, &enabled), 
      (OnRegisterSuccess)bind(&onRegisterSuccess, _1, _2, &registrationResultSuccess),
      [&contentCache, &frameNo, &io, &acquiredAnnotations, serviceInstance, &apub]
      (const ptr_lib::shared_ptr<const Name>& prefix,
            const ptr_lib::shared_ptr<const Interest>& interest, Face& face, 
            uint64_t interestFilterId,
            const ptr_lib::shared_ptr<const InterestFilter>& filter){
        int receivedFrameNo = interest->getName()[-3].toSequenceNumber();

        cout << "---> incoming interest: " 
          << " frame no " << receivedFrameNo << "(latest read " << frameNo 
            << " diff " << (int)frameNo-(int)receivedFrameNo << ") annotations queue:\t" << acquiredAnnotations.size() << endl ;
#ifdef DEBUG
        io.dispatch([&acquiredAnnotations, frameNo, &apub, serviceInstance, &contentCache, interest, &face, receivedFrameNo]()
        {
          // check if we have annotation for this frame
          if (acquiredAnnotations.find(receivedFrameNo) != acquiredAnnotations.end())
          {
            apub.publish(frameNo, acquiredAnnotations.at(receivedFrameNo), serviceInstance);
            cout << "  * published " << receivedFrameNo << endl;
            acquiredAnnotations.erase(acquiredAnnotations.find(receivedFrameNo));
          }
          else
          {
            contentCache.storePendingInterest(interest, face);
            cout << "  * stored interest for " << receivedFrameNo << endl;
          }
        });
#else
        contentCache.storePendingInterest(interest, face);
#endif
      });
      // contentCache.getStorePendingInterest());

    unsigned int n = 10, npublished = 0;
    std::set<int> publishedFrames;

    // Open the feature pipe (from YOLO)
    cout << "> opening pipe..." << std::endl;
#ifndef USE_NANOMSG
    create_pipe(pipeName);
    reopen_readpipe(pipeName, &feature_pipe);

    if(feature_pipe < 0) {
      std::cout << "> failed to open the feature pipe" << std::endl;
      work.reset();
      t.join();
      return -1;
    }
#else
    if (feature_pipe < 0)
    {
        feature_pipe = ipc_setupSubSourceSocket(pipeName);

        if (feature_pipe < 0)
        {
            printf("> failed to setup socket %s: %s (%d)\n", 
                pipeName, ipc_lastError(), ipc_lastErrorCode());
            exit(1);
        }
        else
          printf("> opened feature socket (%s)\n", pipeName);
    }
#endif
    while(enabled){
      
      std::string engine;
      std::string annotations = readAnnotations(feature_pipe, frameNo, engine);

      if (annotations !="")
      {
#ifdef DEBUG
        io.dispatch([frameNo, annotations, &acquiredAnnotations](){
          acquiredAnnotations.insert(std::pair<unsigned int, AnnotationArray>(frameNo, AnnotationArray(annotations)));
        });
#else
        io.dispatch([&apub, frameNo, annotations, engine](){
          apub.publish(frameNo, AnnotationArray(annotations), engine);
        }); 
#endif
      }

      if (!registrationResultSuccess)
        cout << "> prefix registration failed. data won't be served" << std::endl;
    } // while true
  } catch (std::exception& e) {
    cout << "exception: " << e.what() << endl;
  }
  return 0;
}

/**
 * This is called to print an error if the MemoryContentCache can't register
 * with the local forwarder.
 * @param prefix The prefix Name to register.
 * @param enabled On success or error, set *enabled = false.
 */
static void
onRegisterFailed(const ptr_lib::shared_ptr<const Name>& prefix, bool* enabled)
{
  *enabled = false;
  cout << "> Failed to register prefix " << prefix->toUri() << endl;
}

static void
onRegisterSuccess
  (const ptr_lib::shared_ptr<const Name>& registeredPrefix,
   uint64_t registeredPrefixId, bool* result)
{
  *result = true;
  cout << "> Successfully registered prefix " << *registeredPrefix << std::endl;
}

#else // NDN_CPP_HAVE_PROTOBUF

#include <iostream>

using namespace std;

int main(int argc, char** argv)
{
  cout <<
    "This program uses Protobuf but it is not installed. Install it and ./configure again." << endl;
}

#endif // NDN_CPP_HAVE_PROTOBUF

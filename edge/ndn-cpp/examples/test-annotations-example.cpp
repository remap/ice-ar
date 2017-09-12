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
#include <execinfo.h>
#include <set>
#include <ctime>
#include <signal.h>
#include <cassert>
#include <sys/types.h>
#include <sys/stat.h>
#include <fcntl.h>

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
      (cache_, annotationName, 2000, keyChain_, certName_, metaInfo,
       Blob((const uint8_t*)&content[0], content.size()), contentSegmentSize);

    std::cout << "*   published " << annotationName 
      << " (has segments: " << (metaInfo.getHasSegments() ? "YES, content size: " : "NO, content size: ") 
      << content.size()  << ")" << std::endl;
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

int main(int argc, char** argv)
{
  signal(SIGABRT, handler);
  std::srand(std::time(0));

  try {
    KeyChain keyChain;
    Name certificateName = keyChain.getDefaultCertificateName();
    // The default Face will connect using a Unix socket, or to "localhost".
    Face producerFace;
    producerFace.setCommandSigningInfo(keyChain, certificateName);
    MemoryContentCache contentCache(&producerFace);
    bool enabled = true;
    Face consumerFace;

    std::string userId = "peter";
    std::string service = "object_recognizer";
    std::string serviceInstance = "yolo-mock";
    
    
    Name servicePrefix("/icear/user");
    servicePrefix.append(userId).append(service);
    
    AnnotationPublisher apub(servicePrefix, contentCache, &keyChain, certificateName);
    unsigned int n = 10, npublished = 0, frameNo = 0;
    std::set<int> publishedFrames;
    bool registrationResultSuccess = false;

    // Open the feature pipe (from YOLO)
    std::string feature_pipe_name = "/tmp/feature_fifo";
    int feature_pipe = open(feature_pipe_name.c_str(), O_RDONLY);
    if(feature_pipe == -1){
      std::cout<<"Fail to open the feature pipe"<<std::endl;
      return -1;
    }
    char feature_buf[4096];
    int frame_num = 0;
    while(true){
      int feature_len = -1;
      while(feature_len<=0)
          feature_len  = read(feature_pipe, feature_buf, 4096);
      string feature(feature_buf, feature_len);
      std::cout << "Feature read: "<<feature<<std::endl;
      AnnotationArray aa(feature);
      
      //contentCache.storePendingInterest(interest, face);
      apub.publish(frame_num, aa, serviceInstance);
      frame_num++;

    }
      
    
   
    // contentCache.registerPrefix
    //   (servicePrefix, bind(&onRegisterFailed, _1, &enabled), 
    //    (OnRegisterSuccess)bind
    //     (&onRegisterSuccess, _1, _2, &registrationResultSuccess),
    //     [&apub, &publishedFrames, &contentCache, serviceInstance](const ptr_lib::shared_ptr<const Name>& prefix,
    //         const ptr_lib::shared_ptr<const Interest>& interest, Face& face, 
    //         uint64_t interestFilterId,
    //         const ptr_lib::shared_ptr<const InterestFilter>& filter)
    //     {
    //           try {
    //             int frameNo = interest->getName()[-3].toSequenceNumber();
    //             std::cout << " +  will generate annotations for frame " << frameNo << std::endl;
                
    //             assert(publishedFrames.find(frameNo) == publishedFrames.end());

    //             publishedFrames.insert(frameNo);
                
    //             int nAnnotations = std::rand()%7+1;
    //             AnnotationArray aa = generateAnnotationArray(nAnnotations);

    //             //contentCache.storePendingInterest(interest, face);
    //             apub.publish(frameNo, aa, serviceInstance);
    //           }
    //           catch (std::runtime_error &e)
    //           {
    //             std::cout << "ERROR! invalid interest: " << e.what() << std::endl;
    //           }
    //     });

    unsigned int nFetched = 0, nFailed = 0;
    AnnotationConsumer acon(servicePrefix, serviceInstance, &consumerFace);

    srand (time(NULL));
    while (enabled) {
      if (registrationResultSuccess && npublished < n)
      {
        if (0)
        { // spawn annotations fetching process
          acon.fetch(frameNo, [&nFetched](unsigned int frameNo, const AnnotationArray&){
            nFetched++;
          }, 
          [&nFailed](unsigned int frameNo, GeneralizedContent::ErrorCode errorCode, const string& message){
            nFailed++;
          });
        }

        frameNo++;
        npublished++;
      }

      producerFace.processEvents();
      consumerFace.processEvents();
      // We need to sleep for a few milliseconds so we don't use 100% of the CPU.
      usleep(10000);

#ifndef RUN_FOREVER
      enabled = (nFetched+nFailed < n);
#endif
    }
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
  cout << "Failed to register prefix " << prefix->toUri() << endl;
}

static void
onRegisterSuccess
  (const ptr_lib::shared_ptr<const Name>& registeredPrefix,
   uint64_t registeredPrefixId, bool* result)
{
  *result = true;
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

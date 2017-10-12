// 
// frame-io.cpp
//
//  Created by Peter Gusev on 17 March 2016.
//  Copyright 2013-2016 Regents of the University of California
//

#include <stdexcept>
#include <sstream>
#include <fcntl.h>
#include <errno.h>
#include <string.h>
#include <stdlib.h>
#include <errno.h>
#include <sys/types.h>
#include <sys/stat.h>

#include "frame-io.hpp"

#ifdef HAVE_NANOMSG
#include "ipc-shim.h"
#endif

using namespace std;

RawFrame::RawFrame(unsigned int width, unsigned int height):
bufferSize_(0), width_(width), height_(height){}

//******************************************************************************
ArgbFrame::ArgbFrame(unsigned int width, unsigned int height):
RawFrame(width, height)
{
	unsigned long bufSize = getFrameSizeInBytes();
	setBuffer(bufSize, boost::shared_ptr<uint8_t>(new uint8_t[bufSize]));
}

void ArgbFrame::getFrameResolution(unsigned int& width, unsigned int& height) const
{
	width = width_;
	height = height_;
}

unsigned long ArgbFrame::getFrameSizeInBytes() const
{
	return width_*height_*4;
}

//******************************************************************************
void FileFrameStorage::openFile()
{
    if (path_ == "")
        throw runtime_error("invalid file path provided");

    file_ = openFile_impl(path_);

    if (!file_)
        throw runtime_error("couldn't create sink file at path "+path_);

    fseek(file_, 0, SEEK_END);
    fileSize_ = ftell(file_);
    rewind(file_);
}

void FileFrameStorage::closeFile()
{
    if (file_)
    {
        fclose(file_);
        file_ = nullptr;
    }
}

//******************************************************************************
IFrameSink& FileSink::operator<<(const RawFrame& frame)
{
    if (writeFrameNo_)
    {
        unsigned int fNo = frame.getFrameNumber();
        fwrite(&fNo, sizeof(unsigned int), 1, file_);
    }
    
	int r = fwrite(frame.getBuffer().get(), sizeof(uint8_t), frame.getFrameSizeInBytes(), file_);
    isLastWriteSuccessful_ = (r == frame.getFrameSizeInBytes());

    return *this;
}

FILE* FileSink::openFile_impl(string path)
{
    return fopen(path.c_str(), "wb");
}

//******************************************************************************
PipeSink::PipeSink(const std::string& path):pipePath_(path), pipe_(-1), 
isLastWriteSuccessful_(false), isWriting_(false), writeFrameNo_(false)
{ 
    createPipe(pipePath_);
    openPipe(pipePath_);
}

PipeSink::~PipeSink()
{
    if (pipe_ > 0) close(pipe_);
}

IFrameSink& PipeSink::operator<<(const RawFrame& frame)
{
    if (pipe_ < 0) openPipe(pipePath_);
    if (pipe_ > 0) 
    {
        isWriting_ = true;

        uint8_t *buf = frame.getBuffer().get();
        int r = 0;

        if (writeFrameNo_)
        {
            unsigned int fNo = frame.getFrameNumber();
            r = writeExactly((uint8_t*)&fNo, sizeof(fNo), pipe_);
        }

        r = writeExactly(buf, frame.getFrameSizeInBytes(), pipe_);

        isLastWriteSuccessful_ = (r == frame.getFrameSizeInBytes());

        isWriting_ = false;
    }
    else
        isLastWriteSuccessful_ = false;

    return *this;
}

int PipeSink::writeExactly(uint8_t *buffer, size_t bufSize, int pipe)
{   
    int written = 0, r = 0; 
    bool keepWriting = false;

    do {
        r = write(pipe, buffer+written, bufSize-written);
        if (r > 0) written += r;
        keepWriting = (r > 0 && written != bufSize) || (r < 0 && errno == EAGAIN);
    } while (keepWriting);

    if (written != bufSize)
    {
        std::stringstream ss;
        ss << "something bad happened when writing to pipe "
           << pipePath_ << ": " 
           << strerror(errno) << "(" << errno << ")" << std::endl;
        throw std::runtime_error(ss.str());
    }

    return written;
}

void PipeSink::createPipe(const std::string& path)
{
    int res = mkfifo(path.c_str(), 0644);

    if (res < 0 && errno != EEXIST)
    {
        std::stringstream ss;
        ss << "Error creating pipe(" << errno << "): " << strerror(errno);
        throw std::runtime_error(ss.str());
    }
}

void PipeSink::openPipe(const std::string& path)
{
    pipe_ = open(path.c_str(), O_WRONLY|O_NONBLOCK|O_EXCL);
}

#ifdef HAVE_NANOMSG
#include <iostream>

NanoMsgSink::NanoMsgSink(const std::string& handle)
{
    nnSocket_ = ipc_setupPubSourceSocket(handle.c_str());
    if (nnSocket_ < 0)
        throw std::runtime_error(ipc_lastError());
}

NanoMsgSink::~NanoMsgSink()
{
    ipc_shutdownSocket(nnSocket_);
}

IFrameSink& NanoMsgSink::operator<<(const RawFrame& frame) 
{
    uint8_t *buf = frame.getBuffer().get();

    if (writeFrameNo_)
    {
        unsigned int fNo = frame.getFrameNumber();
        isLastWriteSuccessful_ = (ipc_sendFrame(nnSocket_, fNo, buf, frame.getFrameSizeInBytes()) > 0);
    }
    else
        isLastWriteSuccessful_ = (ipc_sendData(nnSocket_, buf, frame.getFrameSizeInBytes()) > 0);

    return *this;
}

#endif

//******************************************************************************
FileFrameSource::FileFrameSource(const string& path):FileFrameStorage(path),
current_(0), readError_(false)
{ 
    openFile(); 
}

IFrameSource& FileFrameSource::operator>>(RawFrame& frame) noexcept
{
    uint8_t *buf = frame.getBuffer().get();
    size_t readBytes = fread(buf, sizeof(uint8_t), frame.getFrameSizeInBytes(), file_);
    current_ = ftell(file_);
    readError_ = (readBytes != frame.getFrameSizeInBytes());

    // {
    //     stringstream msg;
    //     msg << "error trying to read frame of " << frame.getFrameSizeInBytes()
    //         << " bytes from file (read " << readBytes << " bytes): error " 
    //         << ferror(file_) << " eof: " << feof(file_) 
    //         << " current " << current_ << " size " << fileSize_ 
    //         << " ftell " << ftell(file_);
    //     throw runtime_error(msg.str());
    // }
    return *this;
}

bool FileFrameSource::checkSourceForFrame(const std::string& path, 
    const RawFrame& frame)
{
    FILE *f = fopen(path.c_str(), "rb");

    if (!f) 
        return false;
    else
        fseek (f , 0 , SEEK_END);

    long lSize = ftell(f);
    int nFrames = lSize%frame.getFrameSizeInBytes();
    fclose(f);

    return (nFrames == 0);
}

FILE* FileFrameSource::openFile_impl(string path)
{
    return fopen(path.c_str(), "rb");
}

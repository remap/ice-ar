// 
// pipe-utils.h
//
// Copyright (c) 2017. UCLA. All rights reserved
// Author: Peter Gusev
//

#ifndef __pipe_utils_h__
#define __pipe_utils_h__

#include <fcntl.h>
#include <unistd.h>

int create_pipe(const char* fname);
void reopen_readpipe(const char* fname, int* pipe);
int writeExactly(uint8_t *buffer, size_t bufSize, int pipe);

#endif

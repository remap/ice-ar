// 
// 
//
// Copyright (c) 2017. UCLA. All rights reserved
// Author: Peter Gusev
//

#include <sys/types.h>
#include <sys/stat.h>
#include <errno.h>
#include <string.h>
#include <stdint.h>
#include <stdio.h>

#include "pipe-utils.h"

int create_pipe(const char* fname)
{
    int res = 0;
    do {
        res = mkfifo(fname, 0644);
        if (res < 0 && errno != EEXIST)
        {
            printf("error creating pipe %s (%d): %s\n", fname, errno, strerror(errno));
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

int writeExactly(uint8_t *buffer, size_t bufSize, int pipe)
{   
    int written = 0, r = 0; 
    int keepWriting = 0;

    do {
        r = write(pipe, buffer+written, bufSize-written);
        if (r > 0) written += r;
        keepWriting = (r > 0 && written != bufSize) || (r < 0 && errno == EAGAIN);
    } while (keepWriting == 1);

    return written;
}

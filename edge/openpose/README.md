1. Compile & install [openpose](https://github.com/CMU-Perceptual-Computing-Lab/openpose)

2. Build ICE-openpose processor like:

    ```
    g++ -std=c++11 ice-openpose.cpp ipc-shim.c pipe-utils.c jsonOsstream.cpp -o ice-openpose -L/usr/local/lib  -lopenpose -lpthread -lopencv_highgui -lopencv_objdetect -lopencv_imgcodecs -lopencv_imgproc -lopencv_core -lgflags -lnanomsg
    ```
3. Preview as:

    ```
    ffplay -f rawvideo -vcodec rawvideo -s 320x180 -pix_fmt bgr24 -i /tmp/openpose-out
    ```
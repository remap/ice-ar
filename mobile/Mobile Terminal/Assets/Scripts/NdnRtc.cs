/**
 * Copyright (C) 2017 Regents of the University of California.
 * @author: Peter Gusev <peter@remap.ucla.edu>
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using System;
using GoogleARCore.TextureReader;
using System.Threading;

[UnmanagedFunctionPointerAttribute (CallingConvention.Cdecl)]
public delegate void NdnRtcLibLogHandler ([MarshalAs(UnmanagedType.LPStr)]string message);

[UnmanagedFunctionPointerAttribute (CallingConvention.Cdecl)]
public delegate IntPtr FrameFetcherBufferAlloc ([MarshalAs(UnmanagedType.LPStr)]string frameName,
                                                int width, int heght);

[UnmanagedFunctionPointerAttribute (CallingConvention.Cdecl)]
public delegate void FrameFetcherFrameFetched (FrameInfo frameInfo,
                                               int width, int height, IntPtr frameBuffer);


public struct LocalStreamParams
{
	public string basePrefix;
	public int signingOn;
	public int fecOn;
	public int typeIsVideo;
	public int ndnSegmentSize;
	public int frameWidth;
	public int frameHeight;
	public int startBitrate;
	public int maxBitrate;
	public int gop;
	public int dropFrames;
	public string streamName;
	public string threadName;
    public string storagePath;
}

public struct FrameInfo
{
    [MarshalAs(UnmanagedType.I8)]
    public Int64 timestamp_;
    [MarshalAs(UnmanagedType.I4)]
    public int playbackNo_;
    [MarshalAs(UnmanagedType.LPStr)]
    public string ndnName_;
}

public class NdnRtcWrapper
{
    [DllImport ("ndnrtc")]
    public static extern IntPtr ndnrtc_getVersion();

	[DllImport ("ndnrtc")]
	public static extern bool ndnrtc_init (string hostname, string path, 
	                                       string signingIdentity, string instanceId, NdnRtcLibLogHandler logHandler);

	[DllImport ("ndnrtc")]
	public static extern void ndnrtc_deinit ();

	[DllImport ("ndnrtc")]
	public static extern IntPtr ndnrtc_createLocalStream (LocalStreamParams p, 
	                                                     NdnRtcLibLogHandler loggerSink);

	[DllImport ("ndnrtc")]
	public static extern IntPtr ndnrtc_destroyLocalStream (IntPtr stream);

	[DllImport ("ndnrtc")]
	// we use IntPtr return type instead of string, because otherwise runtime will
	// try to free the pointer, which we don't want to happen (it is the property of 
	// unmanaged code in this case)
	public static extern IntPtr ndnrtc_LocalStream_getPrefix (IntPtr stream);

	[DllImport ("ndnrtc")]
	public static extern IntPtr ndnrtc_LocalStream_getBasePrefix (IntPtr stream);

	[DllImport ("ndnrtc")]
	public static extern IntPtr ndnrtc_LocalStream_getStreamName (IntPtr stream);

	[DllImport ("ndnrtc")]
	public static extern int ndnrtc_LocalVideoStream_incomingI420Frame (IntPtr stream, 
	                                                                   uint width, uint height, uint strideY, uint strideU, uint strideV,
	                                                                   IntPtr yPlane, IntPtr uPlane, IntPtr vPlane);

	[DllImport ("ndnrtc")]
	public static extern int ndnrtc_LocalVideoStream_incomingNV21Frame (IntPtr stream, 
		uint width, uint height, uint strideY, uint strideUV, IntPtr yPlane, IntPtr uvPlane);

	[DllImport ("ndnrtc")]
	public static extern int ndnrtc_LocalVideoStream_incomingArgbFrame (IntPtr stream,
	uint width, uint height, IntPtr argbFrameData, uint frameSize);

    [DllImport ("ndnrtc")]
    public static extern FrameInfo ndnrtc_LocalVideoStream_getLastPublishedInfo(IntPtr stream);

    [DllImport ("ndnrtc")]
    public static extern void ndnrtc_FrameFetcher_fetch(IntPtr stream,
                                                        string frameName,
                                                        FrameFetcherBufferAlloc bufferAllocFunc,
                                                        FrameFetcherFrameFetched frameFetched);
}

public class LocalVideoStream
{
	private IntPtr ndnrtcHandle_;
	private string streamName, basePrefix, fullPrefix;
	static private NdnRtcLibLogHandler sinkCallbackDelegate;

	public LocalVideoStream (LocalStreamParams p)
	{
		if (sinkCallbackDelegate == null)
			sinkCallbackDelegate = new NdnRtcLibLogHandler (loggerSinkHandler);

        Debug.Log ("Will create ndnrtc local video stream...");
		ndnrtcHandle_ = NdnRtcWrapper.ndnrtc_createLocalStream (p, sinkCallbackDelegate);
        Debug.Log ("Created ndnrtc local video stream");

		basePrefix = Marshal.PtrToStringAnsi (NdnRtcWrapper.ndnrtc_LocalStream_getBasePrefix (ndnrtcHandle_));
		fullPrefix = Marshal.PtrToStringAnsi (NdnRtcWrapper.ndnrtc_LocalStream_getPrefix (ndnrtcHandle_));
		streamName = Marshal.PtrToStringAnsi (NdnRtcWrapper.ndnrtc_LocalStream_getStreamName (ndnrtcHandle_));

		Debug.Log ("Initialized ndnrtc stream " + streamName + " (full prefix " + fullPrefix + ")");
	}

	~LocalVideoStream ()
	{
		NdnRtcWrapper.ndnrtc_destroyLocalStream (ndnrtcHandle_);
	}

    public IntPtr getHandle() 
    {
        return ndnrtcHandle_;
    }

	public FrameInfo processIncomingFrame (TextureReaderApi.ImageFormatType format, int width, int height, IntPtr pixelBuffer, int bufferSize)
	{
		// Debug.Log ("[ndnrtc::videostream] incoming image format " + format + " size " + width + "x" + height);

		unsafe { 
			byte* ptr = (byte*)pixelBuffer.ToPointer ();
			int offset = 0;

			for (int i = 0; i < height; i++) {
				for (int j = 0; j < width; j++) {

					float r = (float)ptr [offset + 0];
					float g = (float)ptr [offset + 1];
					float b = (float)ptr [offset + 2];
					float a = (float)ptr [offset + 3];
					ptr [offset + 0] = (byte)a;
					ptr [offset + 1] = (byte)r;
					ptr [offset + 2] = (byte)g;
					ptr [offset + 3] = (byte)b;
					offset += 4;

				}
			}
		}

        // publish frame using NDN-RTC
        // return: res < 0 -- frame was skipped due to encoder decision (or library was busy publishing frame)
        //         res >= 0 -- playback number of published frame
        int res = NdnRtcWrapper.ndnrtc_LocalVideoStream_incomingArgbFrame (ndnrtcHandle_, (uint)width, (uint)height, pixelBuffer, (uint)bufferSize);

        // query additional latest published frame information
        FrameInfo finfo = NdnRtcWrapper.ndnrtc_LocalVideoStream_getLastPublishedInfo (ndnrtcHandle_);
        Debug.Log ("res: " + res + " frameNo: " + finfo.playbackNo_ + " timestamp: " + finfo.timestamp_ + " ndn name: " + finfo.ndnName_);

        if (res < 0) finfo.playbackNo_ = -1;
        // return res > 0 ? finfo.playbackNo_ : res;
        return finfo;
	}

	static private void loggerSinkHandler (string logMessage)
	{
		Debug.Log ("[ndnrtc::videostream] " + logMessage);
	}
}


public delegate void OnFrameFetched(FrameInfo finfo, int width, int height, byte [] argbBuffer);
public delegate void OnFrameFetchFailure(string frameName);

public class FrameFetcher 
{
    private IntPtr frameBuffer_;
    private FrameFetcherBufferAlloc bufferAllocDelegate;
    private FrameFetcherFrameFetched frameFetchedDelegate;
    private OnFrameFetched onFrameFetched_;
    private OnFrameFetchFailure onFrameFetchFailure_;

    public FrameFetcher()
    {
        frameBuffer_ = IntPtr.Zero;
    }

    ~FrameFetcher()
    {
        if (frameBuffer_ != IntPtr.Zero)
            Marshal.FreeHGlobal(frameBuffer_);
    }

    public void fetch(string frameName, LocalVideoStream stream, 
                      OnFrameFetched onFrameFetched, OnFrameFetchFailure onFrameFetchFailure)
    {
        onFrameFetched_ = onFrameFetched;
        onFrameFetchFailure_ = onFrameFetchFailure;

        bufferAllocDelegate = new FrameFetcherBufferAlloc(bufferAllocate);
        frameFetchedDelegate = new FrameFetcherFrameFetched(frameFetched);

        NdnRtcWrapper.ndnrtc_FrameFetcher_fetch(stream.getHandle(),
                                                frameName,
                                                bufferAllocDelegate,
                                                frameFetchedDelegate);
    }

    private IntPtr bufferAllocate (string frameName, int width, int height)
    {
        int bytesSize = width*height*4;

        Debug.Log ("[frame-fetcher] Buffer allocate "+bytesSize + " bytes");

        if (frameBuffer_ != IntPtr.Zero)
            Marshal.FreeHGlobal(frameBuffer_);
        frameBuffer_ = Marshal.AllocHGlobal(width*height*4); // ARGB frame

        return frameBuffer_;
    }

    private void frameFetched(FrameInfo frameInfo, int width, int height, IntPtr bufferArgb)
    {
        if (width > 0 && height > 0)
        {
            Debug.Log ("[frame-fetcher] Frame fetched: "+frameInfo.ndnName_);
            
            // copy to managed code and return
            int len = width*height*4;
            byte [] buf = new byte[len];
            Marshal.Copy(bufferArgb, buf, 0, len);

            FrameInfo finfoCopy = frameInfo;

            onFrameFetched_(finfoCopy, width, height, buf);
        }
        else
        {
            Debug.Log ("[frame-fetcher] Frame couldn't be fetched: "+frameInfo.ndnName_);
            onFrameFetchFailure_(frameInfo.ndnName_);
        }
    }
}

public class FrameFetchingTask {
    public string frameName_;
    private OnFrameFetched onFrameFetched_;
    private OnFrameFetchFailure onFrameFetchFailure_;
    private FrameFetcher ff_;
    private LocalVideoStream stream_;
    private OnCompleted onCompleted_;

    public delegate void OnCompleted(FrameFetchingTask ffTask);

    /**
     * Will call callbacks on current thread
     */
    public FrameFetchingTask(string frameName, LocalVideoStream stream,
                             OnFrameFetched onFrameFetched, 
                             OnFrameFetchFailure onFrameFetchedFailure)
    {
        frameName_ = frameName;
        stream_ = stream;
        onFrameFetched_ = onFrameFetched;
        onFrameFetchFailure_ = onFrameFetchedFailure;
    }

    /**
     * Will call onCompleted on current thread
     */
    public void run(OnCompleted onCompleted)
    {
        onCompleted_ = onCompleted;

        ff_ = new FrameFetcher();
        ff_.fetch(frameName_, stream_,
                 delegate(FrameInfo fi, int w, int h, byte[] argbBuffer){
                        Debug.Log ("[ff-task]: succesfully fetched frame "+ fi.ndnName_);
                        onFrameFetched_(fi, w, h, argbBuffer);
                        onCompleted(this);
                    },
                    delegate(string frameName){
                        Debug.Log ("[ff-task]]: failed to fetch "+frameName);
                        onFrameFetchFailure_(frameName);
                        onCompleted(this);
                    });
    }
}

public class NdnRtc : MonoBehaviour
{
    static private Thread frameFetchingThread_;
    static private Semaphore queueSem_;
    static private System.Collections.Generic.Queue<FrameFetchingTask> frameFetchingTaskQueue_;
    static private HashSet<FrameFetchingTask> activeTasks_;
    static private bool runFrameFetching_;

	static private NdnRtcLibLogHandler libraryCallbackDelegate;
	static public LocalVideoStream videoStream;

    public static void fetch(string frameName, LocalVideoStream stream, 
                             OnFrameFetched onFrameFetched, 
                             OnFrameFetchFailure onFrameFetchFailure)
    {
        frameFetchingTaskQueue_.Enqueue(new FrameFetchingTask(frameName, stream, 
                                                              onFrameFetched, 
                                                              onFrameFetchFailure));
        Debug.Log("[ff-task]: enqueued task for "+frameName+". queue size "+frameFetchingTaskQueue_.Count);

        queueSem_.Release();
    }

	public static void Initialize (string signingIdentity, string instanceId)
	{
		if (libraryCallbackDelegate == null) {
			libraryCallbackDelegate = new NdnRtcLibLogHandler (ndnrtcLogHandler);
		}

		bool res;

		try {
            string version = Marshal.PtrToStringAnsi ( NdnRtcWrapper.ndnrtc_getVersion() );
            Debug.Log ( "NDN-RTC version " + version );

			res = NdnRtcWrapper.ndnrtc_init ("localhost", Application.persistentDataPath, signingIdentity, 
				instanceId, libraryCallbackDelegate);

			if (res) {
				LocalStreamParams p = new LocalStreamParams ();

				p.basePrefix = signingIdentity + "/" + instanceId;
				p.signingOn = 1;
				p.dropFrames = 1;
				p.fecOn = 1;
				p.frameHeight = 180;
				p.frameWidth = 320;
				p.gop = 30;
				p.startBitrate = 300;
				p.maxBitrate = 7000;
				p.ndnSegmentSize = 8000;
				p.typeIsVideo = 1;
				p.streamName = "back_camera";
				p.threadName = "vp9";
                p.storagePath = Application.persistentDataPath + "/ndnrtc_storage";

				videoStream = new LocalVideoStream (p);

                runFrameFetching_ = true;
                queueSem_ = new Semaphore(0, 30); // up to 30 requests. why not?...
                activeTasks_ = new HashSet<FrameFetchingTask>();
                frameFetchingTaskQueue_ = new System.Collections.Generic.Queue<FrameFetchingTask>();
                frameFetchingThread_ = new Thread(new ThreadStart(delegate() {
                    while (runFrameFetching_)
                    {
                        Debug.Log("[ff-task-worker]: waiting for new tasks...");
                        // lock on semaphore / event
                        queueSem_.WaitOne();

                        // deque
                        FrameFetchingTask ffTask = frameFetchingTaskQueue_.Dequeue();

                        Debug.Log("[ff-task-worker]: running task for " + ffTask.frameName_);
                        activeTasks_.Add(ffTask);
                        ffTask.run(delegate(FrameFetchingTask fft){
                            Debug.Log("[ff-task-worker]: task completed: "+fft.frameName_);
                            // cleanup when we are done
                            activeTasks_.Remove(fft);
                        });
                    } // while
                }));
                frameFetchingThread_.Start();
			}
		} catch (System.Exception e) {
			Debug.LogError ("Error initializing NDN-RTC: " + e.Message);
		}
	}

	public static void Release ()
	{
        runFrameFetching_ = false;
        queueSem_.Release();
        frameFetchingThread_.Join();
        NdnRtcWrapper.ndnrtc_deinit ();
	}

	// Use this for initialization
	void Start ()
	{
		
	}
	
	// Update is called once per frame
	void Update ()
	{
		
	}

	static private void ndnrtcLogHandler (string message)
	{
		Debug.Log ("[ndnrtc] " + message);
	}
}

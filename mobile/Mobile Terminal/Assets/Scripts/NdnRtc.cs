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

[UnmanagedFunctionPointerAttribute (CallingConvention.Cdecl)]
public delegate void NdnRtcLibLogHandler (string message);

public struct LocalStreamParams
{
	public string basePrefix;
	public int signingOn;
	public int fecOn;
	public int typeIsVideo;
	public int ndnSegmentSize;
	public int ndnDataFreshnessPeriodMs;
	public int frameWidth;
	public int frameHeight;
	public int startBitrate;
	public int maxBitrate;
	public int gop;
	public int dropFrames;
	public string streamName;
	public string threadName;
}

public class NdnRtcWrapper
{
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

		ndnrtcHandle_ = NdnRtcWrapper.ndnrtc_createLocalStream (p, sinkCallbackDelegate);

		basePrefix = Marshal.PtrToStringAnsi (NdnRtcWrapper.ndnrtc_LocalStream_getBasePrefix (ndnrtcHandle_));
		fullPrefix = Marshal.PtrToStringAnsi (NdnRtcWrapper.ndnrtc_LocalStream_getPrefix (ndnrtcHandle_));
		streamName = Marshal.PtrToStringAnsi (NdnRtcWrapper.ndnrtc_LocalStream_getStreamName (ndnrtcHandle_));

		Debug.Log ("Initialized ndnrtc stream " + streamName + " (full prefix " + fullPrefix + ")");
	}

	~LocalVideoStream ()
	{
		NdnRtcWrapper.ndnrtc_destroyLocalStream (ndnrtcHandle_);
	}

	public int processIncomingFrame (TextureReaderApi.ImageFormatType format, int width, int height, IntPtr pixelBuffer, int bufferSize)
	{
		Debug.Log ("[ndnrtc::videostream] incoming image format " + format + " size " + width + "x" + height);

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

//		uint offset = imageData.stride;
//		uint yPlaneSize = imageData.stride * imageData.height;
//		uint vPlaneSize = (imageData.stride / 2) * (imageData.height / 2);
//		uint uvPLaneSize = yPlaneSize / 2;

			//GCHandle pinnedBuffer = GCHandle.Alloc (switchAB, GCHandleType.Pinned);

//		IntPtr yPlane = new IntPtr (pinnedBuffer.AddrOfPinnedObject ().ToInt64 () + offset);
//		offset += yPlaneSize;
//		IntPtr uvPlane = new IntPtr (pinnedBuffer.AddrOfPinnedObject ().ToInt64 () + offset);

			//IntPtr buffer = new IntPtr (pinnedBuffer.AddrOfPinnedObject ().ToInt64 ());

//		public static extern int ndnrtc_LocalVideoStream_incomingARGBFrame (IntPtr stream,
//			uint width, uint height, IntPtr argbFrameData, uint frameSize);
			int frameNo = NdnRtcWrapper.ndnrtc_LocalVideoStream_incomingArgbFrame (ndnrtcHandle_, (uint)width, (uint)height, pixelBuffer, (uint)bufferSize);
			//Debug.Log ("frameNo = " + frameNo);
			//pinnedBuffer.Free ();

			return frameNo;
	}


	static private void loggerSinkHandler (string logMessage)
	{
		Debug.Log ("[ndnrtc::videostream] " + logMessage);
	}
}


public class NdnRtc : MonoBehaviour
{

	static private NdnRtcLibLogHandler libraryCallbackDelegate;
	static public LocalVideoStream videoStream;

	public static void Initialize (string signingIdentity, string instanceId)
	{
		if (libraryCallbackDelegate == null) {
			libraryCallbackDelegate = new NdnRtcLibLogHandler (ndnrtcLogHandler);
		}

		bool res;

		try {
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
				p.ndnDataFreshnessPeriodMs = 2000;
				p.ndnSegmentSize = 8000;
				p.typeIsVideo = 1;
				p.streamName = "back_camera";
				p.threadName = "vp9";

				videoStream = new LocalVideoStream (p);
			}
		} catch (System.Exception e) {
			Debug.LogError ("Error initializing NDN-RTC: " + e.Message);
		}
	}

	public static void Release ()
	{
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

using UnityEngine;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System;


public class KinectWrapper
{
	// constants
	public static class Constants
	{
		public const int BodyCount = 6;
		public const int JointCount = 25;
	}
	
	/// Data structures for interfacing C# with the native wrapper

    [Flags]
    public enum NuiInitializeFlags : uint
    {
		UsesAudio = 0x10000000,
        UsesDepthAndPlayerIndex = 0x00000001,
        UsesColor = 0x00000002,
        UsesSkeleton = 0x00000008,
        UsesDepth = 0x00000020,
		UsesHighQualityColor = 0x00000040
    }
	
	public enum NuiErrorCodes : uint
	{
		FrameNoData = 0x83010001,
		StreamNotEnabled = 0x83010002,
		ImageStreamInUse = 0x83010003,
		FrameLimitExceeded = 0x83010004,
		FeatureNotInitialized = 0x83010005,
		DeviceNotGenuine = 0x83010006,
		InsufficientBandwidth = 0x83010007,
		DeviceNotSupported = 0x83010008,
		DeviceInUse = 0x83010009,
		
		DatabaseNotFound = 0x8301000D,
		DatabaseVersionMismatch = 0x8301000E,
		HardwareFeatureUnavailable = 0x8301000F,
		
		DeviceNotConnected = 0x83010014,
		DeviceNotReady = 0x83010015,
		SkeletalEngineBusy = 0x830100AA,
		DeviceNotPowered = 0x8301027F,
	}

    public enum JointType : int
    {
        HipCenter = 0,
        Spine = 1,
        Neck = 2,
        Head = 3,
        ShoulderLeft = 4,
        ElbowLeft = 5,
        WristLeft = 6,
        HandLeft = 7,
        ShoulderRight = 8,
        ElbowRight = 9,
        WristRight = 10,
        HandRight = 11,
        HipLeft = 12,
        KneeLeft = 13,
        AnkleLeft = 14,
        FootLeft = 15,
        HipRight = 16,
        KneeRight = 17,
        AnkleRight = 18,
        FootRight = 19,
        SpineShoulder = 20,
        HandTipLeft = 21,
        ThumbLeft = 22,
        HandTipRight = 23,
        ThumbRight = 24,
		Count = 25
    }

    public enum TrackingState
    {
        NotTracked = 0,
        Inferred = 1,
        Tracked = 2
    }

	public enum HandState
    {
        Unknown = 0,
        NotTracked = 1,
        Open = 2,
        Closed = 3,
        Lasso = 4
    }
	
	public enum TrackingConfidence
    {
        Low = 0,
        High = 1
    }

    [Flags]
    public enum ClippedEdges
    {
        None = 0,
        Right = 1,
        Left = 2,
        Top = 4,
        Bottom = 8
    }

	[StructLayout(LayoutKind.Sequential)]
	public struct Joint
    {
    	public JointType jointType;
    	public TrackingState trackingState;
    	public Vector3 position;
		public Quaternion orientation;
    }
	
	[StructLayout(LayoutKind.Sequential)]
	public struct BodyData
    {
        public Int64 liTrackingID;
        public Vector3 position;
		public Quaternion orientation;
		
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 25, ArraySubType = UnmanagedType.Struct)]
        public Joint[] joint;
		
		public HandState leftHandState;
		public TrackingConfidence leftHandConfidence;
		public HandState rightHandState;
		public TrackingConfidence rightHandConfidence;
		
        public uint dwClippedEdges;
        public short bIsTracked;
		public short bIsRestricted;
    }
	
	[StructLayout(LayoutKind.Sequential)]
    public struct BodyFrame
    {
        public Int64 liRelativeTime;
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 6, ArraySubType = UnmanagedType.Struct)]
        public BodyData[] bodyData;
        public Vector4 floorClipPlane;
		
		public BodyFrame(bool bInit)
		{
			liRelativeTime = 0;
			floorClipPlane = Vector4.zero;

			bodyData = new BodyData[Constants.BodyCount];
			
			for(int i = 0; i < Constants.BodyCount; i++)
			{
				bodyData[i].joint = new Joint[Constants.JointCount];
			}
		}
    }
	

	// Wrapped native functions
	
	// Pings the server
	[DllImportAttribute(@"Kinect2UnityClient.dll")]
	public static extern int PingKinect2Server();

	// Initializes the default Kinect sensor
	[DllImportAttribute(@"Kinect2UnityClient.dll")]
	public static extern int InitDefaultKinectSensor(NuiInitializeFlags dwFlags, bool bEnableEvents);
	
	// Shuts down the opened Kinect2 sensor
	[DllImportAttribute(@"Kinect2UnityClient.dll")]
	public static extern void ShutdownKinectSensor();
	
	// Returns the maximum number of the bodies
	[DllImportAttribute(@"Kinect2UnityClient.dll")]
	public static extern int GetBodyCount();
	
	// Returns the latest body frame data available
	[DllImportAttribute(@"Kinect2UnityClient.dll")]
	public static extern int GetBodyFrameData(ref BodyFrame pBodyFrame, bool bGetOrientations, bool bGetHandStates);
	

	// Public unility functions
	
	// Returns the NUI error message
	public static string GetNuiErrorString(int hr)
	{
		string message = string.Empty;
		uint uhr = (uint)hr;
		
		switch(uhr)
		{
			case (uint)NuiErrorCodes.FrameNoData:
				message = "Frame contains no data.";
				break;
			case (uint)NuiErrorCodes.StreamNotEnabled:
				message = "Stream is not enabled.";
				break;
			case (uint)NuiErrorCodes.ImageStreamInUse:
				message = "Image stream is already in use.";
				break;
			case (uint)NuiErrorCodes.FrameLimitExceeded:
				message = "Frame limit is exceeded.";
				break;
			case (uint)NuiErrorCodes.FeatureNotInitialized:
				message = "Feature is not initialized.";
				break;
			case (uint)NuiErrorCodes.DeviceNotGenuine:
				message = "Device is not genuine.";
				break;
			case (uint)NuiErrorCodes.InsufficientBandwidth:
				message = "Bandwidth is not sufficient.";
				break;
			case (uint)NuiErrorCodes.DeviceNotSupported:
				message = "Device is not supported (e.g. Kinect for XBox 360).";
				break;
			case (uint)NuiErrorCodes.DeviceInUse:
				message = "Device is already in use.";
				break;
			case (uint)NuiErrorCodes.DatabaseNotFound:
				message = "Database not found.";
				break;
			case (uint)NuiErrorCodes.DatabaseVersionMismatch:
				message = "Database version mismatch.";
				break;
			case (uint)NuiErrorCodes.HardwareFeatureUnavailable:
				message = "Hardware feature is not available.";
				break;
			case (uint)NuiErrorCodes.DeviceNotConnected:
				message = "Device is not connected.";
				break;
			case (uint)NuiErrorCodes.DeviceNotReady:
				message = "Device is not ready.";
				break;
			case (uint)NuiErrorCodes.SkeletalEngineBusy:
				message = "Skeletal engine is busy.";
				break;
			case (uint)NuiErrorCodes.DeviceNotPowered:
				message = "Device is not powered.";
				break;
				
			default:
				message = "hr=0x" + uhr.ToString("X");
				break;
		}
		
		return message;
	}
	
	// Polls for new skeleton data
	public static bool PollSkeleton(ref BodyFrame bodyFrame, Int64 lastFrameTime)
	{
		bool newSkeleton = false;
		
		int hr = KinectWrapper.GetBodyFrameData(ref bodyFrame, true, true);
		if(hr == 0 && (bodyFrame.liRelativeTime > lastFrameTime))
		{
			newSkeleton = true;
		}
		
		return newSkeleton;
	}
	
}
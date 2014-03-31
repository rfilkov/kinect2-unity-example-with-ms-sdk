using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class KinectManager : MonoBehaviour 
{
	// Bool to control whether or not to start Kinect2Server internally
	public bool startKinectServer = true;
	
	// How high off the ground is the sensor (in meters).
	public float sensorHeight = 1.0f;

	// Public Bool to determine whether to receive and compute the user map
	public bool ComputeUserMap = false;
	
	// Public Bool to determine whether to receive and compute the color map
	public bool ComputeColorMap = false;
	
	// Public Bool to determine whether to display user map on the GUI
	public bool DisplayUserMap = false;
	
	// Public Bool to determine whether to display color map on the GUI
	public bool DisplayColorMap = false;
	
	// Public Bool to determine whether to display the skeleton lines on user map
	public bool DisplaySkeletonLines = false;
	
	// Public Floats to specify the width and height of the depth and color maps as % of the camera width and height
	// if percents are zero, they are calculated based on actual Kinect image´s width and height
	public float MapsPercentWidth = 0f;
	public float MapsPercentHeight = 0f;
	
	// Minimum user distance in order to process skeleton data
	public float minUserDistance = 1.0f;
	
	// Public Bool to determine whether to detect only the closest user or not
	public bool detectClosestUser = true;
	
	// Public Bool to determine whether to use only the tracked joints (and ignore the inferred ones)
	public bool ignoreInferredJoints = true;
	
	// GUI Text to show messages.
	public GUIText calibrationText;
	
	
	// Bool to keep track of whether Kinect has been initialized
	private bool kinectInitialized = false; 
	
	// The singleton instance of KinectManager
	private static KinectManager instance = null;

	// KinectServer instance
	private KinectServer kinectServer = null;
	
	// Depth and user maps
	private KinectWrapper.DepthBuffer depthImage;
	private KinectWrapper.BodyIndexBuffer bodyIndexImage;
	private KinectWrapper.UserHistogramBuffer userHistogramImage;
	private Texture2D usersLblTex;
	private Rect usersMapRect;
	private Color32[] usersMapColors;
	private ushort[] usersPrevState;
	private float[] usersHistogramMap;
	private int usersMapSize;
	private int minDepth;
	private int maxDepth;
	
	// Color map
	private KinectWrapper.ColorBuffer colorImage;
	private Texture2D usersClrTex;
	private Rect usersClrRect;
	
	// Kinect body frame data
	private KinectWrapper.BodyFrame bodyFrame;
	private Int64 lastFrameTime = 0;
	
	// List of all users
	private List<Int64> allUserIds;
	private Dictionary<Int64, int> userIdIndex;
	
	// First user ID
	private Int64 liFirstUserId = 0;
	
	// Kinect to world matrix
	private Matrix4x4 kinectToWorld;

	
	// returns the single KinectManager instance
    public static KinectManager Instance
    {
        get
        {
            return instance;
        }
    }
	
	// checks if Kinect is initialized and ready to use. If not, there was an error during Kinect-sensor initialization
	public static bool IsKinectInitialized()
	{
		return instance != null ? instance.kinectInitialized : false;
	}
	
	// checks if Kinect is initialized and ready to use. If not, there was an error during Kinect-sensor initialization
	public bool IsInitialized()
	{
		return kinectInitialized;
	}
	
	// returns the depth image/users histogram texture,if ComputeUserMap is true
    public Texture2D GetUsersLblTex()
    { 
		return usersLblTex;
	}
	
	// returns the color image texture,if ComputeColorMap is true
    public Texture2D GetUsersClrTex()
    { 
		return usersClrTex;
	}
	
	// returns true if at least one user is currently detected by the sensor
	public bool IsUserDetected()
	{
		return kinectInitialized && (allUserIds.Count > 0);
	}
	
	// returns true if the User is calibrated and ready to use
	public bool IsUserCalibrated(Int64 userId)
	{
		return userIdIndex.ContainsKey(userId);
	}
	
	// returns the number of currently detected users
	public int GetUsersCount()
	{
		return allUserIds.Count;
	}
	
	// returns the UserID by the given index
	public Int64 GetUserByIndex(int i)
	{
		if(i >= 0 && i < allUserIds.Count)
		{
			return allUserIds[i];
		}
		
		return 0;
	}
	
	// returns the UserID of the first (or closest) user, if any
	public Int64 GetFirstUser()
	{
		return liFirstUserId;
	}
	
	// returns the User position, relative to the Kinect-sensor, in meters
	public Vector3 GetUserPosition(Int64 userId)
	{
		if(userIdIndex.ContainsKey(userId))
		{
			int index = userIdIndex[userId];
			
			if(index >= 0 && index < KinectWrapper.Constants.BodyCount && 
				bodyFrame.bodyData[index].bIsTracked != 0)
			{
				return bodyFrame.bodyData[index].position;
			}
		}
		
		return Vector3.zero;
	}
	
	// returns the User rotation, relative to the Kinect-sensor
	public Quaternion GetUserOrientation(Int64 userId, bool flip)
	{
		if(userIdIndex.ContainsKey(userId))
		{
			int index = userIdIndex[userId];
			
			if(index >= 0 && index < KinectWrapper.Constants.BodyCount && 
				bodyFrame.bodyData[index].bIsTracked != 0)
			{
				return bodyFrame.bodyData[index].orientation;
			}
		}
		
		return Quaternion.identity;
	}
	
	// returns true if the given joint of the specified user is being tracked
	public bool IsJointTracked(Int64 userId, int joint)
	{
		if(userIdIndex.ContainsKey(userId))
		{
			int index = userIdIndex[userId];
			
			if(index >= 0 && index < KinectWrapper.Constants.BodyCount && 
				bodyFrame.bodyData[index].bIsTracked != 0)
			{
				if(joint >= 0 && joint < KinectWrapper.Constants.JointCount)
				{
					KinectWrapper.Joint jointData = bodyFrame.bodyData[index].joint[joint];
					
					return ignoreInferredJoints ? (jointData.trackingState == KinectWrapper.TrackingState.Tracked) : 
						(jointData.trackingState != KinectWrapper.TrackingState.NotTracked);
				}
			}
		}
		
		return false;
	}
	
	// returns the joint position of the specified user, relative to the Kinect-sensor, in meters
	public Vector3 GetJointPosition(Int64 userId, int joint)
	{
		if(userIdIndex.ContainsKey(userId))
		{
			int index = userIdIndex[userId];
			
			if(index >= 0 && index < KinectWrapper.Constants.BodyCount && 
				bodyFrame.bodyData[index].bIsTracked != 0)
			{
				if(joint >= 0 && joint < KinectWrapper.Constants.JointCount)
				{
					KinectWrapper.Joint jointData = bodyFrame.bodyData[index].joint[joint];
					return jointData.position;
				}
			}
		}
		
		return Vector3.zero;
	}
	
	// returns the joint direction of the specified user, relative to the parent joint
	public Vector3 GetJointDirection(Int64 userId, int joint)
	{
		if(userIdIndex.ContainsKey(userId))
		{
			int index = userIdIndex[userId];
			
			if(index >= 0 && index < KinectWrapper.Constants.BodyCount && 
				bodyFrame.bodyData[index].bIsTracked != 0)
			{
				if(joint >= 0 && joint < KinectWrapper.Constants.JointCount)
				{
					KinectWrapper.Joint jointData = bodyFrame.bodyData[index].joint[joint];
					return jointData.direction;
				}
			}
		}
		
		return Vector3.zero;
	}
	
	// returns the joint rotation of the specified user, relative to the Kinect-sensor
	public Quaternion GetJointOrientation(Int64 userId, int joint, bool flip)
	{
		if(userIdIndex.ContainsKey(userId))
		{
			int index = userIdIndex[userId];
			
			if(index >= 0 && index < KinectWrapper.Constants.BodyCount && 
				bodyFrame.bodyData[index].bIsTracked != 0)
			{
				if(joint >= 0 && joint < KinectWrapper.Constants.JointCount)
				{
					KinectWrapper.Joint jointData = bodyFrame.bodyData[index].joint[joint];
					return jointData.orientation;
				}
			}
		}
		
		return Quaternion.identity;
	}
	
	
	// KinectManager's Internal Methods

	void Start() 
	{
		int hr = 0;
		
		try
		{
			if(startKinectServer)
			{
				// start the Kinect-server app
				hr = StartKinectServer();
	            if (hr != 0)
				{
	            	throw new Exception("Kinect2Server not started");
				}
			}
			
			// try to initialize the default Kinect2 sensor
			KinectWrapper.FrameSource dwFlags = KinectWrapper.FrameSource.TypeBody;
			if(ComputeColorMap)
				dwFlags |= KinectWrapper.FrameSource.TypeColor;
			if(ComputeUserMap)
				dwFlags |= KinectWrapper.FrameSource.TypeDepth | KinectWrapper.FrameSource.TypeBodyIndex | KinectWrapper.FrameSource.TypeInfrared;
			
			hr = KinectWrapper.InitDefaultKinectSensor(dwFlags, KinectWrapper.Constants.ColorImageWidth, KinectWrapper.Constants.ColorImageHeight);
            if (hr != 0)
			{
            	throw new Exception("InitDefaultKinectSensor failed");
			}

			// transform matrix - kinect to world
			kinectToWorld.SetTRS(new Vector3(0.0f, sensorHeight, 0.0f), Quaternion.identity, Vector3.one);
		}
		catch(Exception e)
		{
			string message = e.Message + " - " + KinectWrapper.GetSystemErrorMessage(hr);
			Debug.LogError(message);
			Debug.LogException(e);
			
			if(calibrationText != null)
			{
				calibrationText.guiText.text = message;
			}
			
			return;
		}
		
		// init skeleton structures
		bodyFrame = new KinectWrapper.BodyFrame(true);
		
		// get the main camera rectangle
		Rect cameraRect = Camera.main.pixelRect;
		
		// calculate map width and height in percent, if needed
		if(MapsPercentWidth == 0f)
			MapsPercentWidth = (KinectWrapper.Constants.DepthImageWidth / 2) / cameraRect.width;
		if(MapsPercentHeight == 0f)
			MapsPercentHeight = (KinectWrapper.Constants.DepthImageHeight / 2) / cameraRect.height;
		
		if(ComputeUserMap)
		{
			// init user-depth structures
			//depthImage = new KinectWrapper.DepthBuffer(true);
			//bodyIndexImage = new KinectWrapper.BodyIndexBuffer(true);
			
	        // Initialize depth & label map related stuff
	        usersMapSize = KinectWrapper.Constants.DepthImageWidth * KinectWrapper.Constants.DepthImageHeight;
	        usersLblTex = new Texture2D(KinectWrapper.Constants.DepthImageWidth, KinectWrapper.Constants.DepthImageHeight);
	        usersMapColors = new Color32[usersMapSize];
			usersPrevState = new ushort[usersMapSize];
	        usersMapRect = new Rect(cameraRect.width - cameraRect.width * MapsPercentWidth, cameraRect.height, cameraRect.width * MapsPercentWidth, -cameraRect.height * MapsPercentHeight);
	        usersHistogramMap = new float[8192];
		}
		
		if(ComputeColorMap)
		{
			// init color image structures
			//colorImage = new KinectWrapper.ColorBuffer(true);
			
			// Initialize color map related stuff
	        usersClrTex = new Texture2D(KinectWrapper.Constants.ColorImageWidth, KinectWrapper.Constants.ColorImageHeight);
	        usersClrRect = new Rect(cameraRect.width - cameraRect.width * MapsPercentWidth, cameraRect.height, cameraRect.width * MapsPercentWidth, -cameraRect.height * MapsPercentHeight);
			
			if(ComputeUserMap)
			{
				usersMapRect.x -= cameraRect.width * MapsPercentWidth; //usersClrTex.width / 2;
			}
		}
		
        // Initialize user list to contain all users.
        allUserIds = new List<Int64>();
        userIdIndex = new Dictionary<Int64, int>();
	
		kinectInitialized = true;
		instance = this;
		
		DontDestroyOnLoad(gameObject);
		
		// GUI Text.
		if(calibrationText != null)
		{
			calibrationText.guiText.text = "WAITING FOR USERS";
		}
		
		Debug.Log("Waiting for users.");
	}
	
	// Start the Kinect2Server app
	private int StartKinectServer()
	{
		// start Kinect Server
		kinectServer = new KinectServer();
		kinectServer.RunKinectServer();
		
		float fTimeToWait = Time.realtimeSinceStartup + 10f; // allow 10 seconds time-out
		int iPing = 0;
		
		while(Time.realtimeSinceStartup < fTimeToWait)
		{
			iPing = KinectWrapper.PingKinect2Server();
			if(iPing == 12345678)
				break;
		}
		
		if(iPing == 12345678)
		{
			iPing = 0;
		}
		
		return iPing;
	}
	
	void OnApplicationQuit()
	{
		// shut down the Kinect on quitting.
		if(kinectInitialized)
		{
			KinectWrapper.ShutdownKinectSensor();
			instance = null;
		}
		
		// shut down the server, if any
		if(kinectServer != null)
		{
			kinectServer.ShutdownKinectServer();
		}
	}
	
    void OnGUI()
    {
		if(kinectInitialized)
		{
	        if(ComputeUserMap && DisplayUserMap)
	        {
	            GUI.DrawTexture(usersMapRect, usersLblTex);
	        }

			if(ComputeColorMap && DisplayColorMap)
			{
				GUI.DrawTexture(usersClrRect, usersClrTex);
			}
		}
    }
	
	void Update() 
	{
		if(kinectInitialized)
		{
			if(ComputeUserMap)
			{
				if(KinectWrapper.PollDepthFrame(ref depthImage, ref bodyIndexImage, ref minDepth, ref maxDepth))
				{
		        	UpdateUserMap();
				}
			}
			
			if(ComputeColorMap)
			{
				if(KinectWrapper.PollColorFrame(ref colorImage))
				{
					UpdateColorMap();
				}
			}
			
			if(KinectWrapper.PollSkeleton(ref bodyFrame, lastFrameTime))
			{
				lastFrameTime = bodyFrame.liRelativeTime;
				ProcessBodyFrameData();
			}
			
		}
	}
	
    void UpdateUserMap()
    {
		if(KinectWrapper.PollUserHistogramFrame(ref userHistogramImage, ComputeColorMap))
		{
			// Draw it!
	        usersLblTex.SetPixels32(userHistogramImage.pixels);
	        usersLblTex.Apply();
		}
    }
	
	// Update the color image
	void UpdateColorMap()
	{
        usersClrTex.SetPixels32(colorImage.pixels);
        usersClrTex.Apply();
	}
	
	// Processes body frame data
	private void ProcessBodyFrameData()
	{
		List<Int64> lostUsers = new List<Int64>();
		lostUsers.AddRange(allUserIds);
		
		for(int i = 0; i < KinectWrapper.Constants.BodyCount; i++)
		{
			KinectWrapper.BodyData bodyData = bodyFrame.bodyData[i];
			Int64 userId = bodyData.liTrackingID;
			
			if(bodyData.bIsTracked != 0)
			{
				// get the body position
				Vector3 bodyPos = kinectToWorld.MultiplyPoint3x4(bodyData.position);
				
				if(liFirstUserId == 0)
				{
					// check if this is the closest user
					bool bClosestUser = true;
					int iClosestUserIndex = i;
					
					if(detectClosestUser)
					{
						for(int j = 0; j < KinectWrapper.Constants.BodyCount; j++)
						{
							if(j != i)
							{
								KinectWrapper.BodyData bodyDataOther = bodyFrame.bodyData[j];
								
								if((bodyDataOther.bIsTracked != 0) && 
									(Mathf.Abs(kinectToWorld.MultiplyPoint3x4(bodyDataOther.position).z) < Mathf.Abs(bodyPos.z)))
								{
									bClosestUser = false;
									iClosestUserIndex = j;
									break;
								}
							}
						}
					}
					
					if(bClosestUser)
					{
						CalibrateUser(userId, iClosestUserIndex);
					}
				}

				//if(Mathf.Abs(bodyPos.z) >= Mathf.Abs(minUserDistance))
				{
					// convert Kinect positions to world positions
					bodyFrame.bodyData[i].position = bodyPos;
					
					for (int j = 0; j < KinectWrapper.Constants.JointCount; j++)
					{
						bodyData.joint[j].position = kinectToWorld.MultiplyPoint3x4(bodyData.joint[j].position);
					
						if((bodyData.liTrackingID == liFirstUserId) && (j == (int)KinectWrapper.JointType.HipCenter))
						{
							string debugText = String.Format("Body Pos: {0}", bodyData.joint[j].position);
							
							if(calibrationText && bodyData.joint[j].trackingState == KinectWrapper.TrackingState.Tracked)
							{
								calibrationText.guiText.text = debugText;
							}
						}
					}
				}
//				else
//				{
//					// consider body as not tracked
//					bodyFrame.bodyData[i].bIsTracked = 0;
//				}
				
				lostUsers.Remove(userId);
			}
		}
		
		// remove the lost users if any
		if(lostUsers.Count > 0)
		{
			foreach(Int64 userId in lostUsers)
			{
				RemoveUser(userId);
			}
			
			lostUsers.Clear();
		}
	}
	
	// Adds UserId to the list of users
    void CalibrateUser(Int64 userId, int userIndex)
    {
		if(!allUserIds.Contains(userId))
		{
			allUserIds.Add(userId);
			userIdIndex[userId] = userIndex;
			
			if(liFirstUserId == 0)
			{
				liFirstUserId = userId;
			}
		}
		
		if(liFirstUserId != 0)
		{
			if(calibrationText != null)
			{
				calibrationText.guiText.text = "";
			}
		}
    }
	
	// Remove a lost UserId
	void RemoveUser(Int64 userId)
	{
        // remove from global users list
        allUserIds.Remove(userId);
		userIdIndex.Remove(userId);
		
		if(liFirstUserId == userId)
		{
			if(allUserIds.Count > 0)
			{
				liFirstUserId = allUserIds[0];
			}
			else
			{
				liFirstUserId = 0;
			}
		}
		
		if(liFirstUserId == 0)
		{
			Debug.Log("Waiting for users.");
			if(calibrationText != null)
			{
				calibrationText.guiText.text = "WAITING FOR USERS";
			}
		}
	}
	
}

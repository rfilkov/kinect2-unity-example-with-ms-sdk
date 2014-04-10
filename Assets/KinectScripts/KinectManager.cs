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
	public bool computeUserMap = false;
	
	// Public Bool to determine whether to receive and compute the color map
	public bool computeColorMap = false;
	
	// Public Bool to determine whether to display user map on the GUI
	public bool displayUserMap = false;
	
	// Public Bool to determine whether to display color map on the GUI
	public bool displayColorMap = false;
	
	// Public Bool to determine whether to display the skeleton lines on user map
	public bool displaySkeletonLines = false;
	
	// Public Floats to specify the width and height of the depth and color maps as % of the camera width and height
	// if percents are zero, they are calculated based on actual Kinect image´s width and height
	private float MapsPercentWidth = 0f;
	private float MapsPercentHeight = 0f;
	
	// Minimum user distance in order to process skeleton data
	public float minUserDistance = 0.5f;
	
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
//	private Color32[] usersMapColors;
//	private ushort[] usersPrevState;
//	private float[] usersHistogramMap;
//	private int usersMapSize;
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
	private List<Int64> alUserIds;
	private Dictionary<Int64, int> dictUserIdToIndex;
	
	// Primary (first or closest) user ID
	private Int64 liPrimaryUserId = 0;
	
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
		return kinectInitialized && (alUserIds.Count > 0);
	}
	
	// returns true if the User is calibrated and ready to use
	public bool IsUserCalibrated(Int64 userId)
	{
		return dictUserIdToIndex.ContainsKey(userId);
	}
	
	// returns the number of currently detected users
	public int GetUsersCount()
	{
		return alUserIds.Count;
	}
	
	// returns the UserID by the given index
	public Int64 GetUserIdByIndex(int i)
	{
		if(i >= 0 && i < alUserIds.Count)
		{
			return alUserIds[i];
		}
		
		return 0;
	}
	
	// returns the UserID of the primary user (the first or the closest one), if there is any
	public Int64 GetPrimaryUser()
	{
		return liPrimaryUserId;
	}
	
	// returns the User position, relative to the Kinect-sensor, in meters
	public Vector3 GetUserPosition(Int64 userId)
	{
		if(dictUserIdToIndex.ContainsKey(userId))
		{
			int index = dictUserIdToIndex[userId];
			
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
		if(dictUserIdToIndex.ContainsKey(userId))
		{
			int index = dictUserIdToIndex[userId];
			
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
		if(dictUserIdToIndex.ContainsKey(userId))
		{
			int index = dictUserIdToIndex[userId];
			
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
		if(dictUserIdToIndex.ContainsKey(userId))
		{
			int index = dictUserIdToIndex[userId];
			
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
		if(dictUserIdToIndex.ContainsKey(userId))
		{
			int index = dictUserIdToIndex[userId];
			
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
		if(dictUserIdToIndex.ContainsKey(userId))
		{
			int index = dictUserIdToIndex[userId];
			
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
	
	// checks if the left hand confidence for a user is high
	// returns true if the confidence is high, false if it is low or user is not found
	public bool IsLeftHandConfidenceHigh(Int64 userId)
	{
		if(dictUserIdToIndex.ContainsKey(userId))
		{
			int index = dictUserIdToIndex[userId];
			
			if(index >= 0 && index < KinectWrapper.Constants.BodyCount && 
				bodyFrame.bodyData[index].bIsTracked != 0)
			{
				return (bodyFrame.bodyData[index].leftHandConfidence == KinectWrapper.TrackingConfidence.High);
			}
		}
		
		return false;
	}
	
	// checks if the right hand confidence for a user is high
	// returns true if the confidence is high, false if it is low or user is not found
	public bool IsRightHandConfidenceHigh(Int64 userId)
	{
		if(dictUserIdToIndex.ContainsKey(userId))
		{
			int index = dictUserIdToIndex[userId];
			
			if(index >= 0 && index < KinectWrapper.Constants.BodyCount && 
				bodyFrame.bodyData[index].bIsTracked != 0)
			{
				return (bodyFrame.bodyData[index].rightHandConfidence == KinectWrapper.TrackingConfidence.High);
			}
		}
		
		return false;
	}
	
	// returns the left hand state for a user
	public KinectWrapper.HandState GetLeftHandState(Int64 userId)
	{
		if(dictUserIdToIndex.ContainsKey(userId))
		{
			int index = dictUserIdToIndex[userId];
			
			if(index >= 0 && index < KinectWrapper.Constants.BodyCount && 
				bodyFrame.bodyData[index].bIsTracked != 0)
			{
				return bodyFrame.bodyData[index].leftHandState;
			}
		}
		
		return KinectWrapper.HandState.NotTracked;
	}
	
	// returns the right hand state for a user
	public KinectWrapper.HandState GetRightHandState(Int64 userId)
	{
		if(dictUserIdToIndex.ContainsKey(userId))
		{
			int index = dictUserIdToIndex[userId];
			
			if(index >= 0 && index < KinectWrapper.Constants.BodyCount && 
				bodyFrame.bodyData[index].bIsTracked != 0)
			{
				return bodyFrame.bodyData[index].rightHandState;
			}
		}
		
		return KinectWrapper.HandState.NotTracked;
	}
	
	// returns the interaction box for the left hand of the specified user, in meters
	public bool GetLeftHandInteractionBox(Int64 userId, ref Vector3 leftBotBack, ref Vector3 rightTopFront, bool bValidBox)
	{
		if(dictUserIdToIndex.ContainsKey(userId))
		{
			int index = dictUserIdToIndex[userId];
			
			if(index >= 0 && index < KinectWrapper.Constants.BodyCount && 
				bodyFrame.bodyData[index].bIsTracked != 0)
			{
				KinectWrapper.BodyData bodyData = bodyFrame.bodyData[index];
				bool bResult = true;
				
				if(bodyData.joint[(int)KinectWrapper.JointType.ShoulderRight].trackingState == KinectWrapper.TrackingState.Tracked &&
					bodyData.joint[(int)KinectWrapper.JointType.HipLeft].trackingState == KinectWrapper.TrackingState.Tracked)
				{
					rightTopFront.x = bodyData.joint[(int)KinectWrapper.JointType.ShoulderRight].position.x;
					leftBotBack.x = rightTopFront.x - 2 * (rightTopFront.x - bodyData.joint[(int)KinectWrapper.JointType.HipLeft].position.x);
				}
				else
				{
					bResult = bValidBox;
				}
					
				if(bodyData.joint[(int)KinectWrapper.JointType.HipRight].trackingState == KinectWrapper.TrackingState.Tracked &&
					bodyData.joint[(int)KinectWrapper.JointType.ShoulderRight].trackingState == KinectWrapper.TrackingState.Tracked)
				{
					leftBotBack.y = bodyData.joint[(int)KinectWrapper.JointType.HipRight].position.y;
					rightTopFront.y = bodyData.joint[(int)KinectWrapper.JointType.ShoulderRight].position.y;
					
					float fDelta = (rightTopFront.y - leftBotBack.y) * 2 / 3;
					leftBotBack.y += fDelta;
					rightTopFront.y += fDelta;
				}
				else
				{
					bResult = bValidBox;
				}
					
				if(bodyData.joint[(int)KinectWrapper.JointType.HipCenter].trackingState == KinectWrapper.TrackingState.Tracked)
				{
					leftBotBack.z = bodyData.joint[(int)KinectWrapper.JointType.HipCenter].position.z;
					rightTopFront.z = leftBotBack.z - 0.5f;
				}
				else
				{
					bResult = bValidBox;
				}
				
				return bResult;
			}
		}
		
		return false;
	}
	
	// returns the interaction box for the right hand of the specified user, in meters
	public bool GetRightHandInteractionBox(Int64 userId, ref Vector3 leftBotBack, ref Vector3 rightTopFront, bool bValidBox)
	{
		if(dictUserIdToIndex.ContainsKey(userId))
		{
			int index = dictUserIdToIndex[userId];
			
			if(index >= 0 && index < KinectWrapper.Constants.BodyCount && 
				bodyFrame.bodyData[index].bIsTracked != 0)
			{
				KinectWrapper.BodyData bodyData = bodyFrame.bodyData[index];
				bool bResult = true;
				
				if(bodyData.joint[(int)KinectWrapper.JointType.ShoulderLeft].trackingState == KinectWrapper.TrackingState.Tracked &&
					bodyData.joint[(int)KinectWrapper.JointType.HipRight].trackingState == KinectWrapper.TrackingState.Tracked)
				{
					leftBotBack.x = bodyData.joint[(int)KinectWrapper.JointType.ShoulderLeft].position.x;
					rightTopFront.x = leftBotBack.x + 2 * (bodyData.joint[(int)KinectWrapper.JointType.HipRight].position.x - leftBotBack.x);
				}
				else
				{
					bResult = bValidBox;
				}
					
				if(bodyData.joint[(int)KinectWrapper.JointType.HipLeft].trackingState == KinectWrapper.TrackingState.Tracked &&
					bodyData.joint[(int)KinectWrapper.JointType.ShoulderLeft].trackingState == KinectWrapper.TrackingState.Tracked)
				{
					leftBotBack.y = bodyData.joint[(int)KinectWrapper.JointType.HipLeft].position.y;
					rightTopFront.y = bodyData.joint[(int)KinectWrapper.JointType.ShoulderLeft].position.y;
					
					float fDelta = (rightTopFront.y - leftBotBack.y) * 2 / 3;
					leftBotBack.y += fDelta;
					rightTopFront.y += fDelta;
				}
				else
				{
					bResult = bValidBox;
				}
					
				if(bodyData.joint[(int)KinectWrapper.JointType.HipCenter].trackingState == KinectWrapper.TrackingState.Tracked)
				{
					leftBotBack.z = bodyData.joint[(int)KinectWrapper.JointType.HipCenter].position.z;
					rightTopFront.z = leftBotBack.z - 0.5f;
				}
				else
				{
					bResult = bValidBox;
				}
				
				return bResult;
			}
		}
		
		return false;
	}
	
	
	// KinectManager's Internal Methods
	
	void Awake()
	{
		try
		{
			if(KinectWrapper.EnsureKinectWrapperPresence())
			{
				// reload the same level
				Application.LoadLevel(Application.loadedLevel);
			}
		} 
		catch (Exception ex) 
		{
			Debug.LogError(ex.ToString());
			
			if(calibrationText != null)
			{
				calibrationText.guiText.text = ex.Message;
			}
		}
	}

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
			if(computeColorMap)
				dwFlags |= KinectWrapper.FrameSource.TypeColor;
			if(computeUserMap)
				dwFlags |= KinectWrapper.FrameSource.TypeDepth | KinectWrapper.FrameSource.TypeBodyIndex | KinectWrapper.FrameSource.TypeInfrared;
			
			hr = KinectWrapper.InitDefaultKinectSensor(dwFlags, KinectWrapper.Constants.ColorImageWidth, KinectWrapper.Constants.ColorImageHeight);
            if (hr != 0)
			{
            	throw new Exception("InitDefaultKinectSensor failed");
			}

			// transform matrix - kinect to world
			kinectToWorld.SetTRS(new Vector3(0.0f, sensorHeight, 0.0f), Quaternion.identity, Vector3.one);
		}
		catch(Exception ex)
		{
			string message = ex.Message + " - " + KinectWrapper.GetSystemErrorMessage(hr);
			Debug.LogError(message);
			
			Debug.LogException(ex);
			
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
		
		if(computeUserMap)
		{
			// init user-depth structures
			//depthImage = new KinectWrapper.DepthBuffer(true);
			//bodyIndexImage = new KinectWrapper.BodyIndexBuffer(true);
			
	        // Initialize depth & label map related stuff
//	        usersMapSize = KinectWrapper.Constants.DepthImageWidth * KinectWrapper.Constants.DepthImageHeight;
	        usersLblTex = new Texture2D(KinectWrapper.Constants.DepthImageWidth, KinectWrapper.Constants.DepthImageHeight);
//	        usersMapColors = new Color32[usersMapSize];
//			usersPrevState = new ushort[usersMapSize];
	        usersMapRect = new Rect(cameraRect.width - cameraRect.width * MapsPercentWidth, cameraRect.height, cameraRect.width * MapsPercentWidth, -cameraRect.height * MapsPercentHeight);
//	        usersHistogramMap = new float[8192];
		}
		
		if(computeColorMap)
		{
			// init color image structures
			//colorImage = new KinectWrapper.ColorBuffer(true);
			
			// Initialize color map related stuff
	        usersClrTex = new Texture2D(KinectWrapper.Constants.ColorImageWidth, KinectWrapper.Constants.ColorImageHeight);
	        usersClrRect = new Rect(cameraRect.width - cameraRect.width * MapsPercentWidth, cameraRect.height, cameraRect.width * MapsPercentWidth, -cameraRect.height * MapsPercentHeight);
			
			if(computeUserMap)
			{
				usersMapRect.x -= cameraRect.width * MapsPercentWidth; //usersClrTex.width / 2;
			}
		}
		
        // Initialize user list to contain all users.
        alUserIds = new List<Int64>();
        dictUserIdToIndex = new Dictionary<Int64, int>();
	
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
	        if(computeUserMap && displayUserMap)
	        {
	            GUI.DrawTexture(usersMapRect, usersLblTex);
	        }

			if(computeColorMap && displayColorMap)
			{
				GUI.DrawTexture(usersClrRect, usersClrTex);
			}
		}
    }
	
	void Update() 
	{
		if(kinectInitialized)
		{
			if(KinectWrapper.PollSkeleton(ref bodyFrame, lastFrameTime))
			{
				lastFrameTime = bodyFrame.liRelativeTime;
				ProcessBodyFrameData();
			}
			
			if(computeColorMap)
			{
				if(KinectWrapper.PollColorFrame(ref colorImage))
				{
					UpdateColorMap();
				}
			}
			
			if(computeUserMap)
			{
				if(KinectWrapper.PollDepthFrame(ref depthImage, ref bodyIndexImage, ref minDepth, ref maxDepth))
				{
		        	UpdateUserMap();
				}
			}
			
		}
	}
	
	// Update the user histogram
    void UpdateUserMap()
    {
		if(KinectWrapper.PollUserHistogramFrame(ref userHistogramImage, computeColorMap))
		{
			// draw user histogram
	        usersLblTex.SetPixels32(userHistogramImage.pixels);
			
			// draw skeleton lines
			if(displaySkeletonLines)
			{
				for(int i = 0; i < alUserIds.Count; i++)
				{
					Int64 liUserId = alUserIds[i];
					int index = dictUserIdToIndex[liUserId];
					
					if(index >= 0 && index < KinectWrapper.Constants.BodyCount)
					{
						DrawSkeleton(usersLblTex, ref bodyFrame.bodyData[index]);
					}
				}
			}

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
		lostUsers.AddRange(alUserIds);
		
		for(int i = 0; i < KinectWrapper.Constants.BodyCount; i++)
		{
			KinectWrapper.BodyData bodyData = bodyFrame.bodyData[i];
			Int64 userId = bodyData.liTrackingID;
			
			if(bodyData.bIsTracked != 0 && Mathf.Abs(kinectToWorld.MultiplyPoint3x4(bodyData.position).z) >= minUserDistance)
			{
				// get the body position
				Vector3 bodyPos = kinectToWorld.MultiplyPoint3x4(bodyData.position);
				
				if(liPrimaryUserId == 0)
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
						// calibrate the first or closest user
						CalibrateUser(userId, iClosestUserIndex);
					}
				}
				
				// calibrate current user
				CalibrateUser(userId, i);

				// convert Kinect positions to world positions
				bodyFrame.bodyData[i].position = bodyPos;
				
				for (int j = 0; j < KinectWrapper.Constants.JointCount; j++)
				{
					bodyData.joint[j].position = kinectToWorld.MultiplyPoint3x4(bodyData.joint[j].position);
				
					if((bodyData.liTrackingID == liPrimaryUserId) && (j == (int)KinectWrapper.JointType.HipCenter) &&
						bodyData.joint[j].trackingState == KinectWrapper.TrackingState.Tracked)
					{
						string debugText = String.Format("Body Pos: {0}", bodyData.joint[j].position);
						
						if(calibrationText)
						{
							calibrationText.guiText.text = debugText;
						}
					}
				}
				
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
		if(!alUserIds.Contains(userId))
		{
			alUserIds.Add(userId);
			dictUserIdToIndex[userId] = userIndex;
			
			if(liPrimaryUserId == 0)
			{
				liPrimaryUserId = userId;
		
				if(liPrimaryUserId != 0)
				{
					if(calibrationText != null && calibrationText.guiText.text != "")
					{
						calibrationText.guiText.text = "";
					}
				}
			}
		}
    }
	
	// Remove a lost UserId
	void RemoveUser(Int64 userId)
	{
        // remove from global users list
        alUserIds.Remove(userId);
		dictUserIdToIndex.Remove(userId);
		
		if(liPrimaryUserId == userId)
		{
			if(alUserIds.Count > 0)
			{
				liPrimaryUserId = alUserIds[0];
			}
			else
			{
				liPrimaryUserId = 0;
			}
		}
		
		if(liPrimaryUserId == 0)
		{
			//Debug.Log("Waiting for users.");
			
			if(calibrationText != null && calibrationText.guiText.text == "")
			{
				calibrationText.guiText.text = "WAITING FOR USERS";
			}
		}
	}
	
	// draws the skeleton in the given texture
	private void DrawSkeleton(Texture2D aTexture, ref KinectWrapper.BodyData bodyData)
	{
		int jointsCount = KinectWrapper.Constants.JointCount;
		
		for(int i = 0; i < jointsCount; i++)
		{
			int parent = (int)KinectWrapper.GetParentJoint((KinectWrapper.JointType)i);
			
			if(bodyData.joint[i].trackingState == KinectWrapper.TrackingState.Tracked && bodyData.joint[parent].trackingState == KinectWrapper.TrackingState.Tracked)
			{
				Vector2 posParent = KinectWrapper.GetKinectPointDepthCoords(bodyData.joint[parent].kinectPos);
				Vector2 posJoint = KinectWrapper.GetKinectPointDepthCoords(bodyData.joint[i].kinectPos);
				
//				posParent.y = KinectWrapper.Constants.ImageHeight - posParent.y - 1;
//				posJoint.y = KinectWrapper.Constants.ImageHeight - posJoint.y - 1;
//				posParent.x = KinectWrapper.Constants.ImageWidth - posParent.x - 1;
//				posJoint.x = KinectWrapper.Constants.ImageWidth - posJoint.x - 1;
				
				//Color lineColor = playerJointsTracked[i] && playerJointsTracked[parent] ? Color.red : Color.yellow;
				DrawLine(aTexture, (int)posParent.x, (int)posParent.y, (int)posJoint.x, (int)posJoint.y, Color.yellow);
			}
		}
		
		//aTexture.Apply();
	}
	
	// draws a line in a texture
	private void DrawLine(Texture2D a_Texture, int x1, int y1, int x2, int y2, Color a_Color)
	{
		int width = KinectWrapper.Constants.DepthImageWidth;
		int height = KinectWrapper.Constants.DepthImageHeight;
		
		int dy = y2 - y1;
		int dx = x2 - x1;
	 
		int stepy = 1;
		if (dy < 0) 
		{
			dy = -dy; 
			stepy = -1;
		}
		
		int stepx = 1;
		if (dx < 0) 
		{
			dx = -dx; 
			stepx = -1;
		}
		
		dy <<= 1;
		dx <<= 1;
	 
		if(x1 >= 0 && x1 < width && y1 >= 0 && y1 < height)
			for(int x = -1; x <= 1; x++)
				for(int y = -1; y <= 1; y++)
					a_Texture.SetPixel(x1 + x, y1 + y, a_Color);
		
		if (dx > dy) 
		{
			int fraction = dy - (dx >> 1);
			
			while (x1 != x2) 
			{
				if (fraction >= 0) 
				{
					y1 += stepy;
					fraction -= dx;
				}
				
				x1 += stepx;
				fraction += dy;
				
				if(x1 >= 0 && x1 < width && y1 >= 0 && y1 < height)
					for(int x = -1; x <= 1; x++)
						for(int y = -1; y <= 1; y++)
							a_Texture.SetPixel(x1 + x, y1 + y, a_Color);
			}
		}
		else 
		{
			int fraction = dx - (dy >> 1);
			
			while (y1 != y2) 
			{
				if (fraction >= 0) 
				{
					x1 += stepx;
					fraction -= dy;
				}
				
				y1 += stepy;
				fraction += dx;
				
				if(x1 >= 0 && x1 < width && y1 >= 0 && y1 < height)
					for(int x = -1; x <= 1; x++)
						for(int y = -1; y <= 1; y++)
							a_Texture.SetPixel(x1 + x, y1 + y, a_Color);
			}
		}
		
	}
	
}

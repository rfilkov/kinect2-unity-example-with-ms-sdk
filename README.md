Kinect2 Unity Example with MS-SDK
=================================

This is preliminary software and subject to change.

Kinect2 Unity Example is a test-project for the Kinect2 native wrapper for Unity I started developing. The wrapper and the Unity-package are my contributions to the Kinect2 Developer Preview Program, run by Microsoft.

How It Operates
---------------

As the Kinect2 SDK/Runtime are 64-bit only at this time, but Unity development environment is 32-bit, I considered it reasonable to develop the wrapper/plugin in two parts - Kinect2UnityServer (64-bit exe) and Kinect2UnityClient (32-bit dll). Kinect2 client and server parts communicate using the RPC-mechanism over named pipes. The flow is like this: 

KinectManager.cs (.Net) -> KinectWrapper.cs (.Net wrapper) -> Kinect2UnityClient.dll (32b) -> Kinect2UnityServer.exe (64b) -> Kinect2.dll (64b) -> Kinect2UnityServer.exe (64b) -> Kinect2UnityClient.dll (32b) -> KinectWrapper.cs (.Net wrapper) -> KinectManager.cs (.Net)

How to Start It
---------------

In order to get the Unity-package working, you need to open Kinect2UnityExample-scene and start it. It uses KinectServer-script to run Kinect2UnityServer.exe automatically at the start of the scene and stop it when the application stops. If server start is successful, the Kinect-server window is displayed minimized (intentionally), so you could see the server error messages there. If Unity editor freezes at scene start-up, please start the Kinect2 server manually. To do this, first disable 'Start Kinect Server'-setting of KinectManager. The KinectManager is a component of MainCamera-game object in the scene. Then go to '[project-folder]/KinectServer' and double click 'Kinect2UnityServer.exe'.

What Is In There
----------------

The project is in development. At the moment are implemented the transfer of the raw camera images (color, depth, IR and body-index) and the transfer of the body data. You will see the transfer of the body data in real-time on the cube man in the scene. The depth, body-index and color data may be seen on GUI-textures at the bottom right of the screen. You can enable or disable the transfer and display of these images by enabling or disabling 'Compute User Map' and 'Compute Color Map'-parameters of KinectManager.

What Comes Next
---------------
Next will come the coordinate mapping (between color, depth and body data) and the avatar control. As the next version of Kinect2 SDK is expected very soon, tuning the wrapper to the new SDK will be probably the most urgent next step.


Wish you happy testing. And don't have too big expectations, yet.
Enjoy! :)

R.


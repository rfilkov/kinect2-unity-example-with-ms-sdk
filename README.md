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

In order to get the Unity-package working, you need to open Kinect2UnityExample-scene and start it. It uses KinectServer-script to run Kinect2UnityServer.exe automatically at the start of the scene and stop it when the application stops. If server start is successful, the Kinect-server window is displayed minimized (intentionally not hidden), so you could see the server error messages there. If Unity editor freezes at scene start-up, please start the Kinect2 server manually. To do this, first disable 'Start Kinect Server'-setting of KinectManager. The KinectManager is a component of MainCamera-game object in the scene. Then go to '[project-folder]/KinectServer' and double click 'Kinect2UnityServer.exe'.

What Is In There
----------------

The project is in development. At the moment implemented are these features: the transfer of the raw camera images (color, depth, IR and body-index), the transfer of body data and the coordinate mapping among the Kinect 3d-space and image 2d-spaces. You will see the transfer of the body data in real-time on the cube man in the scene. The depth+bodyindex-data and color data may be seen as GUI-textures at the bottom right of the screen. By default the UserMap (depth and body-index data) is enabled and the ColorMap (color camera images) is disabled. You can enable or disable the transfer and display of these images by enabling or disabling 'Compute User Map' and 'Compute Color Map'-parameters of the KinectManager. This will affect FPS. KinectManager is component of the MainCamera game object.

How to Switch from Mirrored to Non-Mirrored Movement
----------------------------------------------------
To switch from mirrored to non mirrored cube-man, you need to do these two things: 1. Set the Y-rotation of PointManCtrl game object to 0, and 2. Disable 'Mirrored Movement'-parameter of the PointManController. PointManController is component of the PointMan game object (parented to PointManCtrl). To switch back to mirrored movement do the opposite: 1. Set the Y-rotation of PointManCtrl game object to 180 degrees, and 2. Enable 'Mirrored Movement'-parameter of the PointManController.

What Comes Next
---------------
The recent announcement from MS-staff that they're going to provide native integration between Unity and Kinect v2, makes the further development of this project pointless. That's why I decided to stop it. It will continue as private project here: https://bitbucket.org/rfilkov/kinect2-unity-example-with-ms-sdk The goal will be to provide easy to use Kinect-features to all Unity developers: cursor control, interaction, avatar control, gesture recognition and optionally some extras like voice recognition, face tracking, etc. More information about the new project will be published on my blog: http://rfilkov.com

Thank You!
-----------
My sincere thanks to all that have contributed to the development of this project, tested it or gave feedback. Special thanks to these guys for their collaboration: Davy Loots, Robert Cornfield and Andres Soechting!

Enjoy!

R.


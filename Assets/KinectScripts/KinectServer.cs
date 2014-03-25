using UnityEngine;
using System;
using System.Runtime.InteropServices;
//using System.Diagnostics;

public class KinectServer
{
    private System.Diagnostics.Process procServer;
	
	[DllImport("kernel32.dll", SetLastError=true)]
	public static extern bool Wow64DisableWow64FsRedirection(ref IntPtr ptr);
	
 	[DllImport("kernel32.dll", SetLastError=true)]
	public static extern bool Wow64RevertWow64FsRedirection(IntPtr ptr);
	
	
    public void RunKinectServer()
    {
        procServer = new System.Diagnostics.Process();
        procServer.StartInfo.FileName = Application.dataPath + @"/../KinectServer/Kinect2UnityServer.exe";
		procServer.StartInfo.WorkingDirectory = Application.dataPath + @"/../KinectServer";
        procServer.StartInfo.UseShellExecute = false;
        procServer.StartInfo.CreateNoWindow = false;
		procServer.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Minimized;
        procServer.StartInfo.RedirectStandardInput = true;
//        procServer.StartInfo.RedirectStandardOutput = true;
//        procServer.StartInfo.RedirectStandardError = true;
//        procServer.OutputDataReceived += (sender, args) => Debug.Log(args.Data);
//        procServer.ErrorDataReceived += (sender, args) => Debug.LogError(args.Data);
		
        try
        {
			//IntPtr ptr = new IntPtr();
			//Wow64DisableWow64FsRedirection(ref ptr);
			procServer.Start();
			//Wow64RevertWow64FsRedirection(ptr);
		}
        catch(Exception e)
        {
            Debug.LogError("Could not find Kinect2UnityServer.exe");
			Debug.LogException(e);
            procServer = null;
            return;
        }

        //procServer.BeginOutputReadLine();
        ////procServer.StandardInput.Write("0"); // gets rid of the Byte-order mark in the pipe.
    }

    public void ShutdownKinectServer()
    {
        if (procServer == null)
            return;

        try
        {
            System.Diagnostics.Process.GetProcessById(procServer.Id);
        }
        catch (ArgumentException)
        {
            // The other app might have been shut down externally
            return;
        }

        try
        {
			procServer.StandardInput.WriteLine("exit");
        }
        catch (InvalidOperationException)
        {
            // The other app might have been shut down externally already.
        }
    }
}


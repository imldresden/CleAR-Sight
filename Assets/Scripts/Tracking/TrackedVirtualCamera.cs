// Copyright (c) Interactive Media Lab Dresden, Technische Universität Dresden. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using IMLD.Unity.Tracking;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class TrackedVirtualCamera : MonoBehaviour
{
    [Tooltip("The object containing the OptiTrackStreamingClient script.")]
    //public OptitrackStreamingClient StreamingClient;
    public MotiveDirect StreamingClient;

    [Tooltip("The Streaming ID of the tablet in Motive")]
    public int RigidBodyIdCamera;

    public Transform Background;
    public int focalLength = 1294;
    public Vector3 CameraOffset = Vector3.zero;

    int imageWidth, imageHeight;
    Vector2 focalLengthInPixels;
    Vector2 principalPoint;

    // Start is called before the first frame update
    void Start()
    {
        // setup OptiTrack streaming
        // If the user didn't explicitly associate a client, find a suitable default.
        if (StreamingClient == null)
        {
            //StreamingClient = OptitrackStreamingClient.FindDefaultClient();
            StreamingClient = MotiveDirect.FindDefaultClient();

            // If we still couldn't find one, disable this component.
            if (StreamingClient == null)
            {
                //Debug.LogError(GetType().FullName + ": Streaming client not set, and no " + typeof(OptitrackStreamingClient).FullName + " components found in scene; disabling this component.", this);
                Debug.LogError(GetType().FullName + ": Streaming client not set, and no " + typeof(MotiveDirect).FullName + " components found in scene; disabling this component.", this);
                this.enabled = false;
                return;
            }
        }

        // setup camera
        ReadCameraSettings();
    }

    private void Update()
    {
        focalLengthInPixels.x = focalLength;
        focalLengthInPixels.y = focalLength;
        UpdateCameraSettings();
        UpdateCameraPose();
    }

    void OnEnable()
    {
        Application.onBeforeRender += OnBeforeRender;
    }


    void OnDisable()
    {
        Application.onBeforeRender -= OnBeforeRender;
    }


    void OnBeforeRender()
    {
        UpdateCameraPose();
    }

    void UpdateCameraPose()
    {
        //OptitrackRigidBodyState rbStateCamera = StreamingClient.GetLatestRigidBodyState(RigidBodyIdCamera, false);
        MotiveDirect.RigidBody rbStateCamera = StreamingClient.GetLatestRigidBodyState(RigidBodyIdCamera);
        //if (rbStateCamera != null)
        if (rbStateCamera.tracking_valid)
        {
            //Pose newPose = PointCloudSpatialLocalizer.Instance.GetInternalPose(new Pose(rbStateTablet.Pose.Position, rbStateTablet.Pose.Orientation));
            //CameraCache.Main.transform.SetGlobalPose(newPose);

            //CameraCache.Main.transform.SetGlobalPose(new Pose(rbStateCamera.Pose.Position, rbStateCamera.Pose.Orientation));
            CameraCache.Main.transform.SetGlobalPose(new Pose(rbStateCamera.position, rbStateCamera.rotation));
            CameraCache.Main.transform.localPosition += CameraCache.Main.transform.right * CameraOffset.x;
            CameraCache.Main.transform.localPosition += CameraCache.Main.transform.up * CameraOffset.y;
            CameraCache.Main.transform.localPosition += CameraCache.Main.transform.forward * CameraOffset.z;

        }
    }

    private void ReadCameraSettings()
    {
        string path = Application.streamingAssetsPath + "/Intrinsics_Sony_A7III.txt";
        float ax, ay;
        float x0, y0;        

        string[] lines = File.ReadAllLines(path);
        string[] parameters = lines[0].Split(' ');
        string[] resolution = lines[1].Split(' ');

        ax = float.Parse(parameters[0], System.Globalization.CultureInfo.InvariantCulture);
        ay = float.Parse(parameters[1], System.Globalization.CultureInfo.InvariantCulture);
        focalLengthInPixels = new Vector2(ax, ay);
        focalLength = (int)ay;

        x0 = float.Parse(parameters[2], System.Globalization.CultureInfo.InvariantCulture);
        y0 = float.Parse(parameters[3], System.Globalization.CultureInfo.InvariantCulture);
        principalPoint = new Vector2(x0, y0);

        imageWidth = int.Parse(resolution[0]);
        imageHeight = int.Parse(resolution[1]);
    }

    public void UpdateCameraSettings()
    {
        CameraCache.Main.usePhysicalProperties = false;
        CameraCache.Main.fieldOfView = 2f * Mathf.Atan(0.5f * imageHeight / focalLengthInPixels.y) * Mathf.Rad2Deg;


        // Considering https://docs.opencv.org/3.3.0/d4/d94/tutorial_camera_calibration.html, we are looking for X=posX and Y=posY
        // with x=0.5*ImageWidth, y=0.5*ImageHeight (center of the camera projection) and w=Z=cameraBackgroundDistance 
        float localPositionX = (0.5f * imageWidth - principalPoint.x) / focalLengthInPixels.x * Background.localPosition.z;
        float localPositionY = -(0.5f * imageHeight - principalPoint.y) / focalLengthInPixels.y * Background.localPosition.z; // a minus because OpenCV camera coordinates origin is top - left, but bottom-left in Unity

        // Considering https://stackoverflow.com/a/41137160
        // scale.x = 2 * cameraBackgroundDistance * tan(fovx / 2), cameraF.x = imageWidth / (2 * tan(fovx / 2))
        float localScaleX = imageWidth / focalLengthInPixels.x * Background.localPosition.z;
        float localScaleY = imageHeight / focalLengthInPixels.y * Background.localPosition.z;

        // Place and scale the background
        //Background.localPosition = new Vector3(localPositionX, localPositionY, Background.localPosition.z);
        Background.localScale = new Vector3(localScaleX, localScaleY, 1);
    }

    private void ConfigureBackground(int cameraId)
    {

    }

}

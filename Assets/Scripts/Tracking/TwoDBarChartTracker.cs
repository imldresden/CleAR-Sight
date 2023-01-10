// Copyright (c) Interactive Media Lab Dresden, Technische Universität Dresden. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using IMLD.Unity.Tracking;
using IMLD.Unity.Utils;
using Microsoft.MixedReality.Toolkit.Utilities;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class TwoDBarChartTracker : MonoBehaviour
{
    [Tooltip("The object containing the OptiTrackStreamingClient script.")]
    //public OptitrackStreamingClient StreamingClient;
    public MotiveDirect StreamingClient;

    [Tooltip("The Streaming ID of the QR Code origin in Motive")]
    public int RigidBodyIdQRCode;

    [Tooltip("The parent node of the tracking system representation in the scene")]
    public GameObject TrackingSystem;  

    private Vector3 initOTPos = Vector3.zero, initHLPos = Vector3.zero;

    private SimpleInterpolator interpolator;
    private Vector3 manualOffset = Vector3.zero;

    public float xOffset;
    public float yOffset;
    public float zOffset;

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

        UpdateQRPose();
        //StreamingClient.RegisterRigidBody(this, RigidBodyIdTablet);
        //StreamingClient.RegisterRigidBody(this, RigidBodyIdHoloLens);
        //StreamingClient.RegisterRigidBody(this, RigidBodyIdQRCode);

        // Setup helper GameObjects

        //QRCodeGO = new GameObject("QrCodeMock");
        //QRCodeGO.transform.parent = TrackingSystem.transform;
        //HoloLensGO = new GameObject("HoloLensMock");
        //HoloLensGO.transform.parent = QRCodeGO.transform;

        //TabletGO = new GameObject("TabletMock");
        //TabletGO.transform.parent = QRCodeGO.transform;

        interpolator = GetComponent<SimpleInterpolator>();
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
      //  UpdateQRPose();
    }

    // Update is called once per frame
    void Update()
    {        
        //UpdateManualOffset();
       // UpdateBarChartPose();
    }

    void UpdateQRPose()
    {
        //OptitrackRigidBodyState rbStateQR = StreamingClient.GetLatestRigidBodyState(RigidBodyIdQRCode, false);
        MotiveDirect.RigidBody rbStateQR = StreamingClient.GetLatestRigidBodyState(RigidBodyIdQRCode);
        //if (rbStateQR != null)
        if (rbStateQR.tracking_valid)
        {
            //Pose newPose = PointCloudSpatialLocalizer.Instance.GetInternalPose(new Pose(rbStateQR.Pose.Position, rbStateQR.Pose.Orientation));
            Pose newPose = PointCloudSpatialLocalizer.Instance.GetInternalPose(new Pose(rbStateQR.position, rbStateQR.rotation));

            Position2DBarChart(newPose);

            /*if (interpolator)
            {
                interpolator.SetTargetPosition(newPose.position);
                interpolator.SetTargetRotation(newPose.rotation);
            }
            else
            {
                transform.SetGlobalPose(newPose);
            }*/
        }
    }

   /* private void UpdateManualOffset()
    {
        if (Keyboard.current.numpad8Key.isPressed)
        {
            manualOffset.z += 0.0001f;
        }
        if (Keyboard.current.numpad2Key.isPressed)
        {
            manualOffset.z -= 0.0001f;
        }

        if (Keyboard.current.numpad6Key.isPressed)
        {
            manualOffset.x += 0.0001f;
        }
        if (Keyboard.current.numpad4Key.isPressed)
        {
            manualOffset.x -= 0.0001f;
        }

        if (Keyboard.current.numpad9Key.isPressed)
        {
            manualOffset.y += 0.0001f;
        }
        if (Keyboard.current.numpad3Key.isPressed)
        {
            manualOffset.y -= 0.0001f;
        }

        if (Keyboard.current.numpad5Key.isPressed)
        {
            manualOffset = Vector3.zero;
        }

        PointCloudSpatialLocalizer.Instance.ManualOffset = transform.TransformDirection(manualOffset);
    }*/
   /*
    void UpdateBarChartPose()
    {
        OptitrackRigidBodyState rbStateBarChart = StreamingClient.GetLatestRigidBodyState(RigidBodyIdBarChart, false);
        if (rbStateBarChart != null)
        {
            Pose newPose = PointCloudSpatialLocalizer.Instance.GetInternalPose(new Pose(rbStateBarChart.Pose.Position, rbStateBarChart.Pose.Orientation));
            barChart.transform.SetGlobalPose(newPose);
        }
    }*/

    void Position2DBarChart(Pose QRpose)
    {
        Pose offSetPose = new Pose(new Vector3(this.transform.position.x, QRpose.position.y, this.transform.position.z), this.transform.rotation);
        this.transform.SetGlobalPose(offSetPose);
    }
}
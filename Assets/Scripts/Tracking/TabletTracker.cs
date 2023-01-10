// Copyright (c) Interactive Media Lab Dresden, Technische Universität Dresden. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using IMLD.Unity.Tracking;
using IMLD.Unity.Utils;
using Microsoft.MixedReality.Toolkit.Utilities;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class TabletTracker : MonoBehaviour
{
    [Tooltip("The object containing the OptiTrackStreamingClient script.")]
    //public OptitrackStreamingClient StreamingClient;
    public MotiveDirect StreamingClient;

    [Tooltip("The Streaming ID of the tablet in Motive")]
    public int RigidBodyIdTablet;

    [Tooltip("The Streaming ID of the HoloLens in Motive")]
    public int RigidBodyIdHoloLens;

    [Tooltip("The Streaming ID of the QR Code origin in Motive")]
    public int RigidBodyIdQRCode;

    [Tooltip("The Streaming ID of the BarChart in Motive")]
    public int RigidBodyIdBarChart;

    [Tooltip("The parent node of the tracking system representation in the scene")]
    public GameObject TrackingSystem;

    public GameObject barChart;

    private GameObject QRCodeGO, HoloLensGO, TabletGO;

    private Vector3 initOTPos = Vector3.zero, initHLPos = Vector3.zero;

    private SimpleInterpolator interpolator;
    private Vector3 manualOffset = Vector3.zero;

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
        UpdateTabletPose();
    }

    // Update is called once per frame
    void Update()
    {
        UpdateTabletPose();
        UpdateManualOffset();
        UpdateBarChartPose();
    }

    void UpdateTabletPose()
    {
        //OptitrackRigidBodyState rbStateTablet = StreamingClient.GetLatestRigidBodyState(RigidBodyIdTablet, false);
        MotiveDirect.RigidBody rbStateTablet = StreamingClient.GetLatestRigidBodyState(RigidBodyIdTablet);
        //if (rbStateTablet != null)
        if (rbStateTablet.tracking_valid)
        {
            //Pose newPose = PointCloudSpatialLocalizer.Instance.GetInternalPose(new Pose(rbStateTablet.Pose.Position, rbStateTablet.Pose.Orientation));
            Pose newPose = PointCloudSpatialLocalizer.Instance.GetInternalPose(new Pose(rbStateTablet.position, rbStateTablet.rotation));
            if (interpolator)
            {
                interpolator.SetTargetPosition(newPose.position);
                interpolator.SetTargetRotation(newPose.rotation);
            }
            else
            {
                transform.SetGlobalPose(newPose);
            }
        }
    }

    private void UpdateManualOffset()
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
    }

    void UpdateBarChartPose()
    {
        //OptitrackRigidBodyState rbStateBarChart = StreamingClient.GetLatestRigidBodyState(RigidBodyIdBarChart, false);
        MotiveDirect.RigidBody rbStateBarChart = StreamingClient.GetLatestRigidBodyState(RigidBodyIdBarChart);
        //if (rbStateBarChart != null)
        if (rbStateBarChart.tracking_valid)
        {
            //Pose newPose = PointCloudSpatialLocalizer.Instance.GetInternalPose(new Pose(rbStateBarChart.Pose.Position, rbStateBarChart.Pose.Orientation));
            Pose newPose = PointCloudSpatialLocalizer.Instance.GetInternalPose(new Pose(rbStateBarChart.position, rbStateBarChart.rotation));
            barChart.transform.SetGlobalPose(newPose);
        }
    }
}
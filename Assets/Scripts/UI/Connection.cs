// Copyright (c) Interactive Media Lab Dresden, Technische Universität Dresden. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Connection: MonoBehaviour
{
    public IConnectionTarget Source;
    public IConnectionTarget Target;
    public LineConnector LineConnector;

    public bool SetTemporaryEndpoint(Vector3 endpoint)
    {
        if (LineConnector)
        {
            LineConnector.EndPoint = endpoint;
            return true;
        }
        else
        {
            return false;
        }
    }

    private void Start()
    {
        if (!LineConnector)
        {
            Debug.LogError("No Line Connector found. Disabling component.");
            enabled = false;
        }

        if (Source as MonoBehaviour)
        {
            LineConnector.StartTransform = Source.TargetTransform;
        }

        if (Target as MonoBehaviour)
        {
            LineConnector.EndTransform = Target.TargetTransform;
        }
    }

    private void Update()
    {
        if (Source as MonoBehaviour && LineConnector.StartTransform != Source.TargetTransform)
        {
            LineConnector.StartTransform = Source.TargetTransform;
        }

        if (Target as MonoBehaviour && LineConnector.EndTransform != Target.TargetTransform)
        {
            LineConnector.EndTransform = Target.TargetTransform;
        }
    }
}

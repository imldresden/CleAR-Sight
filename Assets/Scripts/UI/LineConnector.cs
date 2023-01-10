// Copyright (c) Interactive Media Lab Dresden, Technische Universität Dresden. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LineConnector : MonoBehaviour
{
    public Transform StartTransform;
    public Transform EndTransform;

    public Vector3 StartPoint = Vector3.zero;
    public Vector3 EndPoint = Vector3.zero;

    public float StartPointMargin = 0f;
    public float EndPointMargin = 0;

    public LineRenderer LineRenderer;

    private Vector3 internalStartPosition, internalEndPosition;

    // Start is called before the first frame update
    void Start()
    {
        if (!CheckPrerequisites())
        {
            return;
        }
                
        
    }

    private bool CheckPrerequisites()
    {
        if (!LineRenderer)
        {
            Debug.LogError("No line renderer found. Disabling script.");
            enabled = false;
            return false;
        }

        return true;
    }

    // Update is called once per frame
    void LateUpdate()
    {
        if (!CheckPrerequisites())
        {
            return;
        }

        Vector3 startPoint, endPoint;

        if (StartTransform)
        {
            startPoint = StartTransform.transform.position;
        }
        else
        {
            startPoint = StartPoint;
        }

        if (EndTransform)
        {
            endPoint = EndTransform.transform.position;
        }
        else
        {
            endPoint = EndPoint;
        }

        Vector3 startPosition = startPoint + (endPoint - startPoint).normalized * StartPointMargin;
        Vector3 endPosition = endPoint + (startPoint - endPoint).normalized * EndPointMargin;

        if (startPosition != internalStartPosition || endPosition != internalEndPosition)
        {
            internalStartPosition = startPosition;
            internalEndPosition = endPosition;
            LineRenderer.useWorldSpace = true;
            LineRenderer.SetPosition(0, startPosition);
            LineRenderer.SetPosition(1, endPosition);
        }        
    }

}

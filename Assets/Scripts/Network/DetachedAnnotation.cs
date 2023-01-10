// Copyright (c) Interactive Media Lab Dresden, Technische Universität Dresden. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using IMLD.Unity.Network;
using IMLD.Unity.Network.Messages;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DetachedAnnotation : MonoBehaviour
{
    public int Id { get; set; }

    [HideInInspector]
    public List<GameObject> lineObjects = new List<GameObject>();

    private Vector3 cachedPosition;
    private Quaternion cachedOrientation;

    // Start is called before the first frame update
    void Start()
    {
        cachedPosition = transform.localPosition;
        cachedOrientation = transform.localRotation;
    }

    // Update is called once per frame
    void LateUpdate()
    {
        if (cachedPosition != transform.localPosition || cachedOrientation != transform.localRotation)
        {
            NetworkManager.Instance?.SendMessage(new MessageAnnotationUpdate(transform.localPosition, transform.localRotation, Id));
            cachedPosition = transform.localPosition;
            cachedOrientation = transform.localRotation;
        }
    }

    /// <summary>
    /// This method updates the pose (position, orientation) of this set of detached annotations.
    /// This should only be used to sync updates made on another client. No network messages are sent as a result of calling this method.
    /// </summary>
    /// <param name="position"></param>
    /// <param name="orientation"></param>
    public void UpdatePose(Vector3 position, Quaternion orientation)
    {
        transform.localPosition = position;
        transform.localRotation = orientation;
        cachedPosition = position;
        cachedOrientation = orientation;
    }
}

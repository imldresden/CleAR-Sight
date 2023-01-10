// Copyright (c) Interactive Media Lab Dresden, Technische Universität Dresden. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraTextureProvider : MonoBehaviour
{
    public int CameraId = 0;
    // Start is called before the first frame update
    void Start()
    {
        WebCamDevice[] devices = WebCamTexture.devices;
        string devicename = devices[CameraId].name;
        var webcamTexture = new WebCamTexture(devicename, 1920, 1080);
        Renderer renderer = GetComponent<Renderer>();
        renderer.material.mainTexture = webcamTexture;
        webcamTexture.requestedHeight = 1080;
        webcamTexture.requestedWidth = 1920;
        webcamTexture.requestedFPS = 30;
        webcamTexture.Play();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

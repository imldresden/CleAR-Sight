// Copyright (c) Interactive Media Lab Dresden, Technische Universität Dresden. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityVolumeRendering;

public class SlicingPanel : MonoBehaviour
{
    public VolumeRenderedObject targetObject;
    GameObject panel;
    GameObject volume;
    public bool freeze;

    void Awake()
    {
        freeze = false;              
    }

    public void SetUp(GameObject panel_, GameObject volume_)
    {       
        Debug.Log("volume:"+volume_);
        panel = panel_;
        volume = volume_;        

        targetObject = volume.GetComponent<VolumeRenderedObject>();        
    }

    private void OnDisable()
    {
        if (targetObject != null)
            targetObject.meshRenderer.sharedMaterial.DisableKeyword("CUTOUT_PLANE");
    }

    /// <summary>
    /// In this <c>Update</c> function, the material of the slicing plane is constantly updated, depending on the position of the panel, except for when the slicing panel is frozen.
    /// </summary>
    private void Update()
    {
        if (targetObject == null)
            return;
        if(panel != null && volume != null && targetObject != null)
        {
            Material mat = targetObject.meshRenderer.sharedMaterial;

            if (!freeze)
            {
                Vector3 pos = panel.transform.position;
                transform.position = new Vector3(pos.x, pos.y, pos.z);
                transform.rotation = panel.transform.rotation;                
                transform.localRotation *= Quaternion.Euler(270, 0, 0);

            }
            mat.EnableKeyword("CUTOUT_PLANE");
            mat.SetMatrix("_CrossSectionMatrix", transform.worldToLocalMatrix * targetObject.transform.localToWorldMatrix);
        }
        
    }
}


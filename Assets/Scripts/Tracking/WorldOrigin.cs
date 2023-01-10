// Copyright (c) Interactive Media Lab Dresden, Technische Universität Dresden. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using IMLD.Unity.Tracking;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class WorldOrigin : MonoBehaviour
{
    public static WorldOrigin Instance = null;
    public GameObject OriginVisual;

    private ARAnchor anchor;

    private void Awake()
    {
        // Singleton pattern implementation
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }

        Instance = this;
    }

    // Update is called once per frame
    void Update()
    {
        if (InteractionHandler.DebugMode)
        {
            if(OriginVisual)
            {
                OriginVisual.SetActive(true);
            }
        }
        else
        {
            if (OriginVisual)
            {
                OriginVisual.SetActive(false);
            }
        }

        UpdateWorldOrigin();
    }

    public void UpdateWorldOrigin()
    {
        UpdateWorldOrigin(Vector3.zero, Quaternion.identity);
    }

    public void UpdateWorldOrigin(Vector3 position, Quaternion orientation)
    {
        Pose newPose = PointCloudSpatialLocalizer.Instance.GetInternalPose(new Pose(position, orientation));

        if (Vector3.Distance(newPose.position, transform.position) > 0.01f)
        {
            // delete old world anchor
            if (anchor)
            {
                DestroyImmediate(anchor);
            }

            transform.SetGlobalPose(newPose);

            // create new anchor
            anchor = gameObject.AddComponent<ARAnchor>();
        }
        else
        {
            transform.SetGlobalPose(newPose);
        }


             
    }
}

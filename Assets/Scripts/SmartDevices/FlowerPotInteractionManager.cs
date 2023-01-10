// Copyright (c) Interactive Media Lab Dresden, Technische Universität Dresden. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using IMLD.Unity.Core;
using IMLD.Unity.Tracking;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlowerPotInteractionManager : MonoBehaviour
{
    public BoxCollider TabletCollider;
    public WaterLevelIndicator WaterLevelPrefab;
    public FlowerPot Pot;

    private Dictionary<FlowerPot, WaterLevelIndicator> WaterLevelDict = new Dictionary<FlowerPot, WaterLevelIndicator>();



    private const float WATERLEVEL_SIZE = 0.03f;

    // Start is called before the first frame update
    void Start()
    {
        if (!TabletCollider)
        {
            Debug.LogError("No BoxCollider for tablet defined!");
            enabled = false;
        }
    }

    // Update is called once per frame
    void Update()
    {
        CheckFlowerPots();
    }

    private void CheckFlowerPots()
    {
        if (!TabletCollider)
        {
            return;
        }

        // get camera/head position
        //Vector3 headPosition = CameraCache.Main.transform.position;
        Vector3 headPosition = UserPositionManager.Instance.GetClosestUser(transform.position).position;

        if (TabletCollider.Raycast(new Ray(headPosition, Pot.transform.position - headPosition), out RaycastHit hit, 5.0f))
        {
            if (!WaterLevelDict.ContainsKey(Pot))
            {
                GameObject go = Instantiate(WaterLevelPrefab.gameObject, TabletCollider.transform);
                Vector3 ColorPickerScale = new Vector3(WATERLEVEL_SIZE / go.transform.parent.localScale.x,
                                      WATERLEVEL_SIZE / go.transform.parent.localScale.z,
                                      1);
                go.transform.localScale = ColorPickerScale;
                WaterLevelDict.Add(Pot, go.GetComponent<WaterLevelIndicator>());
            }

            WaterLevelIndicator level = WaterLevelDict[Pot];
            level.gameObject.SetActive(true);
            level.FlowerPot = Pot;
            level.transform.position = hit.point;

            Vector3 pointOnCollider = TabletCollider.transform.InverseTransformPoint(hit.point);

            // world-scale distance to x and z borders
            float distX = ((0.5f * TabletCollider.size.x) - Mathf.Abs(pointOnCollider.x)) * TabletCollider.gameObject.transform.localScale.x;
            float distZ = ((0.5f * TabletCollider.size.z) - Mathf.Abs(pointOnCollider.z)) * TabletCollider.gameObject.transform.localScale.z;

            // min world-scale distance to borders
            float dist = Mathf.Min(distX, distZ);
            float size = Mathf.Min(2f * dist, WATERLEVEL_SIZE);
            level.transform.localScale = new Vector3(size / level.transform.parent.localScale.x, size / level.transform.parent.localScale.z, 1);
            level.Transparency = size / 0.04f;
        }
        else
        {
            if (WaterLevelDict.ContainsKey(Pot))
            {
                WaterLevelIndicator level = WaterLevelDict[Pot];
                level.gameObject.SetActive(false);
            }
        }

        if (InteractionHandler.DebugMode)
        {
            Pot.Visual.SetActive(true);
        }
        else
        {
            Pot.Visual.SetActive(false);
        }
    }
}

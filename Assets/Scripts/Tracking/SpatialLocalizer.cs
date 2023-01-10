// Copyright (c) Interactive Media Lab Dresden, Technische Universität Dresden. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.MixedReality.QR;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace IMLD.Unity.Tracking
{
    public class SpatialLocalizer : MonoBehaviour, ISpatialLocalizer
    {
        public static SpatialLocalizer Instance = null;

        public Transform SpatialNodeParentTransform;

        public SpatialNode SpatialNodePrefab;

        public List<int> NodeIds;

        private QRCodeManager qrCodeManager;
        private Dictionary<string, SpatialNode> spatialNodes;

        [SerializeField]
        private bool ignorePitchRoll;

        public bool IgnorePitchRoll { get => ignorePitchRoll; set => ignorePitchRoll = value; }
        public Vector3 ManualOffset { get; set; }

        /// <summary>
        /// Converts a pose from the local HoloLens coordinate system into the global OptiTrack coordinate system.
        /// </summary>
        /// <param name="localPose"></param>
        /// <returns></returns>
        public Pose GetExternalPose(Pose localPose)
        {
            if (spatialNodes == null || spatialNodes.Count == 0)
            {
                return default;
            }

            Vector3 nominator = Vector3.zero;
            float denominator = 0.0f;
            float smallestDistance = float.MaxValue;
            float secSmallestDistance = float.MaxValue;
            SpatialNode closestNode = null;
            SpatialNode secClosestNode = null;
            foreach (var node in spatialNodes.Values)
            {
                // compute distance to current node
                float distance = Vector3.Distance(node.ExternalPose.position, localPose.position);

                // if the distance is very small, just use that node
                if (distance < 0.01f)
                {
                    return node.FromInternalToExternalPose(localPose);
                }

                // keep track of two closest nodes (for interpolation of the two best rotations)
                if (distance < secSmallestDistance)
                {
                    if (distance < smallestDistance)
                    {
                        secSmallestDistance = smallestDistance;
                        smallestDistance = distance;
                        secClosestNode = closestNode;
                        closestNode = node;
                    }
                    else
                    {
                        secSmallestDistance = distance;
                        secClosestNode = node;
                    }
                }

                // update distance weighted average of the position
                nominator += node.FromInternalToExternalPose(localPose).position / distance;
                denominator += 1 / distance;
            }

            // compute new pose using the weighted average position and the linearly interpolated rotation between the two closest nodes
            Quaternion closestRot = closestNode.FromInternalToExternalPose(localPose).rotation;
            Quaternion secClosestRot = secClosestNode.FromInternalToExternalPose(localPose).rotation;
            return new Pose(nominator / denominator, Quaternion.Lerp(closestRot, secClosestRot, smallestDistance / (smallestDistance + secSmallestDistance)));
        }

        /// <summary>
        /// Converts a pose from the global OptiTrack coordinate system into the local HoloLens coordinate system.
        /// </summary>
        /// <param name="globalPose"></param>
        /// <returns></returns>
        public Pose GetInternalPose(Pose globalPose)
        {
            if (spatialNodes == null || spatialNodes.Count == 0)
            {
                return default;
            }

            if (spatialNodes.Count == 1)
            {
                return spatialNodes.Values.Single().FromExternalToInternalPose(globalPose);
            }

            Vector3 nominator = Vector3.zero;
            float denominator = 0.0f;
            float smallestDistance = float.MaxValue;
            float secSmallestDistance = float.MaxValue;
            SpatialNode closestNode = null;
            SpatialNode secClosestNode = null;
            foreach (var node in spatialNodes.Values)
            {
                // compute distance to current node
                float distance = Vector3.Distance(node.ExternalPose.position, globalPose.position);

                // if the distance is very small, just use that node
                if (distance < 0.01f)
                {
                    return node.FromExternalToInternalPose(globalPose);
                }

                // keep track of two closest nodes (for interpolation of the two best rotations)
                if (distance < secSmallestDistance)
                {
                    if (distance < smallestDistance)
                    {
                        secSmallestDistance = smallestDistance;
                        smallestDistance = distance;
                        secClosestNode = closestNode;
                        closestNode = node;
                    }
                    else
                    {
                        secSmallestDistance = distance;
                        secClosestNode = node;
                    }
                }

                // update distance weighted average of the position
                nominator += node.FromExternalToInternalPose(globalPose).position / distance;
                denominator += 1 / distance;
            }

            // compute new pose using the weighted average position and the Slerp interpolation of the rotation between the two closest nodes
            Quaternion closestRot = closestNode.FromExternalToInternalPose(globalPose).rotation;
            Quaternion secClosestRot = secClosestNode.FromExternalToInternalPose(globalPose).rotation;
            return new Pose(nominator / denominator, Quaternion.Slerp(closestRot, secClosestRot, smallestDistance / (smallestDistance + secSmallestDistance)));
        }


        void Awake()
        {
            // Singleton pattern implementation
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
            }

            Instance = this;
        }

        // Start is called before the first frame update
        void Start()
        {
            spatialNodes = new Dictionary<string, SpatialNode>();

            // find or add a QRCodeManager component in the scene
            SetupQRCodeManager();
        }

        // Update is called once per frame
        void Update()
        {

        }

        private void SetupQRCodeManager()
        {
            if (qrCodeManager == null)
            {
                qrCodeManager = QRCodeManager.FindDefaultQRCodeManager();
                if (qrCodeManager == null)
                {
                    qrCodeManager = gameObject.AddComponent<QRCodeManager>();
                }
            }

            qrCodeManager.OnQRAdded += QRAdded;
            qrCodeManager.OnQRUpdated += QRUpdated;
        }

        private void QRUpdated(QRCode qrCode)
        {
            if (spatialNodes.ContainsKey(qrCode.Data) == false)
            {
                CreateSpatialNode(qrCode.Data);
            }
        }

        private void QRAdded(QRCode qrCode)
        {
            if (spatialNodes.ContainsKey(qrCode.Data) == false)
            {
                CreateSpatialNode(qrCode.Data);
            }
        }

        private void CreateSpatialNode(string idString)
        {
            foreach (var id in NodeIds)
            {
                if (idString.Contains(id.ToString()))
                {
                    var node = Instantiate(SpatialNodePrefab, SpatialNodeParentTransform);
                    node.gameObject.name = "SpatialNode_" + idString;
                    node.ExternalPoseProvider = new OptiTrackPoseProvider(id);
                    node.InternalPoseProvider = new QRPoseProvider(idString);
                    spatialNodes[idString] = node;
                }
            }

            //GameObject obj = new GameObject("SpatialNode_" + idString);
            //obj.transform.parent = SpatialNodeParentTransform;
            //var node = obj.AddComponent<SpatialNode>();
            //node.ExternalPoseProvider = new OptiTrackPoseProvider(3);
            //node.InternalPoseProvider = new QRPoseProvider(idString);
            //spatialNodes[idString] = node;
        }
    }
}
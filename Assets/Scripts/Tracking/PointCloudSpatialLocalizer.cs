// Copyright (c) Interactive Media Lab Dresden, Technische Universität Dresden. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.MixedReality.QR;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace IMLD.Unity.Tracking
{

    /// <summary>
    /// This component is used to calibrate an external and an internal coordinate systems and to transform poses between them.
    /// </summary>
    public class PointCloudSpatialLocalizer : MonoBehaviour, ISpatialLocalizer
    {
        /// <summary>
        /// Static access to this singleton
        /// </summary>
        public static ISpatialLocalizer Instance;

        /// <summary>
        /// The current coordinate transform between external and internal coordinates.
        /// Use <see cref="GetInternalPose(Pose)"/> to directly convert an external to an internal pose.
        /// </summary>
        public Pose CoordinateTransformation { get; private set; }

        /// <summary>
        /// The provider of external poses, e.g., from a tracking system.
        /// </summary>
        public IPoseProvider ExternalPoseProvider { get => externalPoseProvider; set => externalPoseProvider = value; }

        /// <summary>
        /// The provider of internal poses, e.g., from QR codes.
        /// </summary>
        public IPoseProvider InternalPoseProvider { get => internalPoseProvider; set => internalPoseProvider = value; }


        /// <summary>
        /// Manual offset for the registration.
        /// </summary>
        public Vector3 ManualOffset { get; set; }

        /// <summary>
        /// Optional parent transform for all spatial nodes and calibration points
        /// </summary>
        [Tooltip("Optional parent transform for all spatial nodes and calibration points")]
        [SerializeField]
        private Transform VisualsParentTransform;

        /// <summary>
        /// The prefab that is used for newly created SpatialNodes
        /// </summary>
        [Tooltip("The prefab that is used for newly created SpatialNodes")]
        [SerializeField]
        private SpatialNode SpatialNodePrefab;

        /// <summary>
        /// The prefab that is used for visualizing calibration points
        /// </summary>
        [Tooltip("The prefab that is used for visualizing calibration points")]
        [SerializeField]
        private GameObject PointPrefab;

        [Tooltip("Minimal number of points to collect before calibration starts")]
        [SerializeField]
        private int minPoints = 3;

        [Tooltip("Maximal number of points to use for calibration. If reached, older points will be removed.")]
        [SerializeField]
        private int maxPoints = 3;

        [Tooltip("Maximum velocity in m/s of markers or the camera that is allowed when capturing a calibration point")]
        [SerializeField]
        private float maxVelocity = 0.05f;

        [Tooltip("Minimum distance in m that a new calibration point may have to existing ones")]
        [SerializeField]
        private float minDeltaPos = 0.1f;

        [Tooltip("If checked allows manual offset correction for the calibration by using the numpad keys.")]
        [SerializeField]
        private bool allowManualOffset = true;

        private Pose previousCameraPose;
        private DateTime previousFrameTime;
        private float cameraVelocity;
        private Dictionary<string, StoredPose> storedPoses = new Dictionary<string, StoredPose>();
        private RegistrationHelper registrationHelper;
        private IPoseProvider externalPoseProvider;
        private IPoseProvider internalPoseProvider;
        private QRCodeManager qrCodeManager;
        private Dictionary<string, SpatialNode> spatialNodes = new Dictionary<string, SpatialNode>();

        /// <summary>
        /// Converts an external pose, e.g., from a tracking system, to an internal pose in the devices coordinate system.
        /// </summary>
        /// <param name="globalPose"></param>
        /// <returns></returns>
        public Pose GetInternalPose(Pose globalPose)
        {
            Vector3 offset = Vector3.zero;

            if (allowManualOffset)
            {
                offset = ManualOffset;
            }

            Pose pose;
            pose.position = CoordinateTransformation.rotation * globalPose.position + CoordinateTransformation.position + offset;
            pose.rotation = CoordinateTransformation.rotation * globalPose.rotation;
            return pose;
        }

        private Pose GetInternalPose(Pose globalPose, Pose transform)
        {
            Pose pose;
            pose.position = transform.rotation * globalPose.position + transform.position;
            pose.rotation = transform.rotation * globalPose.rotation;
            return pose;
        }

        /// <summary>
        /// Converts an internal pose, i.e., in the devices coordinate system, to an external pose.
        /// </summary>
        /// <param name="localPose"></param>
        /// <returns></returns>
        public Pose GetExternalPose(Pose localPose)
        {
            Pose pose;
            pose.position = Quaternion.Inverse(CoordinateTransformation.rotation) * (localPose.position - CoordinateTransformation.position);
            pose.rotation =  Quaternion.Inverse(CoordinateTransformation.rotation) * localPose.rotation;
            return pose;
        }

        protected virtual bool CheckCandidatePoseFromSpatialNode(SpatialNode node, string idString)
        {
            // Check that the camera velocity is not too big, as that could make the results unstable.
            if (cameraVelocity > maxVelocity)
            {
                return false;
            }

            // Check that the current approximate velocity for both the external and internal pose is not too big.
            // This prevents the inclusion of poses that are badly aligned between external and internal coordinate system.
            if (node.ExternalPoseProvider.Velocity > maxVelocity || node.InternalPoseProvider.Velocity > maxVelocity)
            {
                return false;
            }

            // Check that the new (external) pose is not too close to already stored poses.
            // This only checks (and we only store) the external pose, as the distances would ideally be the same for the internals poses.
            // This is O(n) for the number of stored poses.
            if (storedPoses.ContainsKey(idString))
            {
                var storedPose = storedPoses[idString];
                bool retValue = true;

                if (Vector3.Distance(storedPose.ExternalPose.position, node.ExternalPose.position) < minDeltaPos)
                {
                    retValue = false;
                }
                else if (Vector3.Distance(storedPose.InternalPose.position, node.InternalPose.position) < minDeltaPos)
                {
                    retValue = false;
                }

                storedPose.ExternalPose = node.ExternalPose;
                storedPose.InternalPose = node.InternalPose;
                storedPose.Visual.transform.SetLocalPose(node.InternalPose);
                storedPoses[idString] = storedPose;

                return retValue;
            }
            else
            {
                var pointGO = Instantiate(PointPrefab, VisualsParentTransform);
                pointGO.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
                pointGO.transform.SetLocalPose(node.InternalPose);
                var storedPose = new StoredPose();
                storedPose.ExternalPose = node.ExternalPose;
                storedPose.InternalPose = node.InternalPose;
                storedPose.Visual = pointGO;
                storedPoses.Add(idString, storedPose);
            }

            // Return true if all checks have been passed successfully.
            return true;
        }

        private void Awake()
        {
            // Singleton pattern implementation
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
            }

            Instance = this;
        }

        // Start is called before the first frame update
        private void Start()
        {
            CoordinateTransformation = new Pose(Vector3.zero, Quaternion.identity);
            previousCameraPose = CameraCache.Main.transform.GetGlobalPose();
            registrationHelper = new RegistrationHelper();
            SetupQRCodeManager();
        }

        // Update is called once per frame
        private void Update()
        {
            CheckDebugMode();

            DateTime now = DateTime.Now;
            Pose currentCameraPose = CameraCache.Main.transform.GetGlobalPose();

            if (previousCameraPose == default || previousFrameTime == null)
            {
                cameraVelocity = 0.0f;
            }
            else
            {
                float ds = Vector3.Distance(currentCameraPose.position, previousCameraPose.position);
                float dt = (float)(now - previousFrameTime).TotalSeconds;
                if (dt == 0)
                {
                    cameraVelocity = 0.0f;
                }
                else
                {
                    cameraVelocity = ds / dt;
                }
            }

            foreach (var kvp in spatialNodes)
            {
                if (CheckCandidatePoseFromSpatialNode(kvp.Value, kvp.Key))
                {
                    //Debug.Log("Added new point. External pose: " + kvp.Value.ExternalPose + ", internal pose: " + kvp.Value.InternalPose);

                    // if we have enough stored poses, compute registration
                    if (storedPoses.Count >= minPoints)
                    {
                        registrationHelper.ClearCorrespondingPoints();

                        foreach (var pose in storedPoses.Values)
                        {
                            Debug.Log("Registration Update:");
                            Debug.Log("ext: " + pose.ExternalPose.position + " - " + "int: " + pose.InternalPose.position);
                            registrationHelper.AddCorrespondingPoints(pose.ExternalPose.position, pose.InternalPose.position);
                        }

                        if (registrationHelper.GetNumberOfPoints() >= minPoints)
                        {
                            Debug.Log("Computing registration with " + registrationHelper.GetNumberOfPoints() + " points:");
                            bool success = registrationHelper.ComputeRegistration();
                            if (success == true)
                            {
                                CoordinateTransformation = new Pose(position: registrationHelper.Translation, rotation: registrationHelper.Rotation);
                                float error = CheckCandidateTransformationPose(CoordinateTransformation);
                                Debug.Log("New calibration: " + CoordinateTransformation);
                                Debug.Log("Mean error: " + error);
                            }
                        }
                    }
                }
            }

            previousFrameTime = now;
            previousCameraPose = CameraCache.Main.transform.GetGlobalPose();
        }

        private void CheckDebugMode()
        {
            if (InteractionHandler.DebugMode)
            {
                foreach (var pose in storedPoses.Values)
                {
                    pose.Visual.SetActive(true);
                }
            }
            else
            {
                foreach (var pose in storedPoses.Values)
                {
                    pose.Visual.SetActive(false);
                }
            }
        }

        private float CheckCandidateTransformationPose(Pose transformationCandidate)
        {
            float error = 0.0f;
            foreach(var node in spatialNodes.Values)
            {
                var pose = GetInternalPose(node.ExternalPose, transformationCandidate);
                error += Vector3.Distance(node.InternalPose.position, pose.position);
            }
            return error / spatialNodes.Count;
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
            if (spatialNodes.ContainsKey(qrCode.Data) == false && int.TryParse(qrCode.Data, out int id))
            {
                CreateSpatialNode(qrCode.Data, id);
            }
        }

        private void QRAdded(QRCode qrCode)
        {
            if (spatialNodes.ContainsKey(qrCode.Data) == false && int.TryParse(qrCode.Data, out int id))
            {
                CreateSpatialNode(qrCode.Data, id);
            }
        }

        private void CreateSpatialNode(string idString, int id)
        {
            var node = Instantiate(SpatialNodePrefab, VisualsParentTransform);
            node.gameObject.name = "SpatialNode_" + idString;
            node.ExternalPoseProvider = new OptiTrackPoseProvider(id);
            node.InternalPoseProvider = new QRPoseProvider(idString);
            spatialNodes[idString] = node;
        }

        private struct StoredPose
        {
            public Pose InternalPose;
            public Pose ExternalPose;
            public GameObject Visual;
        }

    }
}
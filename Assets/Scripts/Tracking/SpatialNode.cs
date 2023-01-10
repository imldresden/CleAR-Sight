// Copyright (c) Interactive Media Lab Dresden, Technische Universität Dresden. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using UnityEngine;

namespace IMLD.Unity.Tracking
{

    /// <summary>
    /// This component collects poses from an external and an internal pose provider. It also serves as a "node" representing this pose in the scene.
    /// </summary>
    public class SpatialNode : MonoBehaviour
    {
        /// <summary>
        /// The external pose. Getting the pose will update it first, if necessary.
        /// </summary>
        public Pose ExternalPose
        {
            get
            {
                if (externalPoseAge > 0)
                {
                    UpdateExternalPose();
                    return externalPose;
                }
                else
                {
                    return externalPose;
                }
            }

            private set => externalPose = value;
        }

        /// <summary>
        /// The internal pose. Getting the pose will update it first, if necessary.
        /// </summary>
        public Pose InternalPose
        {
            get
            {
                if (internalPoseAge > 0)
                {
                    UpdateInternalPose();
                    return internalPose;
                }
                else
                {
                    return internalPose;
                }
            }

            private set => internalPose = value;
        }

        /// <summary>
        /// The previous external pose. Getting the pose will update it first, if necessary.
        /// </summary>
        public Pose PreviousExternalPose
        {
            get
            {
                if (externalPoseAge > 0)
                {
                    UpdateExternalPose();
                    return previousExternalPose;
                }
                else
                {
                    return previousExternalPose;
                }
            }

            private set => previousExternalPose = value;
        }

        /// <summary>
        /// The previous internal pose. Getting the pose will update it first, if necessary.
        /// </summary>
        public Pose PreviousInternalPose
        {
            get
            {
                if (internalPoseAge > 0)
                {
                    UpdateInternalPose();
                    return previousInternalPose;
                }
                else
                {
                    return previousInternalPose;
                }
            }

            private set => previousInternalPose = value;
        }

        /// <summary>
        /// The <see cref="IPoseProvider"/> for the external poses.
        /// </summary>
        public IPoseProvider ExternalPoseProvider { get => externalPoseProvider; set => externalPoseProvider = value; }

        /// <summary>
        /// The <see cref="IPoseProvider"/> for the internal poses.
        /// </summary>
        public IPoseProvider InternalPoseProvider { get => internalPoseProvider; set => internalPoseProvider = value; }

        private Pose externalPose;
        private Pose internalPose;
        private Pose previousExternalPose;
        private Pose previousInternalPose;

        private IPoseProvider externalPoseProvider;
        private IPoseProvider internalPoseProvider;

        private int externalPoseAge;
        private int internalPoseAge;

        /// <summary>
        /// Converts a given external pose to an internal pose
        /// </summary>
        /// <param name="pose"></param>
        /// <returns></returns>
        public Pose FromExternalToInternalPose(Pose pose)
        {
            return InternalPose.Multiply(ExternalPose.Inverse().Multiply(pose));
        }

        /// <summary>
        /// Converts a given internal pose to an external pose
        /// </summary>
        /// <param name="pose"></param>
        /// <returns></returns>
        public Pose FromInternalToExternalPose(Pose pose)
        {
            return ExternalPose.Multiply(InternalPose.Inverse().Multiply(pose));
        }

        private void Update()
        {
            UpdateExternalPose();
            UpdateInternalPose();
            transform.SetLocalPose(InternalPose);
        }

        private void UpdateExternalPose()
        {
            Pose pose = default;
            if (ExternalPoseProvider?.GetCurrentPose(out pose) == true)
            {
                PreviousExternalPose = externalPose;
                externalPose = pose;
                externalPoseAge = 0;
            }
        }

        private void UpdateInternalPose()
        {
            Pose pose = default;
            if (InternalPoseProvider?.GetCurrentPose(out pose) == true)
            {
                PreviousInternalPose = internalPose;
                internalPose = pose;
                internalPoseAge = 0;
            }
        }

        private void LateUpdate()
        {
            externalPoseAge++;
            internalPoseAge++;
        }
    }
}
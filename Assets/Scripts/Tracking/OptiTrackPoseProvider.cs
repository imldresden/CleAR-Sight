// Copyright (c) Interactive Media Lab Dresden, Technische Universität Dresden. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using UnityEngine;

namespace IMLD.Unity.Tracking
{

    /// <summary>
    /// An <see cref="IPoseProvider"/> that uses NaturalPoint's OptiTrack system.
    /// This provider is dependent on a <see cref="MotiveDirect"/> component being present in the scene and will not work without one.
    /// </summary>
    public class OptiTrackPoseProvider : IPoseProvider
    {
        /// <summary>
        /// This constructor creates a new instance that tries to get poses for the rigid body with the provided id.
        /// </summary>
        /// <param name="rigidBodyId">the id of the rigid body</param>
        public OptiTrackPoseProvider(int rigidBodyId) => RigidBodyId = rigidBodyId;

        public float Velocity { get; private set; }

        /// <summary>
        /// The id of the rigid body that is used for this pose provider
        /// </summary>
        public int RigidBodyId { get; set; }

        private Pose lastPose;
        private DateTime lastTime;
        //private OptitrackStreamingClient streamingClient;
        private MotiveDirect streamingClient;

        public bool GetCurrentPose(out Pose pose)
        {
            pose = default;
            var now = DateTime.Now;

            if (streamingClient == null)
            {
                //streamingClient = OptitrackStreamingClient.FindDefaultClient();
                streamingClient = MotiveDirect.FindDefaultClient();

                if (streamingClient == null)
                {
                    return false;
                }
            }

            //OptitrackRigidBodyState rbState = streamingClient?.GetLatestRigidBodyState(RigidBodyId, false);
            MotiveDirect.RigidBody rbState = streamingClient.GetLatestRigidBodyState(RigidBodyId);
            //if (rbState == null)
            if (rbState.tracking_valid != true)
            {
                return false;
            }

            //pose = new Pose(rbState.Pose.Position, rbState.Pose.Orientation);
            pose = new Pose(rbState.position, rbState.rotation);

            float dS = Vector3.Distance(pose.position, lastPose.position);
            float dT = (float)(now - lastTime).TotalSeconds;
            if (dT == 0)
            {
                Velocity = 0f;
            }
            else
            {
                Velocity = dS / dT;
            }

            lastTime = now;
            lastPose = pose;

            return true;
        }
    }
}
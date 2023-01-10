// Copyright (c) Interactive Media Lab Dresden, Technische Universität Dresden. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace IMLD.Unity.Tracking
{

    /// <summary>
    /// This is an interface for Spatial Localizers.
    /// Spatial Localizers take care of transformations between internal and external coordinate systems.
    /// </summary>
    public interface ISpatialLocalizer
    {
        /// <summary>
        /// Gets the external pose for a given internal pose.
        /// </summary>
        /// <param name="internalPose"></param>
        /// <returns></returns>
        public Pose GetExternalPose(Pose internalPose);

        /// <summary>
        /// Gets the internal pose for a given external pose.
        /// </summary>
        /// <param name="externalPose"></param>
        /// <returns></returns>
        public Pose GetInternalPose(Pose externalPose);

        public Vector3 ManualOffset { get; set; }
    }
}
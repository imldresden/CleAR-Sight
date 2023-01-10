// Copyright (c) Interactive Media Lab Dresden, Technische Universität Dresden. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace IMLD.Unity.Tracking
{

    /// <summary>
    /// This is an interface for pose providers.
    /// A pose provider is responsible for making poses (e.g., from a tracking system) available to other classes.
    /// </summary>
    public interface IPoseProvider
    {
        /// <summary>
        /// The current (approximate) velocity of the tracked pose.
        /// </summary>
        public float Velocity { get; }

        /// <summary>
        /// Gets the current pose of this pose provider. It will return false if no pose is available.
        /// </summary>
        /// <param name="pose">The current pose or the default pose if no pose was available. No guarantees are made for how old it is, only that it is the latest available pose.</param>
        /// <returns>true if a pose is available, false otherwise.</returns>
        public bool GetCurrentPose(out Pose pose);
    }
}
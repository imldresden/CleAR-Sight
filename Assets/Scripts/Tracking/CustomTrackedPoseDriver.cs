using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

#if ENABLE_VR || ENABLE_AR
using UnityEngine.XR;
using UnityEngine.Experimental.XR.Interaction;
#endif

#if ENABLE_AR
using UnityEngine.XR.Tango;
#endif

namespace UnityEngine.SpatialTracking
{
    // The DefaultExecutionOrder is needed because TrackedPoseDriver does some
    // of its work in regular Update and FixedUpdate calls, but this needs to
    // be done before regular user scripts have their own Update and
    // FixedUpdate calls, in order that they correctly get the values for this
    // frame and not the previous.
    // -32000 is the minimal possible execution order value; -30000 makes it
    // unlikely users chose lower values for their scripts by accident, but
    // still allows for the possibility.

    /// <summary>
    /// The TrackedPoseDriver component applies the current Pose value of a tracked device to the transform of the GameObject.
    /// TrackedPoseDriver can track multiple types of devices including XR HMDs, controllers, and remotes.
    /// </summary>
    [DefaultExecutionOrder(-30000)]
    [Serializable]
    [AddComponentMenu("XR/Tracked Pose Driver")]
    [HelpURL("https://docs.unity3d.com/Packages/com.unity.xr.legacyinputhelpers@2.1/manual/index.html")]
    public class CustomTrackedPoseDriver : TrackedPoseDriver
    {

        public Vector3 offset;
        /// <summary>
        /// Sets the transform that is being driven by the <see cref="TrackedPoseDriver"/>. will only correct set the rotation or position depending on the <see cref="PoseDataFlags"/>
        /// </summary>
        /// <param name="newPosition">The position to apply.</param>
        /// <param name="newRotation">The rotation to apply.</param>
        /// <param name="poseFlags">The flags indiciating which of the position/rotation values are provided by the calling code.</param>
        protected override void SetLocalTransform(Vector3 newPosition, Quaternion newRotation, PoseDataFlags poseFlags)
        {
            if ((trackingType == TrackingType.RotationAndPosition ||
                trackingType == TrackingType.RotationOnly) &&
                (poseFlags & PoseDataFlags.Rotation) > 0)
            {
                transform.localRotation = newRotation;
            }

            if ((trackingType == TrackingType.RotationAndPosition ||
                trackingType == TrackingType.PositionOnly) &&
                (poseFlags & PoseDataFlags.Position) > 0)
            {
                transform.localPosition = newPosition;
            }
            transform.Translate(offset);
        }
    }
}
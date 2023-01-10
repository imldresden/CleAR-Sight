// Copyright (c) Interactive Media Lab Dresden, Technische Universität Dresden. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using MathNet.Numerics.LinearAlgebra;
using System.Collections.Generic;
using UnityEngine;

namespace IMLD.Unity.Tracking
{

    /// <summary>
    /// This class is used to compute the transform between two sets of corresponding points using the SVD decomposition method.
    /// </summary>
    public class RegistrationHelper
    {
        /// <summary>
        /// The translation between the two sets of points
        /// </summary>
        public Vector3 Translation { get; private set; }

        /// <summary>
        /// The rotation between the two sets of points
        /// </summary>
        public Quaternion Rotation { get; private set; }

        private List<Matrix<double>> pointsInternal = new List<Matrix<double>>();
        private List<Matrix<double>> pointsExternal = new List<Matrix<double>>();

        private Matrix<double> mathNetTranslation;
        private Matrix<double> mathNetRotation;

        /// <summary>
        /// Returns the number of stored points per set.
        /// </summary>
        /// <returns></returns>
        public int GetNumberOfPoints()
        {
            if (pointsInternal.Count == pointsExternal.Count)
            {
                return pointsExternal.Count;
            }
            return -1;
        }

        /// <summary>
        /// Adds two corresponding points.
        /// </summary>
        /// <param name="pointExternal"></param>
        /// <param name="pointInternal"></param>
        public void AddCorrespondingPoints(Vector3 pointExternal, Vector3 pointInternal)
        {
            var mathNetPointExternal = Matrix<double>.Build.DenseOfColumnArrays(new[] { pointExternal.x, pointExternal.y, (double)pointExternal.z });
            var mathNetPointInternal = Matrix<double>.Build.DenseOfColumnArrays(new[] { pointInternal.x, pointInternal.y, (double)pointInternal.z });

            pointsExternal.Add(mathNetPointExternal);
            pointsInternal.Add(mathNetPointInternal);
        }

        /// <summary>
        /// Clears all registered pairs of corresponding points.
        /// </summary>
        public void ClearCorrespondingPoints()
        {
            pointsExternal.Clear();
            pointsInternal.Clear();
        }

        public bool ComputeRegistration()
        {
            // 0. Check prerequisites
            if (pointsInternal.Count != pointsExternal.Count || pointsInternal.Count < 3)
            {
                return false;
            }

            // 1. compute centroid
            int n = pointsInternal.Count;
            Matrix<double> CentroidHoloLens = Matrix<double>.Build.Dense(3, 1);
            Matrix<double> CentroidOptiTrack = Matrix<double>.Build.Dense(3, 1);
            for (int i = 0; i < n; i++)
            {
                CentroidHoloLens += pointsInternal[i];
                CentroidOptiTrack += pointsExternal[i];
            }
            CentroidHoloLens /= n;  // centroid of the internal positions
            CentroidOptiTrack /= n; // centroid of the external positions of the HoloLens

            // 2. find rotation
            Matrix<double> H = Matrix<double>.Build.Dense(3, 3);
            for (int i = 0; i < n; i++)
            {
                Matrix<double> HLCentered = pointsInternal[i] - CentroidHoloLens;
                Matrix<double> OTCentered = pointsExternal[i] - CentroidOptiTrack;

                H += OTCentered * HLCentered.Transpose();
            }
            var SVD = H.Svd();
            //double det = (SVD.U.Transpose() * SVD.VT.Transpose()).Determinant();
            mathNetRotation = SVD.VT.Transpose() * SVD.U.Transpose();

            // 2.2 check for reflection matrix:
            double det = mathNetRotation.Determinant();
            if (det < 0)
            {
                Debug.LogWarning("Registration resulted in reflection matrix!");
                // correct rotation
                var correction = Matrix<double>.Build.Diagonal(new double[] { 1, 1, -1 });
                mathNetRotation = SVD.VT.Transpose() * correction * SVD.U.Transpose();
            }

            // 3. find translation
            mathNetTranslation = -mathNetRotation * CentroidOptiTrack + CentroidHoloLens; // standard

            // 4. fill unity types
            Translation = new Vector3((float)mathNetTranslation[0, 0], (float)mathNetTranslation[1, 0], (float)mathNetTranslation[2, 0]);
            Rotation = GetQuaternionFromMatrix(mathNetRotation);

            return true;
        }

        private Quaternion GetQuaternionFromMatrix(Matrix<double> mathNetRotation)
        {
            Matrix4x4 UnityMatrix = Matrix4x4.identity;
            UnityMatrix[0, 0] = (float)mathNetRotation[0, 0];
            UnityMatrix[1, 0] = (float)mathNetRotation[1, 0];
            UnityMatrix[2, 0] = (float)mathNetRotation[2, 0];

            UnityMatrix[0, 1] = (float)mathNetRotation[0, 1];
            UnityMatrix[1, 1] = (float)mathNetRotation[1, 1];
            UnityMatrix[2, 1] = (float)mathNetRotation[2, 1];

            UnityMatrix[0, 2] = (float)mathNetRotation[0, 2];
            UnityMatrix[1, 2] = (float)mathNetRotation[1, 2];
            UnityMatrix[2, 2] = (float)mathNetRotation[2, 2];

            return UnityMatrix.rotation;
        }
    }
}
// Copyright (c) Interactive Media Lab Dresden, Technische Universität Dresden. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace IMLD.MixedRealityAnalysis.Utils
{
    public class Singleton<T> : MonoBehaviour where T : Component
    {
        private static object _lock = new object();
        private static T instance;
        private static bool destroyed;
        public static T Instance
        {
            get
            {
                lock (_lock)
                {
                    if (destroyed)
                    {
                        return null;
                    }

                    if (instance == null)
                    {
                        instance = FindObjectOfType<T>();
                        if (instance == null)
                        {
                            var obj = new GameObject();
                            instance = obj.AddComponent<T>();
                        }
                    }
                }

                return instance;
            }
        }

        private void OnDestroy()
        {
            destroyed = true;
        }

        private void OnApplicationQuit()
        {
            destroyed = true;
        }
    }
}


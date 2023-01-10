// Copyright (c) Interactive Media Lab Dresden, Technische Universität Dresden. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using LifxNet;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SmartBulb : MonoBehaviour
{
    public LightBulb Device { get; set; }
    public BulbState State { get; private set; }

    public GameObject Visual;

    private void Awake()
    {
        State = new BulbState();
        State.IsChanged = false;
        State.LastUpdate = 0f;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetPower(bool power, float transitionTime = 0.0f)
    {
        State.Power = power;
        State.TransitionTime = transitionTime;
        State.HasChangedPower = true;
    }

    public void SetColor(UnityEngine.Color color, float transitionTime = 0.0f)
    {
        State.Color = color;
        State.TransitionTime = transitionTime;
        State.HasChangedColor = true;
    }

    public void SetColor(int temperature, float transitionTime = 0.0f)
    {
        State.Temperature = temperature;
        State.TransitionTime = transitionTime;
        State.HasChangedColor = true;
    }

    public void SetColor(UnityEngine.Color color, int temperature, float transitionTime = 0.0f)
    {
        State.Color = color;
        State.Temperature = temperature;
        State.TransitionTime = transitionTime;
        State.HasChangedColor = true;
    }

    public class BulbState
    {
        public UnityEngine.Color Color { get; set; }
        public int Temperature
        {
            get => temperature;
            set
            {
                temperature = Math.Min(Math.Max(value, 2500), 9000);
            }
        }
        public bool Power { get; set; }

        public float TransitionTime { get; set; }

        public bool HasChangedColor { get; set; }
        public bool HasChangedPower { get; set; }

        public bool IsChanged
        {
            get
            {
                return HasChangedColor || HasChangedPower;
            }

            set
            {
                if (value == false)
                {
                    HasChangedColor = false;
                    HasChangedPower = false;
                }                
            }
        }

        public float LastUpdate { get; set; }

        private int temperature;
    }
}

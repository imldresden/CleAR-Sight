// Copyright (c) Interactive Media Lab Dresden, Technische Universität Dresden. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;

public class TouchEvent
{
    public Vector3 Position { get; private set; }
    public Vector2 Point { get => Touch.position; }

    public TouchState Touch {get; private set;}

    public IReadOnlyList<TouchState> History { get => historyList; }

    public bool IsConsumed { get; private set; }
    public ITouchable CapturedBy { get; private set; }

    public IReadOnlyList<RaycastHit> Targets { get => targetList; }

    public bool IsLocal { get => Touch.flags == 0; }

    public RaycastHit CurrentTarget { get; set; }

    private List<RaycastHit> targetList = new List<RaycastHit>();
    private List<TouchState> historyList = new List<TouchState>();

    public TouchEvent(TouchState touch, Vector3 position, List<RaycastHit> targets)
    {
        targetList = targets;
        Touch = touch;
        historyList.Add(Touch);
        IsConsumed = false;
        CapturedBy = null;
        Position = position;
    }

    public void Update(TouchState touch, Vector3 position, List<RaycastHit> targets)
    {
        historyList.Add(Touch);
        Touch = touch;
        targetList = targets;
        Position = position;
        IsConsumed = false;
    }

    public void Consume()
    {
        IsConsumed = true;
    }

    public void Capture(ITouchable target)
    {
        CapturedBy = target;
    }

    public float GetAge()
    {
        return (float)(Time.realtimeSinceStartupAsDouble - Touch.startTime);
    }

    public float GetDistance()
    {
        return (History.Last().position - History.First().position).magnitude;
    }
}

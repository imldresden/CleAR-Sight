// Copyright (c) Interactive Media Lab Dresden, Technische Universität Dresden. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using UnityEngine;

public interface ITouchable
{
    public Collider Collider { get; }
    public void OnTap(TouchEvent touch);
    public void OnHold(TouchEvent touch);
    public void OnDoubleTap(TouchEvent touch);
    public void OnTouchDown(TouchEvent touch);
    public void OnTouchMove(TouchEvent touch);
    public void OnTouchUp(TouchEvent touch);
}

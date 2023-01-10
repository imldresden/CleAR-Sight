// ------------------------------------------------------------------------------------
// <copyright file="ClippingPlane.cs" company="Technische Universität Dresden">
//      Copyright (c) Technische Universität Dresden.
//      Licensed under the MIT License.
// </copyright>
// <author>
//      Katja Krug
// </author>
// ------------------------------------------------------------------------------------


using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClippingPlane : MonoBehaviour
{
    public Material material;

    // Update is called once per frame
    void Update()
    {
        if(this.GetComponent<InputHandler>().Mode == InputHandler.Modi.Slice)
        {
            Plane plane = new Plane(transform.up, transform.position);
            Vector4 planeVisulization = new Vector4(plane.normal.x, plane.normal.y, plane.normal.z, plane.distance);
            material.SetVector("_Plane", planeVisulization);
        }       
    }
}
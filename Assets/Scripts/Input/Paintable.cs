// Copyright (c) Interactive Media Lab Dresden, Technische Universität Dresden. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using System;
using System.Collections.Generic;
using IMLD.Unity.Tracking;
using IMLD.Unity.Core;

public class Paintable : MonoBehaviour
{
    [HideInInspector]
    public List<CustomTubeRenderer> lines = new List<CustomTubeRenderer>();
    [HideInInspector]
    public List<GameObject> lineObjects = new List<GameObject>();
    [HideInInspector]
    public Vector3 currentDot;

    public DetachedAnnotation DetachedAnnotationPrefab;

    [HideInInspector]
    public IReadOnlyList<DetachedAnnotation> DetachedAnnotations { get => detachedAnnotationsList.AsReadOnly(); }

    public Material lineMat;
    public InputHandler inputHandler;
    public float brushThickness;
    public float OnTabletbrushThickness;
    public float inSituBrushThickness;
    public float projectedBrushThickness;

    [Tooltip("Defines how far the curve points are projected: 0 - zero distance to 1 - full distance.")]
    public float ProjectionDistanceParameter = 1.0f;

    private List<DetachedAnnotation> detachedAnnotationsList = new List<DetachedAnnotation>();

    private CustomTubeRenderer currentLine;    
    private int positionCount = 0;
    private bool strokeInProgress = false;
    private GameObject lineObject;

    void Start()
    {
        brushThickness = OnTabletbrushThickness;
        currentDot = new Vector3();
        //panel = GameObject.Find("Panel");
        //inputHandler = panel.GetComponent<InputHandler>() as InputHandler;

    }

    /// <summary>
    /// <c>drawInput</c> interprets the touch or digital pen input as a paintable dot and creates Linerenderer lines connecting these dots.
    /// Depending on the panel mode (InSitu or on tablet), the dot is either parented by the panel or directly transformed into world coordinates. 
    /// </summary>
   public void DrawInput(Vector3 input, InputHandler.InputTypes inputType, InputHandler.Modi mode)
    {
        currentDot = input;

        // directly transform input to worldspace if InSitu mode is activated
        //if (mode == InputHandler.Modi.InSitu)
        //{
        //    currentDot = this.transform.TransformPoint(input);
        //    currentLine.DrawInSitu = true;
        //}
        //else if (mode == InputHandler.Modi.Projected)      

        if (inputType == InputHandler.InputTypes.Last)
        {
          //  Debug.Log("Stroke - last");
            strokeInProgress = false;
            lines.Add(currentLine);
        }
        else if (inputType == InputHandler.InputTypes.First)
        {
           // Debug.Log("Stroke - first");
            strokeInProgress = true;
            positionCount = 0;

            lineObject = new GameObject("stroke_" + lineObjects.Count);
            //lineObject.AddComponent<LineRenderer>();
            lineObject.AddComponent<CustomTubeRenderer>();

            CustomTubeRenderer newline = CreateNewLine();
            currentLine = newline;
            currentLine.transform.parent = transform.parent;

            // determine panel as the parent of the line object if the panel is in OnTablet mode
            if (mode == InputHandler.Modi.DrawOnTablet)
            {
                currentLine.DrawInSitu = false;
                //currentLine.useWorldSpace = false;
                currentLine.transform.parent = transform;
                currentLine.transform.localRotation = Quaternion.Euler(0, 0, 0);
                currentLine.transform.localScale = new Vector3(1, 1, 1);
                currentLine.transform.localPosition = new Vector3(0, 0, 0);
            }

            lineObjects.Add(lineObject);
        }

        if (mode == InputHandler.Modi.InSitu)
        {
            //currentDot = GetProjectedPoint(input, ProjectionDistanceParameter);
            currentLine.DrawInSitu = true;
        }

        if (strokeInProgress)
        {
            /*if(inputHandler.Mode == InputHandler.Modi.InSitu)
            {
                brushThickness *= 10;
            }*/
            //Debug.Log("Stroke - in progress");
            currentLine.AddPosition(currentDot, brushThickness, Color.cyan);
            //currentLine.positionCount++;
            //currentLine.SetPosition(positionCount, currentDot);
            positionCount++;
        }
    }

    public Vector3 GetProjectedPoint(Vector3 input, float curveParameter = 1, float maxDistance = 8)
    {
        float parameter = Mathf.Clamp01(curveParameter);
        Vector3 inputWorldPos = transform.TransformPoint(input);
        //Vector3 rayStart = CameraCache.Main.transform.position;
        Vector3 rayStart = UserPositionManager.Instance.GetClosestUser(transform.position).position;
        Vector3 rayDirection = (inputWorldPos - rayStart).normalized;
        RaycastHit hitInfo;
        Vector3 result;
        if (Physics.Raycast(rayStart, rayDirection, out hitInfo, maxDistance, 1 << 31))
        {
            result = inputWorldPos + parameter * (hitInfo.point - inputWorldPos);
        }
        else
        {
            result = inputWorldPos + rayDirection * maxDistance * parameter;
        }

        Debug.Log(result);
        return result;
    }

    /// <summary>
    /// <c>Undo</c> deletes the most recently created stroke.
    /// </summary>
    public void Undo()
    {
        if (lineObjects.Count >= 0)
        {
            GameObject go = lineObjects.LastOrDefault<GameObject>();
            Destroy(go);
            lineObjects.Remove(go);
        }
    }

    /// <summary>
    /// <c>Clear</c> deletes all strokes present in the scene
    /// </summary>
    public void Clear()
    {
        // clear all strokes on the canvas and in-situ
        foreach (GameObject go in lineObjects)
        {
            Destroy(go);
        }

        lineObjects.Clear();

        // clear all strokes on any detached canvas
        foreach (var annotation in detachedAnnotationsList)
        {
            foreach (GameObject go in annotation.lineObjects)
            {
                Destroy(go);
            }

            annotation.lineObjects.Clear();
            Destroy(annotation);
        }

        detachedAnnotationsList.Clear();
    }

    /// <summary>
    /// This method detaches the current set of strokes from the canvas.
    /// It transfers them to a DetachedAnnotation container and modifies the container based on the given position, scale, and rotation.
    /// </summary>
    /// <param name="position"></param>
    /// <param name="scale"></param>
    /// <param name="rotation"></param>
    public void DetachAnnotation(Vector3 position, Vector3 scale, Quaternion rotation)
    {
        // return early if there is nothing to detach
        if (lineObjects.Count == 0)
        {
            return;
        }

        // instantiate a container for the annotations
        DetachedAnnotation holder = Instantiate(DetachedAnnotationPrefab);
        holder.Id = detachedAnnotationsList.Count;
        holder.transform.parent = transform.parent;
        holder.transform.localScale = scale;
        holder.transform.localPosition = position;
        holder.transform.localRotation = rotation;

        List<GameObject> tempList = new List<GameObject>();
        // transfer all lines that are currently on the canvas but not the in-situ lines
        foreach (GameObject line in lineObjects)
        {
            if (line.GetComponent<CustomTubeRenderer>().DrawInSitu == false)
            {
                holder.lineObjects.Add(line);
                line.transform.SetParent(holder.transform);
            }
            else
            {
                tempList.Add(line);
            }
        }

        if (holder.lineObjects.Count > 0)
        {
            detachedAnnotationsList.Add(holder);
            lineObjects.Clear();
            lineObjects = tempList;
        }
        else
        {
            Destroy(holder);
        }        
    }

    private CustomTubeRenderer CreateNewLine()
    {
        var newline = lineObject.GetComponent<CustomTubeRenderer>();
        newline.material = lineMat;
        newline.Sides = 3;
        return newline;
    }
}
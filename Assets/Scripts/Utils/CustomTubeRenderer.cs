// ------------------------------------------------------------------------------------
// <copyright file="CustomTubeRenderer.cs">
//      Copyright (c) Mathias Soeholm & Interactive Media Lab Dresden, Technische Universität Dresden
//		Licensed under the MIT License.
// </copyright>
// <author>
//      Mathias Soeholm, modifications by Wolfgang Büschel
// </author>
// <comment>
//		Source: https://gist.github.com/mathiassoeholm/15f3eeda606e9be543165360615c8bef
//		Original file comment:
//		Author: Mathias Soeholm
//		Date: 05/10/2016
//		No license, do whatever you want with this script
// </comment>
// ------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class CustomTubeRenderer : MonoBehaviour
{
	List<Vector3> positions;
	List<Color> colors;
	List<float> radii;

	public int Sides;
	public float startWidth;
	public float endWidth;
	public Color Color;
	public bool DrawInSitu = false;
	
	private Vector3[] vertices;
	private Color[] vertexColors;
	private Mesh mesh;
	private MeshFilter meshFilter;
	private MeshRenderer meshRenderer;
	private int numSides;
	private bool draw3D;

	public Material material
	{
		get { return meshRenderer.material; }
		set { meshRenderer.material = value; }
	}

	void Awake()
	{
		meshFilter = GetComponent<MeshFilter>();
		if (meshFilter == null)
		{
			meshFilter = gameObject.AddComponent<MeshFilter>();
		}

		meshRenderer = GetComponent<MeshRenderer>();
		if (meshRenderer == null)
		{
			meshRenderer = gameObject.AddComponent<MeshRenderer>();
		}

		mesh = new Mesh();
		meshFilter.mesh = mesh;
	}

	private void OnEnable()
	{
		meshRenderer.enabled = true;
	}

	private void OnDisable()
	{
		meshRenderer.enabled = false;
	}

    private void Start()
    {
		numSides = Sides;
		draw3D = DrawInSitu;
    }

    private void Update()
    {
        if (numSides != Sides || draw3D != DrawInSitu)
        {
            numSides = Sides;
			draw3D = DrawInSitu;
			GenerateMesh();
        }
    }

    public void SetPositions(List<List<Vector3>> segmentsList, List<List<Color>> colorSegmentsList = null)
	{
		bool IsUsingPerVertexColor = true;
		if (segmentsList == null || segmentsList.Count < 1)
		{
			return;
		}

		int TotalPoints = 0;		
		for (int i = 0; i < segmentsList.Count; i++)
		{
			TotalPoints += segmentsList[i].Count+2;
			if (colorSegmentsList == null || colorSegmentsList.Count != segmentsList.Count || segmentsList[i].Count != colorSegmentsList[i].Count)
			{
				IsUsingPerVertexColor = false;
			}
		}

		positions = new List<Vector3>(TotalPoints);
		radii = new List<float>(TotalPoints);

		int CurrentPoint = 0;
		for (int i = 0; i < segmentsList.Count; i++)
		{
			var Segment = segmentsList[i];
			if (Segment.Count < 2)
			{
				return;
			}

			Vector3 v0offset = (Segment[0] - Segment[1]) * 0.01f;
			positions[CurrentPoint] = v0offset + Segment[0];
			radii[CurrentPoint] = 0.0f;
			CurrentPoint++;

			for (int p = 0; p < Segment.Count; p++)
			{
				positions[CurrentPoint] = Segment[p];
				radii[CurrentPoint] = Mathf.Lerp(startWidth, endWidth, (float)CurrentPoint / TotalPoints);
				CurrentPoint++;
			}

			Vector3 v1offset = (Segment[Segment.Count - 1] - Segment[Segment.Count - 2]) * 0.01f;
			positions[CurrentPoint] = v1offset + Segment[Segment.Count - 1];
			radii[CurrentPoint] = 0.0f;
			CurrentPoint++;
		}

		colors = new List<Color>(TotalPoints);
		CurrentPoint = 0;
		if (IsUsingPerVertexColor)
		{
			for (int i = 0; i < segmentsList.Count; i++)
			{
				var ColorList = colorSegmentsList[i];
				if (ColorList.Count < 2)
				{
					return;
				}

                colors[CurrentPoint] = ColorList[0];
				CurrentPoint++;

				for (int p = 0; p < ColorList.Count; p++)
				{
					colors[CurrentPoint] = ColorList[p];
					CurrentPoint++;
				}

				colors[CurrentPoint] = ColorList[ColorList.Count - 1];
				CurrentPoint++;
			}
		}
		else
		{
			for(int i = 0; i < TotalPoints; i++)
            {
				colors[i] = Color;
            }
		}
		GenerateMesh();
	}

	public void AddPosition(Vector3 point, float radius, Color color)
    {
		if(positions == null)
        {
			positions = new List<Vector3>();
			radii = new List<float>();
			colors = new List<Color>();
		}

		switch(positions.Count)
        {
			case 0:
				positions.Add(point);
				radii.Add(0.0f);
				colors.Add(color);

				positions.Add(point); // add point again as placeholder for start cap				
				radii.Add(radius);
				colors.Add(color);
				break;
			case 2:
				positions.Add(point);
				radii.Add(radius);
				colors.Add(color);

				// compute start cap
				positions[0] = positions[1] + (positions[1] - positions[2]) * 0.001f;
				radii[0] = 0.0f;

				// add end cap
				positions.Add(positions[2] + (positions[2] - positions[1]) * 0.001f);
				radii.Add(0.0f);
				colors.Add(color);
				break;
			default:
				positions[positions.Count - 1] = point;
				radii[positions.Count - 1] = radius;

				// update end cap
				positions.Add(positions[positions.Count - 1] + (positions[positions.Count - 1] - positions[positions.Count - 2]) * 0.001f);
				radii.Add(0.0f);
				colors.Add(color);
				break;
        }

		GenerateMesh();
	}

	private void GenerateMesh()
	{
		if (mesh == null || positions == null || positions.Count < 4)
		{
			mesh = new Mesh();
			return;
		}

		var verticesLength = draw3D ? numSides * positions.Count : 2 * positions.Count;
		if (vertices == null || vertices.Length != verticesLength)
		{
			vertices = new Vector3[verticesLength];
			vertexColors = new Color[verticesLength];

			var indices = GenerateIndices();
			var uvs = GenerateUVs();

			if (verticesLength > mesh.vertexCount)
			{
				mesh.vertices = vertices;
				mesh.triangles = indices;
				mesh.uv = uvs;
			}
			else
			{
				mesh.triangles = indices;
				mesh.vertices = vertices;
				mesh.uv = uvs;
			}
		}

		var currentVertIndex = 0;

		for (int i = 0; i < positions.Count; i++)
		{
			var circle = CalculateCircle(i);
			foreach (var vertex in circle)
			{
				vertices[currentVertIndex] = vertex;
				vertexColors[currentVertIndex] = colors[i];
				currentVertIndex++;
			}
		}

		mesh.vertices = vertices;
		mesh.colors = vertexColors;
		mesh.RecalculateNormals();
		mesh.RecalculateBounds();

		meshFilter.mesh = mesh;
	}

	private Vector2[] GenerateUVs()
	{
		int sides;
		if (draw3D)
		{
			sides = numSides;
		}
		else
		{
			sides = 2;
		}

		var uvs = new Vector2[positions.Count* sides];

		for (int segment = 0; segment < positions.Count; segment++)
		{
			for (int side = 0; side < sides; side++)
			{
				var vertIndex = (segment * sides + side);
				var u = side/(sides - 1f);
				var v = segment/(positions.Count - 1f);

				uvs[vertIndex] = new Vector2(u, v);
			}
		}

		return uvs;
	}

	private int[] GenerateIndices()
	{
		int sides;
		if(draw3D)
        {
			sides = numSides;
		}
		else
        {
			sides = 2;
		}
		// Two triangles and 3 vertices
		var indices = new int[positions.Count * sides * 2*3];

		var currentIndicesIndex = 0;
		for (int segment = 1; segment < positions.Count; segment++)
		{
			for (int side = 0; side < sides; side++)
			{
				var vertIndex = (segment* sides + side);
				var prevVertIndex = vertIndex - sides;

				// Triangle one
				indices[currentIndicesIndex++] = prevVertIndex;
				indices[currentIndicesIndex++] = (side == sides - 1) ? (vertIndex - (sides - 1)) : (vertIndex + 1);
				indices[currentIndicesIndex++] = vertIndex;
				

				// Triangle two
				indices[currentIndicesIndex++] = (side == sides - 1) ? (prevVertIndex - (sides - 1)) : (prevVertIndex + 1);
				indices[currentIndicesIndex++] = (side == sides - 1) ? (vertIndex - (sides - 1)) : (vertIndex + 1);
				indices[currentIndicesIndex++] = prevVertIndex;
			}
		}

		return indices;
	}

	private Vector3[] CalculateCircle(int index)
	{
		var dirCount = 0;
		var forward = Vector3.zero;

		// If not first index
		if (index > 0)
		{
			forward += (positions[index] - positions[index - 1]).normalized;
			dirCount++;
		}

		// If not last index
		if (index < positions.Count-1)
		{
			forward += (positions[index + 1] - positions[index]).normalized;
			dirCount++;
		}

		// Forward is the average of the connecting edges directions
		forward = (forward / dirCount).normalized;

		Vector3[] circle;

		if (draw3D)
        {

			var side = Vector3.Cross(forward, forward + new Vector3(.123564f, .34675f, .756892f)).normalized;
			var up = Vector3.Cross(forward, side).normalized;

			circle = new Vector3[numSides];
			var angle = 0f;
			var angleStep = (2 * Mathf.PI) / numSides;

			var t = index / (positions.Count - 1f);
			var radius = radii[index];

			for (int i = 0; i < numSides; i++)
			{
				var x = Mathf.Cos(angle);
				var y = Mathf.Sin(angle);

				circle[i] = positions[index] + side * x * radius + up * y * radius;

				angle += angleStep;
			}
		}
		else
        {
			var side = Vector3.Cross(forward, Vector3.up).normalized;
			var radius = radii[index];
			circle = new Vector3[2];
			circle[0] = positions[index] + side * radius;
			circle[1] = positions[index] - side * radius;
		}


		return circle;
	}
}
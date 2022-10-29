using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;

[ExecuteInEditMode]
public class ProceduralCylinder : MonoBehaviour
{
	private MeshFilter mf;

	[Min(0f)]
	public float radius = 0.5f;
	[Min(0f)]
	public float height = 2f;
	[Range(3, 48)]
	public int slices = 8;

	public void Start()
	{
		mf = GetComponent<MeshFilter>();
		if (!mf)
		{
			mf = gameObject.AddComponent<MeshFilter>();
			mf.sharedMesh = GenerateCylinderMesh(radius, height, slices);
		}

		MeshRenderer mr = GetComponent<MeshRenderer>();
		if (!mr)
		{
			mr = gameObject.AddComponent<MeshRenderer>();
			mr.sharedMaterial = new Material(Shader.Find("Standard"));
		}
	}

	public void OnValidate()
	{
		if (mf)
		{
			mf.sharedMesh = GenerateCylinderMesh(radius, height, slices);
		}
	}

	public static Mesh GenerateCylinderMesh(float radius, float height, int slices)
	{
		Mesh mesh = new Mesh();

		Vector3[] vertices = new Vector3[slices * 2 + 2];
		int[] triangles = new int[slices * 12];

		for (int i = 0; i < slices; i++)
		{
			float angle = 2f * Mathf.PI * ((float)i / slices);
			float x = radius * Mathf.Cos(angle);
			float y = radius * Mathf.Sin(angle);
			vertices[2 * i] = new Vector3(x, y, 0f);
			vertices[2 * i + 1] = new Vector3(x, y, height);
		}

		int top = vertices.Length - 1;
		int buttom = top - 1;
		
		vertices[buttom] = new Vector3(0f, 0f, 0f);
		vertices[top] = new Vector3(0f, 0f, height);

		
		for (int i = 0; i < slices * 12; i += 12)
		{
			int a = i / 6;
			int b = a + 1;
			int c = (a + 2) % (buttom);
			int d = (a + 3) % (buttom);

			// side
			triangles[i + 0] = a;
			triangles[i + 1] = c;
			triangles[i + 2] = b;
			triangles[i + 3] = b;
			triangles[i + 4] = c;
			triangles[i + 5] = d;

			// top	
			triangles[i + 9] = top;
			triangles[i + 10] = b;
			triangles[i + 11] = d;

			// bottom
			triangles[i + 6] = a;
			triangles[i + 7] = buttom;
			triangles[i + 8] = c;
		}

		mesh.vertices = vertices;
		mesh.triangles = triangles;

		mesh.RecalculateNormals();
		mesh.RecalculateTangents();
		mesh.RecalculateBounds();
		mesh.Optimize();

		return mesh;
	}
}

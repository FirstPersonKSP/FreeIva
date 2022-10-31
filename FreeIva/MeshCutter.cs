using Parabox.CSG;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FreeIva
{
	public static class MeshCutter
	{
		public static void Cut(InternalModel model, List<CutParameter> parameters)
		{
			if (parameters.Count == 0)
			{
				return;
			}

			Debug.Log($"[FreeIVA/MeshCutter] Cutting on internal '{model.internalName}'");
			DateTime starTime = DateTime.Now;

			IEnumerable<string> targets = parameters.Select(x => x.target).Distinct();
			foreach (string targetName in targets)
			{
				Debug.Log($"[FreeIVA/MeshCutter] Cutting on target '{targetName}' on internal '{model.internalName}'");
				MeshFilter target = model.FindModelTransform(targetName).gameObject.GetComponent<MeshFilter>();
				List<GameObject> tools = parameters.Where(x => x.target == targetName).Select(parameter => CreateTool(model, parameter)).ToList();
				ApplyCut(target, tools);
			}

			Debug.Log($"[FreeIVA/MeshCutter] Cutting on internal'{model.internalName}' done ({parameters.Count} cut(s) on {targets.Count()} target(s)), time used: {(DateTime.Now - starTime).TotalMilliseconds}ms");
		}

		private static void ApplyCut(MeshFilter target, List<GameObject> tools)
		{
			// All these transform nonsenses are for counteracting a bug in Paradox.CSG
			//
			// where the subtraction only works properly if the transform of the target
			// is at world origin with no rotation and scaling
			//
			// It's probably fixable by editing its source code, but I won't touch that

			// create a temp object to hold all the children
			Transform temp = new GameObject("temp").transform;
			temp.parent = target.transform.parent;
			temp.localPosition = target.transform.localPosition;
			temp.localRotation = target.transform.localRotation;
			temp.localScale = target.transform.localScale;

			// re-parent all the children
			for (int i = target.transform.childCount - 1; i >= 0; i--)
			{
				target.transform.GetChild(i).SetParent(temp);
			}

			// re-parent all the tools to the target object
			foreach (GameObject tool in tools)
			{
				tool.transform.parent = target.transform;
			}

			// reset target transform to world origin
			target.transform.parent = null;
			target.transform.position = Vector3.zero;
			target.transform.rotation = Quaternion.identity;
			target.transform.localScale = Vector3.one;

			// majik!
			Model model = CSG.SubtractMultiple(target.gameObject, tools);

			// remove all but the first sub mesh
			Mesh mesh = new Mesh();
			mesh.vertices = model.mesh.vertices;
			mesh.triangles = model.mesh.GetTriangles(0);
			mesh.normals = model.mesh.normals;
			mesh.tangents = model.mesh.tangents;
			mesh.uv = model.mesh.uv;
			mesh.Optimize();

			// assign the result mesh back to target object
			target.sharedMesh = mesh;

			// put target object back to where it belongs
			target.transform.parent = temp.parent;
			target.transform.localPosition = temp.localPosition;
			target.transform.localRotation = temp.localRotation;
			target.transform.localScale = temp.localScale;

			// give all the children back
			for (int i = temp.childCount - 1; i >= 0; i--)
			{
				temp.GetChild(i).SetParent(target.transform, false);
			}

			// destroy temp object and all the tools
			UnityEngine.Object.Destroy(temp.gameObject);
			foreach (GameObject tool in tools)
			{
				UnityEngine.Object.Destroy(tool);
			}
		}

		private static GameObject CreateTool(InternalModel model, CutParameter parameter)
		{
			if (parameter.type == CutParameter.Type.Mesh)
			{
				return model.FindModelTransform(parameter.tool).gameObject;
			}
			else
			{
				GameObject g;
				if (parameter.type == CutParameter.Type.ProceduralCylinder)
				{
					g = new GameObject("FreeIVA_MeshCutter_Tool");
					g.AddComponent<MeshFilter>().sharedMesh = GenerateCylinderMesh(parameter.radius, parameter.height, parameter.slices);
					g.AddComponent<MeshRenderer>().sharedMaterial = new Material(Shader.Find("Standard")); // has to be a different material
				}
				else
				{
					g = GameObject.CreatePrimitive(parameter.type == CutParameter.Type.Cube ? PrimitiveType.Cube : PrimitiveType.Cylinder);
				}
				g.transform.parent = model.transform;
				g.transform.localPosition = parameter.position;
				g.transform.localRotation = Quaternion.Euler(parameter.rotation);
				g.transform.localScale = parameter.scale;
				return g;
			}
		}

		private static Mesh GenerateCylinderMesh(float radius, float height, int slices)
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
}

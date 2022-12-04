using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Profiling;

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
			var stopwatch = new System.Diagnostics.Stopwatch();
			stopwatch.Start();
			Profiler.BeginSample("MeshCut");

			IEnumerable<string> targets = parameters.Select(x => x.target).Distinct();
			foreach (string targetName in targets)
			{
				Debug.Log($"[FreeIVA/MeshCutter] Cutting on target '{targetName}' on internal '{model.internalName}'");
				MeshFilter target = TransformUtil.FindInternalModelTransform(model, targetName)?.gameObject?.GetComponent<MeshFilter>();

				if (target != null)
				{
					List<GameObject> tools = parameters.Where(x => x.target == targetName).Select(parameter => CreateTool(model, parameter)).ToList();
					ApplyCut(target, tools);
				}
			}

			Profiler.EndSample();
			stopwatch.Stop();
			Debug.Log($"[FreeIVA/MeshCutter] Cutting on internal '{model.internalName}' done ({parameters.Count} cut(s) on {targets.Count()} target(s)), time used: {stopwatch.Elapsed.TotalMilliseconds}ms");
		}

		private static void ApplyCut(MeshFilter target, List<GameObject> tools)
		{
			// majik!
			try
			{
				var cutter = new MeshCutter2(target);

				foreach (var tool in tools)
				{
					cutter.CutMesh(tool);
				}

				cutter.FinalizeMesh();
			}
			catch (Exception ex)
			{
				Debug.LogException(ex);
			}


			foreach (GameObject tool in tools)
			{
				UnityEngine.Object.Destroy(tool);
			}
		}

		private static GameObject CreateTool(InternalModel model, CutParameter parameter)
		{
			if (parameter.type == CutParameter.Type.Mesh)
			{
				return parameter.tool != null ? parameter.tool : TransformUtil.FindInternalModelTransform(model, parameter.toolName).gameObject;
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

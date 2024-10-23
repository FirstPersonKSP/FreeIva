using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Profiling;

namespace FreeIva
{
	public static class MeshCutter
	{
		public static void CreateToolsForCutParameters(InternalModel internalModel, List<CutParameter> parameters)
		{
			foreach(var cutParam in parameters)
			{
				cutParam.tool = CreateTool(internalModel, cutParam);
			}
		}

		public static void DestroyTools(ref List<CutParameter> parameters)
		{
			foreach (var cutParam in parameters)
			{
				GameObject.Destroy(cutParam.tool);
			}

			parameters.Clear();
			parameters = null;
		}

		// Applies many-to-many cuts to the entire internal model (each param specifies what its target is, and the cuts to each target are grouped and executed together)
		public static void CutInternalModel(InternalModel model, List<CutParameter> parameters)
		{
			if (parameters.Count == 0)
			{
				return;
			}

			var stopwatch = new System.Diagnostics.Stopwatch();
			stopwatch.Start();
			Profiler.BeginSample("MeshCut");

			var cutGroups = parameters.GroupBy(cutParameter => cutParameter.target);

			int cutCount = 0;
			int targetCount = 0;

			foreach (var targetCuts in cutGroups)
			{
				var targetName = targetCuts.Key;
				if (targetName == string.Empty) continue;

				++targetCount;
				cutCount += targetCuts.Count();

				Log.Debug($"Cutting on target '{targetName}' on internal '{model.internalName}'");
				Transform targetTransform = TransformUtil.FindInternalModelTransform(model, targetName);

				if (targetTransform == null) continue;

				MeshFilter target = targetTransform.gameObject.GetComponent<MeshFilter>();

				if (target == null)
				{
					Log.Error($"[FreeIva/MeshCutter] transform {targetName} in internal '{model.internalName}' does not have a MeshFilter component");
					continue;
				}

				ApplyCut(target, targetCuts);
			}

			Profiler.EndSample();
			stopwatch.Stop();
			if (cutCount > 0)
			{
				Log.Debug($"Cutting on internal '{model.internalName}': {cutCount} cut(s) on {targetCount} target(s), time used: {stopwatch.Elapsed.TotalMilliseconds}ms");
			}
		}

		// applies a set of cuts to a single target.  The target field of the cutParameters is ignored
		public static void ApplyCut(MeshFilter target, IEnumerable<CutParameter> cutParameters)
		{
			// majik!
			try
			{
				var cutter = new MeshCutter2(target);

				foreach (var cutParam in cutParameters)
				{
					var toolMeshes = cutParam.tool.GetComponentsInChildren<MeshFilter>();
					foreach (var toolMesh in toolMeshes)
					{
						cutter.CutMesh(toolMesh.gameObject);
					}
				}

				cutter.FinalizeMesh();
			}
			catch (Exception ex)
			{
				Log.Exception(ex);
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

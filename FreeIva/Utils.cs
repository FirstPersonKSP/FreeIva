using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.IO;
using System.Reflection;

namespace FreeIva
{
	public static class Utils
	{
		public static char[] CfgSplitChars = new char[] { ',', ' ', '\t' };

		private static Shader _depthMask = null;
		public static Shader GetDepthMask()
		{
			if (_depthMask != null)
				return _depthMask;
			/*string path = Utils.GetDllDirectoryPath() + "../Shaders/DepthMask.shader";
            if (!File.Exists(path))
            {
                Log.Error("[Free IVA] Error loading shader: File not found at " + path);
                return null;
            }*/
			/* No longer supported
            StreamReader sr = new StreamReader(path);
            _depthMask = new Material(sr.ReadToEnd()).shader;
            sr.Close(); */
			_depthMask = Shader.Find("DepthMask");
			if (_depthMask == null)
				Log.Error("[Free IVA] Error loading depth mask shader: Shader not found.");
			return _depthMask;
		}

		private static Material _depthMaskMaterial = null;
		public static Material GetDepthMaskMaterial()
		{
			if (_depthMaskMaterial == null)
			{
				_depthMaskMaterial = new Material(GetDepthMask());
			}
			return _depthMaskMaterial;
		}

		// really we should be loading this at startup or something
		private static Shader _depthMaskCullingShader = null;
		public static Shader GetDepthMaskCullingShader()
		{
			if (_depthMaskCullingShader == null)
			{
				_depthMaskCullingShader = AssetLoader.Instance.GetShader("DepthMask_Culling");
			}

			return _depthMaskCullingShader;
		}

		private static Material _depthMaskCullingMaterial = null;

		public static Material GetDepthMaskCullingMaterial()
		{
			if (_depthMaskCullingMaterial == null)
			{
				_depthMaskCullingMaterial = new Material(GetDepthMaskCullingShader());
			}

			return _depthMaskCullingMaterial;
		}

		public static LineRenderer line;
		//public static LineRenderer upLine;
		//public static LineRenderer rightLine;
		//public static LineRenderer forwardLine;
		public static LineRenderer CreateLine(Color startColor, Color endColor, float startWidth, float endWidth)
		{
			GameObject obj = new GameObject();
			obj.layer = 20;
			LineRenderer line = obj.AddComponent<LineRenderer>();
			line.material = new Material(Shader.Find("Particles/Alpha Blended"));
			line.startColor = startColor;
			line.endColor = endColor;
			line.startWidth = startWidth;
			line.endWidth = endWidth;
			line.positionCount = 2;

			return line;
		}

		/*public void DrawPositionLines()
        {
            upLine.SetPosition(0, InternalCamera.Instance.transform.position);
            upLine.SetPosition(1, InternalCamera.Instance.transform.position + InternalCamera.Instance.transform.up); // Green
            rightLine.SetPosition(0, InternalCamera.Instance.transform.position);
            rightLine.SetPosition(1, InternalCamera.Instance.transform.position + InternalCamera.Instance.transform.right); // Blue
            forwardLine.SetPosition(0, InternalCamera.Instance.transform.position);
            forwardLine.SetPosition(1, InternalCamera.Instance.transform.position + InternalCamera.Instance.transform.forward); // Red
        }*/

		public static T CloneComponent<T>(T oldComponent, GameObject to) where T : Component
		{
			if (oldComponent != null)
			{
				var newComponent = to.AddComponent<T>();

				FieldInfo[] fields = typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				foreach (var field in fields)
				{
					object value = field.GetValue(oldComponent);
					field.SetValue(newComponent, value);
				}

				return newComponent;
			}

			return null;
		}

		public static void PrintInternals(Part p)
		{
			Log.Message("Part: " + p);
			MeshRenderer[] componentsInChildren = p.GetComponentsInChildren<MeshRenderer>();
			MeshRenderer[] array = componentsInChildren;
			for (int i = 0; i < array.Length; i++)
			{
				MeshRenderer meshRenderer = array[i];
				Log.Message("   Mesh renderer: " + meshRenderer);
			}
			SkinnedMeshRenderer[] componentsInChildren2 = p.GetComponentsInChildren<SkinnedMeshRenderer>();
			SkinnedMeshRenderer[] array2 = componentsInChildren2;
			for (int j = 0; j < array2.Length; j++)
			{
				SkinnedMeshRenderer skinnedMeshRenderer = array2[j];
				Log.Message("   Skinned mesh renderer: " + skinnedMeshRenderer);
			}
		}

		private static bool dumpComplete = false;
		public static void DumpIvaContents()
		{
			if (dumpComplete) return;

			StringBuilder sb = new StringBuilder();
			foreach (Part p in FlightGlobals.ActiveVessel.Parts)
			{
				sb.Append("Part ").Append(p.name).Append("\n");

				// External part renderers.
				sb.Append("\t\t").Append("Mesh renderers").Append("\n");
				MeshRenderer[] meshRenderers = p.GetComponentsInChildren<MeshRenderer>();
				if (meshRenderers.Length == 0)
					sb.Append("\t\t\t").Append("-").Append("\n");
				foreach (var mr in meshRenderers)
				{
					sb.Append("\t\t\t").Append(mr.name).Append("\n");
				}
				sb.Append("\t\t").Append("Skinned mesh renderers").Append("\n");
				SkinnedMeshRenderer[] skinnedMeshRenderer = p.GetComponentsInChildren<SkinnedMeshRenderer>();
				if (skinnedMeshRenderer.Length == 0)
					sb.Append("\t\t\t").Append("-").Append("\n");
				foreach (var smr in skinnedMeshRenderer)
				{
					sb.Append("\t\t\t").Append(smr.name).Append("\n");
				}

				// Internals.
				if (p.internalModel == null)
				{
					sb.Append("\t").Append("No internal").Append("\n");
					continue;
				}
				else
					sb.Append("\t").Append(p.internalModel.ToString()).Append("\n");

				sb.Append("\t\t").Append("Mesh renderers").Append("\n");
				MeshRenderer[] iMeshRenderers = p.internalModel.GetComponentsInChildren<MeshRenderer>();
				if (iMeshRenderers.Length == 0)
					sb.Append("\t\t\t").Append("-").Append("\n");
				foreach (var imr in iMeshRenderers)
				{
					//imr.enabled = false;
					sb.Append("\t\t\t").Append(imr.name).Append("\n");
					if (imr.name.Contains("Seat"))
					{
						if (imr.material != null)
						{
							sb.Append("*** Highlighting MR\n");
							imr.enabled = true;
						}
						else
							Log.Message("Highlighting MR - null");
					}
				}
				sb.Append("\t\t").Append("Skinned mesh renderers").Append("\n");
				SkinnedMeshRenderer[] iSkinnedMeshRenderer = p.internalModel.GetComponentsInChildren<SkinnedMeshRenderer>();
				if (iSkinnedMeshRenderer.Length == 0)
					sb.Append("\t\t\t").Append("-").Append("\n");
				foreach (var ismr in iSkinnedMeshRenderer)
				{
					//ismr.enabled = false;
					sb.Append("\t\t\t").Append(ismr.name).Append("\n");
					if (ismr.name.Contains("Seat"))
					{
						if (ismr.material != null)
						{
							sb.Append("*** Highlighting SMR\n");
							ismr.enabled = true;
						}
						else
							Log.Message("Highlighting SMR - null");
					}
				}
			}
			Log.Message(sb.ToString());
			dumpComplete = true;
		}

		public static void ChangeHelmetMesh(GameObject original, Part p)
		{
			//http://forum.kerbalspaceprogram.com/threads/87562-Changing-the-EVA-Kerbal-model/page4
			try
			{
				string modelPath = "FreeIva/Models/TestSphere";
				Log.Debug("#Changing helmet mesh");
				GameObject testSphere = GameDatabase.Instance.GetModel(modelPath);
				if (testSphere != null)
				{
					MeshFilter mfC = original.GetComponent<MeshFilter>();

					MeshFilter mfM = null;
					foreach (ProtoCrewMember c in p.protoModuleCrew)
					{
						if (c.KerbalRef.headTransform != null)
						{
							mfM = c.KerbalRef.headTransform.GetComponentInChildren<MeshFilter>();
							/*MeshFilter[] mfs = c.KerbalRef.headTransform.GetComponentsInChildren<MeshFilter>();
                            foreach (var mf in mfs)
                            {
                                Log.Message(mf);
                                mfM = mf;
                                break;
                            }*/
						}
						if (mfM != null) break;
					}

					Mesh m = FreeIva.Instantiate(mfM.mesh) as Mesh;
					mfC.mesh = m;
					Log.Debug("#Changed mesh");
				}
				else
					Log.Error("TestSphere.dae not found at " + modelPath);
			}
			catch (Exception ex)
			{
				Log.Error("Error Loading mesh: " + ex.Message + ", " + ex.StackTrace);
			}
		}
	}

	static class GameObjectExtension
	{
		public static T GetOrAddComponent<T>(this GameObject gameObject) where T : Component
		{
			T result = gameObject.GetComponent<T>();
			if (result == null)
			{
				result = gameObject.AddComponent<T>();
			}
			return result;
		}
	}
}

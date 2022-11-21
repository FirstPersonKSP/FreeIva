using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace FreeIva
{
	internal static class ColliderUtil
	{
		enum CapsuleAxis
		{
			X = 0,
			Y = 1,
			Z = 2
		}


		public static Collider CreateCollider(Transform t, ConfigNode cfg, string debugContext)
		{
			Collider result = null;
			Vector3 center = Vector3.zero, boxDimensions = Vector3.zero;
			float radius = 0, height = 0;
			CapsuleAxis axis = CapsuleAxis.X;
			string colliderShape = string.Empty;

			if (!cfg.TryGetValue("shape", ref colliderShape))
			{
				Debug.LogError($"[FreeIVA] Collider in {debugContext} does not have a collider in the model and does not have a shape field");
			}
			else switch (colliderShape)
			{
			case "Capsule":
				if (cfg.TryGetValue("center", ref center) &&
					cfg.TryGetValue("radius", ref radius) &&
					cfg.TryGetValue("height", ref height) &&
					cfg.TryGetEnum("axis", ref axis, CapsuleAxis.X))
				{
					var collider = t.gameObject.AddComponent<CapsuleCollider>();
					collider.radius = radius;
					collider.height = height;
					collider.center = center;
					collider.direction = (int)axis;
					result = collider;
				}
				else
				{
					Debug.LogError($"[FreeIVA] Collider in {debugContext}: capsule shape requires center, radius, height, and axis fields");
				}
				break;
			case "Box":
				if (cfg.TryGetValue("center", ref center) &&
					cfg.TryGetValue("dimensions", ref boxDimensions))
				{
					var collider = t.gameObject.AddComponent<BoxCollider>();
					collider.center = center;
					collider.size = boxDimensions;
					result = collider;
				}
				else
				{
					Debug.LogError($"[FreeIVA] Collider in {debugContext}: box shape requires center and dimensions fields");
				}
				break;
			case "Sphere":
				if (cfg.TryGetValue("center", ref center) &&
					cfg.TryGetValue("radius", ref radius))
				{
					var collider = t.gameObject.AddComponent<SphereCollider>();
					collider.center = center;
					collider.radius = radius;
					result = collider;
				}
				else
				{
					Debug.LogError($"[FreeIVA] Collider in {debugContext }: sphere shape requires center and radius fields");
				}
				break;
			default:
				Debug.LogError($"[FreeIVA] Collider in {debugContext} has invalid collider shape '{colliderShape}");
				break;
			}

			AddColliderVisualizer(result);

			return result;
		}

		public static void AddColliderVisualizer(Collider collider)
		{
#if true
			if (collider == null) return;
			if (collider is BoxCollider box)
			{
				var debugObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
				Component.Destroy(debugObject.GetComponent<Collider>());
				debugObject.transform.SetParent(collider.transform, false);
				debugObject.transform.localPosition = box.center;
				debugObject.transform.localScale = box.size;
				debugObject.layer = 20;
			}
			// TODO:
#endif
		}
	}
}

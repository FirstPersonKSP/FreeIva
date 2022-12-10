using System.Collections.Generic;
using UnityEngine;

namespace FreeIva
{
	/* Things learned:
     * Objects from GameObject.CreatePrimitive need a Rigidbody added to take part in collisions.
     * Parenting the object to the camera causes the collisions to be ignored (same as between parts?).
     */
	/// <summary>
	/// Debug utility to print IVA collision information to the screen.
	/// </summary>
	/// <seealso cref="WorldCollisionTracker"/>
	public class IvaCollisionTracker : MonoBehaviour
	{
		public static bool PrintingEnabled = false;

		private List<Collision> m_collisions = new List<Collision>();
		public List<Collision> Collisions => m_collisions;
		public int RailColliderCount = 0;

		public InternalModel CurrentInternalModel { get; set; }

		void PrintCollisionInfo(string eventName, Collision collision)
		{
#if DEBUG
			if (!PrintingEnabled) return;

			var internalModel = collision.gameObject.GetComponentUpwards<InternalModel>();
			var prop = collision.gameObject.GetComponentUpwards<InternalProp>();

			string context = "unknown";

			if (prop != null)
			{
				context = $"prop '{prop.propName}' in internal '{prop.internalModel.internalName}'";
			}
			else if (internalModel != null)
			{
				context = $"internal '{internalModel.internalName}'";
			}


			ScreenMessages.PostScreenMessage($"{eventName} {name} with {collision.gameObject.name} in {context} on layer {collision.gameObject.layer}",
					1f, ScreenMessageStyle.LOWER_CENTER);
#endif
		}

        public void FixedUpdate()
        {
			m_collisions.Clear();
        }

        public void OnCollisionEnter(Collision collision)
		{
			m_collisions.Add(collision);

			PrintCollisionInfo("OnCollisionEnter", collision);
		}

		public void OnCollisionStay(Collision collision)
		{
			m_collisions.Add(collision);
			PrintCollisionInfo("OnCollisionStay", collision);
		}

		public void OnCollisionExit(Collision collision)
		{
			
			PrintCollisionInfo("OnCollisionExit", collision);
		}

		static HandRail ColliderGetHandRail(Collider collider)
		{
			var watcher = collider.gameObject.GetComponent<ClickWatcher>();

			if (watcher != null)
			{
				foreach (var a in watcher.MouseDownActions)
				{
					if (a.Target is HandRail rail)
					{
						return rail;
					}
				}
			}

			return null;
		}

		public void OnTriggerEnter(Collider other)
		{
			HandRail rail = ColliderGetHandRail(other);
			if (rail != null)
			{
				CurrentInternalModel = rail.internalModel;
				++RailColliderCount;
			}

#if DEBUG
			if (PrintingEnabled)
			{
				//Debug.Log("# OnTriggerEnter " + other.transform);
				ScreenMessages.PostScreenMessage("OnTriggerEnter " + other.transform,
				1f, ScreenMessageStyle.LOWER_CENTER);
			}
#endif
		}

		public void OnTriggerExit(Collider other)
		{
			if (ColliderGetHandRail(other) != null)
			{
				--RailColliderCount;
			}
		}
	}
}

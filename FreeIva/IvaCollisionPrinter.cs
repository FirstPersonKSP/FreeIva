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
	public class IvaCollisionPrinter : MonoBehaviour
	{
		public static bool Enabled = false;

		void PrintCollisionInfo(string eventName, Collision collision)
		{
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


			ScreenMessages.PostScreenMessage($"{eventName} {name} with {collision.gameObject} in {context} on layer {collision.gameObject.layer}",
					1f, ScreenMessageStyle.LOWER_CENTER);
		}

		public void OnCollisionEnter(Collision collision)
		{
			if (Enabled)
			{
				PrintCollisionInfo("OnCollisionEnter", collision);
				
			}
		}

		public void OnCollisionStay(Collision collision)
		{
			if (Enabled)
			{
				PrintCollisionInfo("OnCollisionStay", collision);
			}
		}

		public void OnCollisionExit(Collision collision)
		{
			if (Enabled)
			{
				PrintCollisionInfo("OnCollisionExit", collision);
			}
		}

		public void OnTriggerEnter(Collider other)
		{
			if (Enabled)
			{
				//Debug.Log("# OnTriggerEnter " + other.transform);
				ScreenMessages.PostScreenMessage("OnTriggerEnter " + other.transform,
				1f, ScreenMessageStyle.LOWER_CENTER);
			}
		}
	}
}

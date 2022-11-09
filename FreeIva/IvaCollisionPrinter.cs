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

		public void OnCollisionEnter(Collision collision)
		{
			if (Enabled)
			{
				//Debug.Log("# OnCollisionEnter " + name + " with " + collision.gameObject + " layer " + collision.gameObject.layer);
				ScreenMessages.PostScreenMessage("OnCollisionEnter " + name + " with " + collision.gameObject + " layer " + collision.gameObject.layer,
					1f, ScreenMessageStyle.LOWER_CENTER);
			}
		}

		public void OnCollisionStay(Collision collision)
		{
			if (Enabled)
			{
				//Debug.Log("# OnCollisionStay " + collision.gameObject + " with " + collision.transform);
				ScreenMessages.PostScreenMessage("OnCollisionStay " + collision.gameObject + " with " + collision.transform + " layer " + collision.gameObject.layer,
				1f, ScreenMessageStyle.LOWER_CENTER);
			}
		}

		public void OnCollisionExit(Collision collision)
		{
			if (Enabled)
			{
				//Debug.Log("# OnCollisionExit " + collision.gameObject + " with " + collision.transform);
				ScreenMessages.PostScreenMessage("OnCollisionExit " + collision.gameObject + " with " + collision.transform + " layer " + collision.gameObject.layer,
				1f, ScreenMessageStyle.LOWER_CENTER);
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

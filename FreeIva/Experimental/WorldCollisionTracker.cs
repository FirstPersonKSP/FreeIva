#if Experimental
using UnityEngine;

namespace FreeIva
{
	/// <summary>
	/// Experiment into applying collisions from an object in world space to a character collider in IVA space.
	/// i.e. Have two instances of the collider for an IVA kerbal, one for inside IVAs in IVA space, and one 
	/// for external collisions with terrain, world objects and part externals in world space,
	/// then integrate the two somehow.
	/// </summary>
	/// <seealso cref="IvaCollisionPrinter"/>
	public class WorldCollisionTracker : MonoBehaviour
	{
		private Rigidbody _rbToApplyCollisionsTo;
		public void Initialise(Rigidbody rbToApplyCollisionsTo)
		{
			Debug.Log("# Starting WorldCollisionTracker");
			_rbToApplyCollisionsTo = rbToApplyCollisionsTo;
		}

		public void OnCollisionEnter(Collision collision)
		{
			Part collidingPart = collision.gameObject.GetComponent<Part>();
			if (collidingPart != null && FreeIva.CurrentPart == collidingPart)
			{
				return;
			}
			ScreenMessages.PostScreenMessage("World OnCollisionEnter " + name + " with " + collision.gameObject + " with force " + collision.impulse,
				1f, ScreenMessageStyle.LOWER_CENTER);
			_rbToApplyCollisionsTo.AddForce(collision.impulse, ForceMode.Impulse);
		}

		public void OnCollisionStay(Collision collision)
		{
			Part collidingPart = collision.gameObject.GetComponent<Part>();
			if (collidingPart != null && FreeIva.CurrentPart == collidingPart)
			{
				return;
			}
			ScreenMessages.PostScreenMessage("World OnCollisionStay " + name + " with " + collision.gameObject + " with force " + collision.impulse,
				1f, ScreenMessageStyle.LOWER_CENTER);
			_rbToApplyCollisionsTo.AddForce(collision.impulse, ForceMode.Impulse);
		}
	}
}
#endif

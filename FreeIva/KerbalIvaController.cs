using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

/* The kerbal is made up of 3 objects:
 * At the root is the object that contains the collider(s) and rigid body.  This does not rotate except to orient relative to gravity.
 * The origin of the root object is at the eye position (this might change...)
 * Attached to the root object is the camera anchor.  This turns in accordance with the user inputs.  Movement is relative to this transform
 * Attached to the camera anchor is the object that contains the InternalCamera.  This object's local transform is usually identity, except in VR (it represents the head tracking)
 */

namespace FreeIva
{
	public class KerbalIvaController : MonoBehaviour
	{
		public SphereCollider KerbalCollider; // this may eventually change to a Capsule
		public SphereCollider KerbalFeetCollider;
		public Rigidbody KerbalRigidbody;
		public Transform CameraAnchor;
		public ProtoCrewMember ActiveKerbal;
		public IvaCollisionTracker KerbalCollisionTracker;

		public bool WearingHelmet { get; private set; }

		public bool IsOnLadder { get; private set; }

		public bool CollisionEnabled
		{
			get { return KerbalCollider.enabled; }
			set
			{
				KerbalCollider.enabled = value;
				KerbalFeetCollider.enabled = UseRelativeMovement();
			}
		}

		// TODO: Vary this by kerbal stats and equipment carried: 45kg for a kerbal, 94kg with full jetpack and parachute.
		public static float KerbalMass = 1000f * 0.03125f; // From persistent file for EVA kerbal. Use PhysicsGlobals.KerbalCrewMass instead?

		void Awake()
		{
			gameObject.layer = (int)Layers.Kerbals;

			KerbalCollider = gameObject.AddComponent<SphereCollider>();
			KerbalCollider.material.staticFriction = 0.0f;
			KerbalCollider.material.dynamicFriction = 0.0f;
			KerbalCollider.material.bounciness = 0.0f;
			KerbalCollider.material.frictionCombine = PhysicMaterialCombine.Minimum;
			KerbalCollider.material.bounceCombine = PhysicMaterialCombine.Minimum;
			KerbalCollider.isTrigger = false;
			KerbalCollider.radius = Settings.NoHelmetSize;

			KerbalFeetCollider = gameObject.AddComponent<SphereCollider>();
			KerbalFeetCollider.isTrigger = false;
			KerbalFeetCollider.radius = Settings.NoHelmetSize * 0.9f;
			KerbalFeetCollider.center = new Vector3(0, -Settings.NoHelmetSize, 0);

			KerbalRigidbody = gameObject.AddComponent<Rigidbody>();
			KerbalRigidbody.useGravity = false;
			KerbalRigidbody.mass = KerbalMass;
			KerbalRigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;
			// Rotating the object would offset the rotation of the controls from the camera position.
			KerbalRigidbody.constraints = RigidbodyConstraints.FreezeRotation;

			KerbalCollisionTracker = gameObject.AddComponent<IvaCollisionTracker>();

			CameraAnchor = new GameObject("CameraAnchor").transform;
			CameraAnchor.SetParent(transform, false);
		}

		public void OrientToGravity(Vector3 flightAccel)
		{
			if (UseRelativeMovement())
			{
				// get the vector pointing straight down, and pitch it up by 90 degrees
				transform.rotation = Quaternion.LookRotation(flightAccel, transform.rotation * Vector3.forward) * Quaternion.AngleAxis(90, Vector3.left);
			}
		}

		public void Activate(ProtoCrewMember kerbal)
		{
			ActiveKerbal = kerbal;

			previousRotation = InternalCamera.Instance.transform.rotation;
			InternalCamera.Instance.ManualReset(false);

			transform.position = ActiveKerbal.KerbalRef.eyeTransform.position;

			KerbalCollisionTracker.RailColliderCount = 0;
			KerbalCollisionTracker.CurrentInternalModel = kerbal.seat.internalModel;
			currentCentrifuge = InternalModuleFreeIva.GetForModel(KerbalCollisionTracker.CurrentInternalModel)?.ModuleDeployableCentrifuge;

			if (UseRelativeMovement())
			{
				Vector3 flightAccel = KerbalIvaAddon.Instance.GetFlightAccelerationInternalSpace();
				OrientToGravity(flightAccel);
				KerbalFeetCollider.enabled = KerbalIvaAddon.Gravity;

				currentRelativeOrientation = (Quaternion.Inverse(transform.rotation) * InternalCamera.Instance.transform.rotation).eulerAngles;
				currentRelativeOrientation.z = 0; // roll

				if (currentRelativeOrientation.x > 180)
				{
					currentRelativeOrientation.x -= 360;
				}
				if (currentRelativeOrientation.x > 90 || currentRelativeOrientation.x < -90)
				{
					currentRelativeOrientation.y += 180;
					currentRelativeOrientation.x = Mathf.Clamp(currentRelativeOrientation.x, -90, 90);
				}
			}
			else
			{
				transform.rotation = Quaternion.identity;
				KerbalFeetCollider.enabled = false;
			}
			
			InternalCamera.Instance.transform.parent = CameraAnchor;
			InternalCamera.Instance.transform.localPosition = Vector3.zero;
			InternalCamera.Instance.transform.localRotation = Quaternion.identity;

			gameObject.SetActive(true);
		}

		public bool UseRelativeMovement()
		{
			// eventually we might want to include flying in atmosphere, etc
			return FlightGlobals.ActiveVessel.LandedOrSplashed && KerbalIvaAddon.Gravity || (currentCentrifuge != null && currentCentrifuge.CurrentSpinRate > 0);
		}

		public Quaternion previousRotation = new Quaternion();
		public Vector3 currentRelativeOrientation;
		float crouchingFraction = 0;
		public float targetCrouchFraction = 0;

		public void UpdateOrientation(Vector3 rotationInput)
		{
			Vector3 angularSpeed = Time.fixedDeltaTime * new Vector3(
				rotationInput.x * Settings.PitchSpeed,
				rotationInput.y * Settings.YawSpeed,
				rotationInput.z * Settings.RollSpeed);

			if (UseRelativeMovement())
			{
				currentRelativeOrientation += angularSpeed;
				currentRelativeOrientation.z = 0; // when on the ground, we never roll the camera relative to gravity

				currentRelativeOrientation.x = Mathf.Clamp(currentRelativeOrientation.x, -90, 90);
				currentRelativeOrientation.y = currentRelativeOrientation.y % 360;

				CameraAnchor.localRotation = Quaternion.Euler(currentRelativeOrientation);
			}
			else 
			{
				// there's probably a more straightforward way to convert the angular speed to a quaternion and multiply them directly
				Quaternion rotYaw = Quaternion.AngleAxis(angularSpeed.y, previousRotation * Vector3.up);
				Quaternion rotPitch = Quaternion.AngleAxis(angularSpeed.x, previousRotation * Vector3.right);// *Quaternion.Euler(0, 90, 0);
				Quaternion rotRoll = Quaternion.AngleAxis(angularSpeed.z, previousRotation * Vector3.forward);

				// transform.rotation = rotRoll * rotPitch * rotYaw * previousRotation;
				CameraAnchor.localRotation = rotRoll * rotPitch * rotYaw * CameraAnchor.localRotation;
			}
		}

		void UpdatePosition_Weightless(Vector3 movementThrottle)
		{
			Vector3 desiredLocalSpeed = new Vector3(
				movementThrottle.x * Settings.HorizontalSpeed,
				movementThrottle.y * Settings.VerticalSpeed,
				movementThrottle.z * Settings.ForwardSpeed);

			bool tryingToMove = desiredLocalSpeed != Vector3.zero;

			Quaternion orientation = previousRotation; // TODO: should this be the current camera anchor rotation?

			Vector3 desiredInternalVelocity = orientation * desiredLocalSpeed;

			Vector3 velocityDelta = desiredInternalVelocity - KerbalRigidbody.velocity;

			float desiredDeltaSpeed = velocityDelta.magnitude;
			float maxDeltaSpeed = GetMaxDeltaSpeed(tryingToMove, false);
			if (desiredDeltaSpeed > maxDeltaSpeed)
			{
				velocityDelta = velocityDelta.normalized * maxDeltaSpeed;
			}

			if (KerbalRigidbody.velocity.magnitude < 0.02f && !tryingToMove && desiredDeltaSpeed < maxDeltaSpeed)
			{
				KerbalRigidbody.Sleep();
			}
			else
			{
				KerbalRigidbody.AddForce(velocityDelta, ForceMode.VelocityChange);
			}
		}

		Vector3 GetCentrifugeTangentVelocity()
		{
			if (currentCentrifuge == null) return Vector3.zero;

			float omega = Mathf.Deg2Rad * currentCentrifuge.CurrentSpinRate;
			if (omega == 0) return Vector3.zero;

			// for now, we'll assume that the centrifuge is spinning around the "top/bottom" axis - Z in local IVA space
			Transform rotationRoot = currentCentrifuge.IVARotationRoot;
			Vector3 localKerbalPosition = rotationRoot.InverseTransformPoint(transform.position);
			localKerbalPosition.z = 0;

			// centripetal acceleration = omega^2 * r
			Vector3 localAcceleration = omega * omega * localKerbalPosition;

			Vector3 localTangentVelocity = Vector3.Cross(new Vector3(0, 0, -omega), localKerbalPosition);

			return rotationRoot.TransformVector(localTangentVelocity);
		}

		void UpdatePosition_InGravity(Vector3 flightAccel, Vector3 movementThrottle, bool jump)
		{
			Vector3 desiredLocalSpeed = new Vector3(
				movementThrottle.x * Settings.HorizontalSpeed,
				0,
				movementThrottle.z * Settings.ForwardSpeed);

			bool tryingToMove = desiredLocalSpeed != Vector3.zero || jump;

			// take the yaw angle but nothing else (maintain global up)
			Quaternion orientation = transform.rotation * Quaternion.Euler(0, currentRelativeOrientation.y, 0);

			// Make the movement relative to the camera rotation.
			Vector3 desiredInternalVelocity = orientation * desiredLocalSpeed;
			bool grounded = GetGroundPlane(flightAccel, out Plane groundPlane);

			if (KerbalIvaAddon.Gravity && currentCentrifuge == null)
			{
				float gravityScale = grounded ? 0.1f : 1f;
				KerbalRigidbody.AddForce(gravityScale * flightAccel, ForceMode.Acceleration);
			}

			if (grounded)
			{
				// rotate the desired world velocity along the ground plane
				float desiredSpeed = desiredInternalVelocity.magnitude;
				desiredInternalVelocity = Vector3.ProjectOnPlane(desiredInternalVelocity, groundPlane.normal);
				desiredInternalVelocity = desiredInternalVelocity.normalized * desiredSpeed;

				if (jump)
				{
					// Jump in the opposite direction to gravity.
					KerbalRigidbody.AddForce(-flightAccel.normalized * Settings.JumpForce, ForceMode.VelocityChange);
					KerbalIvaAddon.Instance.JumpLatched = true;
				}
			}

			Vector3 velocityDelta = desiredInternalVelocity - KerbalRigidbody.velocity;

			if (currentCentrifuge != null && grounded)
            {
				Vector3 tangentVelocity = GetCentrifugeTangentVelocity();
				velocityDelta += tangentVelocity;
            }

			// if we're not on the ground, don't change velocity in the vertical direction (this stops us from fighting gravity with desired velocity)
			if (!grounded)
			{
				// if we're in a centrifuge, just don't change velocity at all while airborne
				if (currentCentrifuge != null)
				{
					velocityDelta = Vector3.zero;
				}
				else
				{
					velocityDelta = Vector3.ProjectOnPlane(velocityDelta, flightAccel.normalized);
				}
			}

			float desiredDeltaSpeed = velocityDelta.magnitude;
			float maxDeltaSpeed = GetMaxDeltaSpeed(tryingToMove, true);
			if (desiredDeltaSpeed > maxDeltaSpeed)
			{
				velocityDelta = velocityDelta.normalized * maxDeltaSpeed;
			}

			if (KerbalRigidbody.velocity.magnitude < 0.02f && !tryingToMove && desiredDeltaSpeed < maxDeltaSpeed && grounded)
			{
				KerbalRigidbody.Sleep();
			}
			else
			{
				KerbalRigidbody.AddForce(velocityDelta, ForceMode.VelocityChange);
			}
		}

		void UpdatePosition_OnLadder(Vector3 flightAccel, Vector3 movementThrottle)
		{
			Vector3 desiredLocalSpeed = new Vector3(
				movementThrottle.x * Settings.HorizontalSpeed,
				movementThrottle.y * Settings.VerticalSpeed,
				movementThrottle.z * Settings.ForwardSpeed);
			bool tryingToMove = desiredLocalSpeed != Vector3.zero;

			// take the yaw angle but nothing else (maintain global up)
			// TODO: we might want to make movement relative to the ladder itself, but I suspect not all of them will be oriented in a consistent way
			Quaternion orientation = transform.rotation * Quaternion.Euler(0, currentRelativeOrientation.y, 0);
			Vector3 desiredInternalVelocity = orientation * desiredLocalSpeed;

			Vector3 tangentVelocity = GetCentrifugeTangentVelocity();
			Vector3 velocityDelta = desiredInternalVelocity - (KerbalRigidbody.velocity - tangentVelocity);

			float desiredDeltaSpeed = velocityDelta.magnitude;
			float maxDeltaSpeed = GetMaxDeltaSpeed(tryingToMove, true);
			if (desiredDeltaSpeed > maxDeltaSpeed)
			{
				velocityDelta = velocityDelta.normalized * maxDeltaSpeed;
			}

			if (KerbalRigidbody.velocity.magnitude < 0.02f && !tryingToMove && desiredDeltaSpeed < maxDeltaSpeed)
			{
				KerbalRigidbody.Sleep();
			}
			else
			{
				KerbalRigidbody.AddForce(velocityDelta, ForceMode.VelocityChange);
			}
		}

		void UpdateLadderState(Vector3 movementThrottle, bool jump)
		{
			if (!IsOnLadder)
			{
				if (movementThrottle.y != 0 && KerbalCollisionTracker.RailColliderCount > 0)
				{
					IsOnLadder = true;
				}
			}
			else
			{
				// TODO: we may want to allow a grace period or distance after losing contact with the ladder to aid in transitioning between parts
				if (KerbalCollisionTracker.RailColliderCount == 0 || jump)
				{
					IsOnLadder = false;
				}
			}
		}

		void UpdateCrouching()
		{
			if (targetCrouchFraction != crouchingFraction)
			{
				crouchingFraction = Mathf.MoveTowards(crouchingFraction, targetCrouchFraction, Time.fixedDeltaTime * 2);

				KerbalFeetCollider.center = Vector3.Lerp(new Vector3(0, -Settings.NoHelmetSize, 0), Vector3.zero, crouchingFraction);
			}
		}

		SSPX_ModuleDeployableCentrifuge currentCentrifuge;

		public Vector3 GetCentrifugeAccel()
		{
			if (currentCentrifuge == null) return Vector3.zero;

			float omega = Mathf.Deg2Rad * currentCentrifuge.CurrentSpinRate;
			if (omega == 0) return Vector3.zero;

			// for now, we'll assume that the centrifuge is spinning around the "top/bottom" axis - Z in local IVA space
			Transform rotationRoot = currentCentrifuge.IVARotationRoot;
			Vector3 localKerbalPosition = rotationRoot.InverseTransformPoint(transform.position);
			localKerbalPosition.z = 0;

			// centripetal acceleration = omega^2 * r
			Vector3 localAcceleration = omega * omega * localKerbalPosition;

			return rotationRoot.TransformVector(localAcceleration);
		}

		public void UpdatePosition(Vector3 flightAccel, Vector3 movementThrottle, bool jump)
		{
			if (UseRelativeMovement())
			{
				UpdateLadderState(movementThrottle, jump);
				if (IsOnLadder)
				{
					UpdatePosition_OnLadder(flightAccel, movementThrottle);
				}
				else
				{
					UpdateCrouching();
					UpdatePosition_InGravity(flightAccel, movementThrottle, jump);
				}
			}
			else
			{
				UpdatePosition_Weightless(movementThrottle);
			}

#if Experimental
			// Move the world space collider.
			KerbalWorldSpaceCollider.GetComponentCached<Rigidbody>(ref KerbalWorldSpaceRigidbody);
			//KerbalWorldSpaceRigidbody.MovePosition(KerbalCollider.transform.localPosition);
			KerbalWorldSpaceRigidbody.MovePosition(InternalSpace.InternalToWorld(KerbalCollider.transform.localPosition));
#endif
		}

		public void DoFixedUpdate(KerbalIvaAddon.IVAInput input)
		{
			// if we're not in a centrifuge, see if we're trying to grab a rail in one
			if (currentCentrifuge == null && (input.Jump || input.MovementThrottle.y != 0) && KerbalCollisionTracker.RailColliderCount > 0)
			{
				currentCentrifuge = InternalModuleFreeIva.GetForModel(KerbalCollisionTracker.CurrentInternalModel)?.ModuleDeployableCentrifuge;
				KerbalIvaAddon.Instance.JumpLatched = currentCentrifuge != null;
			}

			KerbalFeetCollider.enabled = UseRelativeMovement();

			// TODO: eventually combine these so that a centrifuge on the surface works properly (yikes)
			Vector3 flightAccel;
			if (currentCentrifuge != null)
			{
				flightAccel = GetCentrifugeAccel();
			}
			else
			{
				flightAccel = KerbalIvaAddon.Instance.GetFlightAccelerationInternalSpace();
			}

			//FallthroughCheck();
			OrientToGravity(flightAccel);
			UpdateOrientation(input.RotationInputEuler);
			UpdatePosition(flightAccel, input.MovementThrottle, input.Jump);
		}

		List<ContactPoint> contactPoints = new List<ContactPoint>();

		bool GetGroundPlane(Vector3 gravity, out Plane plane)
		{
			Vector3 up = -Vector3.Normalize(gravity);
			float cosWalkableSlope = Mathf.Cos(Mathf.Deg2Rad * Settings.WalkableSlope);

			Vector3 accumulatedPosition = Vector3.zero;
			Vector3 accumulatedNormal = Vector3.zero;
			int contactPointCount = 0;

			foreach (var collision in KerbalCollisionTracker.Collisions)
			{
				if (collision.contactCount > contactPoints.Capacity)
				{
					contactPoints.Capacity = collision.contactCount;
				}

				contactPoints.Clear();

				collision.GetContacts(contactPoints);

				foreach (var contactPoint in contactPoints)
				{
					if (Vector3.Dot(contactPoint.normal, up) >= cosWalkableSlope)
					{
						accumulatedNormal += contactPoint.normal;
						accumulatedPosition += contactPoint.point;
						++contactPointCount;

						Debug.DrawRay(contactPoint.point, contactPoint.normal, Color.red, 0, true);
					}
				}
			}

			if (contactPointCount > 0)
			{
				accumulatedNormal.Normalize();
				accumulatedPosition /= contactPointCount;

				plane = new Plane(accumulatedNormal, accumulatedPosition);
				return true;
			}

			plane = new Plane();
			return false;
		}

		static float GetMaxDeltaSpeed(bool accelerating, bool isGrounded)
		{
			float result;
			if (accelerating)
			{
				result = Settings.MaxAcceleration;
			}
			else
			{
				result = isGrounded ? Settings.MaxDecelerationGrounded : Settings.MaxDecelerationWeightless;
			}

			return result * Time.fixedDeltaTime;
		}

		public void HelmetOn()
		{
			if (!WearingHelmet)
			{
				WearingHelmet = true;
				KerbalCollider.radius = Settings.HelmetSize;
			}
		}

		public void HelmetOff()
		{
			if (WearingHelmet)
			{
				WearingHelmet = false;
				KerbalCollider.radius = Settings.NoHelmetSize;
			}
		}
	}
}

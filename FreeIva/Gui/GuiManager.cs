using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace FreeIva
{
	class GuiManager
	{
		// GUI fields
		private static int _partIndex = 0;
		private static int _hatchIndex = 0;
		private static int _colliderIndex = 0;
		private static int _propIndex = 0;
		private static int _meshRendererIndex = 0;
		private static int _skinnedMeshRendererIndex = 0;
		private static int _transformIndex = 0;

		private static bool _advancedMode = false;

		static GameObject lastHitGameObject;

		public static void Gui()
		{
			if (GUILayout.Button("Hide GUI until next launch"))
				GuiUtils.DrawGui = false;

			// Part selection
			/*if (GUILayout.Button("Previous Part"))
                partIndex--;
            if (GUILayout.Button("Next Part"))
            {
                partIndex++;
                partChanged = true;
            }
            if (partIndex >= FlightGlobals.ActiveVessel.Parts.Count)
                partIndex = 0;
            if (partIndex < 0)
                partIndex = FlightGlobals.ActiveVessel.Parts.Count - 1;
            CurrentPart = FlightGlobals.ActiveVessel.Parts[partIndex];*/
			GuiUtils.label("Active kerbal", KerbalIvaAddon.Instance.ActiveKerbal == null ? "null" : KerbalIvaAddon.Instance.ActiveKerbal.name);
			GuiUtils.label("Part (" + (_partIndex + 1) + "/" + FlightGlobals.ActiveVessel.Parts.Count + ")", FreeIva.CurrentPart);

			// Internals
			if (FreeIva.CurrentPart == null ||  FreeIva.CurrentPart.internalModel == null)
			{
				GUILayout.Label("No internal model found");
				//return;
			}

			GUILayout.BeginHorizontal();
			Settings.EnableCollisions= !GUILayout.Toggle(!Settings.EnableCollisions, "Disable collisions");
			KerbalIvaAddon.Instance.Gravity = GUILayout.Toggle(KerbalIvaAddon.Instance.Gravity, "Gravity");
			GUILayout.EndHorizontal();


			if (Input.GetMouseButtonDown(0))
			{
				RaycastHit hit;
				//Send a ray from the camera to the mouseposition
				Ray ray = InternalCamera.Instance._camera.ScreenPointToRay(Input.mousePosition);
				//Create a raycast from the Camera and output anything it hits
				if (Physics.Raycast(ray, out hit, 1000f, InternalCamera.Instance._camera.eventMask, QueryTriggerInteraction.Collide))
					//Check the hit GameObject has a Collider
					if (hit.collider != null)
					{
						//Click a GameObject to return that GameObject your mouse pointer hit
						lastHitGameObject = hit.collider.gameObject;
						//Set this GameObject you clicked as the currently selected in the EventSystem
						
					}
			}

			if (lastHitGameObject != null)
			{
				GUILayout.Label("Hit collider: " + lastHitGameObject.name);
			}

			_advancedMode = GUILayout.Toggle(_advancedMode, "Advanced mode");

			if (!_advancedMode)
				return;

			GuiUtils.label("Selected object", FreeIva.SelectedObject);

#if Experimental
			if (ColliderManipulator.MovingObject) return;
#endif

			KerbalColliderGui();

			if (FreeIva.CurrentInternalModuleFreeIva == null)
				GUILayout.Label("Free IVA module not found in this part. ModuleManager_FreeIvaParts.cfg update required.");

			HatchGui();
#if Experimental
			PressureGui();
#endif
			ShadowsGui();
			SeatGui();
			PropGui();
			MeshRendererGui();
			SkinnedMeshRendererGui();
			TransformGui();
			LookGui();
			PhysicsGui();
			AnimationGui();


			//GUILayout.Label("Closest part: " + (closestPart == null ? "None" : closestPart.name));
			//GUILayout.Label("Closest bounds distance: " + DoCollisions());
			//GUILayout.Label("Colliding: " + colliding);
		}

		private static bool showShadowGui = false;
		private static void ShadowsGui()
		{
			if (GUILayout.Button((showShadowGui ? "Hide" : "Show") + " shadows configuration"))
			{
				showShadowGui = !showShadowGui;
			}

			if (!showShadowGui) return;

			IVASun ivaSun = InternalSpace.Instance.transform.Find("IVASun").GetComponent<IVASun>();

			float bias = ivaSun.ivaLight.shadowBias;
			GuiUtils.slider("bias", ref bias, 0, 1);
			ivaSun.ivaLight.shadowBias = bias;

			float normalBias = ivaSun.ivaLight.shadowNormalBias;
			GuiUtils.slider("normal bias", ref normalBias, 0, 1);
			ivaSun.ivaLight.shadowNormalBias = normalBias;

			ivaSun.ivaLight.shadows = (LightShadows)GuiUtils.radioButtons(Enum.GetNames(typeof(LightShadows)), (int)ivaSun.ivaLight.shadows);
		}

		private static bool showKerbalColliderGui = false;
		private static void KerbalColliderGui()
		{
			if (GUILayout.Button((showKerbalColliderGui ? "Hide" : "Show") + " kerbal collider configuration"))
				showKerbalColliderGui = !showKerbalColliderGui;

			if (!showKerbalColliderGui)
				return;


			var distance = InternalCamera.Instance.transform.position - KerbalIvaAddon.Instance.KerbalIva.transform.position;
			GuiUtils.label("Distance from camera to Kerbal IVA", distance);

			GUILayout.BeginHorizontal();
			IvaCollisionTracker.PrintingEnabled = GUILayout.Toggle(IvaCollisionTracker.PrintingEnabled, "Print collisions");
			//KerbalIva.KerbalFeetCollider.enabled = !GUILayout.Toggle(!KerbalIva.KerbalColliderCollider.enabled, "Feet") && KerbalIva.KerbalColliderCollider.enabled;
#if Experimental
			KerbalIvaController.CanHoldItems = GUILayout.Toggle(KerbalIvaController.CanHoldItems, "Can move objects");
#endif
			bool helmet = GUILayout.Toggle(KerbalIvaAddon.Instance.KerbalIva.WearingHelmet, "Helmet");
			//if (helmet != KerbalIva.WearingHelmet)
			//{
			if (helmet)
				KerbalIvaAddon.Instance.KerbalIva.HelmetOn();
			else
				KerbalIvaAddon.Instance.KerbalIva.HelmetOff();
			//}
			GUILayout.EndHorizontal();


			GuiUtils.label("ladder count", KerbalIvaAddon.Instance.KerbalIva.KerbalCollisionTracker.RailColliderCount);
			Settings.KerbalHeight = GuiUtils.editFloat("Kerbal height", Settings.KerbalHeight);
			Settings.KerbalHeightWithHelmet = GuiUtils.editFloat("Kerbal height with helmet", Settings.KerbalHeightWithHelmet);
		}

		private static bool showColliderGui = false;
		private static int _selectedGuiRadioButton = 0;
		//private static GameObject currentCollider = null;
		//private static FixedJoint currentJoint = null;
		private static void ColliderGui(Collider[] colliders)
		{
			if (colliders.Length == 0)
			{
				GUILayout.Label("No Colliders");
				return;
			}
			GuiUtils.label("Colliders", colliders.Length);
			if (_colliderIndex >= colliders.Length)
				_colliderIndex = 0;
			if (_colliderIndex < 0)
				_colliderIndex = colliders.Length - 1;
			GuiUtils.label("Current Collider", _colliderIndex + 1);
			Collider collider = colliders[_colliderIndex];

			GUILayout.BeginHorizontal();
			if (GUILayout.Button("Previous Collider"))
				_colliderIndex--;
			if (GUILayout.Button("Next Collider"))
				_colliderIndex++;
			GUILayout.EndHorizontal();

			GUILayout.Label("<b>Name</b>");
			GUILayout.Label(collider == null ? "null" : collider.ToString());
			if (collider.transform != null)
			{
				GuiUtils.label("Position", collider.transform.localPosition);
				GuiUtils.label("Range", Vector3.Distance(collider.transform.position, InternalCamera.Instance.transform.position));
			}
			GuiUtils.label("Layer", collider.gameObject.layer);
			collider.gameObject.layer = GuiUtils.editInt("Layer", collider.gameObject.layer);
		}

		private static bool showHatchGui = false;
		private static void HatchGui()
		{
			if (GUILayout.Button((showHatchGui ? "Hide" : "Show") + " hatch configuration"))
				showHatchGui = !showHatchGui;
			if (!showHatchGui || FreeIva.CurrentInternalModuleFreeIva == null) return;
			if (FreeIva.CurrentInternalModuleFreeIva.Hatches.Count == 0)
			{
				GUILayout.Label("No hatches");
				return;
			}

			/*if (partChanged)
            {
                partChanged = false;
                foreach (var hat in mfi.Hatches)
                    hat.Init(CurrentPart);
            }*/

			GUILayout.BeginHorizontal();
			if (GUILayout.Button("Previous Hatch"))
				_hatchIndex--;
			if (GUILayout.Button("Next Hatch"))
				_hatchIndex++;
			GUILayout.EndHorizontal();

			if (_hatchIndex >= FreeIva.CurrentInternalModuleFreeIva.Hatches.Count)
				_hatchIndex = 0;
			if (_hatchIndex < 0)
				_hatchIndex = FreeIva.CurrentInternalModuleFreeIva.Hatches.Count - 1;
			GuiUtils.label("Current Hatch", _hatchIndex + 1);
			FreeIvaHatch h = FreeIva.CurrentInternalModuleFreeIva.Hatches[_hatchIndex];
			GuiUtils.label("Hatch (" + (_hatchIndex + 1) + "/" + FreeIva.CurrentInternalModuleFreeIva.Hatches.Count + ")", h);

			GUILayout.Label("<b>Hatch</b>");
			GUILayout.BeginVertical();
			bool openHatch = h.IsOpen;
			openHatch = GUILayout.Toggle(openHatch, "Open");
			if (h.IsOpen != openHatch)
				h.SetDesiredOpen(openHatch);
			// PositionIvaObject(h.gameObject);
			GUILayout.EndVertical();

			float distance = Vector3.Distance(h.transform.position, InternalCamera.Instance.transform.position);
			GUILayout.Label(distance.ToString());

			if (GUILayout.Button("Select current hatch"))
				FreeIva.SelectedObject = h.gameObject;
		}

#if Experimental
		private static bool showPressureGui = false;
		private static void PressureGui()
		{
			if (GUILayout.Button((showPressureGui ? "Hide" : "Show") + " pressure controls"))
				showPressureGui = !showPressureGui;
			if (!showPressureGui) return;

			ModuleInternalPressure internalPressure = FreeIva.CurrentPart.GetModule<ModuleInternalPressure>();
			if (internalPressure == null)
			{
				GUILayout.Label("ModuleInternalPressure not found");
				return;
			}
			GuiUtils.label("Is pressurisable", internalPressure.isPressurisable);
			GuiUtils.label("Internal volume (m³)", internalPressure.internalVolume);
			GuiUtils.label("Internal pressure (kPa)", internalPressure.pressure);
			GuiUtils.label("External pressure (kPa)", FlightGlobals.ActiveVessel.atmDensity * ModuleInternalPressure.OneAtm);
			GuiUtils.label("Internal temperature (K)", internalPressure.temperature);
			GuiUtils.label("External temperature (K)", FlightGlobals.ActiveVessel.externalTemperature);

			GUILayout.BeginHorizontal();
			if (GUILayout.Button("Pressurise"))
				internalPressure.Pressurise();
			if (GUILayout.Button("Depressurise"))
				internalPressure.Depressurise();
			if (GUILayout.Button("Pressurise to external"))
				internalPressure.ChangePressure(FlightGlobals.ActiveVessel.atmDensity * ModuleInternalPressure.OneAtm);
			GUILayout.EndHorizontal();
		}
#endif

		private static bool showSeatGui = false;
		private static void SeatGui()
		{
			if (GUILayout.Button((showSeatGui ? "Hide" : "Show") + " seat controls"))
				showSeatGui = !showSeatGui;

			if (showSeatGui)
			{
				if (FreeIva.CurrentPart.internalModel == null)
				{
					GUILayout.Label("No internal model found.");
					return;
				}

				GUILayout.Label("<b>Seats</b>");
				GUILayout.BeginHorizontal();
				GUILayout.Label("<b>Name</b>");
				GUILayout.Label("<b>Distance</b>");
				GUILayout.Label("<b>Angle</b>");
				GUILayout.Label("<b>Targeted</b>");
				GUILayout.EndHorizontal();
				Transform closestSeat = null;
				float closestDistance = Settings.MaxInteractDistance;

				foreach (var s in FreeIva.CurrentPart.internalModel.seats)
				{
					if (s.taken)
						continue;

					GUILayout.BeginHorizontal();
					GUILayout.Label(s.seatTransform.ToString());
					float distance = Vector3.Distance(s.seatTransform.position, InternalCamera.Instance.transform.position);
					GUILayout.Label(distance.ToString());

					//GuiUtils.label(imr.name, Vector3.Distance(imr.transform.position, InternalCamera.Instance.transform.position));
					float angle = Vector3.Angle(s.seatTransform.position - InternalCamera.Instance.transform.position,
						InternalCamera.Instance.transform.forward);
					//GuiUtils.label("Angle", angle);
					GUILayout.Label(angle.ToString());
					bool targeted = false;
					float radius = 0f;
					if (angle < 90)
					{
						radius = (distance * Mathf.Sin(angle * Mathf.Deg2Rad)) / Mathf.Sin((90 - angle) * Mathf.Deg2Rad);
						targeted = radius <= Settings.ObjectInteractionRadius;
					}
					GUILayout.Label(targeted.ToString());
					GUILayout.EndHorizontal();

					if (targeted && distance < closestDistance)
					{
						closestSeat = s.seatTransform;
						closestDistance = distance;
					}
				}
			}
		}

		private static bool showPropGui = false;
		private static Rigidbody _propRigidbody = null;
		private static void PropGui()
		{
			if (GUILayout.Button((showPropGui ? "Hide" : "Show") + " Prop controls"))
				showPropGui = !showPropGui;
			if (!showPropGui) return;

			if (FreeIva.CurrentPart.internalModel == null)
			{
				GUILayout.Label("No internal model found.");
				return;
			}

			var props = FreeIva.CurrentPart.internalModel.props;

			if (props.Count == 0)
			{
				GUILayout.Label("No Props");
				return;
			}
			GuiUtils.label("Props", props.Count);
			if (_propIndex >= props.Count)
				_propIndex = 0;
			if (_propIndex < 0)
				_propIndex = props.Count - 1;
			GuiUtils.label("Current Props", _propIndex + 1);
			InternalProp prop = props[_propIndex];

			GUILayout.BeginHorizontal();
			if (GUILayout.Button("Previous Prop"))
				_propIndex--;
			if (GUILayout.Button("Next Prop"))
				_propIndex++;
			GUILayout.EndHorizontal();
			GUILayout.BeginHorizontal();
			GUILayout.Label(prop.name);
			MeshRenderer[] mrs = prop.GetComponentsInChildren<MeshRenderer>();
			if (mrs == null || mrs.Length == 0)
			{
				GUILayout.Label("No MeshRenderer");
				GUILayout.EndHorizontal();
			}
			else
			{
				bool propVisible = GUILayout.Toggle(mrs[0].enabled, "Visible");
				foreach (MeshRenderer mr in mrs)
					mr.enabled = propVisible;
				GUILayout.EndHorizontal();

				if (mrs[0].transform != null)
				{
					GUILayout.BeginHorizontal();
					GuiUtils.label("Transform position", mrs[0].transform.localPosition);
					GuiUtils.label("Range", Vector3.Distance(mrs[0].transform.position, InternalCamera.Instance.transform.position));
					GUILayout.EndHorizontal();
				}
			}

			if (prop.name.Equals("Hatch_Plane"))
			{
				if (GUILayout.Button("Spawn open hatch"))
				{
					InternalProp openHatch = PartLoader.GetInternalProp("Hatch_Plane_Frame");
					openHatch.propID = props.Count;
					openHatch.internalModel = prop.internalModel;
					//openHatch.get_transform().set_parent(base.get_transform()); TODO: Set parent
					openHatch.hasModel = true;
					props.Add(openHatch);
					openHatch.transform.rotation = prop.transform.rotation;
					openHatch.transform.position = prop.transform.position;
				}
			}

			// Prop movement.
			GUILayout.BeginHorizontal();
			float xPos = GuiUtils.editFloat("Position X", prop.transform.localPosition.x);
			float yPos = GuiUtils.editFloat("Position Y", prop.transform.localPosition.y);
			float zPos = GuiUtils.editFloat("Position Z", prop.transform.localPosition.z);
			prop.GetComponentCached<Rigidbody>(ref _propRigidbody);
			if (xPos != prop.transform.localPosition.x || yPos != prop.transform.localPosition.y || zPos != prop.transform.localPosition.z)
			{
				prop.transform.localPosition = new Vector3(xPos, yPos, zPos);
				if (_propRigidbody != null)
				{
					_propRigidbody.velocity = Vector3.zero;
					_propRigidbody.angularVelocity = Vector3.zero;
				}
			}
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			float xSc = GuiUtils.editFloat("Scale X", prop.transform.localScale.x);
			float ySc = GuiUtils.editFloat("Scale Y", prop.transform.localScale.x);
			float zSc = GuiUtils.editFloat("Scale Z", prop.transform.localScale.x);
			if (xSc != prop.transform.localScale.x || ySc != prop.transform.localScale.y || zSc != prop.transform.localScale.z)
			{
				//currentJoint = c.IvaGameObject.GetComponent<FixedJoint>();
				//if (currentJoint != null) Destroy(currentJoint);
				prop.transform.localScale = new Vector3(xSc, ySc, zSc);
				//currentJoint = c.IvaGameObject.AddComponent<FixedJoint>();
				//currentJoint.connectedBody = CurrentPart.collider.rigidbody;
				if (_propRigidbody != null)
				{
					_propRigidbody.velocity = Vector3.zero;
					_propRigidbody.angularVelocity = Vector3.zero;
				}
			}
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			float xRot = GuiUtils.editFloat("Rotation X", prop.transform.rotation.eulerAngles.x);
			float yRot = GuiUtils.editFloat("Rotation Y", prop.transform.rotation.eulerAngles.y);
			float zRot = GuiUtils.editFloat("Rotation Z", prop.transform.rotation.eulerAngles.z);
			GUILayout.EndHorizontal();

			if (xRot != prop.transform.rotation.eulerAngles.x || yRot != prop.transform.rotation.eulerAngles.y || zRot != prop.transform.rotation.eulerAngles.z)
			{
				prop.transform.rotation = Quaternion.Euler(xRot, yRot, zRot);
			}

			GUILayout.BeginHorizontal();
			float rotw = GuiUtils.editFloat("Rot W", prop.transform.localRotation.w);
			float rotx = GuiUtils.editFloat("Rot X", prop.transform.localRotation.x);
			float roty = GuiUtils.editFloat("Rot Y", prop.transform.localRotation.y);
			float rotz = GuiUtils.editFloat("Rot Z", prop.transform.localRotation.z);
			GUILayout.EndHorizontal();
			if (rotx != prop.transform.localRotation.x || roty != prop.transform.localRotation.y || rotz != prop.transform.localRotation.z || rotw != prop.transform.localRotation.w)
			{
				prop.transform.localRotation = new Quaternion(rotx, roty, rotz, rotw);
			}

			float xVal = (float)Math.Round((Decimal)prop.transform.localRotation.x, 5, MidpointRounding.AwayFromZero);
			float yVal = (float)Math.Round((Decimal)prop.transform.localRotation.y, 5, MidpointRounding.AwayFromZero);
			float zVal = (float)Math.Round((Decimal)prop.transform.localRotation.z, 5, MidpointRounding.AwayFromZero);
			float wVal = (float)Math.Round((Decimal)prop.transform.localRotation.w, 5, MidpointRounding.AwayFromZero);
			GUILayout.Label("Quaternion XYZW (INTERNAL cfg): " + xVal + ", " + yVal + ", " + zVal + ", " + wVal);

			if (GUILayout.Button("Select current prop"))
				FreeIva.SelectedObject = prop.gameObject;
		}

		private static bool showMRGui = false;
		private static string mrFilter = string.Empty;
		private static void MeshRendererGui()
		{
			if (GUILayout.Button((showMRGui ? "Hide" : "Show") + " MeshRenderer controls"))
				showMRGui = !showMRGui;

			if (showMRGui)
			{
				MeshRenderer[] meshRenderers = null;
				mrFilter = GuiUtils.editText("Filter", mrFilter);
				if (FreeIva.CurrentPart.internalModel != null)
				{
					meshRenderers = FreeIva.CurrentPart.internalModel.GetComponentsInChildren<MeshRenderer>();
				}
				else if (FlightGlobals.ActiveVessel.rootPart != null)
				{
					meshRenderers = FlightGlobals.ActiveVessel.rootPart.GetComponentsInChildren<MeshRenderer>();
				}
				if (meshRenderers == null || meshRenderers.Length == 0)
				{
					GUILayout.Label("No MeshRenderers");
					return;
				}
				if (mrFilter.Length > 0)
				{
					var filteredMeshRenderers = new List<MeshRenderer>();
					foreach (var m in meshRenderers)
					{
						if (m.name.IndexOf(mrFilter, StringComparison.OrdinalIgnoreCase) >= 0)
							filteredMeshRenderers.Add(m);
					}
					meshRenderers = filteredMeshRenderers.ToArray();
					if (meshRenderers.Length == 0)
					{
						GUILayout.Label($"No MeshRenderers matching filter \"{mrFilter}\".");
						return;
					}
				}
				GuiUtils.label("MeshRenderers", meshRenderers.Length);
				if (_meshRendererIndex >= meshRenderers.Length)
					_meshRendererIndex = 0;
				if (_meshRendererIndex < 0)
					_meshRendererIndex = meshRenderers.Length - 1;
				GuiUtils.label("Current MeshRenderer", _meshRendererIndex + 1);
				MeshRenderer mr = meshRenderers[_meshRendererIndex];

				GUILayout.BeginHorizontal();
				if (GUILayout.Button("Previous MeshRenderer"))
					_meshRendererIndex--;
				if (GUILayout.Button("Next MeshRenderer"))
					_meshRendererIndex++;
				GUILayout.EndHorizontal();
				if (mr.material != null)
					mr.enabled = GUILayout.Toggle(mr.enabled, "Enabled");
				else
					GUILayout.Label("Material is null");
				/*if (GUILayout.Button("Hide all MRs but this"))
                {
                    for (int i = 0; i < meshRenderers.Length; i++)
                    {
                        meshRenderers[meshRendererIndex].enabled = i != meshRendererIndex;
                        Debug.Log("MR " + meshRenderers[meshRendererIndex] + " is now " + meshRenderers[meshRendererIndex].enabled);
                    }
                }*/
				GUILayout.Label("<b>MeshRenderer</b>");
				GUILayout.Label(mr == null ? "null" : mr.ToString());
				GUILayout.Label("<b>Material</b>");
				GUILayout.Label(mr.material == null ? "null" : mr.material.ToString());
				if (mr.material != null)
				{
					GUILayout.Label("<b>Render Queue</b>");
					GUILayout.Label(mr.material.renderQueue.ToString());
					mr.material.renderQueue = GUIUtil.EditableIntField("Edit Material Render Queue", mr.material.renderQueue, null);
					GUILayout.Label("<b>Shader</b>");
					GUILayout.Label(mr.material.shader.name + ", " + mr.material.shader.renderQueue);
					mr.material.renderQueue = GUIUtil.EditableIntField("Edit Shader Render Queue", mr.material.shader.renderQueue, null);
				}
				GUILayout.Label("<b>Transform</b>");
				GUILayout.Label(mr.transform == null ? "null" : mr.transform.ToString());
				//GuiUtils.label("MeshRenderer", mr);
				//GuiUtils.label("Material", mr.material);
				//GuiUtils.label("Transform", mr.transform);
				if (mr.transform != null)
				{
					GuiUtils.label("Transform position", mr.transform.localPosition);
					GuiUtils.label("Range", Vector3.Distance(mr.transform.position, InternalCamera.Instance.transform.position));

					GUILayout.Label("Transform Parent");
					GUILayout.Label(mr.transform.parent == null ? "null" : mr.transform.parent.ToString());
					if (mr.transform.parent != null)
					{
						GUILayout.Label("Transform Parent Parent");
						GUILayout.Label(mr.transform.parent.parent == null ? "null" : mr.transform.parent.parent.ToString());
					}
				}

				GUILayout.Label("Part Internal Model Transform");
				if (FreeIva.CurrentPart != null && FreeIva.CurrentPart.internalModel != null)
				{
					GUILayout.Label(FreeIva.CurrentPart.internalModel.transform.ToString());
				}
				else
				{
					GUILayout.Label("null");
				}

				if (GUILayout.Button("Select current MeshRenderer"))
					FreeIva.SelectedObject = mr.gameObject;

				FreeIva.DepthMaskQueue = GUIUtil.EditableIntField("Render Queue for open parts", FreeIva.DepthMaskQueue, null);
				mr.gameObject.layer = GUIUtil.EditableIntField("Layer", mr.gameObject.layer, null);
			}
		}

		private static bool showSMRGui = false;
		private static void SkinnedMeshRendererGui()
		{
			if (GUILayout.Button((showSMRGui ? "Hide" : "Show") + " SkinnedMeshRenderer controls"))
				showSMRGui = !showSMRGui;

			if (showSMRGui)
			{

				SkinnedMeshRenderer[] skinnedMeshRenderers = null;
				if (FreeIva.CurrentPart.internalModel != null)
				{
					skinnedMeshRenderers = FreeIva.CurrentPart.internalModel.GetComponentsInChildren<SkinnedMeshRenderer>();
				}
				else if (FlightGlobals.ActiveVessel.rootPart != null)
				{
					skinnedMeshRenderers = FlightGlobals.ActiveVessel.rootPart.GetComponentsInChildren<SkinnedMeshRenderer>();
				}

				if (skinnedMeshRenderers == null || skinnedMeshRenderers.Length == 0)
				{
					GUILayout.Label("No SkinnedMeshRenderers");
					return;
				}
				GuiUtils.label("SkinnedMeshRenderers", skinnedMeshRenderers.Length);
				if (_skinnedMeshRendererIndex >= skinnedMeshRenderers.Length)
					_skinnedMeshRendererIndex = 0;
				if (_skinnedMeshRendererIndex < 0)
					_skinnedMeshRendererIndex = skinnedMeshRenderers.Length - 1;
				GuiUtils.label("Current SkinnedMeshRenderer", _skinnedMeshRendererIndex + 1);
				SkinnedMeshRenderer smr = skinnedMeshRenderers[_skinnedMeshRendererIndex];

				GUILayout.BeginHorizontal();
				if (GUILayout.Button("Previous SkinnedMeshRenderer"))
					_skinnedMeshRendererIndex--;
				if (GUILayout.Button("Next SkinnedMeshRenderer"))
					_skinnedMeshRendererIndex++;
				GUILayout.EndHorizontal();

				if (smr.material != null)
					smr.enabled = GUILayout.Toggle(smr.enabled, "Enabled");
				else
					GUILayout.Label("Material is null");
				/*if (GUILayout.Button("Hide all SMRs but this"))
                {
                    for (int i = 0; i < skinnedMeshRenderers.Length; i++)
                    {
                        skinnedMeshRenderers[skinnedMeshRendererIndex].enabled = i != skinnedMeshRendererIndex;
                    }
                }*/
				GUILayout.Label("<b>SkinnedMeshRenderer</b>");
				GUILayout.Label(smr == null ? "null" : smr.ToString());
				GUILayout.Label("<b>Material</b>");
				GUILayout.Label(smr.material == null ? "null" : smr.material.ToString());
				if (smr.material != null)
				{
					GUILayout.Label("<b>Render Queue</b>");
					GUILayout.Label(smr.material.renderQueue.ToString());
					smr.material.renderQueue = GUIUtil.EditableIntField("Edit Material Render Queue", smr.material.renderQueue, null);
					GUILayout.Label("<b>Shader</b>");
					GUILayout.Label(smr.material.shader.name + ", " + smr.material.shader.renderQueue);
					smr.material.renderQueue = GUIUtil.EditableIntField("Edit Shader Render Queue", smr.material.shader.renderQueue, null);
				}

				GUILayout.Label("<b>Transform</b>");
				GUILayout.Label(smr.transform == null ? "null" : smr.transform.ToString());
				if (smr.transform != null)
				{
					GuiUtils.label("Transform position", smr.transform.position);
					GuiUtils.label("Range", Vector3.Distance(smr.transform.position, InternalCamera.Instance.transform.position));
				}
				GUILayout.Label("Transform Parent");
				GUILayout.Label(smr.transform.parent == null ? "null" : smr.transform.parent.ToString());
				if (smr.transform.parent != null)
				{
					GUILayout.Label("Transform Parent Parent");
					GUILayout.Label(smr.transform.parent.parent == null ? "null" : smr.transform.parent.parent.ToString());
				}

				if (GUILayout.Button("Select current SkinnedMeshRenderer"))
					FreeIva.SelectedObject = smr.gameObject;
			}
		}

		static bool showTransformGui = false;
		public static void TransformGui()
		{
			if (GUILayout.Button((showTransformGui ? "Hide" : "Show") + " Transform controls"))
				showTransformGui = !showTransformGui;

			if (showTransformGui)
			{
				if (FreeIva.CurrentPart.internalModel == null)
				{
					GUILayout.Label("No internal model found.");
					return;
				}

				Transform[] transforms = null;
				if (FreeIva.CurrentPart.internalModel != null)
				{
					transforms = FreeIva.CurrentPart.internalModel.GetComponentsInChildren<Transform>();
				}
				else if (FlightGlobals.ActiveVessel?.rootPart != null)
				{
					transforms = FlightGlobals.ActiveVessel.rootPart.GetComponentsInChildren<Transform>();
				}
				if (transforms.Length == 0)
				{
					GUILayout.Label("No Transforms");
					return;
				}
				GuiUtils.label("Transforms", transforms.Length);
				if (_transformIndex >= transforms.Length)
					_transformIndex = 0;
				if (_transformIndex < 0)
					_transformIndex = transforms.Length - 1;
				GuiUtils.label("Current Transform", _transformIndex + 1);
				Transform transform = transforms[_transformIndex];

				GUILayout.BeginHorizontal();
				if (GUILayout.Button("Previous Transform"))
					_transformIndex--;
				if (GUILayout.Button("Next Transform"))
					_transformIndex++;
				GUILayout.EndHorizontal();

				GUILayout.Label("<b>Name</b>");
				GUILayout.Label(transform == null ? "null" : transform.ToString());
				GuiUtils.label("Position", transform.localPosition);
				GuiUtils.label("Range", Vector3.Distance(transform.position, InternalCamera.Instance.transform.position));
				GuiUtils.label("Layer", transform.gameObject.layer);
				transform.gameObject.layer = GuiUtils.editInt("Layer", transform.gameObject.layer);

				if (GUILayout.Button("Select current Transform"))
					FreeIva.SelectedObject = transform.gameObject;
			}
		}

		private static bool showLookGui = false;
		public static void LookGui()
		{
			if (GUILayout.Button((showLookGui ? "Hide" : "Show") + " View controls"))
				showLookGui = !showLookGui;

			if (showLookGui)
			{

				Vector3 rotation = InternalCamera.Instance.transform.rotation.eulerAngles;
				GuiUtils.label("Camera absolute", rotation);

				Quaternion FoR = FlightGlobals.GetFoR(FoRModes.SRF_NORTH);
				Vector3 relativeAngles = (Quaternion.Inverse(FoR) * InternalCamera.Instance.transform.rotation).eulerAngles;

				GuiUtils.label("camera relative", relativeAngles);

				Vector3 cameraForwardPlanet = InternalSpace.InternalToWorld(InternalCamera.Instance.transform.rotation) * Vector3.forward;
				GuiUtils.label("Camera forward planet", cameraForwardPlanet);

				Vector3 cameraForwardSurface = Quaternion.Inverse(FlightGlobals.GetFoR(FoRModes.SRF_NORTH)) * cameraForwardPlanet;
				GuiUtils.label("camera forward surface", cameraForwardSurface);


				var flightForces = KerbalIvaAddon.GetFlightAccelerationInternalSpace();
				GuiUtils.label("Absolute X", flightForces.x);
				GuiUtils.label("Absolute Y", flightForces.y);
				GuiUtils.label("Absolute Z", flightForces.z);
			}
		}

		private static bool showPhysicsGui = false;
		public static void PhysicsGui()
		{
			if (GUILayout.Button((showPhysicsGui ? "Hide" : "Show") + " Physics controls"))
				showPhysicsGui = !showPhysicsGui;

			if (showPhysicsGui)
			{
				float staticFriction = KerbalIvaAddon.Instance.KerbalIva.KerbalCollider.material.staticFriction;
				GuiUtils.slider("Static friction", ref staticFriction, 0, 2);
				KerbalIvaAddon.Instance.KerbalIva.KerbalCollider.material.staticFriction = staticFriction;

				float dynamicFriction = KerbalIvaAddon.Instance.KerbalIva.KerbalCollider.material.dynamicFriction;
				GuiUtils.slider("Dynamic friction", ref dynamicFriction, 0, 2);
				KerbalIvaAddon.Instance.KerbalIva.KerbalCollider.material.dynamicFriction = dynamicFriction;

				float bounciness = KerbalIvaAddon.Instance.KerbalIva.KerbalCollider.material.bounciness;
				GuiUtils.slider("Bounciness", ref bounciness, 0, 1);
				KerbalIvaAddon.Instance.KerbalIva.KerbalCollider.material.bounciness = bounciness;

				// Kerbal in EVA suit: 0.09375 (93.75 kg)
				float mass = KerbalIvaAddon.Instance.KerbalIva.KerbalRigidbody.mass;
				GuiUtils.slider("Mass", ref mass, 0, 5);
				KerbalIvaAddon.Instance.KerbalIva.KerbalRigidbody.mass = mass;

				GuiUtils.slider("ForwardSpeed", ref Settings.ForwardSpeed, 0, 50);
				GuiUtils.slider("JumpForce", ref Settings.JumpForce, 0, 20);

				GuiUtils.label("Velocity", KerbalIvaAddon.Instance.KerbalIva.KerbalRigidbody.velocity);
				if (FreeIva.CurrentPart != null && FreeIva.CurrentPart.Rigidbody != null)
				{
					// this is all sorts of wrong - need to transform between internal/world space
					GuiUtils.label("Velocity relative to part", FreeIva.CurrentPart.Rigidbody.velocity - KerbalIvaAddon.Instance.KerbalIva.KerbalRigidbody.velocity);
				}

				GuiUtils.label("Flight forces (world space)", KerbalIvaAddon.flightForces);
				GuiUtils.label("Flight forces (IVA space)", InternalSpace.WorldToInternal(KerbalIvaAddon.flightForces));
				GuiUtils.label("IVA Rot", InternalSpace.WorldToInternal(Quaternion.identity).eulerAngles);

				// Debug
				Vector3 gForce = FlightGlobals.getGeeForceAtPosition(KerbalIvaAddon.Instance.KerbalIva.transform.position);
				Vector3 centrifugalForce = FlightGlobals.getCentrifugalAcc(KerbalIvaAddon.Instance.KerbalIva.transform.position, FreeIva.CurrentPart.orbit.referenceBody);
				Vector3 coriolisForce = FlightGlobals.getCoriolisAcc(FreeIva.CurrentPart.vessel.rb_velocity + Krakensbane.GetFrameVelocityV3f(), FreeIva.CurrentPart.orbit.referenceBody);

				GuiUtils.label("Main G-Force", gForce);
				GuiUtils.label("Centrifugal force", centrifugalForce);
				GuiUtils.label("Coriolis force", coriolisForce);

				GuiUtils.label("Main G Internal", InternalSpace.InternalToWorld(gForce));
			}
		}

		private static bool showAnimationGui = false;
		public static void AnimationGui()
		{
			if (GUILayout.Button((showAnimationGui ? "Hide" : "Show") + " animation controls"))
				showAnimationGui = !showAnimationGui;

			if (showAnimationGui)
			{
				Animation[] partAnimators = FreeIva.CurrentPart.FindModelAnimators();
				Animation[] ivaAnimators = null;
				if (FreeIva.CurrentPart.internalModel != null)
				{
					ivaAnimators = FreeIva.CurrentPart.internalModel.FindModelAnimators();
				}

				if (partAnimators != null)
				{
					GUILayout.Label("Found " + partAnimators.Length + " part animator(s).");
					AnimationButtons(partAnimators);
				}

				if (ivaAnimators != null)
				{
					GUILayout.Label("Found " + ivaAnimators.Length + " IVA animator(s).");
					AnimationButtons(ivaAnimators);
				}
			}
		}

		private static void AnimationButtons(Animation[] animations)
		{

			foreach (Animation animation in animations)
			{
				if (animation.gameObject != null)
				{
					if (animation.gameObject.transform != null)
						GUILayout.Label("Parent: " + animation.gameObject.transform.parent);
					else
						GUILayout.Label("No transform");
				}
				else
					GUILayout.Label("No game object");

				foreach (AnimationState state in animation)
				{
					GUILayout.BeginHorizontal();
					if (GUILayout.Button(state.name) && !animation.isPlaying)
					{
						state.speed = -state.speed;
						if (state.speed > 0)
							state.normalizedTime = 0f;
						else
							state.normalizedTime = 1f;
						animation.enabled = true;
						animation.wrapMode = WrapMode.Once;
						animation.clip = state.clip;
						animation.Play();
					}
					if (state != null)
					{
						GUILayout.Label("time: " + state.time);
						GUILayout.Label("normalizedTime: " + state.normalizedTime);
						GUILayout.Label("speed: " + state.speed);
						GUILayout.Label("normalizedSpeed: " + state.normalizedSpeed);
					}
					GUILayout.EndHorizontal();
				}
			}
		}

		public static void PositionIvaObject(GameObject o)
		{
			GUILayout.BeginHorizontal();

			var rigidbody = o.GetComponent<Rigidbody>();

			Vector3 newLocalPosition = new Vector3(
				GuiUtils.editFloat("Position X", o.transform.localPosition.x),
				GuiUtils.editFloat("Position Y", o.transform.localPosition.y),
				GuiUtils.editFloat("Position Z", o.transform.localPosition.z));
			if (newLocalPosition != o.transform.localPosition)
			{
				o.transform.localPosition = newLocalPosition;
				if (rigidbody != null)
				{
					rigidbody.velocity = Vector3.zero;
					rigidbody.angularVelocity = Vector3.zero;
				}
			}
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			Vector3 newLocalScale = new Vector3(
				GuiUtils.editFloat("Scale X", o.transform.localScale.x),
				GuiUtils.editFloat("Scale Y", o.transform.localScale.y),
				GuiUtils.editFloat("Scale Z", o.transform.localScale.z));
			if (newLocalScale != o.transform.localScale)
			{
				o.transform.localScale = newLocalScale;
				if (rigidbody != null)
				{
					rigidbody.velocity = Vector3.zero;
					rigidbody.angularVelocity = Vector3.zero;
				}
			}
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			Vector3 localRotation = o.transform.localRotation.eulerAngles;
			Vector3 newLocalRotation = new Vector3(
				GuiUtils.editFloat("Rotation X", localRotation.x),
				GuiUtils.editFloat("Rotation Y", localRotation.y),
				GuiUtils.editFloat("Rotation Z", localRotation.z));
			if (newLocalRotation != localRotation)
			{
				o.transform.localRotation = Quaternion.Euler(newLocalRotation);
				if (rigidbody != null)
				{
					rigidbody.velocity = Vector3.zero;
					rigidbody.angularVelocity = Vector3.zero;
				}
			}
			GUILayout.EndHorizontal();
		}
	}
}

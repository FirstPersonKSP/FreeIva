﻿using System;
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
            GuiUtils.label("Active kerbal", KerbalIvaController.ActiveKerbal == null ? "null" : KerbalIvaController.ActiveKerbal.name);
            GuiUtils.label("Part (" + (_partIndex + 1) + "/" + FlightGlobals.ActiveVessel.Parts.Count + ")", FreeIva.CurrentPart);

            // Internals
            if (FreeIva.CurrentPart.internalModel == null)
            {
                GUILayout.Label("No internal model found");
                //return;
            }

            GUILayout.BeginHorizontal();
            KerbalIvaController.KerbalIva.GetComponentCached<SphereCollider>(ref KerbalIvaController.KerbalCollider);
            KerbalIvaController.KerbalCollider.enabled = !GUILayout.Toggle(!KerbalIvaController.KerbalCollider.enabled, "Disable collisions");
            KerbalIvaController.Gravity = GUILayout.Toggle(KerbalIvaController.Gravity, "Gravity");
            GUILayout.EndHorizontal();

            _advancedMode = GUILayout.Toggle(_advancedMode, "Advanced mode");

            if (!_advancedMode)
                return;

            GuiUtils.label("Selected object", FreeIva.SelectedObject);

#if Experimental
            if (ColliderManipulator.MovingObject) return;
#endif

            KerbalColliderGui();

            if (FreeIva.CurrentModuleFreeIva == null)
                GUILayout.Label("Free IVA module not found in this part. ModuleManager_FreeIvaParts.cfg update required.");

            ColliderGui();
            HatchGui();
#if Experimental
            PressureGui();
#endif
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

        private static bool showKerbalColliderGui = false;
        private static void KerbalColliderGui()
        {
            if (GUILayout.Button((showKerbalColliderGui ? "Hide" : "Show") + " kerbal collider configuration"))
                showKerbalColliderGui = !showKerbalColliderGui;

            if (!showKerbalColliderGui)
                return;


            var distance = InternalCamera.Instance.transform.position - KerbalIvaController.KerbalRigidbody.transform.position;
            GuiUtils.label("Distance from camera to Kerbal IVA", distance);

            GUILayout.BeginHorizontal();
            KerbalIvaController.KerbalIva.GetComponentCached<SphereCollider>(ref KerbalIvaController.KerbalCollider);
            KerbalIvaController.KerbalCollider.enabled = !GUILayout.Toggle(!KerbalIvaController.KerbalCollider.enabled, "Disable collisions");
            KerbalIvaController.Gravity = GUILayout.Toggle(KerbalIvaController.Gravity, "Gravity");
            IvaCollisionPrinter.Enabled = GUILayout.Toggle(IvaCollisionPrinter.Enabled, "Print collisions");
            //KerbalIva.KerbalFeetCollider.enabled = !GUILayout.Toggle(!KerbalIva.KerbalColliderCollider.enabled, "Feet") && KerbalIva.KerbalColliderCollider.enabled;
#if Experimental
            KerbalIvaController.CanHoldItems = GUILayout.Toggle(KerbalIvaController.CanHoldItems, "Can move objects");
#endif
            bool helmet = GUILayout.Toggle(KerbalIvaController.WearingHelmet, "Helmet");
            //if (helmet != KerbalIva.WearingHelmet)
            //{
            if (helmet)
                KerbalIvaController.HelmetOn();
            else
                KerbalIvaController.HelmetOff();
            //}
            GUILayout.EndHorizontal();

            Settings.KerbalHeight = GuiUtils.editFloat("Kerbal height", Settings.KerbalHeight);
            Settings.KerbalHeightWithHelmet = GuiUtils.editFloat("Kerbal height with helmet", Settings.KerbalHeightWithHelmet);
        }

        private static bool showColliderGui = false;
        private static int _internalColliderIndex = 0;
        private static int _selectedGuiRadioButton = 0;
        //private static GameObject currentCollider = null;
        //private static FixedJoint currentJoint = null;
        private static InternalCollider.Type _primitiveType = InternalCollider.Type.Cube;
        private static void ColliderGui()
        {
            // TODO: Automatically add colliders to selected props using renderer bounds?
            bool changed = false;
            if (GUILayout.Button((showColliderGui ? "Hide" : "Show") + " collider configuration"))
            {
                showColliderGui = !showColliderGui;
                changed = true;
            }
            if (changed)
            {
                if (FreeIva.CurrentModuleFreeIva == null)
                {
                    InternalCollider.HideAllColliders();
                }
            }
            if (!showColliderGui)
            {
                return;
            }

            GUILayout.BeginHorizontal();
#if DEBUG
            if (GUILayout.Button("Print collision layers"))
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("3 3 2 2 2 2 2 2 2 2 2 2 1 1 1 1 1 1 1 1 1 1                    ");
                sb.Append("1 0 9 8 7 6 5 4 3 2 1 0 9 8 7 6 5 4 3 2 1 0 9 8 7 6 5 4 3 2 1 0");
                for (int i = 0; i < 32; i++)
                {
                    sb.Append("\r\n");
                    if (i < 10) sb.Append(" ");
                    sb.Append(i).Append("\t");
                    for (int j = 31 - i; j >= 0; j--)
                        sb.Append(Physics.GetIgnoreLayerCollision(i, j) ? "T " : "F ");
                }
                Debug.Log(sb.ToString());
            }
#endif

            if (GUILayout.Button("Create new collider"))
            {
                InternalCollider col = new InternalCollider();
                col.Scale = new Vector3(0.5f, 0.5f, 0.5f);
                col.ColliderType = _primitiveType;
                col.Instantiate(FreeIva.CurrentPart);
                col.IvaGameObject.name = "Test collider";
                col.Visible = true;
                FreeIva.CurrentModuleFreeIva.InternalColliders.Add(col);
                _internalColliderIndex = FreeIva.CurrentModuleFreeIva.InternalColliders.Count - 1;
            }
#if DEBUG
            if (GUILayout.Button("Stop all movement"))
            {
                foreach (var col in FreeIva.CurrentModuleFreeIva.InternalColliders)
                {
                    col.IvaGameObjectRigidbody.velocity = Vector3.zero;
                    col.IvaGameObjectRigidbody.angularVelocity = Vector3.zero;
                }
            }
            if (GUILayout.Button((ClickWatcher.Debug ? "Hide" : "Show") + " click interaction"))
            {
                ClickWatcher.Debug = !ClickWatcher.Debug;
            }
#endif
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Toggle(_primitiveType == InternalCollider.Type.Sphere, "Sphere"))
                _primitiveType = InternalCollider.Type.Sphere;
            if (GUILayout.Toggle(_primitiveType == InternalCollider.Type.Capsule, "Capsule"))
                _primitiveType = InternalCollider.Type.Capsule;
            if (GUILayout.Toggle(_primitiveType == InternalCollider.Type.Cylinder, "Cylinder"))
                _primitiveType = InternalCollider.Type.Cylinder;
            if (GUILayout.Toggle(_primitiveType == InternalCollider.Type.Cube, "Cube"))
                _primitiveType = InternalCollider.Type.Cube;
            if (GUILayout.Toggle(_primitiveType == InternalCollider.Type.Plane, "Plane"))
                _primitiveType = InternalCollider.Type.Plane;
            if (GUILayout.Toggle(_primitiveType == InternalCollider.Type.Quad, "Quad"))
                _primitiveType = InternalCollider.Type.Quad;
            GUILayout.EndHorizontal();

            InternalCollider c = null;
            if (FreeIva.CurrentModuleFreeIva == null)
            {
                GUILayout.Label("No ModuleFreeIVA");
            }
            else if (FreeIva.CurrentModuleFreeIva.InternalColliders.Count == 0)
            {
                GUILayout.Label("No colliders");
            }
            else
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Previous InternalCollider"))
                    _internalColliderIndex--;
                if (GUILayout.Button("Next InternalCollider"))
                    _internalColliderIndex++;
                GUILayout.EndHorizontal();
                if (_internalColliderIndex >= FreeIva.CurrentModuleFreeIva.InternalColliders.Count)
                    _internalColliderIndex = 0;
                if (_internalColliderIndex < 0)
                    _internalColliderIndex = FreeIva.CurrentModuleFreeIva.InternalColliders.Count - 1;
                GuiUtils.label("Current collider", _internalColliderIndex + 1);
                c = FreeIva.CurrentModuleFreeIva.InternalColliders[_internalColliderIndex];
                GuiUtils.label("Collider (" + (_internalColliderIndex + 1) + "/" + FreeIva.CurrentModuleFreeIva.InternalColliders.Count + ")", c.Name);
                if (InternalCamera.Instance != null && InternalCamera.Instance.transform.position != null && // On vessel destruction while in IVA
                    c.IvaGameObject != null && c.IvaGameObject.transform != null)
                    GuiUtils.label("Range", Vector3.Distance(c.IvaGameObject.transform.position, InternalCamera.Instance.transform.position));
            }
            if (c != null)
            {
                GUILayout.BeginHorizontal();
                if (c.IvaGameObject != null && GUILayout.Button("Destroy collider"))
                {
                    FreeIva.CurrentModuleFreeIva.InternalColliders.Remove(c);
                    c.IvaGameObject.DestroyGameObject();
                }
                if (GUILayout.Button("Copy to clipboard"))
                {
                    GUIUtility.systemCopyBuffer = PrintCollider(c);
                    ScreenMessages.PostScreenMessage("Copied to clipboard.", 1f, ScreenMessageStyle.LOWER_CENTER);
                }
                GUILayout.EndHorizontal();

                if (c.IvaGameObject != null)
                {
                    GUILayout.BeginHorizontal();
                    c.ColliderEnabled = GUILayout.Toggle(c.ColliderEnabled, "Enabled");
                    bool wasVisible = c.Visible;
                    bool v = GUILayout.Toggle(wasVisible, "Visible");
                    if (v != wasVisible)
                        c.Visible = v;
                    if (GUILayout.Button("Show All"))
                    {
                        for (int i = 0; i < FreeIva.CurrentModuleFreeIva.InternalColliders.Count; i++)
                        {
                            FreeIva.CurrentModuleFreeIva.InternalColliders[i].Visible = true;
                        }
                    }
                    if (GUILayout.Button("Hide All"))
                    {
                        for (int i = 0; i < FreeIva.CurrentModuleFreeIva.InternalColliders.Count; i++)
                        {
                            FreeIva.CurrentModuleFreeIva.InternalColliders[i].Visible = false;
                        }
                    }
                    GUILayout.EndHorizontal();
                }

                if (c != null)
                    PositionIvaObject(c);

                if (c.IvaGameObjectRigidbody != null)
                {
                    GUILayout.BeginHorizontal();
                    GuiUtils.label("Velocity", c.IvaGameObjectRigidbody.velocity.magnitude);
                    GuiUtils.label("Angular velocity", c.IvaGameObjectRigidbody.angularVelocity.magnitude);
                    GUILayout.EndHorizontal();
                }

                if (GUILayout.Button("Select current collider"))
                    FreeIva.SelectedObject = c.IvaGameObject;
            }

            _selectedGuiRadioButton = GuiUtils.radioButtons(new string[] { "Part Collider GUI", "Internal Model Collider GUI" }, _selectedGuiRadioButton);

            if (_selectedGuiRadioButton == 0)
            {
                Collider[] colliders = null;
                if (FreeIva.CurrentPart != null)
                {
                    colliders = FreeIva.CurrentPart.GetComponentsInChildren<Collider>();
                }
                else if (FlightGlobals.ActiveVessel.rootPart != null)
                {
                    colliders = FlightGlobals.ActiveVessel.rootPart.GetComponentsInChildren<Collider>();
                }
                ColliderGui(colliders);
            }
            else
            {
                if (FreeIva.CurrentPart.internalModel == null)
                {
                    GUILayout.Label("No internal model");
                }
                else
                {
                    Collider[] imColliders = FreeIva.CurrentPart.internalModel.GetComponentsInChildren<Collider>();
                    ColliderGui(imColliders);
                }
            }
        }

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
            if (!showHatchGui || FreeIva.CurrentModuleFreeIva == null) return;
            if (FreeIva.CurrentModuleFreeIva.Hatches.Count == 0)
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

            if (_hatchIndex >= FreeIva.CurrentModuleFreeIva.Hatches.Count)
                _hatchIndex = 0;
            if (_hatchIndex < 0)
                _hatchIndex = FreeIva.CurrentModuleFreeIva.Hatches.Count - 1;
            GuiUtils.label("Current Hatch", _hatchIndex + 1);
            Hatch h = FreeIva.CurrentModuleFreeIva.Hatches[_hatchIndex];
            GuiUtils.label("Hatch (" + (_hatchIndex + 1) + "/" + FreeIva.CurrentModuleFreeIva.Hatches.Count + ")", h);

            GUILayout.Label("<b>Hatch</b>");
            GUILayout.BeginVertical();
            bool openHatch = h.IsOpen;
            openHatch = GUILayout.Toggle(openHatch, "Open");
            if (h.IsOpen != openHatch)
                h.Open(openHatch);
            // PositionIvaObject(h.gameObject);
            GUILayout.EndVertical();

            float distance = Vector3.Distance(h.WorldPosition, InternalCamera.Instance.transform.position);
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
                GuiUtils.editFloat("Relative Vector X", KerbalIvaController.targetDirection.x);
                GuiUtils.editFloat("Relative Vector Y", KerbalIvaController.targetDirection.y);

                var flightForces = KerbalIvaController.Instance.GetFlightForcesWorldSpace();
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
                float staticFriction = KerbalIvaController.KerbalCollider.material.staticFriction;
                GuiUtils.slider("Static friction", ref staticFriction, 0, 2);
                KerbalIvaController.KerbalCollider.material.staticFriction = staticFriction;

                float dynamicFriction = KerbalIvaController.KerbalCollider.material.dynamicFriction;
                GuiUtils.slider("Dynamic friction", ref dynamicFriction, 0, 2);
                KerbalIvaController.KerbalCollider.material.dynamicFriction = dynamicFriction;

                float bounciness = KerbalIvaController.KerbalCollider.material.bounciness;
                GuiUtils.slider("Bounciness", ref bounciness, 0, 1);
                KerbalIvaController.KerbalCollider.material.bounciness = bounciness;

                // Kerbal in EVA suit: 0.09375 (93.75 kg)
                float mass = KerbalIvaController.KerbalRigidbody.mass;
                GuiUtils.slider("Mass", ref mass, 0, 5);
                KerbalIvaController.KerbalRigidbody.mass = mass;

                GuiUtils.slider("ForwardSpeed", ref Settings.ForwardSpeed, 0, 50);
                GuiUtils.slider("JumpForce", ref Settings.JumpForce, 0, 20);

                GuiUtils.label("Velocity", KerbalIvaController.KerbalRigidbody.velocity);
                if (FreeIva.CurrentPart != null && FreeIva.CurrentPart.Rigidbody != null)
                {
                    GuiUtils.label("Velocity relative to part", FreeIva.CurrentPart.Rigidbody.velocity - KerbalIvaController.KerbalRigidbody.velocity);
                }

                GUILayout.Label("Flight forces (world space)");
                GUILayout.BeginHorizontal();
                GuiUtils.label("X", KerbalIvaController.flightForces.x);
                GuiUtils.label("Y", KerbalIvaController.flightForces.y);
                GuiUtils.label("Z", KerbalIvaController.flightForces.z);
                GUILayout.EndHorizontal();
                GUILayout.Label("Flight forces (IVA space)");
                GUILayout.BeginHorizontal();
                var ivaForces = InternalSpace.WorldToInternal(KerbalIvaController.flightForces);
                GuiUtils.label("X", ivaForces.x);
                GuiUtils.label("Y", ivaForces.y);
                GuiUtils.label("Z", ivaForces.z);
                GUILayout.EndHorizontal();
                GuiUtils.label("IVA rot X", InternalSpace.WorldToInternal(Quaternion.identity).x);
                GuiUtils.label("IVA rot Y", InternalSpace.WorldToInternal(Quaternion.identity).y);
                GuiUtils.label("IVA rot Z", InternalSpace.WorldToInternal(Quaternion.identity).z);

                // Debug
                Vector3 gForce = FlightGlobals.getGeeForceAtPosition(KerbalIvaController.KerbalIva.transform.position);
                Vector3 centrifugalForce = FlightGlobals.getCentrifugalAcc(KerbalIvaController.KerbalIva.transform.position, FreeIva.CurrentPart.orbit.referenceBody);
                Vector3 coriolisForce = FlightGlobals.getCoriolisAcc(FreeIva.CurrentPart.vessel.rb_velocity + Krakensbane.GetFrameVelocityV3f(), FreeIva.CurrentPart.orbit.referenceBody);

                GUILayout.BeginHorizontal();
                GuiUtils.label("Main G-Force X", gForce.x);
                GuiUtils.label("Main G-Force Y", gForce.y);
                GuiUtils.label("Main G-Force Z", gForce.z);
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GuiUtils.label("Centrifugal force X", centrifugalForce.x);
                GuiUtils.label("Centrifugal force Y", centrifugalForce.y);
                GuiUtils.label("Centrifugal force Z", centrifugalForce.z);
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GuiUtils.label("Coriolis force X", coriolisForce.x);
                GuiUtils.label("Coriolis force Y", coriolisForce.y);
                GuiUtils.label("Coriolis force Z", coriolisForce.z);
                GUILayout.EndHorizontal();

                Vector3 gForceInt = InternalSpace.InternalToWorld(gForce);
                GUILayout.BeginHorizontal();
                GuiUtils.label("Main G Internal X", gForceInt.x);
                GuiUtils.label("Main G Internal Y", gForceInt.y);
                GuiUtils.label("Main G Internal Z", gForceInt.z);
                GUILayout.EndHorizontal();
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

        public static void PositionIvaObject(IIvaObject o)
        {
            GUILayout.BeginHorizontal();
            if (o.IvaGameObject != null)
                o.IvaGameObjectRigidbody = o.IvaGameObject.GetComponent<Rigidbody>();

            float xPos = GuiUtils.editFloat("Position X", o.LocalPosition.x);
            float yPos = GuiUtils.editFloat("Position Y", o.LocalPosition.y);
            float zPos = GuiUtils.editFloat("Position Z", o.LocalPosition.z);
            if (xPos != o.LocalPosition.x || yPos != o.LocalPosition.y || zPos != o.LocalPosition.z)
            {
                o.LocalPosition = new Vector3(xPos, yPos, zPos);
                if (o.IvaGameObjectRigidbody != null)
                {
                    o.IvaGameObjectRigidbody.velocity = Vector3.zero;
                    o.IvaGameObjectRigidbody.angularVelocity = Vector3.zero;
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            float xSc = GuiUtils.editFloat("Scale X", o.Scale.x);
            float ySc = GuiUtils.editFloat("Scale Y", o.Scale.y);
            float zSc = GuiUtils.editFloat("Scale Z", o.Scale.z);
            if (xSc != o.Scale.x || ySc != o.Scale.y || zSc != o.Scale.z)
            {
                o.Scale = new Vector3(xSc, ySc, zSc);
                if (o.IvaGameObjectRigidbody != null)
                {
                    o.IvaGameObjectRigidbody.velocity = Vector3.zero;
                    o.IvaGameObjectRigidbody.angularVelocity = Vector3.zero;
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            float xRot = GuiUtils.editFloat("Rotation X", o.Rotation.eulerAngles.x);
            float yRot = GuiUtils.editFloat("Rotation Y", o.Rotation.eulerAngles.y);
            float zRot = GuiUtils.editFloat("Rotation Z", o.Rotation.eulerAngles.z);
            if (xRot != o.Rotation.eulerAngles.x || yRot != o.Rotation.eulerAngles.y || zRot != o.Rotation.eulerAngles.z)
            {
                o.Rotation = Quaternion.Euler(xRot, yRot, zRot);
                if (o.IvaGameObjectRigidbody != null)
                {
                    o.IvaGameObjectRigidbody.velocity = Vector3.zero;
                    o.IvaGameObjectRigidbody.angularVelocity = Vector3.zero;
                }
            }
            GUILayout.EndHorizontal();
        }

        private static string PrintCollider(InternalCollider c)
        {
            var sb = new StringBuilder();
            sb.AppendLine("InternalCollider");
            sb.AppendLine("{");
            sb.AppendLine($"\tname = {c.Name}");
            sb.AppendLine($"\ttype = {c.ColliderType}");
            sb.AppendLine($"\tposition = {c.LocalPosition.x}, {c.LocalPosition.y}, {c.LocalPosition.z}");
            sb.AppendLine($"\tscale = {c.Scale.x}, {c.Scale.y}, {c.Scale.z}");
            sb.AppendLine($"\trotation = {c.Rotation.eulerAngles.x}, {c.Rotation.eulerAngles.y}, {c.Rotation.eulerAngles.z}");
            sb.AppendLine("}");

            return sb.ToString();
        }
    }
}

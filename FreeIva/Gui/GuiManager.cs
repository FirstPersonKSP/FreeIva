using System;
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
            GuiUtils.label("Active kerbal", KerbalIva.ActiveKerbal == null ? "null" : KerbalIva.ActiveKerbal.name);
            GuiUtils.label("Part (" + (_partIndex + 1) + "/" + FlightGlobals.ActiveVessel.Parts.Count + ")", FreeIva.CurrentPart);

            // Internals
            if (FreeIva.CurrentPart.internalModel == null)
            {
                GUILayout.Label("No internal model found");
                //return;
            }

            KerbalIva.MouseLook = GUILayout.Toggle(KerbalIva.MouseLook, "Mouse look");

            GuiUtils.label("Selected object", FreeIva.SelectedObject);

            /*GuiUtils.label("ship_acceleration x", FlightGlobals.ship_acceleration.x);
            GuiUtils.label("ship_acceleration y", FlightGlobals.ship_acceleration.y);
            GuiUtils.label("ship_acceleration z", FlightGlobals.ship_acceleration.z);

            var fcp = FlightGlobals.getGeeForceAtPosition(FlightCamera.fetch.transform.position);
            GuiUtils.label("getGeeForceAtPosition x", fcp.x);
            GuiUtils.label("getGeeForceAtPosition y", fcp.y);
            GuiUtils.label("getGeeForceAtPosition z", fcp.z);

            GuiUtils.label("gForce x", FlightGlobals.ActiveVessel.gForce.x);
            GuiUtils.label("gForce y", FlightGlobals.ActiveVessel.gForce.y);
            GuiUtils.label("gForce z", FlightGlobals.ActiveVessel.gForce.z);*/

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
            if (GUILayout.Button((showColliderGui ? "Hide" : "Show") + " kerbal collider configuration"))
                showKerbalColliderGui = !showKerbalColliderGui;

            if (!showKerbalColliderGui)
                return;

            GUILayout.BeginHorizontal();
            KerbalIva.KerbalCollider.GetComponentCached<Collider>(ref KerbalIva.KerbalColliderCollider);
            KerbalIva.KerbalColliderCollider.enabled = !GUILayout.Toggle(!KerbalIva.KerbalColliderCollider.enabled, "NoClip");
            KerbalIva.Gravity = GUILayout.Toggle(KerbalIva.Gravity, "Gravity");
            KerbalIva.KerbalFeetCollider.enabled = !GUILayout.Toggle(!KerbalIva.KerbalColliderCollider.enabled, "Feet") && KerbalIva.KerbalColliderCollider.enabled;
#if Experimental
            KerbalIva.CanHoldItems = GUILayout.Toggle(KerbalIva.CanHoldItems, "Can move objects");
#endif
            bool helmet = GUILayout.Toggle(KerbalIva.WearingHelmet, "Helmet");
            if (helmet != KerbalIva.WearingHelmet)
            {
                if (helmet)
                    KerbalIva.HelmetOn();
                else
                    KerbalIva.HelmetOff();
            }
            GUILayout.EndHorizontal();


            GUILayout.BeginHorizontal();

            GUILayout.Label("Feet height");
            float yPos = float.Parse(GUILayout.TextField(KerbalIva.KerbalFeet.transform.localPosition.y.ToString()));
            KerbalIva.KerbalFeet.transform.localPosition = new Vector3(KerbalIva.KerbalFeet.transform.localPosition.x,
                yPos,
                KerbalIva.KerbalFeet.transform.localPosition.z);
            GUILayout.EndHorizontal();
        }

        private static bool showColliderGui = false;
        private static int _internalColliderIndex = 0;
        //private static GameObject currentCollider = null;
        //private static FixedJoint currentJoint = null;
        private static PrimitiveType _primitiveType = PrimitiveType.Cube;
        private static void ColliderGui()
        {
            // TODO: Automatically add colliders to selected props using renderer bounds?

            if (GUILayout.Button((showColliderGui ? "Hide" : "Show") + " collider configuration"))
                showColliderGui = !showColliderGui;
            if (!showColliderGui || FreeIva.CurrentModuleFreeIva == null)
            {
                InternalCollider.HideAllColliders();
                return;
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Print collision layers"))
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("                    1 1 1 1 1 1 1 1 1 1 2 2 2 2 2 2 2 2 2 2 3 3");
                sb.Append("0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1");
                for (int i = 0; i < 32; i++)
                {
                    sb.Append("\r\n");
                    if (i < 10) sb.Append(" ");
                    sb.Append(i).Append("\t");
                    for (int j = 0; j < 32; j++)
                        sb.Append(Physics.GetIgnoreLayerCollision(i, j) ? "T " : "F ");
                }
                Debug.Log(sb.ToString());
            }

            if (GUILayout.Button("Create new collider"))
            {
                InternalCollider col = new InternalCollider();
                col.Scale = new Vector3(0.5f, 0.5f, 0.5f);
                col.Instantiate(FreeIva.CurrentPart, _primitiveType);
                col.IvaGameObject.name = "Test collider";
                col.Visible = true;
                FreeIva.CurrentModuleFreeIva.InternalColliders.Add(col);
                _internalColliderIndex = FreeIva.CurrentModuleFreeIva.InternalColliders.Count - 1;
            }
            if (GUILayout.Button("Stop all movement"))
            {
                foreach (var col in FreeIva.CurrentModuleFreeIva.InternalColliders)
                {
                    col.IvaGameObjectRigidbody.velocity = Vector3.zero;
                    col.IvaGameObjectRigidbody.angularVelocity = Vector3.zero;
                }
            }
            if (GUILayout.Button((ClickWatcher.Debug ? "Hide" : "Show") + " interaction"))
            {
                ClickWatcher.Debug = !ClickWatcher.Debug;
            }
            GUILayout.EndHorizontal();

            int colCount = 0;
            if (FreeIva.CurrentPart != null)
            {
                Collider[] colliders = FreeIva.CurrentPart.GetComponentsInChildren<Collider>();
                colCount = colliders.Length;
                if (colCount > 0)
                {
                    GuiUtils.label("Collider0 layer", colliders[0].gameObject.layer);
                }
            }
            GuiUtils.label("Colliders in current part", colCount);

            GUILayout.BeginHorizontal();
            if (GUILayout.Toggle(_primitiveType == PrimitiveType.Sphere, "Sphere"))
                _primitiveType = PrimitiveType.Sphere;
            if (GUILayout.Toggle(_primitiveType == PrimitiveType.Capsule, "Capsule"))
                _primitiveType = PrimitiveType.Capsule;
            if (GUILayout.Toggle(_primitiveType == PrimitiveType.Cylinder, "Cylinder"))
                _primitiveType = PrimitiveType.Cylinder;
            if (GUILayout.Toggle(_primitiveType == PrimitiveType.Cube, "Cube"))
                _primitiveType = PrimitiveType.Cube;
            if (GUILayout.Toggle(_primitiveType == PrimitiveType.Plane, "Plane"))
                _primitiveType = PrimitiveType.Plane;
            if (GUILayout.Toggle(_primitiveType == PrimitiveType.Quad, "Quad"))
                _primitiveType = PrimitiveType.Quad;
            GUILayout.EndHorizontal();

            InternalCollider c = null;
            if (FreeIva.CurrentModuleFreeIva.InternalColliders.Count == 0)
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
                if (InternalCamera.Instance.transform.position != null) // On vessel destruction while in IVA
                    GuiUtils.label("Range", Vector3.Distance(c.IvaGameObject.transform.position, InternalCamera.Instance.transform.position));
            }
            if (c != null)
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Destroy collider"))
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

                GUILayout.BeginHorizontal();
                c.IvaGameObject.GetComponentCached(ref c.IvaGameObjectCollider);
                c.IvaGameObjectCollider.enabled = GUILayout.Toggle(c.IvaGameObjectCollider.enabled, "Enabled");
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

                if (c != null)
                    FreeIva.PositionIvaObject(c);

                GUILayout.BeginHorizontal();
                GuiUtils.label("Velocity", c.IvaGameObjectRigidbody.velocity.magnitude);
                GuiUtils.label("Angular velocity", c.IvaGameObjectRigidbody.angularVelocity.magnitude);
                GUILayout.EndHorizontal();
            }

            if (FreeIva.CurrentPart.internalModel == null)
            {
                GUILayout.Label("No internal model");
            }
            else
            {
                Collider[] imColliders = FreeIva.CurrentPart.internalModel.GetComponentsInChildren<Collider>();
                if (imColliders.Length == 0)
                {
                    GUILayout.Label("No Colliders");
                    return;
                }
                GuiUtils.label("Colliders", imColliders.Length);
                if (_colliderIndex >= imColliders.Length)
                    _colliderIndex = 0;
                if (_colliderIndex < 0)
                    _colliderIndex = imColliders.Length - 1;
                GuiUtils.label("Current Collider", _colliderIndex + 1);
                Collider collider = imColliders[_colliderIndex];

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

                if (GUILayout.Button("Select current collider"))
                    FreeIva.SelectedObject = c.IvaGameObject;
            }
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

            if (GUILayout.Button("Previous Hatch"))
                _hatchIndex--;
            if (GUILayout.Button("Next Hatch"))
                _hatchIndex++;
            if (_hatchIndex >= FreeIva.CurrentModuleFreeIva.Hatches.Count)
                _hatchIndex = 0;
            if (_hatchIndex < 0)
                _hatchIndex = FreeIva.CurrentModuleFreeIva.Hatches.Count - 1;
            GuiUtils.label("Current Hatch", _hatchIndex + 1);
            IHatch h = FreeIva.CurrentModuleFreeIva.Hatches[_hatchIndex];
            GuiUtils.label("Hatch (" + (_hatchIndex + 1) + "/" + FreeIva.CurrentModuleFreeIva.Hatches.Count + ")", h);

            GUILayout.Label("<b>Hatch</b>");
            GUILayout.BeginVertical();
            bool openHatch = h.IsOpen;
            openHatch = GUILayout.Toggle(openHatch, "Open");
            if (h.IsOpen != openHatch)
                h.Open(openHatch);
            FreeIva.PositionIvaObject(h);
            GUILayout.EndVertical();

            float distance = Vector3.Distance(h.WorldPosition, InternalCamera.Instance.transform.position);
            GUILayout.Label(distance.ToString());

            if (GUILayout.Button("Select current hatch"))
                FreeIva.SelectedObject = h.IvaGameObject;

            /*GUILayout.Label("Rotation x");
            hatchRotX = float.Parse(GUILayout.TextField(hatchRotX.ToString()));
            GUILayout.Label("Rotation Y");
            hatchRotY = float.Parse(GUILayout.TextField(hatchRotY.ToString()));
            GUILayout.Label("Rotation Z");
            hatchRotZ = float.Parse(GUILayout.TextField(hatchRotZ.ToString()));
            GUILayout.Label("Scale X");
            hatchScaleX = float.Parse(GUILayout.TextField(hatchScaleX.ToString()));
            GUILayout.Label("Scale Y");
            hatchScaleY = float.Parse(GUILayout.TextField(hatchScaleY.ToString()));
            GUILayout.Label("Scale Z");
            hatchScaleZ = float.Parse(GUILayout.TextField(hatchScaleZ.ToString()));*/
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

            if (FreeIva.CurrentPart.internalModel == null)
            {
                GUILayout.Label("No internal model found.");
                return;
            }

            if (showSeatGui)
            {
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
            GUILayout.Label("Position X");
            float xPos = float.Parse(GUILayout.TextField(prop.transform.localPosition.x.ToString()));
            GUILayout.Label("Position Y");
            float yPos = float.Parse(GUILayout.TextField(prop.transform.localPosition.y.ToString()));
            GUILayout.Label("Position Z");
            float zPos = float.Parse(GUILayout.TextField(prop.transform.localPosition.z.ToString()));
            prop.GetComponentCached<Rigidbody>(ref _propRigidbody);
            if (xPos != prop.transform.localPosition.x || yPos != prop.transform.localPosition.y || zPos != prop.transform.localPosition.z)
            {
                //currentJoint = c.IvaGameObject.GetComponent<FixedJoint>();
                //if (currentJoint != null) Destroy(currentJoint);
                prop.transform.localPosition = new Vector3(xPos, yPos, zPos);
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
            GUILayout.Label("Scale X");
            float xSc = float.Parse(GUILayout.TextField(prop.transform.localScale.x.ToString()));
            GUILayout.Label("Scale Y");
            float ySc = float.Parse(GUILayout.TextField(prop.transform.localScale.y.ToString()));
            GUILayout.Label("Scale Z");
            float zSc = float.Parse(GUILayout.TextField(prop.transform.localScale.z.ToString()));
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
            GUILayout.Label("Rotation X");
            float xRot = float.Parse(GUILayout.TextField(prop.transform.rotation.eulerAngles.x.ToString()));
            GUILayout.Label("Rotation Y");
            float yRot = float.Parse(GUILayout.TextField(prop.transform.rotation.eulerAngles.y.ToString()));
            GUILayout.Label("Rotation Z");
            float zRot = float.Parse(GUILayout.TextField(prop.transform.rotation.eulerAngles.z.ToString()));
            GUILayout.EndHorizontal();

            if (xRot != prop.transform.rotation.eulerAngles.x || yRot != prop.transform.rotation.eulerAngles.y || zRot != prop.transform.rotation.eulerAngles.z)
            {
                //currentJoint = c.IvaGameObject.GetComponent<FixedJoint>();
                //if (currentJoint != null) Destroy(currentJoint);
                prop.transform.rotation = Quaternion.Euler(xRot, yRot, zRot);
                //currentJoint = c.IvaGameObject.AddComponent<FixedJoint>();
                //currentJoint.connectedBody = CurrentPart.collider.rigidbody;

                /* Props don't have colliders.
                prop.rigidbody.velocity = Vector3.zero;
                prop.rigidbody.angularVelocity = Vector3.zero;*/
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label("Rot X");
            float rotx = float.Parse(GUILayout.TextField(prop.transform.localRotation.x.ToString()));
            GUILayout.Label("Rot Y");
            float roty = float.Parse(GUILayout.TextField(prop.transform.localRotation.y.ToString()));
            GUILayout.Label("Rot Z");
            float rotz = float.Parse(GUILayout.TextField(prop.transform.localRotation.z.ToString()));
            GUILayout.Label("Rot W");
            float rotw = float.Parse(GUILayout.TextField(prop.transform.localRotation.w.ToString()));
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
        private static void MeshRendererGui()
        {
            if (GUILayout.Button((showMRGui ? "Hide" : "Show") + " MeshRenderer controls"))
                showMRGui = !showMRGui;

            if (FreeIva.CurrentPart.internalModel == null)
            {
                GUILayout.Label("No internal model found.");
                return;
            }

            if (showMRGui)
            {
                MeshRenderer[] meshRenderers = FreeIva.CurrentPart.internalModel.GetComponentsInChildren<MeshRenderer>();
                if (meshRenderers.Length == 0)
                {
                    GUILayout.Label("No MeshRenderers");
                    return;
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
                GUILayout.Label(FreeIva.CurrentPart.internalModel.transform == null ? "null" : FreeIva.CurrentPart.internalModel.transform.ToString());

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

            if (FreeIva.CurrentPart.internalModel == null)
            {
                GUILayout.Label("No internal model found.");
                return;
            }

            if (showSMRGui)
            {
                SkinnedMeshRenderer[] skinnedMeshRenderers = FreeIva.CurrentPart.internalModel.GetComponentsInChildren<SkinnedMeshRenderer>();
                if (skinnedMeshRenderers.Length == 0)
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
                GUILayout.Label("<b>Transform</b>");
                GUILayout.Label(smr.transform == null ? "null" : smr.transform.ToString());
                //GUILayout.Label("SkinnedMeshRenderer");
                //GuiUtils.label("SkinnedMeshRenderer", smr);
                //GuiUtils.label("Material", smr.material);
                //GuiUtils.label("Transform", smr.transform);
                //if (smr.transform != null)
                {
                    GuiUtils.label("Transform position", smr.transform.position);
                    GuiUtils.label("Range", Vector3.Distance(smr.transform.position, InternalCamera.Instance.transform.position));
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

            if (FreeIva.CurrentPart.internalModel == null)
            {
                GUILayout.Label("No internal model found.");
                return;
            }

            if (showTransformGui)
            {
                Transform[] transforms = FreeIva.CurrentPart.internalModel.GetComponentsInChildren<Transform>();
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
            if (GUILayout.Button((showSMRGui ? "Hide" : "Show") + " View controls"))
                showLookGui = !showLookGui;

            if (showLookGui)
            {
                GuiUtils.label("Absolute X", KerbalIva._mouseAbsolute.x);
                GuiUtils.label("Absolute Y", KerbalIva._mouseAbsolute.y);

                GuiUtils.editFloat("Relative Vector X", KerbalIva.targetDirection.x);
                GuiUtils.editFloat("Relative Vector Y", KerbalIva.targetDirection.y);

                var flightForces = KerbalIva.Instance.GetFlightForcesWorldSpace();
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
                float staticFriction = KerbalIva.KerbalColliderPhysics.staticFriction;
                GuiUtils.slider("Static friction", ref staticFriction, 0, 2);
                KerbalIva.KerbalColliderPhysics.staticFriction = staticFriction;

                float dynamicFriction = KerbalIva.KerbalColliderPhysics.dynamicFriction;
                GuiUtils.slider("Dynamic friction", ref dynamicFriction, 0, 2);
                KerbalIva.KerbalColliderPhysics.dynamicFriction = dynamicFriction;

                float bounciness = KerbalIva.KerbalColliderPhysics.bounciness;
                GuiUtils.slider("Bounciness", ref bounciness, 0, 1);
                KerbalIva.KerbalColliderPhysics.bounciness = bounciness;

                // Kerbal in EVA suit: 0.09375 (93.75 kg)
                float mass = KerbalIva.KerbalColliderRigidbody.mass;
                GuiUtils.slider("Mass", ref mass, 0, 5);
                KerbalIva.KerbalColliderRigidbody.mass = mass;

                GuiUtils.slider("ForwardSpeed", ref Settings.ForwardSpeed, 0, 50);
                GuiUtils.slider("JumpForce", ref Settings.JumpForce, 0, 2);

                GUILayout.Label("Flight Forces (World space)");
                GUILayout.BeginHorizontal();
                GuiUtils.label("X", KerbalIva.flightForces.x);
                GuiUtils.label("Y", KerbalIva.flightForces.y);
                GuiUtils.label("Z", KerbalIva.flightForces.z);
                GUILayout.EndHorizontal();
                GUILayout.Label("Flight Forces (IVA space)");
                GUILayout.BeginHorizontal();
                var ivaForces = InternalSpace.WorldToInternal(KerbalIva.flightForces);
                GuiUtils.label("X", ivaForces.x);
                GuiUtils.label("Y", ivaForces.y);
                GuiUtils.label("Z", ivaForces.z);
                GUILayout.EndHorizontal();
                GuiUtils.label("IVA Rot X", InternalSpace.WorldToInternal(Quaternion.identity).x);
                GuiUtils.label("IVA Rot Y", InternalSpace.WorldToInternal(Quaternion.identity).y);
                GuiUtils.label("IVA Rot Z", InternalSpace.WorldToInternal(Quaternion.identity).z);

                // Debug
                Vector3 gForce = FlightGlobals.getGeeForceAtPosition(KerbalIva.KerbalCollider.transform.position);
                Vector3 centrifugalForce = FlightGlobals.getCentrifugalAcc(KerbalIva.KerbalCollider.transform.position, FreeIva.CurrentPart.orbit.referenceBody);
                Vector3 coriolisForce = FlightGlobals.getCoriolisAcc(FreeIva.CurrentPart.vessel.rb_velocity + Krakensbane.GetFrameVelocityV3f(), FreeIva.CurrentPart.orbit.referenceBody);

                GUILayout.BeginHorizontal();
                GuiUtils.label("Main G-Force X", gForce.x);
                GuiUtils.label("Main G-Force Y", gForce.y);
                GuiUtils.label("Main G-Force Z", gForce.z);
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GuiUtils.label("Centrifugal Force X", centrifugalForce.x);
                GuiUtils.label("Centrifugal Force Y", centrifugalForce.y);
                GuiUtils.label("Centrifugal Force Z", centrifugalForce.z);
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GuiUtils.label("Coriolis Force X", coriolisForce.x);
                GuiUtils.label("Coriolis Force Y", coriolisForce.y);
                GuiUtils.label("Coriolis Force Z", coriolisForce.z);
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
                    GUILayout.Label("Found " + partAnimators.Length + " part animators.");
                if (ivaAnimators != null)
                    GUILayout.Label("Found " + ivaAnimators.Length + " IVA animators.");

                foreach (Animation anim in ivaAnimators)
                {
                    if (anim.gameObject != null)
                    {
                        if (anim.gameObject.transform != null)
                            GUILayout.Label("Parent: " + anim.gameObject.transform.parent);
                        else
                            GUILayout.Label("No transform");
                    }
                    else
                        GUILayout.Label("No game object");

                    //AnimationState state = anim["openHatch"];
                    foreach (AnimationState state in anim)
                    {
                        GUILayout.BeginHorizontal();
                        if (GUILayout.Button(state.name) && !anim.isPlaying)
                        {
                            state.speed = -state.speed;
                            if (state.speed > 0)
                                state.normalizedTime = 0f;
                            else
                                state.normalizedTime = 1f;
                            anim.enabled = true;
                            anim.wrapMode = WrapMode.Once;
                            anim.clip = state.clip;
                            anim.Play();
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

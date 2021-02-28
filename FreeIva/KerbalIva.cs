using System;
using UnityEngine;

namespace FreeIva
{
    /// <summary>
    /// Character controller for IVA movement.
    /// </summary>
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class KerbalIva : MonoBehaviour
    {
        public static GameObject KerbalCollider;
        public static Collider KerbalColliderCollider;
        public static PhysicMaterial KerbalColliderPhysics;
        public static Rigidbody KerbalColliderRigidbody;
        // TODO: Vary this by kerbal stats and equipment carried.
        public static float KerbalMass = 0.03125f; // From persistent file for EVA kerbal.
        public static Renderer KerbalColliderRenderer;

        public static GameObject KerbalFeet;
        public static Collider KerbalFeetCollider;
        public static PhysicMaterial KerbalFeetPhysics;
        public static Rigidbody KerbalFeetRigidbody;
        public static Renderer KerbalFeetRenderer;

#if Experimental
        public static GameObject KerbalWorldSpace;
        public static Collider KerbalWorldSpaceCollider;
        public static PhysicMaterial KerbalWorldSpacePhysics;
        public static Rigidbody KerbalWorldSpaceRigidbody;
        public static Renderer KerbalWorldSpaceRenderer;
#endif

        public static bool WearingHelmet { get; private set; }
        public bool buckled = true;
        public bool cameraPositionLocked = false;
        public Quaternion previousRotation = new Quaternion();
        public static ProtoCrewMember ActiveKerbal;
        public InternalSeat OriginalSeat = null;
        public InternalSeat TargetedSeat = null;
        public int TargetedSeatIndex = -1;
        public static bool MouseLook = true;
        public static bool Gravity = false;
#if Experimental
        public static bool CanHoldItems = false;
#endif
        public static Vector3 flightForces;

        private CameraManager.CameraMode _lastCameraMode = CameraManager.CameraMode.Flight;
        private Vector3 _previousPos = Vector3.zero;
        private bool _changingCurrentIvaCrew = false;

        public static KerbalIva _instance;
        public static KerbalIva Instance { get { return _instance; } }

        public void Start()
        {
            CreateCameraCollider();
            //cameraCollider.renderer.enabled = false;
            //cameraCollider.transform.parent = transform;
            //transform.parent = FlightCamera.fetch.transform; ?

            //cameraCollider.transform.parent = InternalCamera.Instance.transform;//FlightCamera.fetch.transform;
            SetCameraToSeat();

            GameEvents.OnCameraChange.Add(OnCameraChange);
            _instance = this;
        }

        public void Update()
        {
            if (CameraManager.Instance.currentCameraMode != CameraManager.CameraMode.IVA)
            {
                if (_lastCameraMode == CameraManager.CameraMode.IVA)
                {
                    // Switching away from IVA.
                    //InputLockManager.RemoveControlLock("FreeIVA");
                    // Return the kerbal to its original seat.
                    TargetedSeat = ActiveKerbal.seat;
                    if (!buckled)
                        Buckle();
                }
            }
            else
            {
                // In IVA.

                if (_lastCameraMode != CameraManager.CameraMode.IVA)
                {
                    // Switching to IVA.
                    FreeIva.EnableInternals();
                    UpdateActiveKerbal();//false);
                    SetCameraToSeat();
                    InternalCollider.HideAllColliders();
                }

                // Check if we're changing crew member using the 'V' key.
                if (GameSettings.CAMERA_NEXT.GetKeyDown())
                    _changingCurrentIvaCrew = true;
                else
                {
                    if (_changingCurrentIvaCrew)
                    {
                        UpdateActiveKerbal();//false);
                        _changingCurrentIvaCrew = false;
                    }
                }

                GetInput();

                /*FreeIva.InitialPart.Events.Clear();
                if (_transferStart != 0 && Planetarium.GetUniversalTime() > (0.25 + _transferStart))
                {
                    FlightGlobals.ActiveVessel.SpawnCrew();
                    GameEvents.onVesselChange.Fire(FlightGlobals.ActiveVessel);
                    InternalCamera.Instance.transform.parent = ActiveKerbal.KerbalRef.eyeTransform;
                    _transferStart = 0;
                }*/

            }
            _lastCameraMode = CameraManager.Instance.currentCameraMode;
        }

        CameraManager.CameraMode _previousCameraMode = CameraManager.CameraMode.Flight;
        public void OnCameraChange(CameraManager.CameraMode cameraMode)
        {
            if (cameraMode != CameraManager.CameraMode.IVA && _previousCameraMode == CameraManager.CameraMode.IVA)
            {
#if Experimental
                KerbalWorldSpaceCollider.enabled = false;
#endif
                if (!buckled)
                {
                    ReturnToSeat();
                }
            }
            if (cameraMode == CameraManager.CameraMode.IVA)
            {
                //KerbalWorldSpaceCollider.enabled = true;
            }
            _previousCameraMode = cameraMode;
        }

        // Returns (8.018946, 0.0083341, -5.557827) while clamped to the runway.
        public Vector3 GetFlightForcesWorldSpace()
        {
            Vector3 gForce = FlightGlobals.getGeeForceAtPosition(KerbalCollider.transform.position);
            Vector3 centrifugalForce = FlightGlobals.getCentrifugalAcc(KerbalCollider.transform.position, FreeIva.CurrentPart.orbit.referenceBody);
            Vector3 coriolisForce = FlightGlobals.getCoriolisAcc(FreeIva.CurrentPart.vessel.rb_velocity + Krakensbane.GetFrameVelocityV3f(), FreeIva.CurrentPart.orbit.referenceBody);

            return gForce + centrifugalForce + coriolisForce;
        }

        public void FixedUpdate()
        {
            ApplyGravity();
        }

        private void ApplyGravity()
        {
            KerbalCollider.GetComponentCached(ref KerbalColliderRigidbody);
            if (Gravity)
            {
                Vector3 gForce = FlightGlobals.getGeeForceAtPosition(KerbalCollider.transform.position);
                Vector3 centrifugalForce = FlightGlobals.getCentrifugalAcc(KerbalCollider.transform.position, FreeIva.CurrentPart.orbit.referenceBody);
                Vector3 coriolisForce = FlightGlobals.getCoriolisAcc(FreeIva.CurrentPart.vessel.rb_velocity + Krakensbane.GetFrameVelocityV3f(), FreeIva.CurrentPart.orbit.referenceBody);

                gForce = InternalSpace.WorldToInternal(gForce);
                centrifugalForce = InternalSpace.WorldToInternal(centrifugalForce);
                coriolisForce = InternalSpace.WorldToInternal(coriolisForce);
                flightForces = gForce + centrifugalForce + coriolisForce;

                KerbalColliderRigidbody.AddForce(gForce, ForceMode.Acceleration);
                KerbalColliderRigidbody.AddForce(centrifugalForce, ForceMode.Acceleration);
                KerbalColliderRigidbody.AddForce(coriolisForce, ForceMode.Acceleration);
                KerbalColliderRigidbody.AddForce(Krakensbane.GetLastCorrection(), ForceMode.VelocityChange);

                if (FreeIva.SelectedObject != null)
                {
                    Rigidbody rbso = FreeIva.SelectedObject.GetComponent<Rigidbody>();
                    if (rbso == null)
                        rbso = FreeIva.SelectedObject.AddComponent<Rigidbody>();
                    rbso.AddForce(gForce, ForceMode.Acceleration);
                    rbso.AddForce(centrifugalForce, ForceMode.Acceleration);
                    rbso.AddForce(coriolisForce, ForceMode.Acceleration);
                }
            }
            else
            {
                KerbalColliderRigidbody.velocity = Vector3.zero;
            }
        }

        private void CreateCameraCollider()
        {
            KerbalCollider = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            KerbalCollider.name = "Kerbal collider";
            KerbalCollider.GetComponentCached<Collider>(ref KerbalColliderCollider);
            KerbalColliderCollider.enabled = false;
            KerbalColliderPhysics = KerbalColliderCollider.material;
            KerbalColliderRigidbody = KerbalCollider.AddComponent<Rigidbody>();
            KerbalColliderRigidbody.useGravity = false;
            KerbalColliderRigidbody.mass = KerbalMass;
            // Rotating the object would offset the rotation of the controls from the camera position.
            KerbalColliderRigidbody.constraints = RigidbodyConstraints.FreezeRotation;
#if DEBUG
            KerbalCollider.AddComponent<IvaCollisionPrinter>();
#endif
            KerbalColliderCollider.isTrigger = false;
            KerbalCollider.layer = (int)Layers.InternalSpace; //KerbalCollider.layer = (int)Layers.Kerbals; 2021-02-26
            KerbalCollider.transform.localScale = new Vector3(Settings.NoHelmetSize, Settings.NoHelmetSize, Settings.NoHelmetSize);
            KerbalCollider.GetComponentCached<Renderer>(ref KerbalColliderRenderer);
            KerbalColliderRenderer.enabled = false;

            KerbalFeet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            KerbalFeet.name = "Kerbal feet collider";
            KerbalFeet.GetComponentCached<Collider>(ref KerbalFeetCollider);
            KerbalFeetCollider.enabled = false;
            KerbalFeetPhysics = KerbalFeetCollider.material;
            KerbalFeetRigidbody = KerbalFeet.AddComponent<Rigidbody>();
            KerbalFeetRigidbody.useGravity = false;
            KerbalFeetRigidbody.constraints = RigidbodyConstraints.FreezeRotation;
            KerbalFeetCollider.isTrigger = false;
            KerbalFeet.layer = (int)Layers.InternalSpace; //KerbalFeet.layer = (int)Layers.Kerbals; 2021-02-26
            KerbalFeet.transform.parent = KerbalCollider.transform;
            KerbalFeet.transform.localScale = new Vector3(Settings.NoHelmetSize, Settings.NoHelmetSize, Settings.NoHelmetSize);
            KerbalFeet.transform.localPosition = new Vector3(0, 0, 0.2f);
            KerbalFeet.GetComponentCached<Renderer>(ref KerbalFeetRenderer);
            KerbalFeetRenderer.enabled = false;

#if Experimental
            KerbalWorldSpace = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            KerbalWorldSpace.name = "Kerbal World Space collider";
            KerbalWorldSpace.GetComponentCached<Collider>(ref KerbalWorldSpaceCollider);
            KerbalWorldSpaceCollider.enabled = false; // TODO: Makes vessel explode.
            KerbalWorldSpacePhysics = KerbalWorldSpaceCollider.material;
            KerbalWorldSpaceRigidbody = KerbalWorldSpace.AddComponent<Rigidbody>();
            KerbalWorldSpaceRigidbody.useGravity = false;
            KerbalWorldSpaceRigidbody.constraints = RigidbodyConstraints.FreezeRotation;

            WorldCollisionTracker KerbalWorldCollisionTracker = KerbalWorldSpace.AddComponent<WorldCollisionTracker>();
#endif
#if DEBUG
            KerbalWorldSpace.AddComponent<IvaCollisionPrinter>();
#endif
#if Experimental
            KerbalWorldCollisionTracker.Initialise(KerbalColliderRigidbody);

            KerbalWorldSpaceCollider.isTrigger = false;
            KerbalWorldSpace.layer = (int)Layers.LocalScenery;
            KerbalWorldSpaceCollider.transform.parent = KerbalCollider.transform;
            KerbalWorldSpace.transform.localScale = new Vector3(Settings.HelmetSize, Settings.HelmetSize, Settings.HelmetSize);
            KerbalWorldSpace.transform.localPosition = new Vector3(0, 0, 0.2f);
            KerbalWorldSpace.GetComponentCached<Renderer>(ref KerbalWorldSpaceRenderer);
            KerbalWorldSpaceRenderer.enabled = true;
#endif
        }

        private void SetCameraToSeat()
        {
            if (InternalCamera.Instance == null)
            {
                Debug.LogError("InternalCamera was null");
                Debug.Log("Searching for camera: " + InternalCamera.FindObjectOfType<Camera>());
                return;
            }
            KerbalCollider.transform.position = InternalCamera.Instance.transform.position; //forward;// FlightCamera.fetch.transform.forward;
            //previousRotation = InternalCamera.Instance.transform.rotation;// *Quaternion.AngleAxis(-90, InternalCamera.Instance.transform.right);
            previousRotation = Quaternion.AngleAxis(-90, InternalCamera.Instance.transform.right) * InternalCamera.Instance.transform.localRotation; // Fixes "unbuckling looking at feet"
        }

        private void GetInput()
        {

            if (Input.GetKeyDown(Settings.UnbuckleKey))
            {
                if (Input.GetKey(Settings.ModifierKey))
                {
                    cameraPositionLocked = !cameraPositionLocked;
                    ScreenMessages.PostScreenMessage(cameraPositionLocked ? "Camera locked" : "Camera unlocked",
                        1f, ScreenMessageStyle.LOWER_CENTER);
                }
                else
                {
                    if (buckled)
                        Unbuckle();
                    else
                        Buckle();
                }
            }

            if (!buckled && !FreeIva.Paused)
            {
                UpdateOrientation();
                UpdatePosition();
                TargetSeats();
                TargetHatches();
                if (_lastCameraMode != CameraManager.CameraMode.IVA)
                    InputLockManager.SetControlLock(ControlTypes.ALL_SHIP_CONTROLS, "FreeIVA");
            }
        }

        //private bool _reseatingCrew = false;
        public void Buckle()
        {
            if (TargetedSeat == null || TargetedSeatIndex == -1)
                return;

            Debug.Log(ActiveKerbal.name + " is entering seat " + TargetedSeat.transform.name + " at index " + TargetedSeatIndex + " in part " + FreeIva.CurrentPart);

            //InitialPart.RemoveCrewmember(ActiveKerbal); // Don't do this on unbuckle or we lose the InternalCamera.

            /*TargetedSeat.part.AddCrewmemberAt(ActiveKerbal, TargetedSeatIndex);
            CurrentPart.SpawnCrew();*/
        /*HideCurrentKerbal(false);

        FreeIva.InitialPart.RemoveCrewmember(ActiveKerbal);
        FreeIva.CurrentPart.AddCrewmemberAt(ActiveKerbal, TargetedSeatIndex);
        HideCurrentKerbal(false);

        if (FreeIva.InitialPart != FreeIva.CurrentPart)
        {
            GameEvents.onCrewTransferred.Fire(new GameEvents.HostedFromToAction<ProtoCrewMember, Part>(ActiveKerbal, FreeIva.InitialPart, FreeIva.CurrentPart));
            GameEvents.onVesselChange.Fire(FlightGlobals.ActiveVessel);
            /*InitialPart.SpawnCrew();
            CurrentPart.SpawnCrew();
            InitialPart.RegisterCrew();
            CurrentPart.RegisterCrew();
            FlightGlobals.ActiveVessel.SpawnCrew();* /
        }
        //_reseatingCrew = true;
        FreeIva.EnableInternals();*/

        //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        /*if (FreeIva.InitialPart != null && FreeIva.InitialPart != FreeIva.CurrentPart)
        {*/
        //TransferCrewTest(ActiveKerbal, FreeIva.InitialPart, FreeIva.CurrentPart);
        //FreeIva.InitialPart.RemoveCrewmember(ActiveKerbal); - This part kills the InternalCamera.Instance by calling internalModel.UnseatKerbal
        /*if (FreeIva.InitialPart.protoModuleCrew.Contains(ActiveKerbal))
        {
            ActiveKerbal.UnregisterExperienceTraits(FreeIva.InitialPart);
            ActiveKerbal.rosterStatus = ProtoCrewMember.RosterStatus.Available;
            FreeIva.InitialPart.protoModuleCrew.Remove(ActiveKerbal);
            if (FreeIva.InitialPart.internalModel)
            {
                OriginalSeat.crew.seatIdx = -1;
                OriginalSeat.crew.seat = null;
                OriginalSeat.crew = null;
                OriginalSeat.taken = false;
                if (OriginalSeat.kerbalRef)
                {
                    //OriginalSeat.DespawnCrew();
                }
            }
            FreeIva.CurrentPart.AddCrewmemberAt(ActiveKerbal, TargetedSeatIndex);
        }*/
        //OriginalSeat.kerbalRef = null;
        //TargetedSeat.kerbalRef = ActiveKerbal.KerbalRef;
        //FreeIva.CurrentPart.AddCrewmember(ActiveKerbal);
        //if (ActiveKerbal.seat != null)
        //ActiveKerbal.seat.SpawnCrew();
        //FreeIva.InitialPart = FreeIva.CurrentPart;

        InternalCamera.Instance.transform.parent = ActiveKerbal.KerbalRef.eyeTransform;
            CameraManager.Instance.SetCameraFlight();
            buckled = true;
            CrewTransfer_MoveCrewTo();
            /*}
            else
            {
                SetCameraToSeat();
                if (InternalCamera.Instance != null)
                {
                    // The Kerbal's eye transform is the InternalCamera's parent normally, not InternalSpace.Instance as previously thought.
                    InternalCamera.Instance.transform.parent = ActiveKerbal.KerbalRef.eyeTransform;
                }
            }*/
            KerbalCollider.GetComponentCached<Collider>(ref KerbalColliderCollider);
            KerbalColliderCollider.enabled = false;
            HideCurrentKerbal(false);
            DisablePartHighlighting(false);
            InputLockManager.RemoveControlLock("FreeIVA");
            //ActiveKerbal.flightLog.AddEntry("Buckled");
            ScreenMessages.PostScreenMessage("Buckled", 1f, ScreenMessageStyle.LOWER_CENTER);
        }

        public void ReturnToSeat()
        {
            //TargetedSeat = OriginalSeat;
            //Buckle();
            buckled = true;
            ScreenMessages.PostScreenMessage(ActiveKerbal.name + " returned to their seat.", 1f, ScreenMessageStyle.LOWER_CENTER);
        }

        private Vector3 _initialEyePosition;
        public void CrewTransfer_MoveCrewTo()
        {
            _initialEyePosition = ActiveKerbal.KerbalRef.eyeInitialPos;
            FreeIva.InitialPart.RemoveCrewmember(ActiveKerbal);
            FreeIva.CurrentPart.AddCrewmemberAt(ActiveKerbal, TargetedSeatIndex);
            GameEvents.onCrewTransferred.Fire(new GameEvents.HostedFromToAction<ProtoCrewMember, Part>(ActiveKerbal, FreeIva.InitialPart, FreeIva.CurrentPart));
            FlightGlobals.ActiveVessel.DespawnCrew();
            base.StartCoroutine(CallbackUtil.DelayedCallback(1, new Callback(CrewTransfer_waitAndCompleteTransfer)));
        }
        private void CrewTransfer_waitAndCompleteTransfer()
        {
            FlightGlobals.ActiveVessel.SpawnCrew();
            ActiveKerbal.KerbalRef.eyeInitialPos = _initialEyePosition;
            CameraManager.Instance.SetCameraIVA(ActiveKerbal.KerbalRef, false);
            InternalCamera.Instance.transform.position += TargetedSeat.kerbalEyeOffset;
            KerbalCollider.transform.position = InternalCamera.Instance.transform.position;
        }

        //Transform _internalCameraParent = null;

        public void Unbuckle()
        {
            _previousPos = Vector3.zero;
            FreeIva.EnableInternals();
            UpdateActiveKerbal();
            FreeIva.InitialPart = FreeIva.CurrentPart;
            OriginalSeat = ActiveKerbal.seat;

            HideCurrentKerbal(true);

            KerbalCollider.transform.position = InternalCamera.Instance.transform.position;
            // The Kerbal's eye transform is the InternalCamera's parent normally, not InternalSpace.Instance as previously thought.
            InternalCamera.Instance.transform.parent = KerbalCollider.transform;

            InputLockManager.SetControlLock(ControlTypes.ALL_SHIP_CONTROLS, "FreeIVA");
            //ActiveKerbal.flightLog.AddEntry("Unbuckled");
            ScreenMessages.PostScreenMessage("Unbuckled", 1f, ScreenMessageStyle.LOWER_CENTER);
            KerbalCollider.GetComponentCached<Collider>(ref KerbalColliderCollider).enabled = true;
            buckled = false;

            InitialiseFpsControls();
            DisablePartHighlighting(true);
        }

        /*private void TransferCrewTest(ProtoCrewMember crew, Part fromPart, Part toPart)
        {
            BaseEvent baseEvent = new BaseEvent(fromPart.Events, "transfer" + crew.name, () => TransferEvent(crew, fromPart, toPart),
                new KSPEvent { guiName = "Transfer" + crew.name, guiActive = true });
            fromPart.Events.Add(baseEvent);
        }

        private double _transferStart = 0;
        private void TransferEvent(ProtoCrewMember crew, Part fromPart, Part toPart)
        {
            fromPart.RemoveCrewmember(crew);
            toPart.AddCrewmember(crew);
            crew.seat.SpawnCrew();
            _transferStart = Planetarium.GetUniversalTime();
        }*/

        private void InitialiseFpsControls()
        {
            // Set target direction to the camera's initial orientation.
            targetDirection = InternalCamera.Instance.transform.rotation.eulerAngles;
        }

        private void UpdateActiveKerbal()//bool nextKerbal) TODO !!!
        {
            ActiveKerbal = CameraManager.Instance.IVACameraActiveKerbal.protoCrewMember;
            if (ActiveKerbal.KerbalRef != null && ActiveKerbal.KerbalRef.InPart != null)
                FreeIva.UpdateCurrentPart(ActiveKerbal.KerbalRef.InPart);
            return;
            // TODO !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!! Test if the above (new to 1.1) is working, if so delete the below).

            /*try
            {
                // TODO: Find a way of detecting a viewpoint switch ('V' in IVA).
                List<ProtoCrewMember> vesselCrew = FlightGlobals.fetch.activeVessel.GetVesselCrew();

                // TODO: There has to be a better way of doing this.
                // Attempt to find the active crew member by searching for the one with the hidden head.
                foreach (ProtoCrewMember c in vesselCrew)
                {
                    if (nextKerbal && c == ActiveKerbal)
                        continue;
                    if (c.KerbalRef.headTransform != null)
                    {
                        Renderer[] rs = c.KerbalRef.headTransform.GetComponentsInChildren<Renderer>();
                        if (rs.Length > 0 && !rs[0].isVisible) // Normally the left eye
                        {
                            Debug.Log("Updating active kerbal from " + (ActiveKerbal == null ? "null" : ActiveKerbal.name) + " to " + c.name);
                            ActiveKerbal = c;
                            if (ActiveKerbal.KerbalRef != null && ActiveKerbal.KerbalRef.InPart != null)
                                FreeIva.UpdateCurrentPart(ActiveKerbal.KerbalRef.InPart);
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("[FreeIVA] Error updating active kerbal: " + ex.Message + ", " + ex.StackTrace);
            }*/
        }

        /// <summary>
        /// Hides or unhides the kerbal IVA model of the currently controlled crew member.
        /// </summary>
        /// <param name="hidden"></param>
        public void HideCurrentKerbal(bool hidden)
        {
            try
            {
                // Set the visible/invisible state for each component of the kerbal model.
                // This won't unhide a helmet hidden elsewhere.
                if (ActiveKerbal != null)
                {
                    Renderer[] renderers = ActiveKerbal.KerbalRef.GetComponentsInChildren<Renderer>();
                    foreach (var r in renderers)
                    {
                        r.enabled = !hidden;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("[FreeIVA] Error hiding current kerbal: " + ex.Message + ", " + ex.StackTrace);
            }
        }

        public void DisablePartHighlighting(bool isDisabled)
        {
            int partCount = FlightGlobals.ActiveVessel.parts.Count;
            for (int i = 0; i < partCount; i++)
            {
                if (isDisabled)
                    FlightGlobals.ActiveVessel.parts[i].highlightType = Part.HighlightType.Disabled;
                else
                {
                    FlightGlobals.ActiveVessel.parts[i].highlightType = Part.HighlightType.OnMouseOver;
                }
            }
        }

        // TODO: Replace this with clickable interaction colliders.
        public bool IsTargeted(Vector3 position)
        {
            float distance = Vector3.Distance(position, InternalCamera.Instance.transform.position);
            float angle = Vector3.Angle(position - InternalCamera.Instance.transform.position, InternalCamera.Instance.transform.forward);

            if (angle > 90)
                return false;

            float radius = (distance * Mathf.Sin(angle * Mathf.Deg2Rad)) / Mathf.Sin((90 - angle) * Mathf.Deg2Rad);
            return (radius <= Settings.ObjectInteractionRadius);
        }

        // TODO: Replace this with clickable interaction colliders.
        public void TargetSeats()
        {
            float closestDistance = Settings.MaxInteractDistance;
            TargetedSeat = null;
            TargetedSeatIndex = -1;
            if (FreeIva.CurrentPart.internalModel == null)
                return;

            for (int i = 0; i < FreeIva.CurrentPart.internalModel.seats.Count; i++)
            {
                if (FreeIva.CurrentPart.internalModel.seats[i].taken && !FreeIva.CurrentPart.internalModel.seats[i].crew.Equals(ActiveKerbal))
                    continue;

                if (FreeIva.CurrentPart.internalModel.seats[i].seatTransform == null)
                    continue; // Some parts were originally designed to have more seats, but later had their transforms removed without changing the seat count.

                if (IsTargeted(FreeIva.CurrentPart.internalModel.seats[i].seatTransform.position))
                {
                    float distance = Vector3.Distance(FreeIva.CurrentPart.internalModel.seats[i].seatTransform.position,
                        InternalCamera.Instance.transform.position);
                    if (distance < closestDistance)
                    {
                        TargetedSeat = FreeIva.CurrentPart.internalModel.seats[i];
                        TargetedSeatIndex = i;
                        closestDistance = distance;
                    }
                }
            }

            if (TargetedSeat != null)
                ScreenMessages.PostScreenMessage("Enter seat [" + Settings.UnbuckleKey + "]", 0.1f, ScreenMessageStyle.LOWER_CENTER);
        }

        // TODO: Replace this with clickable interaction colliders.
        public void TargetHatches()
        {
            if (FreeIva.CurrentModuleFreeIva == null) return;

            Hatch targetedHatch = null;
            float closestDistance = Settings.MaxInteractDistance;

            if (FreeIva.CurrentModuleFreeIva.Hatches.Count != 0)
            {
                //for (int i = 0; i < CurrentModuleFreeIva.Hatches.Count; i++)
                foreach (Hatch h in FreeIva.CurrentModuleFreeIva.Hatches)
                {
                    if (IsTargeted(h.WorldPosition))
                    {
                        float distance = Vector3.Distance(h.WorldPosition, InternalCamera.Instance.transform.position);
                        if (distance < closestDistance)
                        {
                            targetedHatch = h;
                            closestDistance = distance;
                        }
                    }
                }
            }
            else
            {
                // Part has no hatches but does have a ModuleFreeIva. Passable part without hatches, like a tube.
                // TODO: Restrict this to node attachments?
                ModuleFreeIva parentModule = FreeIva.CurrentPart.parent.GetModule<ModuleFreeIva>();
                if (parentModule != null)
                {
                    foreach (Hatch h in parentModule.Hatches)
                    {
                        if (IsTargeted(h.WorldPosition))
                        {
                            float distance = Vector3.Distance(h.WorldPosition, InternalCamera.Instance.transform.position);
                            if (distance < closestDistance)
                            {
                                targetedHatch = h;
                                closestDistance = distance;
                            }
                        }
                    }
                }

                foreach (Part child in FreeIva.CurrentPart.children)
                {
                    ModuleFreeIva childModule = child.GetModule<ModuleFreeIva>();
                    if (childModule == null)
                        continue;
                    foreach (Hatch h in childModule.Hatches)
                    {
                        if (IsTargeted(h.WorldPosition))
                        {
                            float distance = Vector3.Distance(h.WorldPosition, InternalCamera.Instance.transform.position);
                            if (distance < closestDistance)
                            {
                                targetedHatch = h;
                                closestDistance = distance;
                            }
                        }
                    }
                }
            }

            if (targetedHatch != null)
            {
                ScreenMessages.PostScreenMessage((targetedHatch.IsOpen ? "Close" : "Open") + " hatch [" + Settings.OpenHatchKey + "]",
                        0.1f, ScreenMessageStyle.LOWER_CENTER);

                if (Input.GetKeyDown(Settings.OpenHatchKey) && !Input.GetKey(Settings.ModifierKey))
                    targetedHatch.ToggleHatch();

                // Allow reaching through an open hatch to open or close the connected hatch.
                if (targetedHatch.IsOpen && targetedHatch.ConnectedHatch != null && IsTargeted(targetedHatch.ConnectedHatch.WorldPosition))
                {
                    float distance = Vector3.Distance(targetedHatch.ConnectedHatch.WorldPosition, InternalCamera.Instance.transform.position);
                    if (distance < Settings.MaxInteractDistance)
                    {
                        ScreenMessages.PostScreenMessage((targetedHatch.ConnectedHatch.IsOpen ? "Close" : "Open") + " far hatch [" + Settings.ModifierKey + " + " + Settings.OpenHatchKey + "]",
                                0.1f, ScreenMessageStyle.LOWER_CENTER);
                        if (Input.GetKeyDown(Settings.OpenHatchKey) && Input.GetKey(Settings.ModifierKey))
                            targetedHatch.ConnectedHatch.ToggleHatch();
                    }
                }
            }
        }

        private bool _wasFreeControls = true;

        public void UpdateOrientation()
        {
            /*Vector3 gForce = FlightGlobals.getGeeForceAtPosition(FlightCamera.fetch.transform.position);
            Vector3 gForceInternal = InternalSpace.WorldToInternal(gForce);

            
            Quaternion cameraRotation = InternalCamera.Instance.transform.rotation;


            previousRotation = cameraRotation;
            FlightCamera.fetch.transform.rotation = InternalSpace.InternalToWorld(InternalCamera.Instance.transform.rotation);*/

            if (Gravity && GetFlightForcesWorldSpace().magnitude > 1)
            {
                if (_wasFreeControls)
                    InitialiseFpsControls();

                RelativeOrientation();
                _wasFreeControls = false;
            }
            else
            {
                FreeOrientation();
                _wasFreeControls = true;
            }
        }

        // Free movement with no "down".
        private void FreeOrientation()
        {
            float yaw = 0;
            float pitch = 0;
            float roll = 0;

            if (!cameraPositionLocked)
            {
                if (MouseLook)
                {
                    float mouseX = Input.GetAxis("Mouse X");
                    yaw = mouseX * Settings.YawSpeed;
                    float mouseY = Input.GetAxis("Mouse Y");
                    pitch = -mouseY * Settings.PitchSpeed;
                }
                else
                {
                    yaw = 0;
                    if (Input.GetKey(KeyCode.L))
                        yaw = Settings.YawSpeed * Time.deltaTime * 10;
                    else if (Input.GetKey(KeyCode.J))
                        yaw = -Settings.YawSpeed * Time.deltaTime * 10;
                    pitch = 0;
                    if (Input.GetKey(KeyCode.I))
                        pitch = Settings.PitchSpeed * Time.deltaTime * 10;
                    else if (Input.GetKey(KeyCode.K))
                        pitch = -Settings.PitchSpeed * Time.deltaTime * 10;
                    roll = 0;
                }
            }
            if (Input.GetKey(Settings.RollCCWKey))
                roll = Settings.RollSpeed * Time.deltaTime;
            else if (Input.GetKey(Settings.RollCWKey))
                roll = -Settings.RollSpeed * Time.deltaTime;

            Quaternion rotYaw = Quaternion.AngleAxis(yaw, previousRotation * Vector3.up);
            Quaternion rotPitch = Quaternion.AngleAxis(pitch, previousRotation * Vector3.right);// *Quaternion.Euler(0, 90, 0);


            Transform cameraTransform = InternalCamera.Instance.transform;


            /*Vector3 gForce = FlightGlobals.getGeeForceAtPosition(FlightCamera.fetch.transform.position);
            Vector3 gDirection = gForce.normalized;
            Vector3 gDirectionInternal = InternalSpace.WorldToInternal(-gDirection).normalized;*/

            //Utils.line.SetPosition(0, InternalCamera.Instance.transform.localPosition);
            //Utils.line.SetPosition(1, InternalCamera.Instance.transform.localPosition - Quaternion.AngleAxis(90, Vector3.up) * gDirectionInternal);

            /*float angle = Vector3.Angle(Vector3.Project(gDirectionInternal, cameraTransform.forward).normalized, cameraTransform.up);
            Quaternion rotRoll = Quaternion.AngleAxis(angle, previousRotation * Vector3.forward);*/
            //Quaternion.LookRotation



            Quaternion rotRoll = Quaternion.AngleAxis(roll, previousRotation * Vector3.forward);
            /* new *
            if (Gravity)
            {
                gForce = FlightGlobals.getGeeForceAtPosition(KerbalCollider.transform.position);
                gForce = InternalSpace.WorldToInternal(gForce);
                Quaternion rotation = Quaternion.LookRotation(-gForce, Vector3.forward);
                Quaternion current = cameraTransform.rotation;
                rotRoll = Quaternion.Slerp(current, rotation, Time.deltaTime);
            }*/


            cameraTransform.rotation = rotRoll * rotPitch * rotYaw * previousRotation;
            previousRotation = cameraTransform.rotation;
            FlightCamera.fetch.transform.rotation = InternalSpace.InternalToWorld(cameraTransform.rotation);
        }




        public static Vector2 clampInDegrees = new Vector2(360, 180);
        public static Vector2 sensitivity = new Vector2(Settings.YawSensitivity, Settings.PitchSensitivity);
        public static Vector2 smoothing = new Vector2(3, 3);
        public static Vector2 targetDirection;
        public static Vector2 targetCharacterDirection;
        public static Vector2 _mouseAbsolute;
        private static Vector2 _smoothMouse;

        /*/ FPS-style movement relative to a downward force.
        // This works fine, but downwards is always relative to the IVA itself.
        // Uses smooth mouselook from here: http://forum.unity3d.com/threads/a-free-simple-smooth-mouselook.73117/
        private void IvaRelativeOrientation()
        {
            // Allow the script to clamp based on a desired target value.
            var targetOrientation = Quaternion.Euler(targetDirection);

            if (!cameraPositionLocked)
            {
                // Get raw mouse input for a cleaner reading on more sensitive mice.
                Vector2 mouseDelta = new Vector2(-Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));

                // Scale input against the sensitivity setting and multiply that against the smoothing value.
                mouseDelta = Vector2.Scale(mouseDelta, new Vector2(sensitivity.x * smoothing.x, sensitivity.y * smoothing.y));

                // Interpolate mouse movement over time to apply smoothing delta.
                _smoothMouse.x = Mathf.Lerp(_smoothMouse.x, mouseDelta.x, 1f / smoothing.x);
                _smoothMouse.y = Mathf.Lerp(_smoothMouse.y, mouseDelta.y, 1f / smoothing.y);

                // Find the absolute mouse movement value from point zero.
                _mouseAbsolute += _smoothMouse;
            }

            // Clamp and apply the local x value first, so as not to be affected by world transforms.
            if (clampInDegrees.x < 360)
                _mouseAbsolute.x = Mathf.Clamp(_mouseAbsolute.x, -clampInDegrees.x * 0.5f, clampInDegrees.x * 0.5f);

            Quaternion xRotation = Quaternion.AngleAxis(-_mouseAbsolute.y, targetOrientation * Vector3.right);
            InternalCamera.Instance.transform.rotation = xRotation;
            
            // Then clamp and apply the global y value.
            if (clampInDegrees.y < 360)
                _mouseAbsolute.y = Mathf.Clamp(_mouseAbsolute.y, -clampInDegrees.y * 0.5f, clampInDegrees.y * 0.5f);

            InternalCamera.Instance.transform.rotation *= targetOrientation;

            Quaternion yRotation = Quaternion.AngleAxis(_mouseAbsolute.x, InternalCamera.Instance.transform.InverseTransformDirection(Vector3.forward));
            InternalCamera.Instance.transform.rotation *= yRotation;

            FlightCamera.fetch.transform.rotation = InternalSpace.InternalToWorld(InternalCamera.Instance.transform.rotation);
        }*/

        private void RelativeOrientation()
        {
            // Allow the script to clamp based on a desired target value.
            //var targetOrientation = Quaternion.Euler(targetDirection);

            if (!cameraPositionLocked)
            {
                // Get raw mouse input for a cleaner reading on more sensitive mice.
                Vector2 mouseDelta = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));

                // Scale input against the sensitivity setting and multiply that against the smoothing value.
                mouseDelta = Vector2.Scale(mouseDelta, new Vector2(sensitivity.x * smoothing.x, sensitivity.y * smoothing.y));

                // Interpolate mouse movement over time to apply smoothing delta.
                _smoothMouse.x = Mathf.Lerp(_smoothMouse.x, mouseDelta.x, 1f / smoothing.x);
                _smoothMouse.y = Mathf.Lerp(_smoothMouse.y, mouseDelta.y, 1f / smoothing.y);

                // Find the absolute mouse movement value from point zero.
                _mouseAbsolute += _smoothMouse;
            }
            Vector3 totalForce = KerbalColliderRigidbody.velocity; /*new Vector3(0f, 0f, -9.81f);*/ //GetFlightForces();
            var targetOrientation = Quaternion.Euler(totalForce);

            // Clamp and apply the local x value first, so as not to be affected by world transforms.
            if (clampInDegrees.x < 360)
                _mouseAbsolute.x = Mathf.Clamp(_mouseAbsolute.x, -clampInDegrees.x * 0.5f, clampInDegrees.x * 0.5f);

            // Then clamp and apply the global y value.
            if (clampInDegrees.y < 360)
                _mouseAbsolute.y = Mathf.Clamp(_mouseAbsolute.y, -clampInDegrees.y * 0.5f, clampInDegrees.y * 0.5f);

            //InternalCamera.Instance.transform.rotation *= targetOrientation;

            InternalCamera.Instance.transform.localRotation = InternalSpace.WorldToInternal(Quaternion.AngleAxis(-_mouseAbsolute.y, targetOrientation * Vector3.up/*right*/) * targetOrientation); // Pitch
            Quaternion yaw = Quaternion.AngleAxis(-_mouseAbsolute.x, /*-gForce);*/ InternalCamera.Instance.transform.InverseTransformDirection(totalForce));//Vector3.up));
            InternalCamera.Instance.transform.localRotation *= yaw;

            Quaternion roll = Quaternion.Euler(0, 0, 90);
            InternalCamera.Instance.transform.localRotation *= roll;

            FlightCamera.fetch.transform.rotation = InternalSpace.InternalToWorld(InternalCamera.Instance.transform.rotation);
        }

        private void UpdatePosition()
        {
            float moveForward = Input.GetKey(Settings.ForwardKey) ? Settings.ForwardSpeed * Time.deltaTime : 0;
            moveForward -= Input.GetKey(Settings.BackwardKey) ? Settings.ForwardSpeed * Time.deltaTime : 0;
            float moveRight = Input.GetKey(Settings.RightKey) ? Settings.HorizontalSpeed * Time.deltaTime : 0;
            moveRight -= Input.GetKey(Settings.LeftKey) ? Settings.HorizontalSpeed * Time.deltaTime : 0;
            float moveUp = Input.GetKey(Settings.UpKey) ? Settings.VerticalSpeed * Time.deltaTime : 0;
            moveUp -= Input.GetKey(Settings.DownKey) ? Settings.VerticalSpeed * Time.deltaTime : 0;
            Vector3 movement = new Vector3(moveRight, moveUp, moveForward);

            // Make the movement relative to the camera rotation.
            Vector3 newPos = KerbalCollider.transform.localPosition + (InternalCamera.Instance.transform.localRotation * movement);

            //KerbalCollider.rigidbody.velocity = new Vector3(0, 0, 0);
            KerbalCollider.GetComponentCached<Rigidbody>(ref KerbalColliderRigidbody);
            KerbalColliderRigidbody.MovePosition(newPos);

            // Jump. TODO: Detect when not in contact with the ground to prevent jetpacking.
            if (Input.GetKey(Settings.JumpKey))
                KerbalColliderRigidbody.AddForce(-InternalSpace.WorldToInternal(GetFlightForcesWorldSpace()) * Settings.JumpForce);

            FlightCamera.fetch.transform.position = InternalSpace.InternalToWorld(InternalCamera.Instance.transform.position);

#if Experimental
            // Move the world space collider.
            KerbalWorldSpaceCollider.GetComponentCached<Rigidbody>(ref KerbalWorldSpaceRigidbody);
            //KerbalWorldSpaceRigidbody.MovePosition(KerbalCollider.transform.localPosition);
            KerbalWorldSpaceRigidbody.MovePosition(InternalSpace.InternalToWorld(KerbalCollider.transform.localPosition));
#endif
        }

#if Experimental
        private static Transform _oldParent = null;
        private static Transform HeldItem = null;
        public static bool HoldingItem { get; private set; }
        public static void HoldItem(Transform t)
        {
            if (!CanHoldItems) return;

            if (HoldingItem)
                HeldItem.transform.parent = _oldParent;

            _oldParent = t.transform.parent;
            HeldItem = t;
            HeldItem.transform.parent = InternalCamera.Instance.transform;
            HoldingItem = true;
        }

        public static void DropHeldItem()
        {
            if (HeldItem != null && HeldItem.transform != null)
                HeldItem.transform.parent = _oldParent;
        }
#endif

        public void OnCollisionEnter(Collision collision)
        {
            //Debug.Log("# OnCollisionEnter " + name + " with " + collision.gameObject + " layer " + collision.gameObject.layer);
            ScreenMessages.PostScreenMessage("OnCollisionEnter " + name + " with " + collision.gameObject + " layer " + collision.gameObject.layer,
                1f, ScreenMessageStyle.LOWER_CENTER);
        }

        public void OnCollisionStay(Collision collision)
        {
            //Debug.Log("# OnCollisionStay " + collision.gameObject + " with " + collision.transform);
            ScreenMessages.PostScreenMessage("OnCollisionStay " + collision.gameObject + " with " + collision.transform + " layer " + collision.gameObject.layer,
                1f, ScreenMessageStyle.LOWER_CENTER);
        }

        public void OnCollisionExit(Collision collision)
        {
            //Debug.Log("# OnCollisionExit " + collision.gameObject + " with " + collision.transform);
            ScreenMessages.PostScreenMessage("OnCollisionExit " + collision.gameObject + " with " + collision.transform + " layer " + collision.gameObject.layer,
                1f, ScreenMessageStyle.LOWER_CENTER);
        }


        public static void HelmetOn()
        {
            WearingHelmet = true;
            KerbalCollider.transform.localScale = new Vector3(Settings.HelmetSize, Settings.HelmetSize, Settings.HelmetSize);
        }

        public static void HelmetOff()
        {
            WearingHelmet = false;
            KerbalCollider.transform.localScale = new Vector3(Settings.NoHelmetSize, Settings.NoHelmetSize, Settings.NoHelmetSize);
        }
    }
}

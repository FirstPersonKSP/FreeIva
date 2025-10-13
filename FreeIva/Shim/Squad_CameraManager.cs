using Expansions.Missions.Runtime;
using KSP.Localization;
using KSP.UI.Screens.Flight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static CameraManager;

namespace FreeIva
{
	static class CameraManagerExtensions
	{
		public static bool SetCameraIVA_Editor(this CameraManager cameraManager, Kerbal kerbal, bool resetCamera)
		{
			if (!MissionSystem.AllowCameraSwitch(CameraMode.IVA))
			{
				ScreenMessages.PostScreenMessage(Localizer.Format("#autoLOC_8003104"), 5f, ScreenMessageStyle.UPPER_CENTER);
				return false;
			}
			ICameras_DeactivateAll();
			if (cameraManager.ivaCameraActiveKerbal != null && cameraManager.ivaCameraActiveKerbal != kerbal)
			{
				ICameras_ResetAll();
				cameraManager.ivaCameraActiveKerbal.IVADisable();
			}
			FlightCamera.fetch.EnableCamera();
			FlightCamera.fetch.DeactivateUpdate();
			FlightCamera.fetch.gameObject.SetActive(value: true);
			CrewHatchController.fetch.DisableInterface();
			if (kerbal.protoCrewMember.seat != null)
			{
				Vector3 kerbalEyeOffset = kerbal.protoCrewMember.seat.kerbalEyeOffset;
				if (kerbal.protoCrewMember.gender == ProtoCrewMember.Gender.Female)
				{
					kerbalEyeOffset *= GameSettings.FEMALE_EYE_OFFSET_SCALE;
					kerbalEyeOffset.x += GameSettings.FEMALE_EYE_OFFSET_X;
					kerbalEyeOffset.y += GameSettings.FEMALE_EYE_OFFSET_Y;
					kerbalEyeOffset.z += GameSettings.FEMALE_EYE_OFFSET_Z;
				}
				kerbal.eyeTransform.localPosition = kerbal.eyeInitialPos + kerbalEyeOffset;
			}
			else
			{
				kerbal.eyeTransform.localPosition = kerbal.eyeInitialPos;
			}
			InternalCamera.Instance.SetTransform(kerbal.eyeTransform, resetCamera);
			InternalCamera.Instance.EnableCamera();
			cameraManager.ivaCameraActiveKerbal = kerbal;
			cameraManager.ivaCameraActiveKerbal.IVAEnable();
			//cameraManager.ivaCameraActiveKerbalIndex = FlightGlobals.fetch.activeVessel.GetVesselCrew().IndexOf(ivaCameraActiveKerbal.protoCrewMember);
			
			cameraManager.activeInternalPart = cameraManager.ivaCameraActiveKerbal.InPart;
			FlightGlobals.ActiveVessel.SetActiveInternalSpace(cameraManager.activeInternalPart);
			if (cameraManager.currentCameraMode != CameraMode.IVA)
			{
				cameraManager.previousCameraMode = cameraManager.currentCameraMode;
				cameraManager.currentCameraMode = CameraMode.IVA;
				GameEvents.OnCameraChange.Fire(CameraMode.IVA);
			}
			return true;
		}
	}
}

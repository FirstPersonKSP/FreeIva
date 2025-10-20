using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace FreeIva
{
	class SecondaryCamera : MonoBehaviour
	{
		// This is pretty weird, but it's hardcoded all over the place in the stock code
		// See FlightCamera.fovDefault or ScaledCamera.fovDefault
		public static float DefaultFOV = 60f;

		Camera[] m_cameras;

		void Start()
		{
			m_cameras = gameObject.GetComponentsInChildren<Camera>(true);
		}

		private void LateUpdate()
		{
			Vector3 cameraWorldPosition = InternalSpace.InternalToWorld(InternalCamera.Instance.transform.position);
			Quaternion cameraWorldRotation = InternalSpace.InternalToWorld(InternalCamera.Instance.transform.rotation);

			transform.SetPositionAndRotation(cameraWorldPosition, cameraWorldRotation);

			foreach (var camera in m_cameras)
			{
				camera.fieldOfView = InternalCamera.Instance._camera.fieldOfView;
			}
		}

		void OnDestroy()
		{
			foreach (var camera in m_cameras)
			{
				camera.fieldOfView = DefaultFOV;
			}
		}
	}
}

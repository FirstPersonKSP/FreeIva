using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace FreeIva.InternalModules
{
	// TODO: limit rotation to specific axes
	// TODO: provide angle limits for rotation
	// TODO: smoothing
	// TODO: return to neutral when weightless?

	internal class GravityGimbal : InternalModule
	{
		[KSPField]
		public Vector3 rotationAxis = new Vector3(1, 0, 0); // in prop space

		[KSPField]
		public string transformName = string.Empty;

		[KSPField]
		public float minAccel = 0.05f;

		[KSPField]
		public float smoothingFactor = 0.2f;

		[SerializeField] Transform m_controlledTransform;

		InternalModuleFreeIva m_freeIvaModule;
		Vector3 m_rotationAxisInternalSpace;
		Quaternion m_defaultRotation;

		public override void OnLoad(ConfigNode node)
		{
			base.OnLoad(node);

			if (HighLogic.LoadedScene == GameScenes.LOADING)
			{
				if (transformName != string.Empty)
				{
					m_controlledTransform = TransformUtil.FindPropTransform(internalProp, transformName);
				}
				else
				{
					m_controlledTransform = internalProp.hasModel ? transform : internalModel.transform;
				}
			}
		}

		protected void Start()
		{
			m_freeIvaModule = InternalModuleFreeIva.GetForModel(internalModel);
			m_rotationAxisInternalSpace = transform.TransformDirection(rotationAxis);
			m_defaultRotation = transform.rotation;
		}

		void FixedUpdate()
		{
			if (m_controlledTransform != null)
			{
				Vector3 subjectiveGravity = FreeIva.GetInternalSubjectiveAcceleration(m_freeIvaModule, m_controlledTransform.position);
				Quaternion targetRotation;

				if (KerbalIvaController.UseHorizon(subjectiveGravity, m_freeIvaModule.Centrifuge != null))
				{
					Vector3 forward = Vector3.Cross(subjectiveGravity, m_rotationAxisInternalSpace);
					Vector3 up = Vector3.Cross(forward, m_rotationAxisInternalSpace);

					targetRotation = Quaternion.LookRotation(forward, up);
				}
				else
				{
					targetRotation = m_defaultRotation;
				}

				m_controlledTransform.rotation = Quaternion.Lerp(m_controlledTransform.rotation, targetRotation, smoothingFactor);
			}
		}
	}
}

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
		public Vector3 upAxis = new Vector3(0, 1, 0);

		[KSPField]
		public string transformName = string.Empty;

		[SerializeField]
		public Transform controlledTransform;

		InternalModuleFreeIva m_freeIvaModule;
		Quaternion m_defaultRotation;

		public override void OnLoad(ConfigNode node)
		{
			base.OnLoad(node);

			if (HighLogic.LoadedScene == GameScenes.LOADING)
			{
				if (transformName != string.Empty)
				{
					controlledTransform = TransformUtil.FindPropTransform(internalProp, transformName);
				}
				else
				{
					controlledTransform = internalProp.hasModel ? transform : internalModel.transform;
				}
			}
		}

		protected void Start()
		{
			m_defaultRotation = controlledTransform?.rotation ?? Quaternion.identity;
			m_freeIvaModule = InternalModuleFreeIva.GetForModel(internalModel);
		}

		void FixedUpdate()
		{
			if (controlledTransform != null)
			{
				// TODO: use upAxis 
				Vector3 subjectiveGravity = FreeIva.GetInternalSubjectiveAcceleration(m_freeIvaModule, controlledTransform.position);
				controlledTransform.up = -subjectiveGravity;
			}
		}
	}
}

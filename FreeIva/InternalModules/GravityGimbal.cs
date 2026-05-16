using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace FreeIva.InternalModules
{
	internal class GravityGimbal : InternalModule
	{
		[KSPField]
		public Vector3 upAxis = new Vector3(0, 1, 0);

		[KSPField]
		public string transformName = string.Empty;

		[SerializeField]
		public Transform controlledTransform;

		InternalModuleFreeIva m_freeIvaModule;

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
					controlledTransform = internalProp.hasModel ? controlledTransform : internalModel.transform;
				}
			}
		}

		protected void Start()
		{
			m_freeIvaModule = InternalModuleFreeIva.GetForModel(internalModel);
		}

		public override void OnFixedUpdate()
		{
			base.OnFixedUpdate();

			if (controlledTransform != null)
			{
				Vector3 subjectiveGravity = FreeIva.GetInternalSubjectiveAcceleration(m_freeIvaModule, controlledTransform.position);
				controlledTransform.up = -subjectiveGravity;
			}
		}
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace FreeIva
{
	public class SSPX_ModuleDeployableCentrifuge : SSPX_ModuleDeployableHabitat, ICentrifuge
	{
		#region static

		static Type x_ModuleDeployableCentrifugeType;
		static FieldInfo x_CurrentSpinRateFieldInfo;
		static FieldInfo x_IVARotationRootFieldInfo;
		static FieldInfo x_propDictFieldInfo;
		static MethodInfo x_ResetIVATransformMethodInfo;
		static MethodInfo x_DoIVASetupMethodInfo;


		static SSPX_ModuleDeployableCentrifuge()
		{
			x_ModuleDeployableCentrifugeType = AssemblyLoader.GetClassByName(typeof(PartModule), "ModuleDeployableCentrifuge");

			if (x_ModuleDeployableCentrifugeType == null) return;

			x_CurrentSpinRateFieldInfo = x_ModuleDeployableCentrifugeType.GetField("CurrentSpinRate", BindingFlags.Instance | BindingFlags.Public);
			x_IVARotationRootFieldInfo = x_ModuleDeployableCentrifugeType.GetField("IVARotationRoot", BindingFlags.Instance | BindingFlags.NonPublic);
			x_propDictFieldInfo = x_ModuleDeployableCentrifugeType.GetField("propDict", BindingFlags.Instance | BindingFlags.NonPublic);
			x_ResetIVATransformMethodInfo = x_ModuleDeployableCentrifugeType.GetMethod("ResetIVATransform", BindingFlags.Instance | BindingFlags.Public);
			x_DoIVASetupMethodInfo = x_ModuleDeployableCentrifugeType.GetMethod("DoIVASetup", BindingFlags.Instance | BindingFlags.NonPublic);
		}

		public static new SSPX_ModuleDeployableCentrifuge Create(Part part)
		{
			PartModule centrifugeModule = part.GetModule(x_ModuleDeployableCentrifugeType);
			if (centrifugeModule == null) return null;
			return new SSPX_ModuleDeployableCentrifuge(centrifugeModule);
		}

		#endregion

		SSPX_ModuleDeployableCentrifuge(PartModule module) : base(module)
		{
		}

		new public void OnInternalCreated()
		{
			// replace IVARotationRoot with the internal model transform
			var ivaRotationRoot = (Transform)x_IVARotationRootFieldInfo.GetValue(m_partModule);

			if (ivaRotationRoot == null)
			{
				x_DoIVASetupMethodInfo.Invoke(m_partModule, null);
				ivaRotationRoot = (Transform)x_IVARotationRootFieldInfo.GetValue(m_partModule);
			}

			m_rotationRoot = m_partModule.part.internalModel.FindModelTransform("model");

			if (m_rotationRoot != ivaRotationRoot)
			{
				GameObject.Destroy(ivaRotationRoot.gameObject);
			}

			x_IVARotationRootFieldInfo.SetValue(m_partModule, m_rotationRoot);

			// attach all the props to the rotation root
			foreach (var prop in m_partModule.part.internalModel.props)
			{
				prop.transform.SetParent(m_rotationRoot, true);
			}

			// clear the propDict so that the SSPX module doesn't mess with the prop transforms
			var propDict = (Dictionary<Transform, Transform>)x_propDictFieldInfo.GetValue(m_partModule);
			foreach (var proxy in propDict.Keys)
			{
				GameObject.Destroy(proxy.gameObject);
			}
			propDict.Clear();

			// unhook the event handler so SSPX doesn't redo the IVA setup every time crew is transferred
			var resetIVATransformDelegate = (EventData<GameEvents.HostedFromToAction<ProtoCrewMember, Part>>.OnEvent)x_ResetIVATransformMethodInfo.CreateDelegate(typeof(EventData<GameEvents.HostedFromToAction<ProtoCrewMember, Part>>.OnEvent), m_partModule);
			GameEvents.onCrewTransferred.Remove(resetIVATransformDelegate);
		}

		Transform m_rotationRoot;

		public void Update() { }

		public float CurrentSpinRate
		{
			get { return (float)x_CurrentSpinRateFieldInfo.GetValue(m_partModule); }
		}

		public Transform IVARotationRoot
		{
			get { return m_rotationRoot; }
		}
	}
}

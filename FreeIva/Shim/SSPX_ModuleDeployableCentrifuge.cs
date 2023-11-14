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

		static TypeInfo x_ModuleDeployableCentrifugeTypeInfo;
		static FieldInfo x_CurrentSpinRateFieldInfo;
		static FieldInfo x_IVARotationRootFieldInfo;
		static FieldInfo x_propDictFieldInfo;
		static MethodInfo x_ResetIVATransformMethodInfo;
		static MethodInfo x_DoIVASetupMethodInfo;


		static SSPX_ModuleDeployableCentrifuge()
		{
			var type = AssemblyLoader.GetClassByName(typeof(PartModule), "ModuleDeployableCentrifuge");

			if (type == null) return;

			x_ModuleDeployableCentrifugeTypeInfo = type.GetTypeInfo();
			x_CurrentSpinRateFieldInfo = x_ModuleDeployableCentrifugeTypeInfo.GetField("CurrentSpinRate", BindingFlags.Instance | BindingFlags.Public);
			x_IVARotationRootFieldInfo = x_ModuleDeployableCentrifugeTypeInfo.GetField("IVARotationRoot", BindingFlags.Instance | BindingFlags.NonPublic);
			x_propDictFieldInfo = x_ModuleDeployableCentrifugeTypeInfo.GetField("propDict", BindingFlags.Instance | BindingFlags.NonPublic);
			x_ResetIVATransformMethodInfo = x_ModuleDeployableCentrifugeTypeInfo.GetMethod("ResetIVATransform", BindingFlags.Instance | BindingFlags.Public);
			x_DoIVASetupMethodInfo = x_ModuleDeployableCentrifugeTypeInfo.GetMethod("DoIVASetup", BindingFlags.Instance | BindingFlags.NonPublic);
		}

		public static new SSPX_ModuleDeployableCentrifuge Create(Part part)
		{
			if (x_ModuleDeployableCentrifugeTypeInfo == null) return null;

			PartModule module = null;
			foreach (var m in part.modules.modules)
			{
				if (m.GetType() == x_ModuleDeployableCentrifugeTypeInfo.AsType())
				{
					module = m;
					break;
				}
			}

			if (module == null) return null;

			return new SSPX_ModuleDeployableCentrifuge(module);
		}

		#endregion

		SSPX_ModuleDeployableCentrifuge(PartModule module) : base(module)
		{
			// replace IVARotationRoot with the internal model transform
			var ivaRotationRoot = (Transform)x_IVARotationRootFieldInfo.GetValue(module);

			if (ivaRotationRoot == null)
			{
				x_DoIVASetupMethodInfo.Invoke(module, null);
				ivaRotationRoot = (Transform)x_IVARotationRootFieldInfo.GetValue(module);
			}

			m_rotationRoot = module.part.internalModel.FindModelTransform("model");

			if (m_rotationRoot != ivaRotationRoot)
			{
				GameObject.Destroy(ivaRotationRoot.gameObject);
			}
			
			x_IVARotationRootFieldInfo.SetValue(m_partModule, m_rotationRoot);

			// attach all the props to the rotation root
			foreach (var prop in module.part.internalModel.props)
			{
				prop.transform.SetParent(m_rotationRoot, true);
			}

			// clear the propDict so that the SSPX module doesn't mess with the prop transforms
			var propDict = (Dictionary<Transform, Transform>)x_propDictFieldInfo.GetValue(module);
			foreach (var proxy in propDict.Keys)
			{
				GameObject.Destroy(proxy.gameObject);
			}
			propDict.Clear();

			// unhook the event handler so SSPX doesn't redo the IVA setup every time crew is transferred
			var resetIVATransformDelegate = (EventData<GameEvents.HostedFromToAction<ProtoCrewMember, Part>>.OnEvent)x_ResetIVATransformMethodInfo.CreateDelegate(typeof(EventData<GameEvents.HostedFromToAction<ProtoCrewMember, Part>>.OnEvent), module);
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

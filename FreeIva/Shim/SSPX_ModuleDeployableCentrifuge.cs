using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace FreeIva
{
	public class SSPX_ModuleDeployableCentrifuge : Centrifuge
	{
		#region static

		static TypeInfo x_ModuleDeployableCentrifugeTypeInfo;
		static FieldInfo x_CurrentSpinRateFieldInfo;
		static FieldInfo x_IVARotationRootFieldInfo;

		static SSPX_ModuleDeployableCentrifuge()
		{
			var type = AssemblyLoader.GetClassByName(typeof(PartModule), "ModuleDeployableCentrifuge");

			if (type == null) return;

			x_ModuleDeployableCentrifugeTypeInfo = type.GetTypeInfo();
			x_CurrentSpinRateFieldInfo = x_ModuleDeployableCentrifugeTypeInfo.GetField("CurrentSpinRate", BindingFlags.Instance | BindingFlags.Public);
			x_IVARotationRootFieldInfo = x_ModuleDeployableCentrifugeTypeInfo.GetField("IVARotationRoot", BindingFlags.Instance | BindingFlags.NonPublic);
		}

		public new static SSPX_ModuleDeployableCentrifuge Create(Part part)
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

			var result = new SSPX_ModuleDeployableCentrifuge();
			result.m_moduleDeployableCentrifuge = module;
			return result;
		}

		#endregion

		PartModule m_moduleDeployableCentrifuge;

		public override float CurrentSpinRate
		{
			get { return (float)x_CurrentSpinRateFieldInfo.GetValue(m_moduleDeployableCentrifuge); }
		}

		public override Transform IVARotationRoot
		{
			get { return (Transform)x_IVARotationRootFieldInfo.GetValue(m_moduleDeployableCentrifuge); }
		}
	}
}

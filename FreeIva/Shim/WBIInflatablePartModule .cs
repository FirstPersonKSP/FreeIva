using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace FreeIva.Shim
{
	internal class WBIInflatablePartModule : IDeployable
	{
		#region static

		static TypeInfo x_WBIInflatablePartModule_TypeInfo;
		static FieldInfo x_isDeployed_FieldInfo;

		static WBIInflatablePartModule()
		{
			x_WBIInflatablePartModule_TypeInfo = AssemblyLoader.GetClassByName(typeof(PartModule), "WBIInflatablePartModule")?.GetTypeInfo();
			
			if (x_WBIInflatablePartModule_TypeInfo == null) return;

			x_isDeployed_FieldInfo = x_WBIInflatablePartModule_TypeInfo.GetField("isDeployed", BindingFlags.Instance | BindingFlags.Public);
		}

		public static WBIInflatablePartModule Create(Part part)
		{
			if (x_isDeployed_FieldInfo == null) return null;

			PartModule module = null;
			foreach (var m in part.modules.modules)
			{
				if (m.GetType() == x_WBIInflatablePartModule_TypeInfo)
				{
					module = m;
					break;
				}
			}

			if (module == null) return null;

			return new WBIInflatablePartModule(module);
		}

		#endregion

		PartModule m_module;

		WBIInflatablePartModule(PartModule module)
		{
			m_module = module;
		}

		public bool IsDeployed
		{
			get
			{
				object valueObj = x_isDeployed_FieldInfo.GetValue(m_module);
				return (bool)Convert.ToBoolean(valueObj);
			}
		}
	}
}

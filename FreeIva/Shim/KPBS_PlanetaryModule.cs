using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace FreeIva
{
	public class KPBS_PlanetaryModule : IDeployable
	{
		#region static

		static TypeInfo x_PlanetaryModuleTypeInfo;
		static FieldInfo x_moduleStatusFieldInfo;

		static KPBS_PlanetaryModule()
		{
			var type = AssemblyLoader.GetClassByName(typeof(PartModule), "PlanetaryModule");

			if (type == null) return;

			x_PlanetaryModuleTypeInfo = type.GetTypeInfo();
			x_moduleStatusFieldInfo = x_PlanetaryModuleTypeInfo.GetField("moduleStatus", BindingFlags.Instance | BindingFlags.Public);
		}

		public static KPBS_PlanetaryModule Create(Part part)
		{
			if (x_PlanetaryModuleTypeInfo == null) return null;

			PartModule module = null;
			foreach (var m in part.modules.modules)
			{
				if (m.GetType() == x_PlanetaryModuleTypeInfo.AsType())
				{
					module = m;
					break;
				}
			}

			if (module == null) return null;

			return new KPBS_PlanetaryModule(module);
		}

		#endregion

		PartModule m_planetaryModule;
		public KPBS_PlanetaryModule(PartModule module)
		{
			m_planetaryModule = module;
		}

		public void OnInternalCreated() { }

		public bool IsDeployed
		{
			get
			{
				object valueObj = x_moduleStatusFieldInfo.GetValue(m_planetaryModule);
				int value = (int)Convert.ChangeType(valueObj, typeof(int));
				return value == 2;
			}
		}
	}
}

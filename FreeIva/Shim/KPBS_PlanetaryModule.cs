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

		static Type x_PlanetaryModuleType;
		static FieldInfo x_moduleStatusFieldInfo;

		static KPBS_PlanetaryModule()
		{
			x_PlanetaryModuleType = AssemblyLoader.GetClassByName(typeof(PartModule), "PlanetaryModule");

			if (x_PlanetaryModuleType == null) return;

			x_moduleStatusFieldInfo = x_PlanetaryModuleType.GetField("moduleStatus", BindingFlags.Instance | BindingFlags.Public);
		}

		public static KPBS_PlanetaryModule Create(Part part)
		{
			PartModule planetaryModule = part.GetModule(x_PlanetaryModuleType);
			
			if (planetaryModule == null) return null;

			return new KPBS_PlanetaryModule(planetaryModule);
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

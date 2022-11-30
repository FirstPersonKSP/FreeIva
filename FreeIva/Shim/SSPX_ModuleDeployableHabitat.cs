using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace FreeIva
{
	internal class SSPX_ModuleDeployableHabitat
	{
		#region static

		static TypeInfo x_ModuleDeployableHabitatTypeInfo;
		static FieldInfo x_deployStateFieldInfo;

		static SSPX_ModuleDeployableHabitat()
		{
			 var type = AssemblyLoader.GetClassByName(typeof(PartModule), "ModuleDeployableHabitat");

			if (type == null) return;

			x_ModuleDeployableHabitatTypeInfo = type.GetTypeInfo();
			x_deployStateFieldInfo = x_ModuleDeployableHabitatTypeInfo.GetField("deployState", BindingFlags.Instance | BindingFlags.NonPublic);			
		}

		public static SSPX_ModuleDeployableHabitat Create(Part part)
		{
			if (x_ModuleDeployableHabitatTypeInfo == null) return null;

			PartModule module = null;
			foreach (var m in part.modules.modules)
			{
				if (m.GetType().IsSubclassOf(x_ModuleDeployableHabitatTypeInfo.AsType()))
				{
					module = m;
					break;
				}
			}

			if (module == null) return null;

			var result = new SSPX_ModuleDeployableHabitat();
			result.m_moduleDeployableHabitat = module;
			return result;
		}

		#endregion

		PartModule m_moduleDeployableHabitat;

		public bool IsDeployed
		{
			get
			{
				object valueObj = x_deployStateFieldInfo.GetValue(m_moduleDeployableHabitat);
				int value = (int)Convert.ChangeType(valueObj, typeof(int));
				return value == 2;
			}
		}
	}
}

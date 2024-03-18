using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace FreeIva
{
	public class SSPX_ModuleDeployableHabitat : IDeployable
	{
		#region static

		static Type x_ModuleDeployableHabitatType;
		static FieldInfo x_deployStateFieldInfo;

		static SSPX_ModuleDeployableHabitat()
		{
			x_ModuleDeployableHabitatType = AssemblyLoader.GetClassByName(typeof(PartModule), "ModuleDeployableHabitat");

			if (x_ModuleDeployableHabitatType == null) return;

			x_deployStateFieldInfo = x_ModuleDeployableHabitatType.GetField("deployState", BindingFlags.Instance | BindingFlags.NonPublic);			
		}

		public static SSPX_ModuleDeployableHabitat Create(Part part)
		{
			PartModule habitatModule = part.GetModule(x_ModuleDeployableHabitatType);
			if (habitatModule == null) return null;
			return new SSPX_ModuleDeployableHabitat(habitatModule);
		}

		#endregion

		protected SSPX_ModuleDeployableHabitat(PartModule module)
		{
			m_partModule = module;
		}

		protected PartModule m_partModule;

		public void OnInternalCreated() { }

		public bool IsDeployed
		{
			get
			{
				object valueObj = x_deployStateFieldInfo.GetValue(m_partModule);
				int value = (int)Convert.ChangeType(valueObj, typeof(int));
				return value == 2;
			}
		}
	}
}

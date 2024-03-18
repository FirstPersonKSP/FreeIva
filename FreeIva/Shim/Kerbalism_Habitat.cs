using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace FreeIva
{
	internal class Kerbalism_Habitat : IDeployable
	{
		#region static

		static Type x_HabitatModuleType;
		static FieldInfo x_stateField;

		static Kerbalism_Habitat()
		{
			x_HabitatModuleType = AssemblyLoader.GetClassByName(typeof(PartModule), "Habitat");

			if (x_HabitatModuleType == null) return;

			x_stateField = x_HabitatModuleType.GetField("state", BindingFlags.Instance | BindingFlags.Public);
		}

		public static Kerbalism_Habitat Create(Part part)
		{
			var habitatModule = part.GetModule(x_HabitatModuleType);

			if (habitatModule == null) return null;

			return new Kerbalism_Habitat(habitatModule);
		}

		#endregion

		PartModule m_habitatModule;

		Kerbalism_Habitat(PartModule habitatModule)
		{
			m_habitatModule = habitatModule;
		}

		public void OnInternalCreated() { }

		public bool IsDeployed
		{
			get
			{
				object valueObj = x_stateField.GetValue(m_habitatModule);
				int value = (int)Convert.ToInt32(valueObj);
				return value == 1; // disabled, enabled, pressurizing, depressurizing
			}
		}
	}
}

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

		static TypeInfo x_HabitatTypeInfo;
		static FieldInfo x_stateField;

		static Kerbalism_Habitat()
		{
			x_HabitatTypeInfo = AssemblyLoader.GetClassByName(typeof(PartModule), "Habitat")?.GetTypeInfo();

			if (x_HabitatTypeInfo == null) return;

			x_stateField = x_HabitatTypeInfo.GetField("state", BindingFlags.Instance | BindingFlags.Public);
		}

		public static Kerbalism_Habitat Create(Part part)
		{
			if (x_HabitatTypeInfo == null) return null;

			PartModule module = null;
			foreach (var m in part.modules.modules)
			{
				if (m.GetType() == x_HabitatTypeInfo.AsType())
				{
					module = m;
					break;
				}
			}

			if (module == null) return null;

			return new Kerbalism_Habitat(module);
		}

		#endregion

		PartModule m_module;

		Kerbalism_Habitat(PartModule module)
		{
			m_module = module;
		}

		public void OnInternalCreated() { }

		public bool IsDeployed
		{
			get
			{
				object valueObj = x_stateField.GetValue(m_module);
				int value = (int)Convert.ToInt32(valueObj);
				return value == 1; // disabled, enabled, pressurizing, depressurizing
			}
		}
	}
}

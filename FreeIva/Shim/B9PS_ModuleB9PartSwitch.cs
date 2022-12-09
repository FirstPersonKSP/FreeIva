using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace FreeIva
{
	internal class B9PS_ModuleB9PartSwitch
	{
		#region Static

		static TypeInfo x_ModuleB9PPartSwitchTypeInfo;
		static FieldInfo x_moduleIDFieldInfo;
		static PropertyInfo x_CurrentSubtypeNamePropertyInfo;

		static B9PS_ModuleB9PartSwitch()
		{
			var type = AssemblyLoader.GetClassByName(typeof(PartModule), "ModuleB9PartSwitch");
			if (type == null) return;
			x_ModuleB9PPartSwitchTypeInfo = type.GetTypeInfo();
			x_CurrentSubtypeNamePropertyInfo = x_ModuleB9PPartSwitchTypeInfo.GetProperty("CurrentSubtypeName", BindingFlags.Instance | BindingFlags.Public);
			x_moduleIDFieldInfo = x_ModuleB9PPartSwitchTypeInfo.GetField("moduleID", BindingFlags.Instance | BindingFlags.Public);
		}

		public static B9PS_ModuleB9PartSwitch Create(Part part, string moduleID)
		{
			if (x_ModuleB9PPartSwitchTypeInfo == null) return null;

			PartModule module = null;
			foreach (var m in part.modules.modules)
			{
				if (m.GetType() == x_ModuleB9PPartSwitchTypeInfo.AsType() && (string)x_moduleIDFieldInfo.GetValue(m) == moduleID)
				{
					module = m;
					break;
				}
			}

			if (module == null) return null;

			return new B9PS_ModuleB9PartSwitch(module);
		}

		#endregion

		PartModule m_moduleB9PartSwitch;

		B9PS_ModuleB9PartSwitch(PartModule module)
		{
			m_moduleB9PartSwitch = module;
		}

		public string CurrentSubtypeName()
		{
			return (string)x_CurrentSubtypeNamePropertyInfo.GetValue(m_moduleB9PartSwitch);
		}
	}
}

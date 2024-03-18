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

		static Type x_WBIInflatablePartModule_Type;
		static FieldInfo x_isDeployed_FieldInfo;

		static WBIInflatablePartModule()
		{
			x_WBIInflatablePartModule_Type = AssemblyLoader.GetClassByName(typeof(PartModule), "WBIInflatablePartModule");
			
			if (x_WBIInflatablePartModule_Type == null) return;

			x_isDeployed_FieldInfo = x_WBIInflatablePartModule_Type.GetField("isDeployed", BindingFlags.Instance | BindingFlags.Public);
		}

		public static WBIInflatablePartModule Create(Part part)
		{
			PartModule module = part.GetModule(x_WBIInflatablePartModule_Type);
			if (module == null) return null;
			return new WBIInflatablePartModule(module);
		}

		#endregion

		PartModule m_module;

		WBIInflatablePartModule(PartModule module)
		{
			m_module = module;
		}

		public void OnInternalCreated() { }

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

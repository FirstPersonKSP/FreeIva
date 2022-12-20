using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace FreeIva
{
	internal class Kerbalism_Animator
	{
		#region static

		static TypeInfo x_AnimatorTypeInfo;
		static MethodInfo x_PlayingMethodInfo;

		static Kerbalism_Animator()
		{
			var kerbalismAssemly = AssemblyLoader.loadedAssemblies.FirstOrDefault(a => a.assembly.GetName().Name == "Kerbalism");
			if (kerbalismAssemly == null) return;

			x_AnimatorTypeInfo = kerbalismAssemly.assembly.GetType("KERBALISM.Animator").GetTypeInfo();
			x_PlayingMethodInfo = x_AnimatorTypeInfo.GetMethod("Playing", BindingFlags.Instance | BindingFlags.Public);
		}

		#endregion

		object m_obj;
		public Kerbalism_Animator(object obj)
		{
			m_obj = obj;
		}

		public bool Playing()
		{
			return (bool)x_PlayingMethodInfo.Invoke(m_obj, null);
		}
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace FreeIva
{
	internal class Kerbalism_Animator
	{
		#region static

		static TypeInfo x_AnimatorTypeInfo;
		static MethodInfo x_PlayingMethodInfo;
		static FieldInfo x_animFieldInfo;

		static Kerbalism_Animator()
		{
			var kerbalismAssemly = AssemblyLoader.loadedAssemblies.FirstOrDefault(a => a.assembly.GetName().Name == "Kerbalism");
			if (kerbalismAssemly == null) return;

			x_AnimatorTypeInfo = kerbalismAssemly.assembly.GetType("KERBALISM.Animator").GetTypeInfo();
			x_PlayingMethodInfo = x_AnimatorTypeInfo.GetMethod("Playing", BindingFlags.Instance | BindingFlags.Public);
			x_animFieldInfo = x_AnimatorTypeInfo.GetField("anim", BindingFlags.Instance | BindingFlags.NonPublic);
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

		public Animation anim
		{
			get { return (Animation)x_animFieldInfo.GetValue(m_obj); }
		}
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace FreeIva
{
	internal class Kerbalism_Transformator
	{
		static TypeInfo x_TransformatorTypeInfo;
		static FieldInfo x_rotate_ivaFieldInfo;
		static FieldInfo x_transformFieldInfo;
		static FieldInfo x_CurrentSpinRateFieldInfo;

		static Kerbalism_Transformator()
		{
			var kerbalismAssemly = AssemblyLoader.loadedAssemblies.FirstOrDefault(a => a.assembly.GetName().Name == "Kerbalism");
			if (kerbalismAssemly == null) return;
			x_TransformatorTypeInfo = kerbalismAssemly.assembly.GetType("KERBALISM.Transformator").GetTypeInfo();
			x_rotate_ivaFieldInfo = x_TransformatorTypeInfo.GetField("rotate_iva", BindingFlags.Instance | BindingFlags.NonPublic);
			x_transformFieldInfo = x_TransformatorTypeInfo.GetField("transform", BindingFlags.Instance | BindingFlags.NonPublic);
			x_CurrentSpinRateFieldInfo = x_TransformatorTypeInfo.GetField("CurrentSpinRate", BindingFlags.Instance | BindingFlags.NonPublic);
		}

		object m_object;

		public Kerbalism_Transformator(object obj)
		{
			m_object = obj;
		}

		public Transform transform
		{
			get { return (Transform)x_transformFieldInfo.GetValue(m_object); }
			set { x_transformFieldInfo.SetValue(m_object, value); }
		}

		public bool rotate_iva
		{
			get { return (bool)x_rotate_ivaFieldInfo.GetValue(m_object); }
			set { x_rotate_ivaFieldInfo.SetValue(m_object, value); }
		}

		public float CurrentSpinRate
		{
			get { return (float)x_CurrentSpinRateFieldInfo.GetValue(m_object); }
			set { x_CurrentSpinRateFieldInfo.SetValue(m_object, value); }
		}
	}
}

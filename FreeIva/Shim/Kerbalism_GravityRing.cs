using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace FreeIva
{
	internal class Kerbalism_GravityRing : ICentrifuge
	{
		#region static

		static TypeInfo x_GravityRingTypeInfo;
		static FieldInfo x_rotate_transfFieldInfo;
		static FieldInfo x_deployedFieldInfo;
		static FieldInfo x_deploy_animFieldInfo;

		static Kerbalism_GravityRing()
		{
			var type = AssemblyLoader.GetClassByName(typeof(PartModule), "GravityRing");

			if (type == null) return;

			x_GravityRingTypeInfo = type.GetTypeInfo();
			x_rotate_transfFieldInfo = x_GravityRingTypeInfo.GetField("rotate_transf", BindingFlags.Instance | BindingFlags.Public);
			x_deployedFieldInfo = x_GravityRingTypeInfo.GetField("deployed", BindingFlags.Instance | BindingFlags.Public);
			x_deploy_animFieldInfo = x_GravityRingTypeInfo.GetField("deploy_anim", BindingFlags.Instance | BindingFlags.Public);
		}

		public static Kerbalism_GravityRing Create(Part part)
		{
			if (x_GravityRingTypeInfo == null) return null;

			PartModule module = null;
			foreach (var m in part.modules.modules)
			{
				if (m.GetType() == x_GravityRingTypeInfo.AsType())
				{
					module = m;
					break;
				}
			}

			if (module == null) return null;

			return new Kerbalism_GravityRing(module);
		}

		#endregion

		PartModule m_module;
		Kerbalism_Transformator m_transformator;

		static Quaternion partToInternalRotation = Quaternion.Euler(-90, 0, 0);
		static Quaternion postRotation = Quaternion.Euler(180, 0, 180);

		public Kerbalism_GravityRing(PartModule module)
		{
			m_module = module;
			m_transformator = new Kerbalism_Transformator(x_rotate_transfFieldInfo.GetValue(m_module));

			m_transformator.rotate_iva = false;
		}

		public void Update()
		{
			m_module.part.internalModel.transform.localRotation = partToInternalRotation * m_transformator.transform.localRotation * postRotation;
		}

		public float CurrentSpinRate
		{
			get { return m_transformator.CurrentSpinRate; }
		}

		public Transform IVARotationRoot => m_module.part.internalModel.transform;
	}
}

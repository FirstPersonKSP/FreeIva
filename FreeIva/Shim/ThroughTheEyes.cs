using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace FreeIva
{
	internal static class ThroughTheEyes
	{
		static TypeInfo x_FirstPersonEVA_TypeInfo;
		static FieldInfo x_FirstPersonEVA_instance_FieldInfo;
		static FieldInfo x_FirstPersonEVA_fpCameraManager_FieldInfo;

		static TypeInfo x_FirstPersonCameraManager_TypeInfo;
		static FieldInfo x_FirstPersonCameraManager_isFirstPerson_FieldInfo;
		static MethodInfo x_FirstPersonCameraManager_CheckAndSetFirstPerson_MethodInfo;

		static ThroughTheEyes()
		{
			var tteAssembly = AssemblyLoader.loadedAssemblies.FirstOrDefault(a => a.assembly.GetName().Name == "ThroughTheEyes");

			if (tteAssembly == null) return;

			x_FirstPersonEVA_TypeInfo = tteAssembly.assembly.GetType("FirstPerson.FirstPersonEVA").GetTypeInfo();
			x_FirstPersonEVA_instance_FieldInfo = x_FirstPersonEVA_TypeInfo.GetField("instance", BindingFlags.Static | BindingFlags.Public);
			x_FirstPersonEVA_fpCameraManager_FieldInfo = x_FirstPersonEVA_TypeInfo.GetField("fpCameraManager", BindingFlags.Instance | BindingFlags.Public);

			x_FirstPersonCameraManager_TypeInfo = tteAssembly.assembly.GetType("FirstPerson.FirstPersonCameraManager").GetTypeInfo();
			x_FirstPersonCameraManager_isFirstPerson_FieldInfo = x_FirstPersonCameraManager_TypeInfo.GetField("isFirstPerson", BindingFlags.Instance | BindingFlags.Public);
			x_FirstPersonCameraManager_CheckAndSetFirstPerson_MethodInfo = x_FirstPersonCameraManager_TypeInfo.GetMethod("CheckAndSetFirstPerson", BindingFlags.Instance | BindingFlags.Public);
		}

		public static void EnterFirstPerson()
		{
			if (x_FirstPersonCameraManager_CheckAndSetFirstPerson_MethodInfo == null) return;

			var fpInstance = x_FirstPersonEVA_instance_FieldInfo.GetValue(null);
			var fpCameraManager = x_FirstPersonEVA_fpCameraManager_FieldInfo.GetValue(fpInstance);

			x_FirstPersonCameraManager_isFirstPerson_FieldInfo.SetValue(fpCameraManager, false);
			x_FirstPersonCameraManager_CheckAndSetFirstPerson_MethodInfo.Invoke(fpCameraManager, new object[] { FlightGlobals.ActiveVessel });
		}
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace FreeIva
{
	public interface IDeployable
	{
		bool IsDeployed { get; }
	}

	public static class DeployableFactory
	{
		// TODO: this is a half-measure; really we should be creating this once per InternalModuleFreeIva that needs it
		public static IDeployable Create(Part part, string requiredAnimationName)
		{
			return 
				(IDeployable)Squad_ModuleAnimateGeneric.Create(part, requiredAnimationName) ?? 
				(IDeployable)SSPX_ModuleDeployableHabitat.Create(part) ?? 
				(IDeployable)KPBS_PlanetaryModule.Create(part) ??
				(IDeployable)Kerbalism_Habitat.Create(part);
		}
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace FreeIva
{
	public abstract class Deployable
	{
		public abstract bool IsDeployed { get; }

		// TODO: this is a half-measure; really we should be creating this once per InternalModuleFreeIva that needs it
		public static Deployable Create(Part part, string requiredAnimationName)
		{
			return 
				(Deployable)Squad_ModuleAnimateGeneric.Create(part, requiredAnimationName) ?? 
				(Deployable)SSPX_ModuleDeployableHabitat.Create(part) ?? 
				(Deployable)KPBS_PlanetaryModule.Create(part);
		}
	}
}

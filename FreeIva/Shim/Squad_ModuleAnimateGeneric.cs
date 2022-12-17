using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeIva
{
	public class Squad_ModuleAnimateGeneric : Deployable
	{
		public static Squad_ModuleAnimateGeneric Create(Part part, string requiredAnimationName)
		{
			foreach (var module in part.modules.OfType<ModuleAnimateGeneric>())
			{
				if (module.animationName == requiredAnimationName)
				{
					return new Squad_ModuleAnimateGeneric(module);
				}
			}

			return null;
		}

		Squad_ModuleAnimateGeneric(ModuleAnimateGeneric animationModule)
		{
			m_animationModule = animationModule;
		}

		ModuleAnimateGeneric m_animationModule;

		public override bool IsDeployed
		{
			get
			{
				return m_animationModule.GetState().normalizedTime == 1.0f;
			}
		}
	}
}

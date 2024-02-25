using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeIva
{
	public class Squad_RetractableLadder : IDeployable
	{
		public static Squad_RetractableLadder Create(Part part, string requiredAnimationName)
		{
			foreach (var module in part.modules.OfType<RetractableLadder>())
			{
				if (module.ladderRetractAnimationName == requiredAnimationName)
				{
					return new Squad_RetractableLadder(module);
				}
			}

			return null;
		}

		Squad_RetractableLadder(RetractableLadder ladderModule)
		{
			m_ladderModule = ladderModule;
		}

		RetractableLadder m_ladderModule;

		public void OnInternalCreated() { }

		public bool IsDeployed
		{
			get
			{
				return m_ladderModule.ladderFSM.currentState == m_ladderModule.st_extended;
			}
		}
	}
}

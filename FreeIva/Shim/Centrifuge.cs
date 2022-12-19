using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace FreeIva
{
	public interface ICentrifuge
	{
		void Update();

		float CurrentSpinRate { get; }
		Transform IVARotationRoot { get; }
	}

	public static class CentrifugeFactory
	{
		public static ICentrifuge Create(Part part)
		{
			return (ICentrifuge)SSPX_ModuleDeployableCentrifuge.Create(part)
				?? Kerbalism_GravityRing.Create(part);
		}
	}
}

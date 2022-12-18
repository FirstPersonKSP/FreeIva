using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace FreeIva
{
	public abstract class Centrifuge
	{

		public static Centrifuge Create(Part part)
		{
			return SSPX_ModuleDeployableCentrifuge.Create(part);
		}

		public abstract float CurrentSpinRate { get; }
		public abstract Transform IVARotationRoot { get; }
	}
}

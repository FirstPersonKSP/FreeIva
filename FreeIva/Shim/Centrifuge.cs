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
			return (Centrifuge)SSPX_ModuleDeployableCentrifuge.Create(part)
				?? Kerbalism_GravityRing.Create(part);
		}

		public virtual void Update() { }

		public abstract float CurrentSpinRate { get; }
		public abstract Transform IVARotationRoot { get; }
	}
}

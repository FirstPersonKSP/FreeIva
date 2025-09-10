using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeIva
{
	public class ModuleFreeIvaPartInfo : PartModule
	{
		[KSPField]
		public string message;

		public override string GetInfo()
		{
			return message;
		}

		public override void OnAwake()
		{
			// make this module cheaper in update loops
			isEnabled = false;
			enabled = false;
		}
	}
}

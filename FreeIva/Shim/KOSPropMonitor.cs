using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace FreeIva
{
	static class KOSPropMonitor
	{
		private const string CONTROL_LOCKOUT = "kPMCore";

		public static bool IsLocked()
		{
			return InputLockManager.lockStack.ContainsKey(CONTROL_LOCKOUT);
		}
	}
}

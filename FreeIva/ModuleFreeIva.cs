using System.Collections.Generic;
using UnityEngine;

namespace FreeIva
{
	/// <summary>
	/// This class is currently empty, but in the future it may contain data to persist hatch state etc across saves.
	/// </summary>
	public class ModuleFreeIva : PartModule
	{
		[KSPField]
		public string passThroughNodeA = string.Empty;
		[KSPField]
		public string passThroughNodeB = string.Empty;

		[KSPField]
		public bool doesNotBlockEVA = false;

		[KSPField]
		public bool allowsUnbuckling = true;

		public override string GetModuleDisplayName()
		{
			return "FreeIVA";
		}

		public override string GetInfo()
		{
			if (!allowsUnbuckling) return string.Empty;

			string result = "This part can be traversed in IVA";

			if (doesNotBlockEVA)
			{
				string hatchInfo = "This part will not block EVA hatches";

				if (passThroughNodeA == string.Empty)
				{
					result = hatchInfo;
				}
				else
				{
					result += "\n" + hatchInfo;
				}
			}

			return result;
		}
	}
}

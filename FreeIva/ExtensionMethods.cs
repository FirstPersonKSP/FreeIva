using System;
using System.Collections.Generic;

namespace FreeIva
{
	public static class ExtensionMethods
	{
		// this is similar to Part.FindModuleImplementing, except that it can take an arbitrary type and will populate the cache on failure
		public static PartModule GetModule(this Part part, Type moduleType)
		{
			if (part == null || moduleType == null) return null;
			if (part.cachedModules == null)
			{
				part.cachedModules = new Dictionary<Type, PartModule>();
			}

			if (part.cachedModules.TryGetValue(moduleType, out var partModule))
			{
				return partModule;
			}

			foreach (var module in part.modules.modules)
			{
				if (moduleType.IsAssignableFrom(module.GetType()))
				{
					part.cachedModules.Add(moduleType, module);
					return module;
				}
			}

			part.cachedModules.Add(moduleType, null);
			return null;
		}

		public static T GetModule<T>(this Part part) where T : PartModule
		{
			return (T)GetModule(part, typeof(T));
		}
	}
}

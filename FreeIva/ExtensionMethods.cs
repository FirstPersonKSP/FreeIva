using System;
using System.Collections.Generic;
using UnityEngine;

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

		public static AudioSource PlayUnspatializedClip(this AudioClip clip, float volume = 1.0f)
		{
			GameObject gameObject = new GameObject("One shot audio");
			AudioSource audioSource = (AudioSource)gameObject.AddComponent(typeof(AudioSource));
			audioSource.clip = clip;
			audioSource.spatialBlend = 0f;
			audioSource.spatialize = false;
			audioSource.volume = volume;
			audioSource.Play();
			GameObject.Destroy(gameObject, clip.length * ((Time.timeScale < 0.01f) ? 0.01f : Time.timeScale));
			return audioSource;
		}
	}
}

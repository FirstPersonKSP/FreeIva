using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace FreeIva
{
	public class AssetLoaderBase : MonoBehaviour
	{
		protected Dictionary<string, GameObject> gameObjectsDictionary = new Dictionary<string, GameObject>();
		protected Dictionary<string, Shader> shadersDictionary = new Dictionary<string, Shader>();
		protected Dictionary<string, Font> fontsDictionary = new Dictionary<string, Font>();
		protected Dictionary<string, TMP_FontAsset> tmpFontsDictionary = new Dictionary<string, TMP_FontAsset>();

		protected void LoadAssets(IEnumerable<string> bundlePaths)
		{
			foreach (var path in bundlePaths)
			{
				var fullPath = Path.Combine(KSPUtil.ApplicationRootPath, "GameData", path);
				AssetBundle bundle = AssetBundle.LoadFromFile(fullPath);
				if (bundle == null)
				{
					Log.Error("Error loading asset bundle from: " + fullPath);
				}
				else
				{
					// enumerate assets
					string[] assetNames = bundle.GetAllAssetNames();
					for (int i = 0; i < assetNames.Length; i++)
					{
						string assetName = assetNames[i];
#if DEBUG
						Log.Message("Bundle: " + bundle.name + ", Asset: " + assetName);
#endif

						// find prefabs
						if (assetName.EndsWith(".prefab"))
						{
							GameObject assetGameObject = bundle.LoadAsset<GameObject>(assetName);
							gameObjectsDictionary.Add(assetGameObject.name, assetGameObject);
							Log.Message($"Loaded GameObject '{assetGameObject.name}' from '{assetName}'");
						}
						else if (assetName.EndsWith(".shader"))
						{
							Shader assetShader = bundle.LoadAsset<Shader>(assetName);
							shadersDictionary.Add(assetShader.name, assetShader);
							Log.Message($"Loaded Shader '{assetShader.name}' from '{assetName}'");
						}
						else if (assetName.EndsWith(".ttf"))
						{
							Font assetFont = bundle.LoadAsset<Font>(assetName);
							fontsDictionary.Add(assetFont.name, assetFont);
							Log.Message($"Loaded Font '{assetFont.name}' from '{assetName}'");
						}
					}

					bundle.Unload(false);
				}
			}
		}

		public GameObject GetGameObject(string gameObjectName)
		{
			if (gameObjectsDictionary.TryGetValue(gameObjectName, out GameObject obj))
			{
				return obj;
			}
			Log.Error($"No gameobject named '{gameObjectName}' found in assetbundles!");
			return null;
		}


		public Shader GetShader(string shaderName)
		{
			if (shadersDictionary.TryGetValue(shaderName, out Shader shader))
			{
				return shader;
			}
			Log.Error($"No shader named '{shaderName}' found in assetbundles!");
			return null;
		}


		public Font GetFont(string fontName)
		{
			if (fontsDictionary.TryGetValue(fontName, out Font font))
			{
				return font;
			}
			Log.Error($"No font named '{fontName}' found in assetbundles!");
			return null;
		}


		public TMP_FontAsset GetTmpFont(string fontName)
		{
			if (tmpFontsDictionary.TryGetValue(fontName, out TMP_FontAsset font))
			{
				return font;
			}
			Log.Error($"No tmpFont named '{fontName}' found in assetbundles!");
			return null;
		}
	}

	[KSPAddon(KSPAddon.Startup.Instantly, true)]
	internal class AssetLoader : AssetLoaderBase
	{
		public static AssetLoader Instance;

		void Start()
		{
			Instance = this;
		}

		public void ModuleManagerPostLoad()
		{
			LoadAssets( new string[] { "FreeIva/FreeIva.assetbundle" });
		}
	}
}

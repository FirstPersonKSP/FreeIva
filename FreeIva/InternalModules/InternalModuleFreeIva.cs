using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace FreeIva
{
	public class InternalModuleFreeIva : InternalModule
	{
		#region Cache
		private static Dictionary<InternalModel, InternalModuleFreeIva> perModelCache = new Dictionary<InternalModel, InternalModuleFreeIva>();
		public static InternalModuleFreeIva GetForModel(InternalModel model)
		{
			if (model == null) return null;
			perModelCache.TryGetValue(model, out InternalModuleFreeIva module);
			return module;
		}

		public static void RefreshInternals()
		{
			foreach (var ivaModule in perModelCache.Values)
			{
				if (ivaModule.vessel != FlightGlobals.ActiveVessel)
				{
					ivaModule.part.DespawnIVA();
				}
				else
				{
					foreach (var hatch in ivaModule.Hatches)
					{
						var otherHatch = hatch.ConnectedHatch;
						if (otherHatch == null || otherHatch.vessel != hatch.vessel)
						{
							hatch.Open(false);
						}
						hatch.RefreshConnection();
					}
				}
			}
		}

		#endregion

		[KSPField]
		public bool CopyPartCollidersToInternalColliders = false;

		public List<FreeIvaHatch> Hatches = new List<FreeIvaHatch>(); // hatches will register themselves with us

		List<CutParameter> cutParameters = new List<CutParameter>();
		int propCutsRemaining = 0;

		[KSPField]
		public string secondaryInternalName = string.Empty;
		public InternalModel SecondaryInternalModel { get; private set; }

		public override void OnLoad(ConfigNode node)
		{
			base.OnLoad(node);

			foreach (var shellColliderName in node.GetValues("shellColliderName"))
			{
				var transform = TransformUtil.FindPropTransform(internalProp, shellColliderName);
				if (transform != null)
				{
					var colliders = transform.GetComponentsInChildren<MeshCollider>();
					foreach (var meshCollider in colliders)
					{
						meshCollider.convex = false;
					}

					if (colliders.Length == 0)
					{
						Debug.LogError($"[FreeIva] shellCollider {shellColliderName} in internal {internalModel.internalName} exists but does not have a MeshCollider");
					}
				}
				else
				{
					Debug.LogError($"[FreeIva] shellCollider {shellColliderName} not found in internal {internalModel.internalName}");
				}
			}

			if (CopyPartCollidersToInternalColliders)
			{
				var partBoxColliders = GetComponentsInChildren<BoxCollider>();

				if (partBoxColliders.Length > 0)
				{
					foreach (var c in partBoxColliders)
					{
						if (c.isTrigger || c.tag != "Untagged")
						{
							continue;
						}

						var go = Instantiate(c.gameObject);
						go.transform.parent = internalModel.transform;
						go.layer = (int)Layers.Kerbals;
						go.transform.position = InternalSpace.WorldToInternal(c.transform.position);
						go.transform.rotation = InternalSpace.WorldToInternal(c.transform.rotation);
					}
				}
			}

			var disableColliderNode = node.GetNode("DisableCollider");
			if (disableColliderNode != null)
			{
				DisableCollider.DisableColliders(internalProp, disableColliderNode);
			}

			var deleteObjectsNode = node.GetNode("DeleteInternalObject");
			if (deleteObjectsNode != null)
			{
				DeleteInternalObject.DeleteObjects(internalProp, deleteObjectsNode);
			}

			var cutNodes = node.GetNodes("Cut");
			foreach (var cutNode in cutNodes)
			{
				CutParameter cp = CutParameter.LoadFromCfg(cutNode);

				if (cp != null)
				{
					cutParameters.Add(cp);
				}
			}

			// I can't find a better way to gather all the prop cuts and execute them at once for the entire IVA
			propCutsRemaining = CountPropCuts();

			if (propCutsRemaining == 0)
			{
				ExecuteMeshCuts();
			}
			else
			{
				// need to add cuts from props that are already loaded
				foreach (var prop in internalModel.props)
				{
					foreach (var hatchModule in prop.internalModules.OfType<FreeIvaHatch>())
					{
						if (hatchModule.cutoutTransformName != string.Empty && hatchModule.cutoutTargetTransformName != string.Empty)
						{
							AddPropCut(hatchModule);
						}
					}
				}
			}
		}

		int CountPropCuts()
		{
			int count = 0;

			foreach (var propNode in internalModel.internalConfig.GetNodes("PROP"))
			{
				foreach (var moduleNode in propNode.GetNodes("MODULE"))
				{
					if (moduleNode.GetValue("name") == nameof(HatchConfig) && moduleNode.HasValue("cutoutTargetTransformName"))
					{
						++count;
					}
				}
			}

			return count;
		}

		public void AddPropCut(FreeIvaHatch hatch)
		{
			var tool = TransformUtil.FindPropTransform(hatch.internalProp, hatch.cutoutTransformName);
			if (tool != null)
			{
				CutParameter cp = new CutParameter();
				cp.target = hatch.cutoutTargetTransformName;
				cp.tool = tool.gameObject;
				cp.type = CutParameter.Type.Mesh;
				cutParameters.Add(cp);
			}
			else
			{
				Debug.LogError($"[FreeIva] could not find cutout transform {hatch.cutoutTransformName} on prop {hatch.internalProp.propName}");
			}

			if (--propCutsRemaining == 0)
			{
				ExecuteMeshCuts();
			}
		}

		InternalModel CreateInternalModel(string internalName)
		{
			InternalModel internalPart = PartLoader.GetInternalPart(internalName);
			if (internalPart == null)
			{
				Debug.LogError($"[FreeIva] Could not find INTERNAL named '{internalName}' referenced from INTERNAL '{internalModel.name}'");
				return null;
			}
			var result = UnityEngine.Object.Instantiate(internalPart);
			result.gameObject.name = internalPart.internalName + " interior";
			result.gameObject.SetActive(value: true);
			if (result == null)
			{
				Debug.LogError($"[FreeIva] Failed to instantiate INTERNAL named '{internalName}' referenced from INTERNAL '{internalModel.name}'");
				return null;
			}
			result.part = part;
			result.Load(new ConfigNode());
			result.Initialize(part);
			return result;
		}

		void Start()
		{
			if (!HighLogic.LoadedSceneIsFlight) return;

			if (secondaryInternalName != string.Empty)
			{
				SecondaryInternalModel = CreateInternalModel(secondaryInternalName);
			}
		}

		void Update()
		{
			this.internalModel.transform.position = InternalSpace.WorldToInternal(part.transform.position);
			this.internalModel.transform.rotation = InternalSpace.WorldToInternal(part.transform.rotation) * Quaternion.Euler(90, 0, 180);
		}

		void ExecuteMeshCuts()
		{
			if (HighLogic.LoadedScene != GameScenes.LOADING) return;

			if (cutParameters.Any())
			{
				MeshCutter.Cut(internalModel, cutParameters);
			}

			cutParameters.Clear();
			cutParameters = null;
		}

		new void Awake()
		{
			if (HighLogic.LoadedScene == GameScenes.FLIGHT)
			{
				perModelCache[internalModel] = this;
			}
			base.Awake();
		}

		void OnDestroy()
		{
			perModelCache.Remove(internalModel);
		}
	}
}

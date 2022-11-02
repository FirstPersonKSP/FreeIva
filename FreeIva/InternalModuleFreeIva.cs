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
        #endregion

        [KSPField]
        public string shellColliderName = string.Empty;

        [KSPField]
        public bool CopyPartCollidersToInternalColliders = false;

        public List<FreeIvaHatch> Hatches = new List<FreeIvaHatch>(); // hatches will register themselves with us

        List<CutParameter> cutParameters = new List<CutParameter>();
        int propCutsRemaining = 0;

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);

            if (shellColliderName != string.Empty)
            {
                var transform = internalProp.FindModelTransform(shellColliderName);
                if (transform != null)
                {
                    foreach (var meshCollider in transform.GetComponentsInChildren<MeshCollider>())
                    {
                        meshCollider.convex = false;
                    }
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
        }

        int CountPropCuts()
        {
            int count = 0;

            foreach (var propNode in internalModel.internalConfig.GetNodes("PROP"))
            {
                foreach (var moduleNode in propNode.GetNodes("MODULE"))
                {
                    if (moduleNode.GetValue("name") == nameof(PropHatchConfig) && moduleNode.HasValue("cutoutTargetTransformName"))
                    {
                        ++count;
                    }
                }
            }

            return count;
        }

        public void AddPropCut(string target, FreeIvaHatch hatch)
        {
            var tool = hatch.internalProp.FindModelTransform(hatch.cutoutTransformName);
            if (tool != null)
            {
                CutParameter cp = new CutParameter();
                cp.target = target;
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

        public override void OnAwake()
        {
            perModelCache[internalModel] = this;
        }

        void OnDestroy()
        {
            perModelCache.Remove(internalModel);
        }
    }
}

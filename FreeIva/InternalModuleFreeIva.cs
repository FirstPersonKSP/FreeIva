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

        public List<Hatch> Hatches = new List<Hatch>(); // hatches will register themselves with us

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
            List<CutParameter> cutParameters = new List<CutParameter>();
            foreach (var cutNode in cutNodes)
            {
                CutParameter cp = CutParameter.LoadFromCfg(cutNode);

                if (cp != null)
                {
                    cutParameters.Add(cp);
                }
            }

            if (cutParameters.Any())
            {
                MeshCutter.Cut(internalModel, cutParameters);
            }
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

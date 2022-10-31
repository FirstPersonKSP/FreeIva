using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FreeIva
{
    // Thanks to stupid_chris:
    // https://github.com/StupidChris/RealChute/blob/master/RealChute/PersistentManager.cs
    [KSPAddon(KSPAddon.Startup.Instantly, true)]
    public class PersistenceManager : MonoBehaviour
    {
        public static PersistenceManager instance { get; private set; }

        private static Dictionary<string, List<Hatch>> _hatches = new Dictionary<string, List<Hatch>>();
        private static Dictionary<Type, Dictionary<string, ConfigNode>> nodes = new Dictionary<Type, Dictionary<string, ConfigNode>>();
        private static Dictionary<string, List<CutParameter>> _cuts = new Dictionary<string, List<CutParameter>>();

        private void Awake()
        {
            instance = this;
            DontDestroyOnLoad(this);
        }

        public void AddHatches(string partName, List<Hatch> hatches)
        {
            if (!_hatches.ContainsKey(partName))
            {
                Debug.Log("[FreeIVA] Adding " + hatches.Count + " hatches for part " + partName);
                _hatches.Add(partName, hatches);
            }
            else
            {
                Debug.Log("# NOT adding duplicate " + hatches.Count + " hatches for part " + partName);
                Debug.Log("# Dictionary entries: " + _hatches.Count());
            }
        }

        public void AddCutParameters(string partName, List<CutParameter> cuts)
        {
            if (!_cuts.ContainsKey(partName))
            {
                Debug.Log("[FreeIVA] Adding " + cuts.Count + " cuts for part " + partName);
                _cuts.Add(partName, cuts);
            }
            else
            {
                Debug.Log("[FreeIVA] NOT adding duplicate " + cuts.Count + " cuts for part " + partName);
                Debug.Log("[FreeIVA] Dictionary entries: " + _cuts.Count());
            }
        }

        public List<CutParameter> GetCutParametersForPartInstance(string partName)
        {
            List<CutParameter> cutTemplates;
            if (_cuts.TryGetValue(partName, out cutTemplates))
            {
                Debug.Log("[FreeIVA] Cut FOUND for part " + partName);
                List<CutParameter> cutInstances = new List<CutParameter>();
                foreach (CutParameter cutTemplate in cutTemplates)
                {
                    cutInstances.Add(cutTemplate.Clone());
                }
                return cutInstances;
            }
            else
                Debug.Log("[FreeIVA] Cuts not found in dictionary for part " + partName);
            return new List<CutParameter>();
        }

        /// <summary>
        /// Stores a ConfigNode value in a persistent dictionary, sorted by PartModule type and Part name
        /// </summary>
        /// <typeparam name="T">PartModule type</typeparam>
        /// <param name="partName">Part name to use as Key</param>
        /// <param name="node">ConfigNode to store</param>
        public void AddNode<T>(string partName, ConfigNode node) where T : PartModule
        {
            Dictionary<string, ConfigNode> dict;
            Type type = typeof(T);
            if (!nodes.TryGetValue(type, out dict))
            {
                dict = new Dictionary<string, ConfigNode>();
                nodes.Add(type, dict);
            }
            if (!dict.ContainsKey(partName))
            {
                dict.Add(partName, node);
            }
        }

        /// <summary>
        /// Retreives a ConfigNode for the given PartModule type and Part name
        /// </summary>
        /// <typeparam name="T">PartModule type</typeparam>
        /// <param name="name">Part name to get the node for</param>
        public bool TryGetNode<T>(string name, ref ConfigNode node) where T : PartModule
        {
            Dictionary<string, ConfigNode> dict;
            if (nodes.TryGetValue(typeof(T), out dict))
            {
                if (dict.TryGetValue(name, out node))
                {
                    return true;
                }
            }
            return false;
        }
    }
}

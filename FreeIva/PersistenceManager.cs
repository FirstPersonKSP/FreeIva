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
        private static Dictionary<string, List<InternalCollider>> _internalColliderTemplates = new Dictionary<string, List<InternalCollider>>();
        private static Dictionary<Type, Dictionary<string, ConfigNode>> nodes = new Dictionary<Type, Dictionary<string, ConfigNode>>();

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

        public void AddInternalColliders(string partName, List<InternalCollider> colliders)
        {
            if (!_internalColliderTemplates.ContainsKey(partName))
            {
                Debug.Log("[FreeIVA] Adding " + colliders.Count + " internal colldiers for part " + partName);
                _internalColliderTemplates.Add(partName, colliders);
            }
            else
            {
                Debug.Log("# NOT adding duplicate " + colliders.Count + " internal colldier for part " + partName);
                Debug.Log("# Dictionary entries: " + _internalColliderTemplates.Count());
            }
        }

        public List<InternalCollider> GetCollidersForPartInstance(string partName)
        {
            List<InternalCollider> partColliderTemplates;
            if (_internalColliderTemplates.TryGetValue(partName, out partColliderTemplates))
            {
                Debug.Log("# Internal collider FOUND for part " + partName);
                var colliderInstances = new List<InternalCollider>();
                foreach (var colliderTemplate in partColliderTemplates)
                {
                    colliderInstances.Add(colliderTemplate.Clone());
                }
                return colliderInstances;
            }
            else
                Debug.Log("# Internal collider not found in dictionary for part " + partName);
            return new List<InternalCollider>();
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace FreeIva
{
    /*public class Opening : IvaObject
    {
        public virtual Vector3 WorldPosition
        {
            get
            {
                if (IvaGameObject != null)
                    return IvaGameObject.transform.position;
                return Vector3.zero;
            }
        }

        // The name of the part attach node this hatch is positioned on, as defined in the part.cfg's "node definitions".
        // e.g. node_stack_top
        public string AttachNodeId { get; set; }

        private Opening _connectedOpening = null;
        // Any other opening that this one is connected or docked to, if present.
        public Opening ConnectedOpening
        {
            get
            {
                if (_connectedOpening == null)
                    GetConnectedOpening();
                return _connectedOpening;
            }
        }

        private AttachNode _openingNode;
        // The part attach node this opening is positioned on.
        public AttachNode OpeningNode
        {
            get
            {
                if (_openingNode == null)
                    _openingNode = GetOpeningNode(AttachNodeId);
                return _openingNode;
            }
        }

        public Part Part;

        public override void Instantiate(Part p)
        {
            Part = p;
            Debug.Log("# Creating opening for part " + p);
            if (IvaGameObject != null)
            {
                Debug.LogError("[FreeIVA] Opening has already been instantiated.");
                return;
            }

            // These values will be cleared on creating the object.
            Vector3 localPosition = LocalPosition;
            if (p.internalModel == null)
                p.CreateInternalModel(); // TODO: Detect this in an event instead.
            IvaGameObject.transform.parent = p.internalModel.transform;

            // Restore cleared values.
            LocalPosition = localPosition;
            IvaGameObject.transform.localPosition = localPosition;
            IvaGameObject.name = Name;
        }

        private void GetConnectedOpening()
        {
            AttachNode openingNode = GetOpeningNode(AttachNodeId);
            if (openingNode == null) return;

            ModuleFreeIva iva = openingNode.attachedPart.GetModule<ModuleFreeIva>();
            if (iva == null) return;
            for (int i = 0; i < iva.Opening.Count; i++)
            {
                AttachNode otherOpeningNode = iva.Hatches[i].HatchNode;
                if (otherOpeningNode != null && otherOpeningNode.attachedPart != null && otherOpeningNode.attachedPart.Equals(Part))
                {
                    _connectedOpening = iva.Hatches[i];
                    break;
                }
            }
        }

        /// <summary>
        /// Find the part attach node this hatch is associated with.
        /// </summary>
        /// <param name="attachNodeId"></param>
        /// <returns></returns>
        private AttachNode GetOpeningNode(string attachNodeId)
        {
            string nodeName = RemoveNodePrefix(attachNodeId);
            foreach (AttachNode n in Part.attachNodes)
            {
                if (n.id == nodeName)
                    return n;
            }
            return null;
        }

        private static string RemoveNodePrefix(string attachNodeId)
        {
            string nodeName;
            string prefix = @"node_stack_";
            if (attachNodeId.StartsWith(prefix))
            {
                nodeName = attachNodeId.Substring(prefix.Length, attachNodeId.Length - prefix.Length);
            }
            else
                nodeName = attachNodeId;
            return nodeName;
        }

        public static Opening LoadFromCfg(ConfigNode node)
        {
            Vector3 position = Vector3.zero;
            if (!node.HasValue("attachNodeId"))
            {
                Debug.LogWarning("[FreeIVA] Opening attachNodeId not found: Skipping opening.");
                return null;
            }
            Opening opening = new Opening();

            opening.AttachNodeId = node.GetValue("attachNodeId");

            if (node.HasValue("position"))
            {
                string posString = node.GetValue("position");
                string[] p = posString.Split(Utils.CfgSplitChars, StringSplitOptions.RemoveEmptyEntries);
                if (p.Length != 3)
                {
                    Debug.LogWarning("[FreeIVA] Invalid hatch position definition \"" + posString + "\": Must be in the form x, y, z.");
                    return null;
                }
                else
                    opening.LocalPosition = new Vector3(float.Parse(p[0]), float.Parse(p[1]), float.Parse(p[2]));
            }
            else
            {
                Debug.LogWarning("[FreeIVA] Hatch position not found: Skipping hatch.");
                return null;
            }
            return opening;
        }
    }*/
}

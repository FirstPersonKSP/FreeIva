using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace FreeIva.InternalModules
{
	class ConfigNodeHolder : ScriptableObject
	{
		public ConfigNode Node;
	}

	internal class InstanceInternal : InternalModule
	{
		[SerializeReference]
		ConfigNodeHolder m_prefabInternalNode;
		ConfigNode m_internalNode;

		[KSPField]
		public string parentTransformName = string.Empty;
		[SerializeField] Transform m_parentTransform;

		public override void OnLoad(ConfigNode moduleNode)
		{
			base.OnLoad(moduleNode);

			var internalNode = moduleNode.GetNode("INTERNAL");

			if (HighLogic.LoadedScene == GameScenes.LOADING)
			{
				m_prefabInternalNode = ScriptableObject.CreateInstance<ConfigNodeHolder>();
				m_prefabInternalNode.Node = internalNode;

				if (parentTransformName != string.Empty)
				{
					m_parentTransform = TransformUtil.FindPropTransform(internalProp, parentTransformName);
				}
				else
				{
					m_parentTransform = internalProp.hasModel ? transform : internalModel.transform;
				}
			}

			if (internalNode != null)
			{
				m_internalNode = internalNode;
			}
		}

		void Start()
		{
			m_internalNode = m_internalNode ?? m_prefabInternalNode?.Node;
			string internalName = m_internalNode?.GetValue("name");

			if (internalName == null || m_parentTransform == null)
			{
				return;
			}

			var internalPrefab = PartLoader.GetInternalPart(internalName);
			if (internalPrefab != null)
			{
				var internalModel = GameObject.Instantiate(internalPrefab);
				internalModel.transform.SetParent(m_parentTransform, false);
				internalModel.part = part;
				internalModel.gameObject.SetActive(true);
				internalModel.Load(m_internalNode);
			}
		}
	}
}

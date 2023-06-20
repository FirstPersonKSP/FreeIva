using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace FreeIva
{
	class ShadowCascadeTweak : MonoBehaviour
	{
		Vector3 m_originalSplits;
		public Vector3 AdjustedSplit;

		void Start()
		{
			m_originalSplits = QualitySettings.shadowCascade4Split;
			AdjustedSplit = m_originalSplits;
		}

		void OnDestroy()
		{
			QualitySettings.shadowCascade4Split = m_originalSplits;
		}

		void OnPreRender()
		{
			QualitySettings.shadowCascade4Split = AdjustedSplit;
		}

		void OnPostRender()
		{
			QualitySettings.shadowCascade4Split = m_originalSplits;
		}
	}
}

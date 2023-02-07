using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace FreeIva
{
	public class CivilianKerbal : InternalModule
	{
		[KSPField]
		public string prefabName;

		[KSPField]
		public string childObjectName = string.Empty;

		void Start()
		{
			var prefab = AssetBase.GetPrefab(prefabName) ?? GameDatabase.Instance.GetModel(prefabName);
			var bodyPrefab = childObjectName == string.Empty ? prefab : prefab.transform.Find(childObjectName).gameObject;
			var kerbal = GameObject.Instantiate(bodyPrefab);

			var seat = internalProp.GetComponent<InternalSeat>();

			kerbal.transform.SetParent(seat.seatTransform, false);
			kerbal.transform.localPosition = Vector3.zero;
			kerbal.transform.localRotation = Quaternion.identity;
			kerbal.SetLayerRecursive(16);
			kerbal.SetActive(true);
		}
	}
}

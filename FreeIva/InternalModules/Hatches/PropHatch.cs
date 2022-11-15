using System;
using System.Collections.Generic;
using UnityEngine;

namespace FreeIva
{
	/// <summary>
	/// A module that can be placed on a hatch prop.  Swaps that prop with an 'opened' version when opened
	/// </summary>
	public class PropHatch : FreeIvaHatch
	{
		[KSPField]
		public string openPropName = string.Empty;

		[KSPField]
		public Vector3 openPropPosition = Vector3.zero;

		[KSPField]
		public Vector3 openPropScale = Vector3.one;

		[KSPField]
		public Vector3 openPropRotation = Vector3.zero; // as euler angles

		public InternalProp ClosedProp => internalProp;
		public InternalProp OpenProp;

		public override void OnAwake()
		{
			if (!HighLogic.LoadedSceneIsFlight) return;

			base.OnAwake();

			CreateOpenProp();
		}

		private void CreateOpenProp()
		{
			if (string.IsNullOrEmpty(openPropName)) return;

			OpenProp = CreateProp(openPropName, internalProp, openPropPosition, Quaternion.Euler(openPropRotation), openPropScale);

			if (OpenProp != null)
			{
				OpenProp.gameObject.SetActive(false);
			}
		}

		public static InternalProp CreateProp(string propName, InternalProp atProp)
		{
			return CreateProp(propName, atProp, Vector3.zero, Quaternion.identity, Vector3.one);
		}

		public static InternalProp CreateProp(string propName, InternalProp atProp, Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
		{
			InternalProp result = PartLoader.GetInternalProp(propName);
			if (result == null)
			{
				Debug.LogError("[FreeIVA] Unable to load open prop hatch \"" + propName + "\" in internal " + atProp.internalModel.internalName);
			}
			else
			{
				result.propID = FreeIva.CurrentPart.internalModel.props.Count;
				result.internalModel = atProp.internalModel;

				// position the prop relative to this one, then attach it to the internal model
				result.transform.SetParent(atProp.transform, false);
				result.transform.localRotation = localRotation;
				result.transform.localPosition = localPosition;
				result.transform.localScale = localScale;
				result.transform.SetParent(atProp.internalModel.transform, true);

				result.hasModel = true;
				atProp.internalModel.props.Add(result);
			}

			return result;
		}

		public override void Open(bool open, bool allowSounds = true)
		{
			if (ClosedProp != null)
			{
				ClosedProp.gameObject.SetActive(!open);
			}
			else
			{
				Debug.Log("# ClosedProp was null");
			}
			if (OpenProp != null)
			{
				OpenProp.gameObject.SetActive(open);
			}
			else
			{
				Debug.Log("# OpenProp was null");
			}

			base.Open(open, allowSounds);
		}
	}
}

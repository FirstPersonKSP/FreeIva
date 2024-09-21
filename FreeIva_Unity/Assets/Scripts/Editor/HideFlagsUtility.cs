using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public static class HideFlagsUtility
{
	static void SetHideFlagsRecursive(GameObject go)
	{
		Debug.Log($"{go.name} - {go.hideFlags}");

		switch (go.hideFlags)
		{
			case HideFlags.HideAndDontSave:
				go.hideFlags = HideFlags.DontSave;
				break;
			case HideFlags.HideInHierarchy:
			case HideFlags.HideInInspector:
			case HideFlags.NotEditable:
				go.hideFlags = HideFlags.None;
				break;
		}

		foreach (Transform child in go.transform)
		{
			SetHideFlagsRecursive(child.gameObject);
		}
	}

	[MenuItem("Help/Hide Flags/Show All Objects")]
	private static void ShowAll()
	{
		foreach (GameObject go in Selection.objects)
		{
			SetHideFlagsRecursive(go);
		}
	}
}
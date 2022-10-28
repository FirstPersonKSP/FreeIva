using System;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.UI.Image;

[Serializable]
public struct CutParams
{
	public enum Shape
	{
		Rectangle,
		Circle
	}

	public Vector3 origin;

	public Vector3 rotation;

	public Shape shape;

	/// <summary>
	/// If <see cref="shape"/> is <see cref="Shape.Rectangle"/>, 
	/// <see cref="Vector2.x"/> is width and <see cref="Vector2.y"/> 
	/// is height.<br/>
	/// If <see cref="shape"/> is <see cref="Shape.Circle"/>, 
	/// <see cref="Vector2.x"/> is radius and <see cref="Vector2.y"/> 
	/// is the number of sides.
	/// </summary>
	public Vector2 size;

	public float depth;

	public Vector3[] GetOriginVertices()
	{
		Vector3[] result = new Vector3[shape is Shape.Rectangle ? 4 : (int)size.y];

		if (shape is Shape.Rectangle)
		{
			Vector2 halfSize = size / 2f;
			result[0] = origin + new Vector3(halfSize.x, halfSize.y);
			result[1] = origin + new Vector3(halfSize.x, -halfSize.y);
			result[2] = origin + new Vector3(-halfSize.x, halfSize.y);
			result[3] = origin + new Vector3(-halfSize.x, -halfSize.y);
		}
		else
		{
			for (int i = 0; i < result.Length; i++)
			{
				float angle = 2f * Mathf.PI * ((float)i / result.Length);
				result[i] = origin + new Vector3(size.x * Mathf.Cos(angle), size.x * Mathf.Sin(angle));
			}
		}

		for (int i = 0; i < result.Length; i++)
		{
			result[i] = Quaternion.Euler(rotation) * (result[i] - origin) + origin;
		}

		return result;
	}
}
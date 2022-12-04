using Parabox.CSG;
using UnityEngine;

public class MeshCutter : MonoBehaviour
{
	public GameObject[] tools;

	void Start()
	{
		MeshCutter2 cutter = new MeshCutter2(GetComponent<MeshFilter>());

		foreach (var tool in tools)
		{
			cutter.CutMesh(tool);
		}

		cutter.FinalizeMesh();
	}
}

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DepthMaskGenerator : MonoBehaviour
{
	public GameObject[] CuttingTools;

	// Start is called before the first frame update
	void Start()
	{
		var convexHullCalculator = new GK.ConvexHullCalculator();

		List<Vector3> vertices = null;
		List<int> indices = null;
		List<Vector3> normals = null;

		var meshFilter = gameObject.GetComponent<MeshFilter>();
		var mesh = meshFilter.mesh;

		convexHullCalculator.GenerateHull(mesh.vertices.ToList(), true, ref vertices, ref indices, ref normals);

		mesh.vertices = vertices.ToArray();
		mesh.triangles = indices.ToArray();
		mesh.normals = normals.ToArray();

		var cutter = new MeshCutter2(meshFilter);

		foreach (var tool in CuttingTools)
		{
			cutter.CutMesh(tool);
		}

		cutter.FinalizeMesh();

		gameObject.GetComponent<MeshRenderer>().material = new Material(Shader.Find("Unlit"));
	}

	// Update is called once per frame
	void Update()
	{
		
	}
}

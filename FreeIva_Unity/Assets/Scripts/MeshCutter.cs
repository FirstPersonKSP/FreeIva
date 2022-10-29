using Parabox.CSG;
using UnityEngine;

public class MeshCutter : MonoBehaviour
{
	public GameObject[] tools;

	void Start()
	{
		Transform p = new GameObject("temp").transform;
		p.parent = transform.parent;

		p.localPosition = transform.localPosition;
		p.localRotation = transform.localRotation;
		p.localScale = transform.localScale;
		for (int i = transform.childCount - 1; i >= 0; i--)
		{
			transform.GetChild(i).SetParent(p);
		}

		foreach (GameObject tool in tools)
		{
			tool.transform.parent = transform;
		}

		transform.parent = null;
		transform.position = Vector3.zero;
		transform.rotation = Quaternion.identity;
		transform.localScale = Vector3.one;

		Model model = CSG.SubtractMultiple(gameObject, tools);
		
		Mesh mesh = new Mesh();
		mesh.vertices = model.mesh.vertices;
		mesh.triangles = model.mesh.GetTriangles(0);
		mesh.normals = model.mesh.normals;
		mesh.tangents = model.mesh.tangents;
		mesh.uv = model.mesh.uv;
		mesh.Optimize(); 

		GetComponent<MeshFilter>().sharedMesh = mesh;

		transform.parent = p.parent;
		transform.localPosition = p.localPosition;
		transform.localRotation = p.localRotation;
		transform.localScale = p.localScale;
		for (int i = p.childCount - 1; i >= 0; i--)
		{
			p.GetChild(i).SetParent(transform, false);
		}

		Destroy(p.gameObject);
		foreach (GameObject tool in tools)
		{
			Destroy(tool);
		}
	}
}

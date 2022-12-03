using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class MeshCutter2 : MonoBehaviour
{
	public GameObject tool;

	private Mesh m_mesh;
	private List<Vector3> m_vertices;
	private List<Vector3> m_normals;
	private List<Vector2> m_uvs;
	private List<Vector4> m_tangents;
	private List<int> m_indices;

	// maps a pair of vertex indices to the new vertex index that was created between them
	private Dictionary<Tuple<int, int>, int> m_cutEdgeMapping = new Dictionary<Tuple<int, int>, int>();

	int AddInterpolatedVertex(int indexA, int indexB, float t)
	{
		Tuple<int, int> edge = new Tuple<int, int>(Math.Min(indexA, indexB), Math.Max(indexA, indexB));
		if (m_cutEdgeMapping.TryGetValue(edge, out int index))
		{
			return index;
		}

		int result = m_vertices.Count;
		m_vertices.Add(Vector3.Lerp(m_vertices[indexA], m_vertices[indexB], t));
		m_normals.Add(Vector3.Slerp(m_normals[indexA], m_normals[indexB], t));
		m_uvs.Add(Vector2.Lerp(m_uvs[indexA], m_uvs[indexB], t));

		m_cutEdgeMapping[edge] = result;

		return result;
	}

	void AddTriangle(int a, int b, int c)
	{
		m_indices.Add(a);
		m_indices.Add(b);
		m_indices.Add(c);
	}

	void Start()
	{
		m_mesh = GetComponent<MeshFilter>().mesh;
		
		m_vertices = new List<Vector3> (m_mesh.vertices);
		m_indices = new List<int> (m_mesh.triangles);
		m_normals = new List<Vector3> (m_mesh.normals);
		m_uvs = new List<Vector2>(m_mesh.uv);
		m_tangents = new List<Vector4>(m_mesh.tangents);

		Plane plane = new Plane(tool.transform.up, tool.transform.position);

		int numIndices = m_indices.Count;

		m_cutEdgeMapping.Clear();

		for (int firstIndexIndex = 0; firstIndexIndex < numIndices; firstIndexIndex += 3)
		{
			int indexA = m_indices[firstIndexIndex];
			int indexB = m_indices[firstIndexIndex + 1];
			int indexC = m_indices[firstIndexIndex + 2];

			Vector3 vA = m_vertices[indexA];
			Vector3 vB = m_vertices[indexB];
			Vector3 vC = m_vertices[indexC];

			float dA = plane.GetDistanceToPoint(vA);
			float dB = plane.GetDistanceToPoint(vB);
			float dC = plane.GetDistanceToPoint(vC);

			bool abSameSide = dA * dB >= 0;
			bool bcSameSide = dB * dC >= 0;

			if (abSameSide && bcSameSide) continue;

			bool acSameSide = dA * dC >= 0;

			if (abSameSide)
			{
				// cut edges BC and CA
				int indexBC = AddInterpolatedVertex(indexB, indexC, -dB / (dC - dB));
				int indexCA = AddInterpolatedVertex(indexC, indexA, -dC / (dA - dC));

				// change A-B-C -> A-B-BC
				m_indices[firstIndexIndex + 2] = indexBC;
				AddTriangle(indexBC, indexC, indexCA);
				AddTriangle(indexCA, indexA, indexBC);
			}
			else if (bcSameSide)
			{
				// cut edges AB and CA
				int indexAB = AddInterpolatedVertex(indexA, indexB, -dA / (dB - dA));
				int indexCA = AddInterpolatedVertex(indexC, indexA, -dC / (dA - dC));

				// change A-B-C to A-AB-CA
				m_indices[firstIndexIndex + 1] = indexAB;
				m_indices[firstIndexIndex + 2] = indexCA;
				AddTriangle(indexCA, indexAB, indexB);
				AddTriangle(indexB, indexC, indexCA);
			}
			else if (acSameSide)
			{
				// cut edges AB and BC
				int indexAB = AddInterpolatedVertex(indexA, indexB, -dA / (dB - dA));
				int indexBC = AddInterpolatedVertex(indexB, indexC, -dB / (dC - dB));
				// change A-B-C to A-AB-C
				m_indices[firstIndexIndex + 1] = indexAB;
				AddTriangle(indexC, indexAB, indexBC);
				AddTriangle(indexBC, indexAB, indexB);

			}
		}

		m_mesh.vertices = m_vertices.ToArray();
		m_mesh.triangles = m_indices.ToArray();
		m_mesh.normals = m_normals.ToArray();
		m_mesh.uv = m_uvs.ToArray();
		m_mesh.RecalculateTangents();
		m_mesh.Optimize();
	}
}

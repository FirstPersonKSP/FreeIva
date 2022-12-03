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
	private List<bool> m_skipCuttingTriangle;
	private List<VertexClassification> m_vertexClassifications;

	enum VertexClassification
	{
		Inside,
		OnSurface,
		Outside
	}

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
		m_vertexClassifications.Add(VertexClassification.OnSurface);

		m_cutEdgeMapping[edge] = result;

		return result;
	}

	void AddTriangle(int a, int b, int c, bool skipCutting)
	{
		m_indices.Add(a);
		m_indices.Add(b);
		m_indices.Add(c);
		m_skipCuttingTriangle.Add(skipCutting);
	}

	void TransformPlanesToMeshSpace(List<Plane> planes)
	{
		for (int i = 0; i < planes.Count; i++)
		{
			Plane plane = planes[i];
			Vector3 worldNormal = tool.transform.TransformDirection(plane.normal);
			Vector3 worldPlanePoint = tool.transform.TransformPoint(plane.normal * -plane.distance);

			Vector3 localNormal = transform.InverseTransformDirection(worldNormal);
			Vector3 localPlanePoint = transform.InverseTransformPoint(worldPlanePoint);

			planes[i] = new Plane(localNormal, localPlanePoint);
		}
	}
	void Start()
	{
		m_mesh = GetComponent<MeshFilter>().mesh;
		
		m_vertices = new List<Vector3>(m_mesh.vertices);
		m_indices = new List<int>(m_mesh.triangles);
		m_normals = new List<Vector3>(m_mesh.normals);
		m_uvs = new List<Vector2>(m_mesh.uv);
		m_tangents = new List<Vector4>(m_mesh.tangents);
		m_skipCuttingTriangle= new List<bool>();
		m_skipCuttingTriangle.AddRange(Enumerable.Repeat(false, m_indices.Count / 3));

		// note these planes are in the tool's coordinate space
		var planes = GetPlanesFromTool(tool);
		TransformPlanesToMeshSpace(planes);

		m_vertexClassifications = new List<VertexClassification>(m_vertices.Count);
		foreach (var vertex in m_vertices)
		{
			m_vertexClassifications.Add(ClassifyVertex(vertex, planes));
		}

		foreach (var plane in planes)
		{
			CutByPlane(plane);
		}

		m_mesh.vertices = m_vertices.ToArray();
		m_mesh.triangles = m_indices.ToArray();
		m_mesh.normals = m_normals.ToArray();
		m_mesh.uv = m_uvs.ToArray();
		m_mesh.RecalculateTangents();
		m_mesh.Optimize();
	}

	private VertexClassification ClassifyVertex(Vector3 vertex, List<Plane> planes)
	{
		VertexClassification vertexClassification = VertexClassification.Inside;

		foreach (var plane in planes)
		{
			float d = PlaneGetDistanceToPoint(plane, vertex);

			if (d > 0)
			{
				return VertexClassification.Outside;
			}
			else if (d == 0)
			{
				vertexClassification = VertexClassification.OnSurface;
			}
		}

		return vertexClassification;
	}

	List<Plane> GetPlanesFromTool(GameObject tool)
	{
		Mesh toolMesh = tool.GetComponent<MeshFilter>().mesh;
		List<Plane> planes = new List<Plane>(toolMesh.triangles.Length / 3);

		for (int firstIndexIndex = 0; firstIndexIndex < toolMesh.triangles.Length; firstIndexIndex += 3)
		{
			int indexA = toolMesh.triangles[firstIndexIndex];
			int indexB = toolMesh.triangles[firstIndexIndex + 1];
			int indexC = toolMesh.triangles[firstIndexIndex + 2];

			Vector3 vA = toolMesh.vertices[indexA];
			Vector3 vB = toolMesh.vertices[indexB];
			Vector3 vC = toolMesh.vertices[indexC];

			planes.Add(new Plane(vA, vB, vC));
		}

		return planes.Distinct(PlaneComparison.Instance).ToList();
	}

	// returns 0 for points that are approximately on the plane
	static float PlaneGetDistanceToPoint(Plane plane, Vector3 point)
	{
		float d = plane.GetDistanceToPoint(point);

		if (Mathf.Abs(d) > Mathf.Epsilon)
		{
			return d;
		}

		return 0;
	}

	void CutByPlane(Plane plane)
	{
		int numTriangles = m_indices.Count / 3;

		m_cutEdgeMapping.Clear();

		for (int triangleIndex = 0; triangleIndex < numTriangles; ++triangleIndex)
		{
			if (m_skipCuttingTriangle[triangleIndex]) continue;

			int firstIndexIndex = triangleIndex * 3;
			int indexA = m_indices[firstIndexIndex];
			int indexB = m_indices[firstIndexIndex + 1];
			int indexC = m_indices[firstIndexIndex + 2];

			Vector3 vA = m_vertices[indexA];
			Vector3 vB = m_vertices[indexB];
			Vector3 vC = m_vertices[indexC];

			float dA = PlaneGetDistanceToPoint(plane, vA);
			float dB = PlaneGetDistanceToPoint(plane, vB);
			float dC = PlaneGetDistanceToPoint(plane, vC);

			bool abSameSide = dA * dB >= 0;
			bool bcSameSide = dB * dC >= 0;

			if (abSameSide && bcSameSide) continue;

			bool acSameSide = dA * dC >= 0;

			bool aOutside = dA > 0;
			m_skipCuttingTriangle[triangleIndex] = aOutside;

			if (abSameSide)
			{
				// cut edges BC and CA
				int indexBC = AddInterpolatedVertex(indexB, indexC, -dB / (dC - dB));
				int indexCA = AddInterpolatedVertex(indexC, indexA, -dC / (dA - dC));

				// change A-B-C -> A-B-BC
				m_indices[firstIndexIndex + 2] = indexBC;
				AddTriangle(indexBC, indexC, indexCA, !aOutside);
				AddTriangle(indexCA, indexA, indexBC, aOutside);
			}
			else if (bcSameSide)
			{
				// cut edges AB and CA
				int indexAB = AddInterpolatedVertex(indexA, indexB, -dA / (dB - dA));
				int indexCA = AddInterpolatedVertex(indexC, indexA, -dC / (dA - dC));

				// change A-B-C to A-AB-CA
				m_indices[firstIndexIndex + 1] = indexAB;
				m_indices[firstIndexIndex + 2] = indexCA;
				AddTriangle(indexCA, indexAB, indexB, !aOutside);
				AddTriangle(indexB, indexC, indexCA, !aOutside);
			}
			else if (acSameSide)
			{
				// cut edges AB and BC
				int indexAB = AddInterpolatedVertex(indexA, indexB, -dA / (dB - dA));
				int indexBC = AddInterpolatedVertex(indexB, indexC, -dB / (dC - dB));
				// change A-B-C to A-AB-C
				m_indices[firstIndexIndex + 1] = indexAB;
				AddTriangle(indexC, indexAB, indexBC, aOutside);
				AddTriangle(indexBC, indexAB, indexB, !aOutside);
			}
		}
	}

	class PlaneComparison : IEqualityComparer<Plane>
	{
		public static PlaneComparison Instance { get; private set; } = new PlaneComparison();

		public bool Equals(Plane x, Plane y)
		{
			return x.normal == y.normal && Mathf.Approximately(x.distance, y.distance);
		}

		public int GetHashCode(Plane obj)
		{
			return obj.GetHashCode();
		}
	}
}

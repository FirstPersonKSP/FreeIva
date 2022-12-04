using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class MeshCutter2
{
	private Transform m_meshTransform;
	private Mesh m_mesh;
	private List<Vector3> m_vertices;
	private List<Vector3> m_normals;
	private List<Vector2> m_uvs;
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

	void TransformPlanesToMeshSpace(List<Plane> planes, Transform cuttingToolTransform)
	{
		for (int i = 0; i < planes.Count; i++)
		{
			Plane plane = planes[i];
			Vector3 worldNormal = cuttingToolTransform.TransformVector(plane.normal);
			Vector3 worldPlanePoint = cuttingToolTransform.TransformPoint(plane.normal * -plane.distance);

			Vector3 localNormal = m_meshTransform.InverseTransformVector(worldNormal);
			Vector3 localPlanePoint = m_meshTransform.InverseTransformPoint(worldPlanePoint);

			planes[i] = new Plane(localNormal, localPlanePoint);
		}
	}

	// removes a triangle and swaps the last one into its spot
	void RemoveTriangle(int triangleIndex)
	{
		int firstIndexIndex = triangleIndex * 3;
		int lastTriangleIndex = m_skipCuttingTriangle.Count - 1;
		int lastTriangleFirstIndexIndex = lastTriangleIndex * 3;

		m_indices[firstIndexIndex] = m_indices[lastTriangleFirstIndexIndex];
		m_indices[firstIndexIndex + 1] = m_indices[lastTriangleFirstIndexIndex + 1];
		m_indices[firstIndexIndex + 2] = m_indices[lastTriangleFirstIndexIndex + 2];

		m_skipCuttingTriangle[triangleIndex] = m_skipCuttingTriangle[lastTriangleIndex];

		m_skipCuttingTriangle.RemoveAt(lastTriangleIndex);
		m_indices.RemoveRange(lastTriangleFirstIndexIndex, 3);
	}

	void RemoveInteriorTriangles()
	{
		int triangleIndex = 0;
		while (triangleIndex < m_skipCuttingTriangle.Count)
		{
			int firstIndexIndex = triangleIndex * 3;
			int indexA = m_indices[firstIndexIndex];
			int indexB = m_indices[firstIndexIndex + 1];
			int indexC = m_indices[firstIndexIndex + 2];

			if (m_vertexClassifications[indexA] != VertexClassification.Outside && 
				m_vertexClassifications[indexB] != VertexClassification.Outside &&
				m_vertexClassifications[indexC] != VertexClassification.Outside)
			{
				RemoveTriangle(triangleIndex);
			}
			else
			{
				++triangleIndex;
			}
		}
	}

	public MeshCutter2(MeshFilter target)
	{
		m_mesh = target.mesh;
		m_meshTransform = target.transform;

		m_vertices = new List<Vector3>(m_mesh.vertices);
		m_indices = new List<int>(m_mesh.triangles);
		m_normals = new List<Vector3>(m_mesh.normals);
		m_uvs = new List<Vector2>(m_mesh.uv);
		m_skipCuttingTriangle = new List<bool>();
		m_skipCuttingTriangle.AddRange(Enumerable.Repeat(false, m_indices.Count / 3));
	}
	
	public void FinalizeMesh()
	{
		m_mesh.vertices = m_vertices.ToArray();
		m_mesh.triangles = m_indices.ToArray();
		m_mesh.normals = m_normals.ToArray();
		m_mesh.uv = m_uvs.ToArray();
		m_mesh.RecalculateTangents();
		m_mesh.Optimize();
	}

	public void CutMesh(GameObject tool)
	{
		var planes = GetPlanesFromTool(tool);
		CutMesh(planes, tool.transform);
	}

	public void CutMesh(List<Plane> cuttingPlanes, Transform cuttingToolTransform)
	{
		int initialvertexCount = m_vertices.Count;

		// make a copy of the planes cause we need to transform them
		List<Plane> planes = cuttingPlanes.ToList();
		TransformPlanesToMeshSpace(planes, cuttingToolTransform);

		for (int i = 0; i < m_skipCuttingTriangle.Count; i++)
		{
			m_skipCuttingTriangle[i] = false;
		}

		m_vertexClassifications = new List<VertexClassification>(m_vertices.Count);
		foreach (var vertex in m_vertices)
		{
			m_vertexClassifications.Add(ClassifyVertex(vertex, planes));
		}

		RemoveInteriorTriangles();

		// This algorithm tends to create a lot of extra vertices because it's not very smart about ordering the planes for each triangle
		// instead of iterating over the planes, and cutting the mesh with each one, it might be better to iterate over the mesh triangles, sort the planes somehow, and then cut the triangle with the sorted planes
		// not really sure what the right sorting criteria would be
		foreach (var plane in planes)
		{
			CutByPlane(plane);
		}

		// reclassify the new vertices
		for (int i = initialvertexCount; i < m_vertexClassifications.Count; i++)
		{
			m_vertexClassifications[i] = ClassifyVertex(m_vertices[i], planes);
		}

		RemoveInteriorTriangles();

		
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

	// returns a set of planes in tool-local space (so the same set of planes can be re-used for many different transforms of the same tool)
	public List<Plane> GetPlanesFromTool(GameObject tool)
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

		if (Mathf.Abs(d) > 1e-4f)
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

			// if all 3 vertices are above this plane, then it's completely outside the cutting tool and we never have to consider it again
			if (dA >= 0 && dB >= 0 && dC >= 0)
			{
				m_skipCuttingTriangle[triangleIndex] = true;
				continue;
			}

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

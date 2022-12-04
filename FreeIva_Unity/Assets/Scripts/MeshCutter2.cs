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

	int FindOppositePlane(List<Plane> planes, int fromPlaneIndex)
	{
		float bestDot = 2;
		int bestIndex = -1;

		Vector3 normal = planes[fromPlaneIndex].normal;

		for (int planeIndex = fromPlaneIndex + 1; planeIndex < planes.Count; ++planeIndex)
		{
			float dot = Vector3.Dot(planes[planeIndex].normal, normal);

			if (dot < bestDot)
			{
				bestDot = dot;
				bestIndex = planeIndex;
			}
		}

		return bestIndex;
	}

	int FindPerpendicularPlane(List<Plane> planes, int fromPlaneIndex)
	{
		float bestDot = 2;
		int bestIndex = -1;

		Vector3 normal = planes[fromPlaneIndex].normal;

		for (int planeIndex = fromPlaneIndex + 1; planeIndex < planes.Count; ++planeIndex)
		{
			float dot = Vector3.Dot(planes[planeIndex].normal, normal);

			if (Mathf.Abs(dot) < bestDot)
			{
				bestDot = dot;
				bestIndex = planeIndex;
			}
		}

		return bestIndex;
	}

	static void Swap<T>(List<T> list, int a, int b)
	{
		T temp = list[a];
		list[a] = list[b];
		list[b] = temp;
	}

	// heuristic to apply cutting planes in a more useful order - every other plane should be as "opposite" as possible, and then the next one should be as perpendicular to the previous as possible
	void SortPlanes(List<Plane> planes)
	{
		int currentPlaneIndex = 0;
		while (currentPlaneIndex < planes.Count - 1)
		{
			int oppositePlaneIndex = FindOppositePlane(planes, currentPlaneIndex);

			++currentPlaneIndex;
			Swap(planes, currentPlaneIndex, oppositePlaneIndex);

			if (currentPlaneIndex < planes.Count - 1)
			{
				int perpendicularPlaneIndex = FindPerpendicularPlane(planes, currentPlaneIndex);

				++currentPlaneIndex;
				Swap(planes, currentPlaneIndex, perpendicularPlaneIndex);
			}
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

	// for each triangle in the mesh, determines if it's completely inside or outside the planes
	// triangles completely inside the planes are removed, and ones completely outside are marked to be skipped
	void FilterTriangles(List<Plane> cuttingPlanes)
	{
		for (int triangleIndex = 0; triangleIndex < m_skipCuttingTriangle.Count; ++triangleIndex)
		{
			if (m_skipCuttingTriangle[triangleIndex]) continue; // if we've already determined that this triangle is safe, no need to check it again

			int firstIndexIndex = triangleIndex * 3;
			int indexA = m_indices[firstIndexIndex];
			int indexB = m_indices[firstIndexIndex + 1];
			int indexC = m_indices[firstIndexIndex + 2];

			
			Vector3 vA = m_vertices[indexA];
			Vector3 vB = m_vertices[indexB];
			Vector3 vC = m_vertices[indexC];

			bool insideA = true;
			bool insideB = true;
			bool insideC = true;

			foreach (var plane in cuttingPlanes)
			{
				float dA = PlaneGetDistanceToPoint(plane, vA);
				float dB = PlaneGetDistanceToPoint(plane, vB);
				float dC = PlaneGetDistanceToPoint(plane, vC);

				// if all 3 vertices are above a single plane, the triangle is completely outside the hull and can be skipped entirely
				if (dA >= 0 && dB >= 0 && dC >= 0)
				{
					m_skipCuttingTriangle[triangleIndex] = true;
					break;
				}

				insideA = insideA && dA <= 0;
				insideB = insideB && dB <= 0;
				insideC = insideC && dC <= 0;
			}

			// if all 3 vertices are inside all of the cutting planes, remove the triangle entirely
			if (!m_skipCuttingTriangle[triangleIndex] && insideA && insideB && insideC)
			{
				RemoveTriangle(triangleIndex);
				--triangleIndex; // repeat same index
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

		SortPlanes(planes);

		for (int i = 0; i < m_skipCuttingTriangle.Count; i++)
		{
			m_skipCuttingTriangle[i] = false;
		}

		FilterTriangles(planes);
		
		// This algorithm tends to create a lot of extra vertices because it's not very smart about ordering the planes for each triangle
		// instead of iterating over the planes, and cutting the mesh with each one, it might be better to iterate over the mesh triangles, sort the planes somehow, and then cut the triangle with the sorted planes
		// not really sure what the right sorting criteria would be
		foreach (var plane in planes)
		{
			CutByPlane(plane);
		}

		FilterTriangles(planes);
	}

	// returns a negative number if the vertex is inside all the planes, 0 if it's on the surface, or a positive number if it's outside all of the planes
	private float ClassifyVertex(Vector3 vertex, List<Plane> planes)
	{
		foreach (var plane in planes)
		{
			float d = PlaneGetDistanceToPoint(plane, vertex);

			if (d >= 0)
			{
				return d;
			}
		}

		return -1;
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

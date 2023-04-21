using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

// NOTE: this file should not contain any KSP-specific code.  It's pretty important that we're able to test it on meshes directly in the Unity editor.
// In fact this file lives in the *unity* project and should only be referenced from the KSP mod code

public class MeshCutter2
{
	private Transform m_meshTransform;
	private Mesh m_mesh;
	private List<Vector3> m_vertices;
	private List<Vector3> m_normals;
	private List<Vector2> m_uvs;
	private List<Vector2> m_uvs2;
	private List<int> m_indices;
	private List<bool> m_skipCuttingTriangle;

	// maps a pair of vertex indices to the new vertex index that was created between them
	private Dictionary<Tuple<int, int>, int> m_cutEdgeMapping = new Dictionary<Tuple<int, int>, int>();

	public MeshCutter2(MeshFilter target)
	{
		m_mesh = target.mesh;
		m_meshTransform = target.transform;

		m_vertices = new List<Vector3>(m_mesh.vertices);
		m_indices = new List<int>(m_mesh.triangles);
		m_normals = m_mesh.HasVertexAttribute(VertexAttribute.Normal) ? m_mesh.normals.ToList() : null;
		m_uvs = m_mesh.HasVertexAttribute(VertexAttribute.TexCoord0) ? m_mesh.uv.ToList() : null;
		m_uvs2 = m_mesh.HasVertexAttribute(VertexAttribute.TexCoord1) ? new List<Vector2>(m_mesh.uv2) : null;
		m_skipCuttingTriangle = new List<bool>();
		m_skipCuttingTriangle.AddRange(Enumerable.Repeat(false, m_indices.Count / 3));

		// transform vertices into worldspace because extreme scaling on the mesh will break the cutting thresholds
		var localToWorld = m_meshTransform.localToWorldMatrix;
		for (int i = 0; i < m_vertices.Count; i++)
		{
			m_vertices[i] = localToWorld.MultiplyPoint(m_vertices[i]);
		}
	}

	public void FinalizeMesh()
	{
		// convert vertices back to mesh space
		var worldToLocal = m_meshTransform.worldToLocalMatrix;
		for (int i = 0; i < m_vertices.Count; ++i)
		{
			m_vertices[i] = worldToLocal.MultiplyPoint(m_vertices[i]);
		}

		m_mesh.SetVertices(m_vertices);
		m_mesh.SetTriangles(m_indices, 0);
		if (m_normals != null) m_mesh.SetNormals(m_normals);
		if (m_uvs != null) m_mesh.SetUVs(0, m_uvs);
		if (m_uvs2 != null) m_mesh.SetUVs(1, m_uvs2.ToArray());
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
		TransformPlanesToWorldSpace(planes, cuttingToolTransform);

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

	int AddInterpolatedVertex(int indexA, int indexB, float t)
	{
		Tuple<int, int> edge = new Tuple<int, int>(Math.Min(indexA, indexB), Math.Max(indexA, indexB));
		if (m_cutEdgeMapping.TryGetValue(edge, out int index))
		{
			return index;
		}

		int result = m_vertices.Count;
		m_vertices.Add(Vector3.Lerp(m_vertices[indexA], m_vertices[indexB], t));

		if (m_normals != null)
		{
			m_normals.Add(Vector3.Slerp(m_normals[indexA], m_normals[indexB], t));
		}

		if (m_uvs != null)
		{
			m_uvs.Add(Vector2.Lerp(m_uvs[indexA], m_uvs[indexB], t));
		}

		if (m_uvs2 != null)
		{
			m_uvs2.Add(Vector2.Lerp(m_uvs2[indexA], m_uvs2[indexB], t));
		}

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

	void TransformPlanesToWorldSpace(List<Plane> planes, Transform cuttingToolTransform)
	{
		var toolToWorld = cuttingToolTransform.localToWorldMatrix;
		for (int i = 0; i < planes.Count; i++)
		{
			Plane plane = planes[i];
			Vector3 worldNormal = toolToWorld.MultiplyVector(plane.normal);
			Vector3 worldPlanePoint = toolToWorld.MultiplyPoint(plane.normal * -plane.distance);

			planes[i] = new Plane(worldNormal, worldPlanePoint);
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
					// unless all 3 are on the plane - then we might still want to remove it.
					if (dA != 0 || dB != 0 || dC != 0)
					{
						m_skipCuttingTriangle[triangleIndex] = true;
						break;
					}
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

	// returns 0 for points that are approximately on the plane
	static float PlaneGetDistanceToPoint(Plane plane, Vector3 point)
	{
		float d = plane.GetDistanceToPoint(point);

		if (Mathf.Abs(d) > 1e-3f)
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
			else if (dA <= 0 && dB <= 0 && dC <= 0)
			{
				continue;
			}

			// exactly one or two of these booleans must be true (not zero and not three)
			bool cutAB = dA * dB < 0;
			bool cutBC = dB * dC < 0;
			bool cutCA = dC * dA < 0;

			int indexAB = cutAB ? AddInterpolatedVertex(indexA, indexB, -dA / (dB - dA)) : -1;
			int indexBC = cutBC ? AddInterpolatedVertex(indexB, indexC, -dB / (dC - dB)) : -1;
			int indexCA = cutCA ? AddInterpolatedVertex(indexC, indexA, -dC / (dA - dC)) : -1;

			if (cutAB)
			{
				bool aOutside = dA > 0;
				m_skipCuttingTriangle[triangleIndex] = aOutside; // the existing triangle keeps vertex A
				if (cutBC) // AB and BC - B is on one side and AC are on the other
				{
					// change A-B-C to A-AB-C
					m_indices[firstIndexIndex + 1] = indexAB;
					AddTriangle(indexC, indexAB, indexBC, aOutside);
					AddTriangle(indexBC, indexAB, indexB, !aOutside);
				}
				else if (cutCA) // AB and CA: A is on one side and BC are on the other
				{
					// change A-B-C to A-AB-CA
					m_indices[firstIndexIndex + 1] = indexAB;
					m_indices[firstIndexIndex + 2] = indexCA;
					AddTriangle(indexCA, indexAB, indexB, !aOutside);
					AddTriangle(indexB, indexC, indexCA, !aOutside);
				}
				else // AB alone: C is on the plane, AB are on opposite sides
				{
					// change A-B-C to A-AB-C
					m_indices[firstIndexIndex + 1] = indexAB;
					AddTriangle(indexAB, indexB, indexC, !aOutside);
				}
			}
			else if (cutBC)
			{
				bool bOutside = dB > 0;
				m_skipCuttingTriangle[triangleIndex] = bOutside; // existing triangle keeps vertex B
				if (cutCA) // BC and CA: C is on one side and AB are on the other
				{
					// change A-B-C -> A-B-BC
					m_indices[firstIndexIndex + 2] = indexBC;
					AddTriangle(indexBC, indexC, indexCA, !bOutside);
					AddTriangle(indexCA, indexA, indexBC, bOutside);
				}
				else // BC alone: A is on the plane, BC are on opposite sides
				{
					// change A-B-C to A-B-BC
					m_indices[firstIndexIndex + 2] = indexBC;
					AddTriangle(indexBC, indexC, indexA, !bOutside);
				}
			}
			else // CA alone: B is on the plane, CA are on opposite sides
			{
				bool aOutside = dA > 0;
				m_skipCuttingTriangle[triangleIndex] = aOutside; // the existing triangle keeps vertex A
				// change A-B-C to A-B-CA
				m_indices[firstIndexIndex + 2] = indexCA;
				AddTriangle(indexB, indexC, indexCA, !aOutside);
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

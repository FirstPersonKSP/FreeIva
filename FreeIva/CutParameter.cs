using System;
using UnityEngine;

namespace FreeIva
{
	public class CutParameter
	{
		public enum Type
		{
			Cube,
			ProceduralCylinder,
			Cylinder,
			Mesh,
		}

		public string target;

		public Type type;

		public Vector3 position;

		public Vector3 rotation;

		public Vector3 scale;

		public float radius;

		public float height;

		public int slices;

		public string tool;

		public CutParameter()
		{

		}

		public CutParameter(string target, Type type, Vector3 position, Vector3 rotation, Vector3 scale, float radius, float depth, int sides, string tool)
		{
			this.target = target;
			this.type = type;
			this.position = position;
			this.rotation = rotation;
			this.scale = scale;
			this.radius = radius;
			this.height = depth;
			this.slices = sides;
			this.tool = tool;
		}

		public CutParameter Clone()
		{
			return new CutParameter(target, type, position, rotation, scale, radius, height, slices, tool);
		}

		public static CutParameter LoadFromCfg(ConfigNode node)
		{
			/* 
			 Possible configurations:
			 
			    Cut
			    {	
			    	target = target_object
			    	type = Mesh
			    	tool = tool_object
			    }
			    
			    Cut
			    {	
			    	target = target_object
			    	type = Cube
			    	position = 0, 0, 0
			    	rotation = 0, 0, 0
			    	scale = 1, 1, 1
			    }	
			    
			    Cut
			    {	
			    	target = target_object
			    	type = Cylinder
			    	position = 0, 0, 0
			    	rotation = 0, 0, 0
			    	scale = 1, 1, 1
			    }	
			    
			    Cut
			    {	
			    	target = target_object
			    	type = ProceduralCylinder
			    	radius = 0.5
			    	height = 1
			    	slices = 8
			    	position = 0, 0, 0
			    	rotation = 0, 0, 0
			    	scale = 1, 1, 1
			    }	
			 */


			if (!node.HasValue("type"))
			{
				Debug.LogWarning("[FreeIVA] CutParameter type not found, skipping.");
				return null;
			}

			CutParameter param = new CutParameter();
			string typeString = node.GetValue("type");
			if (!Enum.TryParse(typeString, out param.type))
			{
				Debug.LogWarning($"[FreeIVA] invalid Cut Parameter type {typeString}");
				return null;
			}

			param.target = node.GetValue("target");

			if (param.type == Type.Mesh)
			{
				param.tool = node.GetValue("tool");
			}
			else
			{
				if (param.type == Type.ProceduralCylinder)
				{
					if (!float.TryParse(node.GetValue("radius"), out param.radius) ||
					!float.TryParse(node.GetValue("height"), out param.height) ||
					!int.TryParse(node.GetValue("slices"), out param.slices))
					{
						Debug.LogWarning($"[FreeIVA] invalid Cut Parameter radius, depth, or sides");
						return null;
					}
				}
				param.position = ConfigNode.ParseVector3(node.GetValue("position"));
				param.rotation = ConfigNode.ParseVector3(node.GetValue("rotation"));
				param.scale = ConfigNode.ParseVector3(node.GetValue("scale"));
			}

			return param;
		}
	}
}

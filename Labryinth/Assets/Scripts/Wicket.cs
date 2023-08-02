using System;
using System.Collections.Generic;
using UnityEngine;

namespace CaveCreator
{
	public class Wicket
	{
		protected int[] Points;
		public readonly Vector3[] Vertices;
		public readonly Vector3[] Normals; // no longer using these, but leaving them for now in case I change my mind again

		public Wicket(int[] indices, Vector3[] vertices, Vector3[] normals)
		{
			Points = indices;
			Vertices = vertices;
			Normals = normals;
		}

		public IEnumerable<int> GetPoints => Points;

		public int this[Index index]
		{
			get
			{
				return Points[index];
			}
			set
			{
				Points[index] = value;
			}
		}
	}
}
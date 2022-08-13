#region ================== Copyright (c) 2022 Boris Iwanski

/*
 * This program is free software: you can redistribute it and/or modify
 *
 * it under the terms of the GNU General Public License as published by
 * 
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 *    but WITHOUT ANY WARRANTY; without even the implied warranty of
 * 
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
 * 
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program.If not, see<http://www.gnu.org/licenses/>.
 */

#endregion

#region ================== Namespaces

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodeImp.DoomBuilder.Geometry;

#endregion

namespace CodeImp.DoomBuilder.UDBScript.Wrapper
{
	class PlaneWrapper
	{
		#region ================== Variables

		private Plane plane;

		#endregion

		#region ================== Properties

		/// <summary>
		/// The plane's normal vector.
		/// </summary>
		public Vector3D normal
		{
			get
			{
				return plane.Normal;
			}
		}

		/// <summary>
		/// The distance of the plane along the normal vector.
		/// </summary>
		public double offset
		{
			get
			{
				return plane.Offset;
			}
			set
			{
				plane.Offset = value;
			}
		}

		/// <summary>
		/// The `a` value of the plane equation. This is the `x` value of the normal vector.
		/// </summary>
		public double a
		{
			get
			{
				return plane.Normal.x;
			}
		}

		/// <summary>
		/// The `b` value of the plane equation. This is the `y` value of the normal vector.
		/// </summary>
		public double b
		{
			get
			{
				return plane.Normal.y;
			}
		}

		/// <summary>
		/// The `c` value of the plane equation. This is the `z` value of the normal vector.
		/// </summary>
		public double c
		{
			get
			{
				return plane.Normal.z;
			}
		}

		/// <summary>
		/// The `d` value of the plane equation. This is the same as the `offset` value.
		/// </summary>
		public double d
		{
			get
			{
				return plane.Offset;
			}
			set
			{
				plane.Offset = value;
			}
		}

		#endregion

		#region ================== Constructors

		/// <summary>
		/// Creates a new `Plane` from a normal and an offset.
		/// ```
		/// let plane1 = new UDB.Plane(new Vector3D(0.0, -0.707, 0.707), 32);
		/// let plane2 = new UDB.Plane([ 0.0, -0.707, 0.707 ], 32);
		/// ```
		/// </summary>
		/// <param name="normal">Normal vector of the plane</param>
		/// <param name="offset">Distance of the plane from the origin</param>
		public PlaneWrapper(object normal, double offset)
		{
			plane = new Plane((Vector3D)BuilderPlug.Me.GetVectorFromObject(normal, true), offset);
		}

		public PlaneWrapper(object p1, object p2, object p3, bool up)
		{
			Vector3D v1 = (Vector3D)BuilderPlug.Me.GetVectorFromObject(p1, true);
			Vector3D v2 = (Vector3D)BuilderPlug.Me.GetVectorFromObject(p2, true);
			Vector3D v3 = (Vector3D)BuilderPlug.Me.GetVectorFromObject(p3, true);

			plane = new Plane(v1, v2, v3, up);
		}

		/*
		public PlaneWrapper(object center, double anglexy, double anglez, bool up)
		{
			try
			{
				Vector3D c = (Vector3D)BuilderPlug.Me.GetVectorFromObject(center, true);
				plane = new Plane(c, anglexy, anglez, up);
			}
			catch (CantConvertToVectorException e)
			{
				throw BuilderPlug.Me.ScriptRunner.CreateRuntimeException(e.Message);
			}
		}
		*/

		#endregion

		#region ================== Methods

		/// <summary>
		/// Checks if the line between `from` and `to` intersects the plane.
		/// 
		/// It returns an `Array`, where the first element is a `bool` vaue indicating if there is an intersector, and the second element is the position of the intersection on the line between the two points.
		/// 
		/// ```
		/// const plane = new UDB.Plane([ 0, 0, 1 ], 0);
		/// const [intersecting, u] = plane.getIntersection([0, 0, 32], [0, 0, -32]);
		/// UDB.log(`${intersecting} / ${u}`); // Prints "true / 0.5"
		/// ```
		/// </summary>
		/// <param name="from">`Vector3D` of the start of the line</param>
		/// <param name="to">`Vector3D` of the end of the line</param>
		/// <returns></returns>
		public object[] getIntersection(object from, object to)
		{
			Vector3D f = (Vector3D)BuilderPlug.Me.GetVectorFromObject(from, true);
			Vector3D t = (Vector3D)BuilderPlug.Me.GetVectorFromObject(to, true);

			double u_ray = double.NaN;

			bool r = plane.GetIntersection(f, t, ref u_ray);

			return new object[] { r, u_ray };
		}

		/// <summary>
		/// Computes the distance between the `Plane` and a point. The given point can be a `Vector3D` or an `Array` of three numbers. A result greater than 0 means the point is on the front of the plane, less than 0 means the point is behind the plane.
		/// ```
		/// const plane = new UDB.Plane([ 0, 0, 0 ], [ 32, 0, 0 ], [ 32, 32, 16 ], true);
		/// UDB.log(plane.distance([ 16, 16, 32 ])); // Prints '21.466252583998'
		/// ```
		/// </summary>
		/// <param name="p">Point to compute the distnace to</param>
		/// <returns>Distance between the `Plane` and the point as `number`</returns>
		public double distance(object p)
		{
			Vector3D v = (Vector3D)BuilderPlug.Me.GetVectorFromObject(p, true);

			return plane.Distance(v);
		}

		/// <summary>
		/// Returns the point that's closest to the given point on the `Plane`. The given point can be a `Vector3D` or an `Array` of three numbers.
		/// ```
		/// const plane = new UDB.Plane([ 0, 0, 0 ], [ 32, 0, 0 ], [ 32, 32, 16 ], true);
		/// UDB.log(plane.closestOnPlane([ 16, 16, 32 ])); // Prints '16, 25.6, 12.8'
		/// ```
		/// </summary>
		/// <param name="p">Point to get the closest position from</param>
		/// <returns>Point as `Vector3D` on the plane closest to the given point</returns>
		public Vector3DWrapper closestOnPlane(object p)
		{
			Vector3D v = (Vector3D)BuilderPlug.Me.GetVectorFromObject(p, true);

			return new Vector3DWrapper(plane.ClosestOnPlane(v));
		}

		/// <summary>
		/// Returns the position on the z axis of the plane for the given point. The given point can be a `Vector2D` or an `Array` of two numbers.
		/// ```
		/// const plane = new UDB.Plane([ 0, 0, 0 ], [ 32, 0, 0 ], [ 32, 32, 16 ], true);
		/// UDB.log(plane.getZ([ 16, 16 ])); // Prints '8'
		/// ```
		/// </summary>
		/// <param name="p"></param>
		/// <returns></returns>
		public double getZ(object p)
		{
			Vector2D v = (Vector2D)BuilderPlug.Me.GetVectorFromObject(p, true);

			return plane.GetZ(v);
		}

		public override bool Equals(object obj)
		{
			if (!(obj is PlaneWrapper other)) return false;

			return plane.Equals(other.plane);
		}

		public override int GetHashCode()
		{
			return plane.GetHashCode();
		}

		#endregion

		#region ================== Statics

		public static bool operator ==(PlaneWrapper a, PlaneWrapper b) => a.plane == b.plane;

		public static bool operator !=(PlaneWrapper a, PlaneWrapper b) => a.plane != b.plane;

		#endregion
	}
}

using System;

namespace ragdoll
{
	public class Vector2D
	{
		private double _x = 0;
		private double _y = 0;

		public Vector2D()
		{
		}

		public Vector2D(double x, double y)
		{
			_x = x;
			_y = y;
		}

		public Vector2D(Vector2D vector)
		{
			_x = vector.x;
			_y = vector.y;
		}
		
		public static Vector2D operator+(Vector2D left, Vector2D right)
		{
			return new Vector2D(left.x + right.x, left.y + right.y);
		}

		public static Vector2D operator-(Vector2D left, Vector2D right)
		{
			return new Vector2D(left.x - right.x, left.y - right.y);
		}

		public static double operator*(Vector2D left, Vector2D right)
		{
			return left.x * right.x + left.y * right.y;
		}

		public static Vector2D operator*(Vector2D left, double scalar)
		{
			return new Vector2D(left.x * scalar, left.y * scalar);
		}

		public static Vector2D operator*(double scalar, Vector2D right)
		{
			return new Vector2D(right.x * scalar, right.y * scalar);
		}

		public static Vector2D operator<(Vector2D left, Vector2D right)
		{
			return min(left, right);
		}

		public static Vector2D operator>(Vector2D left, Vector2D right)
		{
			return max(left, right);
		}

		public static Vector2D min(Vector2D left, Vector2D right)
		{
			Vector2D final = new Vector2D();
			
			final.x	= (left.x < right.x) ? left.x : right.x;
			final.y	= (left.y < right.y) ? left.y : right.y;

			return final;
		}

		public static Vector2D max(Vector2D left, Vector2D right)
		{
			Vector2D final = new Vector2D();
			
			final.x	= (left.x > right.x) ? left.x : right.x;
			final.y	= (left.y > right.y) ? left.y : right.y;

			return final;
		}

		public double length_to_vector(Vector2D vector)
		{
			Vector2D final = this - vector;
			return final.length();
		}

		public double length()
		{
			return Math.Sqrt(_x * _x + _y * _y);
		}

		public void normalize()
		{
			double l = length();
			_x = _x / l;
			_y = _y / l;
		}

		public double x
		{
			get{return _x;}
			set{_x = value;}
		}

		public double y
		{
			get{return _y;}
			set{_y = value;}
		}
	}
}

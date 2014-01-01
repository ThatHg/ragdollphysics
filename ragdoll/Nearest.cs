using System.Collections.Generic;

namespace ragdoll
{
	public static class Nearest
	{
		private static int		_index;
		private static Vector2D	_nearest_point;
		private static bool		_busy	= false;

		private static void internal_check_nearest(Vector2D from, Vector2D to, int ragdoll_index)
		{
			if(_nearest_point == null || from.length_to_vector(to) < _nearest_point.length_to_vector(to))
			{
				_nearest_point = from;
				_index = ragdoll_index;
			}
		}

		public static void check_nearest(List<Skeleton> skeletons, Vector2D to)
		{
			if(!_busy)
			{
				foreach(Skeleton skeleton in skeletons)
				{
					Vector2D[] points = skeleton.points;
					for(int i = 0; i < points.Length; ++i)
					{
						internal_check_nearest(points[i], to, skeleton.index);
					}
				}
			}
			_busy = true;
		}

		public static void release_check()
		{
			_busy			= false;
			_nearest_point	= null;
			_index			= -1;
		}

		public static int index
		{
			get{ return _index; }
		}
	}
}

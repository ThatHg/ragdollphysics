using System;
using System.Drawing;

namespace ragdoll
{
	public class SnowGenerator
	{
		private Vector2D[]	_snow_old	= new Vector2D[400];
		private Vector2D[]	_snow_curr	= new Vector2D[400];
		private Vector2D[]	_forces		= new Vector2D[400];
		private double[]	_radius		= new double[400];
		private Vector2D	_gravity	= new Vector2D(0, 0.1);
		private bool		_is_on		= false;

		private void init()
		{
			Random rand = new Random();
			int NUM_POINTS = _snow_curr.Length;
			for(int i = 0; i < NUM_POINTS; ++i)
			{
				_snow_curr[i] = new Vector2D(rand.Next(20, 1270), -rand.Next(50, 2020));
				_snow_old[i] = _snow_curr[i];
				double r		= rand.Next(20, 60) * 0.1;
				_radius[i]		= r;
			}
		}

		private void accum_forces()
		{
			Random rand = new Random();
			int NUM_POINTS = _snow_curr.Length;
			for(int i = 0; i < NUM_POINTS; ++i)
			{
				_forces[i] = _gravity;

				double x = rand.Next(-100, 100) * 0.01;
				double y = rand.Next(-100, 100) * 0.01;

				_forces[i] += new Vector2D(x, y);
			}
		}

		private void verlet(double rate)
		{
			int NUM_POINTS = _snow_curr.Length;
			for(int i = 0; i < NUM_POINTS; ++i)
			{				
				
					Vector2D old_pos	= _snow_curr[i];

					// Verlet integration
					_snow_curr[i]		+= _snow_curr[i] - _snow_old[i] + _forces[i] * (rate * rate);

					// Store the old position until next calculation
					_snow_old[i]		= old_pos;
			}
		}

		private void handle_outside()
		{
			Random rand = new Random();
			int NUM_POINTS = _snow_curr.Length;
			for(int i = 0; i < NUM_POINTS; ++i)
			{
				if(_snow_curr[i].y > 720 + 2 * _radius[i])
				{
					_snow_old[i] = _snow_curr[i] = new Vector2D(rand.Next(20, 1270), -rand.Next(20, 100));
				}
			}
		}

		public SnowGenerator()
		{
			init();
		}

		public void update(double rate)
		{
			if(_is_on)
			{
				accum_forces();
				verlet(rate);
				handle_outside();
			}
		}

		public void draw()
		{
			int NUM_POINTS = _snow_curr.Length;
			for(int i = 0; i < NUM_POINTS; ++i)
			{
				GraphicsManager.draw_point(_snow_curr[i], Color.White, (float)(2 *_radius[i]));
			}
		}

		public void toggle()
		{
			init();
			_is_on = !_is_on;
		}

		public bool is_on
		{
			get{ return _is_on; }
		}
	}
}

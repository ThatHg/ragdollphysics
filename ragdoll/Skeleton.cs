using System;
using System.Drawing;
using System.Collections;
using System.Collections.Generic;

namespace ragdoll
{
	public class Skeleton
	{
		private enum ConstraintType
		{
			LINEAR,
			BEND
		}

		private struct Constraint
		{
			public ConstraintType _type;
			public int		_point_a;
			public int		_point_b;
			public double	_rest_length;
		}

		private struct Force
		{
			public int		_index;
			public Vector2D _force;
		}
		
		private struct Point
		{
			public Vector2D	_pos;
			public double	_mass;
			public double	_inv_mass;
		}

		// Position of connecting points in the skeleton
		private Point[]			_pos			= new Point[16];
		private Point[]			_old_pos		= new Point[16];

		// Linear constraints between specific points in skeleton
		// And 14 extra for bending constraints at joints
		private Constraint[]	_constraints	= new Constraint[18 + 13];
		
		// Store a summation force for every point in skeleton
		private Vector2D[]		_forces			= new Vector2D[16];

		// Climbing points
		private Vector2D[]		_grips			= new Vector2D[40];
		private float[]			_grip_radius	= new float[40];

		// Stores forces in a queue, mostly external forces like drag skeleton around with mouse
		private Queue<Force>	_add_forces		= new Queue<Force>();

		// Pinned points to wall, immovable points
		private List<int>		_pinned_points	= new List<int>();

		// Gravity force acting on skeleton
		private Vector2D		_gravity		= new Vector2D(0, 3.82);

		// Window constraints
		private Vector2D		_lt				= new Vector2D(0,0);
		private Vector2D		_rb				= new Vector2D(1280, 720);

		// Toggle debug drawings
		private bool			_debug_forces	= false;
		private bool			_debug_bones	= false;

		// Fun cristmas mode toggeler
		private bool			_christmas		= false;

		// If true render ragdoll body
		private bool			_graphics		= true;

		// store current nearest point_index to mouse
		private int				_mouse_point	= 0;
		
		// When mouse down, lock current point
		private bool			_lock_point		= false;

		// Activated climbing mode
		private bool			_climbing_mode	= false;

		// Index of this skeleton
		private int				_index;

		// Current avaible index
		public static int		_current_index;

		// Do verlet integration
		private void verlet(double rate)
		{
			int NUM_POINTS = _pos.Length;
			for(int i = 0; i < NUM_POINTS; ++i)
			{				
				// Store position as old position
				if(!_pinned_points.Contains(i))
				{
					Vector2D old_pos	= _pos[i]._pos;

					// Verlet integration
					_pos[i]._pos	+= _pos[i]._pos - _old_pos[i]._pos + _forces[i] * (rate * rate);

					// Store the old position until next calculation
					_old_pos[i]._pos	= old_pos;
				}
			}
		}

		private void update_constaints(int iterations)
		{
			for(int j = 0; j < iterations; ++j)
			{
				// Keep bone lengts and restrain freedom of movement
				int NUM_CONSTR = _constraints.Length;
				for(int i = NUM_CONSTR - 1; i >= 0; --i)
				{
					Point		point_a	= _pos[_constraints[i]._point_a];
					Point		point_b	= _pos[_constraints[i]._point_b];

					// Just skip to next constraint if both inv_masses are 0
					if(point_a._inv_mass + point_b._inv_mass == 0)
						continue;

					Vector2D	delta	= point_b._pos - point_a._pos;

					double diff = 0;
					double rest_length = _constraints[i]._rest_length;

					switch(_constraints[i]._type)
					{
						case ConstraintType.LINEAR:
							// contstraint with masses from Jacobsen http://graphics.cs.cmu.edu/nsp/course/15-869/2006/papers/jakobsen.htm
							double deltalength = Math.Sqrt(delta*delta);
							diff = (deltalength-rest_length)/
							(deltalength * (point_a._inv_mass + point_b._inv_mass));
							_pos[_constraints[i]._point_a]._pos += point_a._inv_mass*delta*diff;
							_pos[_constraints[i]._point_b]._pos -= point_b._inv_mass*delta*diff;

							break;

						case ConstraintType.BEND:
							
							double curr_length = point_b._pos.length_to_vector(point_a._pos);
							if(curr_length < rest_length)
							{
								delta.normalize();
								diff = (rest_length - curr_length);

								double total_mass = point_a._inv_mass + point_b._inv_mass;

								_pos[_constraints[i]._point_a]._pos -= delta * diff * (point_a._inv_mass / total_mass);
								_pos[_constraints[i]._point_b]._pos += delta * diff * (point_b._inv_mass / total_mass);
							}
							break;
						default:
							break;
						
					}
				}
				int NUM_POINTS = _pos.Length;
				for(int i = 0; i < NUM_POINTS; ++i)
				{
					// Min / Max constraint.
					// Ie. force skeleton inside a box
					_pos[i]._pos = (_pos[i]._pos > _lt) < _rb;
				}
			}
		}

		private void accumulate_forces()
		{
			// Gravity, this step also remove old forces
			int NUM_POINTS = _pos.Length;
			for(int i = 0; i < NUM_POINTS; ++i)
			{
				_forces[i] = _gravity * _pos[i]._mass;
			}
			while(_add_forces.Count != 0)
			{
				Force f = _add_forces.Dequeue();
				_forces[f._index] += f._force;
			}
		}

		private void internal_draw_body(Vector2D mouse)
		{
			// Hands
			GraphicsManager.draw_point(_pos[7]._pos, Color.Khaki, 20);
			GraphicsManager.draw_point(_pos[8]._pos, Color.Khaki, 20);

			// Head
			GraphicsManager.draw_point(_pos[0]._pos, Color.Khaki, 50);

			// Neck
			GraphicsManager.draw_line(_pos[0]._pos, _pos[1]._pos, Color.Khaki, 50);

			Vector2D moues_dir = mouse - _pos[0]._pos;
			Vector2D dir = _pos[1]._pos - _pos[0]._pos;
			Vector2D perp_vec = new Vector2D(dir.y, dir.x * -1);
			moues_dir.normalize();
			dir.normalize();
			perp_vec.normalize();

			Vector2D left_eye = _pos[0]._pos + perp_vec * 10;
			Vector2D right_eye = _pos[0]._pos + perp_vec * -10;

			// Christmas headpiece
			if(_christmas)
			{
				Vector2D[] hat_points		= new Vector2D[3];
				hat_points[0] = _pos[0]._pos + perp_vec * 20 + dir * -15;
				hat_points[1] = _pos[0]._pos + perp_vec * -20 + dir * -15;
				hat_points[2] = _pos[0]._pos + dir * -60;

				GraphicsManager.draw_triangle(hat_points, Color.Red);
				GraphicsManager.draw_point(hat_points[2], Color.White, 20);
				GraphicsManager.draw_line(hat_points[0], hat_points[1], Color.White, 10);
			}

			// Eyes
			GraphicsManager.draw_point(left_eye, Color.White, 20);
			GraphicsManager.draw_point(right_eye, Color.White, 20);
			GraphicsManager.draw_point(left_eye + moues_dir * 4, Color.Black, 10);
			GraphicsManager.draw_point(right_eye + moues_dir * 4, Color.Black, 10);

			Vector2D mouth_left		= _pos[0]._pos + (perp_vec * -10) + dir * 15;
			Vector2D mouth_right	= _pos[0]._pos + (perp_vec * 10) + dir * 15;
			
			// Mouth
			GraphicsManager.draw_line(mouth_left, mouth_right, Color.Black, 2);

			// Left upper arm
			GraphicsManager.draw_line(_pos[3]._pos, _pos[6]._pos, Color.Black, 50);

			// Right upper arm
			GraphicsManager.draw_line(_pos[2]._pos, _pos[5]._pos, Color.Black, 50);

			// Left lower arm
			GraphicsManager.draw_line(_pos[6]._pos, _pos[8]._pos, Color.Black, 50);

			// Right lower arm
			GraphicsManager.draw_line(_pos[5]._pos, _pos[7]._pos, Color.Black, 50);

			// Waist
			GraphicsManager.draw_line(_pos[11]._pos, _pos[12]._pos, Color.Red, 50);

			// Left upper leg
			GraphicsManager.draw_line(_pos[12]._pos, _pos[14]._pos, Color.Red, 50);

			// Right upper leg
			GraphicsManager.draw_line(_pos[11]._pos, _pos[13]._pos, Color.Red, 50);

			// Left lower leg
			GraphicsManager.draw_line(_pos[14]._pos, _pos[9]._pos, Color.Red, 50);

			// Right lower leg
			GraphicsManager.draw_line(_pos[13]._pos, _pos[15]._pos, Color.Red, 50);

			// Feets
			GraphicsManager.draw_point(_pos[9]._pos, Color.Khaki, 20);
			GraphicsManager.draw_point(_pos[15]._pos, Color.Khaki, 20);

			// Torso
			Vector2D[] left		= new Vector2D[3];
			Vector2D[] middle	= new Vector2D[3];
			Vector2D[] right	= new Vector2D[3];

			left[0] = _pos[1]._pos;
			left[1] = _pos[2]._pos;
			left[2] = _pos[11]._pos;

			middle[0] = _pos[11]._pos;
			middle[1] = _pos[12]._pos;
			middle[2] = _pos[1]._pos;

			right[0] = _pos[12]._pos;
			right[1] = _pos[3]._pos;
			right[2] = _pos[1]._pos;


			GraphicsManager.draw_triangle(left, Color.Black);
			GraphicsManager.draw_triangle(middle, Color.Black);
			GraphicsManager.draw_triangle(right, Color.Black);
		}

		private void internal_draw_forces()
		{
			int FORCES = _forces.Length;
			for(int i = 0; i < FORCES; ++i)
			{
				if(_forces[i] != null)
					GraphicsManager.draw_line(_pos[i]._pos, _pos[i]._pos + _forces[i], Color.Cyan, 3);
			}
		}

		private void internal_draw_points()
		{
			int NUM_POINTS = _pos.Length;
			for(int i = 0; i < NUM_POINTS; ++i)
			{
				// displays which point mouse_force is acting on
				if(i==_mouse_point)
					GraphicsManager.draw_point(_pos[i]._pos, Color.Blue, 14);
				else
					GraphicsManager.draw_point(_pos[i]._pos, Color.AntiqueWhite, 10);
			}
		}

		private void internal_draw_joints()
		{
			int NUM_COSTR = _constraints.Length - 1;
			for(int i = 0; i < NUM_COSTR; ++i)
			{	
				switch(_constraints[i]._type)
				{
					case ConstraintType.LINEAR:
						GraphicsManager.draw_line(_pos[_constraints[i]._point_a]._pos, _pos[_constraints[i]._point_b]._pos, Color.AntiqueWhite, 3);
						break;
					case ConstraintType.BEND:
						GraphicsManager.draw_line(_pos[_constraints[i]._point_a]._pos, _pos[_constraints[i]._point_b]._pos, Color.MediumVioletRed, 2);
						break;
				}
			}
		}

		private void internal_draw_grips()
		{
			for(int i = 0; i < _grips.Length; ++i)
			{
				GraphicsManager.draw_point(_grips[i], Color.Black, 2 * _grip_radius[i]);
			}
		}

		private void internal_draw(Vector2D mouse)
		{
			if(_climbing_mode)
			{
				internal_draw_grips();
			}
			if(_graphics)
			{
				internal_draw_body(mouse);
			}
			if(_debug_bones)
			{
				internal_draw_joints();
				internal_draw_points();
			}
			if(_debug_forces)
			{
				internal_draw_forces();
			}
		}

		private void internal_generate_climbing_points()
		{
			Random rand = new Random();
			for(int i = 0; i < _grips.Length; ++i)
			{
				// Generate grips inside screenspace
				_grips[i] = new Vector2D(rand.Next(0, 1280), rand.Next(0, 720));
				_grip_radius[i] = rand.Next(10, 18);
			}
		}

		private void internal_toggle_climbing_mode()
		{
			if(!_climbing_mode)
				internal_generate_climbing_points();

			_climbing_mode = !_climbing_mode;
		}

		private void internal_release_point_lock()
		{
			_lock_point = false;
			_mouse_point = 999999;
		}


		private void internal_mouse_force(Vector2D mouse_position)
		{
			if(Nearest.index != _index)
				return;
				
			Vector2D force = new Vector2D();

			// Skip this if mouse button continues to be
			// down.
			// This works as locking to nearest point
			// at first frame whem mouse button was down
			if(!_lock_point)
			{
				int POINT_COUNT	= _pos.Length;
				double nearest_l	= 99999999;

				// Find closest point to mouse
				for(int i = 0; i < POINT_COUNT; ++i)
				{
					double length = _pos[i]._pos.length_to_vector(mouse_position);

					if(length < nearest_l)
					{
						nearest_l = length;
						_mouse_point = i;
					}
				}
				_lock_point = true;
			}
			
			// Calculate a vector force between nearest point and mouse
			force = mouse_position - _pos[_mouse_point]._pos;

			// Register this force in the force queue
			internal_add_force(force, _mouse_point);
		}

		private void internal_pin_point()
		{
			// Makes a point immovable or movable
			if(_lock_point)
			{
				bool reached_grip = false;
				if(_climbing_mode)
				{
					for(int i = 0; i < _grips.Length; ++i)
						if(_mouse_point < _pos.Length && _pos[_mouse_point]._pos.length_to_vector(_grips[i]) < 1 + _grip_radius[i])
							reached_grip = true;
				}
				if(_pinned_points.Contains(_mouse_point) || (_climbing_mode && !reached_grip))
				{
					_pinned_points.Remove(_mouse_point);
					if(_pos[_mouse_point]._mass != 0)
						_pos[_mouse_point]._inv_mass = 1 / _pos[_mouse_point]._mass;
				}
				else
				{
					// If climbing mode is on then its only possible
					// to pin points at specific grip points
					// Only possible to grab with hands and feets
					if(!_climbing_mode || (reached_grip && (_mouse_point == 15 || _mouse_point == 9 || _mouse_point == 7 || _mouse_point == 8)))
					{
						_pinned_points.Add(_mouse_point);
						_pos[_mouse_point]._inv_mass = 0;
					}
				}
			}
		}

		private void internal_add_force(Vector2D force, int index)
		{
			Force my_force = new Force();
			my_force._force = force;
			my_force._index = index;
			_add_forces.Enqueue(my_force);
		}

		public Skeleton()
		{
			create_human();
			_index = _current_index;
			_current_index++;
		}

		// Draw the skeleton
		public void draw(Vector2D mouse)
		{
			internal_draw(mouse);
		}

		// Update skeleton, calculate forces, verlet integration satisfy constraints etc.
		public void update(double rate)
		{
			accumulate_forces();
			verlet(rate); // rate = frames/sec
			update_constaints(3); // Iterations = 3
		}

		// Add force on a point in skeleton
		public void add_force(Vector2D force, int index)
		{
			internal_add_force(force, index);
		}

		// Calculate force when draging around skeleton with mouse
		public void mouse_force(Vector2D mouse_position)
		{
			internal_mouse_force(mouse_position);
		}

		public void pin_point()
		{
			internal_pin_point();
		}

		public void toggle_forces()
		{
			_debug_forces = !_debug_forces;
		}

		public void toggle_bones()
		{
			_debug_bones = !_debug_bones;
		}

		public void toggle_climbing_mode()
		{
			internal_toggle_climbing_mode();
		}
		
		public void toggle_christmas_mode()
		{
			_christmas = !_christmas;
		}

		public void toggle_graphics()
		{
			_graphics = !_graphics;
		}

		// Releases the mouse point lock
		public void release_point_lock()
		{
			internal_release_point_lock();
		}

		// Construct a basic human skeleton
		private void create_human()
		{
			int NUM_CONSTR = _constraints.Length;
			for(int i = 0; i < NUM_CONSTR; ++i)
			{
				_constraints[i]._type = ConstraintType.LINEAR;
			}

			// Head/Neck
			double neck_ldt = 40;
			_pos[0]._pos		= new Vector2D(640, 360);
			_pos[1]._pos		= new Vector2D(640, 360 + neck_ldt);

			_constraints[0]._point_a = 0;
			_constraints[0]._point_b = 1;
			_constraints[0]._rest_length = _pos[0]._pos.length_to_vector(_pos[1]._pos);
			_constraints[0]._type = ConstraintType.LINEAR;

			// Shoulder
			double shou_wdt = 40;
			double shou_ypos = neck_ldt + 20;
			_pos[2]._pos		= new Vector2D(640 - shou_wdt, 360 + shou_ypos);
			_pos[3]._pos		= new Vector2D(640 + shou_wdt, 360 + shou_ypos);

			_constraints[1]._point_a = 1;
			_constraints[1]._point_b = 2;
			_constraints[1]._rest_length = _pos[1]._pos.length_to_vector(_pos[2]._pos);
			
			_constraints[2]._point_a = 1;
			_constraints[2]._point_b = 3;
			_constraints[2]._rest_length = _pos[1]._pos.length_to_vector(_pos[3]._pos);
			
			// Ribbcage
			double rib_size = shou_ypos + 70;
			_pos[4]._pos		= 	new Vector2D(640, 360 + rib_size);

			_constraints[3]._point_a = 1;
			_constraints[3]._point_b = 4;
			_constraints[3]._rest_length = _pos[1]._pos.length_to_vector(_pos[4]._pos);
			
			_constraints[15]._point_a = 2;
			_constraints[15]._point_b = 4;
			_constraints[15]._rest_length = _pos[2]._pos.length_to_vector(_pos[4]._pos);

			_constraints[16]._point_a = 3;
			_constraints[16]._point_b = 4;
			_constraints[16]._rest_length = _pos[3]._pos.length_to_vector(_pos[4]._pos);

			// Upper arms
			double u_arm_ldt = shou_ypos + 80;
			_pos[5]._pos		= 	new Vector2D(640 - shou_wdt, 360 + u_arm_ldt);
			_pos[6]._pos		= 	new Vector2D(640 + shou_wdt, 360 + u_arm_ldt);

			_constraints[4]._point_a = 2;
			_constraints[4]._point_b = 5;
			_constraints[4]._rest_length = _pos[2]._pos.length_to_vector(_pos[5]._pos);

			_constraints[5]._point_a = 3;
			_constraints[5]._point_b = 6;
			_constraints[5]._rest_length = _pos[3]._pos.length_to_vector(_pos[6]._pos);

			// Lower arms
			double l_arm_ldt = u_arm_ldt + 80;
			_pos[7]._pos		= 	new Vector2D(640 - shou_wdt, 360 + l_arm_ldt);
			_pos[8]._pos		= 	new Vector2D(640 + shou_wdt, 360 + l_arm_ldt);


			_constraints[6]._point_a = 5;
			_constraints[6]._point_b = 7;
			_constraints[6]._rest_length = _pos[5]._pos.length_to_vector(_pos[7]._pos);

			_constraints[7]._point_a = 6;
			_constraints[7]._point_b = 8;
			_constraints[7]._rest_length = _pos[6]._pos.length_to_vector(_pos[8]._pos);
			
			// Lower back
			double l_back_ldt = rib_size + 1;
			_pos[10]._pos	= new Vector2D(640, 360 + l_back_ldt);

			_constraints[8]._point_a = 4;
			_constraints[8]._point_b = 10;
			_constraints[8]._rest_length = _pos[4]._pos.length_to_vector(_pos[10]._pos);

			// Upper legs
			double u_legs = l_back_ldt + 30;
			double leg_away = 30;
			_pos[11]._pos	= new Vector2D(640 - leg_away, 360 + u_legs);
			_pos[12]._pos	= new Vector2D(640 + leg_away, 360 + u_legs);

			_constraints[9]._point_a = 10;
			_constraints[9]._point_b = 11;
			_constraints[9]._rest_length = _pos[10]._pos.length_to_vector(_pos[11]._pos);

			_constraints[10]._point_a = 10;
			_constraints[10]._point_b = 12;
			_constraints[10]._rest_length = _pos[10]._pos.length_to_vector(_pos[12]._pos);

			_constraints[17]._point_a = 11;
			_constraints[17]._point_b = 12;
			_constraints[17]._rest_length = _pos[11]._pos.length_to_vector(_pos[12]._pos);

			// Knees
			double knee = u_legs + 80;
			_pos[13]._pos	= new Vector2D(640 - leg_away, 360 + knee);
			_pos[14]._pos	= new Vector2D(640 + leg_away, 360 + knee);

			_constraints[11]._point_a = 11;
			_constraints[11]._point_b = 13;
			_constraints[11]._rest_length = _pos[11]._pos.length_to_vector(_pos[13]._pos);

			_constraints[12]._point_a = 12;
			_constraints[12]._point_b = 14;
			_constraints[12]._rest_length = _pos[12]._pos.length_to_vector(_pos[14]._pos);

			// Lower legs
			double l_legs = knee + 80;
			_pos[15]._pos	= new Vector2D(640 - leg_away, 360 + l_legs);
			_pos[9]._pos	= new Vector2D(640 + leg_away, 360 + l_legs);

			_constraints[13]._point_a = 13;
			_constraints[13]._point_b = 15;
			_constraints[13]._rest_length = _pos[13]._pos.length_to_vector(_pos[15]._pos);

			_constraints[14]._point_a = 14;
			_constraints[14]._point_b = 9;
			_constraints[14]._rest_length = _pos[14]._pos.length_to_vector(_pos[9]._pos);


			// All the angular constraints...

			// Head bendiness
			_constraints[18]._point_a = 0;
			_constraints[18]._point_b = 4;
			_constraints[18]._rest_length = _pos[0]._pos.length_to_vector(_pos[4]._pos) - 10;
			_constraints[18]._type = ConstraintType.BEND;

			// Left shoulder bendiness
			_constraints[19]._point_a = 5;
			_constraints[19]._point_b = 1;
			_constraints[19]._rest_length = 80;
			_constraints[19]._type = ConstraintType.BEND;

			// Right shoulder bendiness
			_constraints[20]._point_a = 6;
			_constraints[20]._point_b = 1;
			_constraints[20]._rest_length = 80;
			_constraints[20]._type = ConstraintType.BEND;

			// Left elbow bendiness
			_constraints[21]._point_a = 7;
			_constraints[21]._point_b = 2;
			_constraints[21]._rest_length = 45;
			_constraints[21]._type = ConstraintType.BEND;

			// Right elbow bendiness
			_constraints[22]._point_a = 8;
			_constraints[22]._point_b = 3;
			_constraints[22]._rest_length = 45;
			_constraints[22]._type = ConstraintType.BEND;

			// Rib-cage/Upperback bendiness
			_constraints[23]._point_a = 2;
			_constraints[23]._point_b = 11;
			_constraints[23]._rest_length = 80;
			_constraints[23]._type = ConstraintType.BEND;

			// Middleback bendiness
			_constraints[24]._point_a = 4;
			_constraints[24]._point_b = 10;
			_constraints[24]._rest_length = 0;
			_constraints[24]._type = ConstraintType.BEND;

			// Pelvis/Back bendiness
			_constraints[25]._point_a = 3;
			_constraints[25]._point_b = 12;
			_constraints[25]._rest_length = 80;
			_constraints[25]._type = ConstraintType.BEND;
			
			
			// Left leg/pelvis bendiness
			_constraints[26]._point_a = 10;
			_constraints[26]._point_b = 13;
			_constraints[26]._rest_length = _pos[10]._pos.length_to_vector(_pos[13]._pos) - 2;
			_constraints[26]._type = ConstraintType.BEND;

			// Right leg/pelvis bendiness
			_constraints[27]._point_a = 10;
			_constraints[27]._point_b = 14;
			_constraints[27]._rest_length = _pos[10]._pos.length_to_vector(_pos[14]._pos) - 2;
			_constraints[27]._type = ConstraintType.BEND;

			// Left knee bendiness
			_constraints[28]._point_a = 15;
			_constraints[28]._point_b = 11;
			_constraints[28]._rest_length = 40;
			_constraints[28]._type = ConstraintType.BEND;

			// Right knee bendiness
			_constraints[29]._point_a = 9;
			_constraints[29]._point_b = 12;
			_constraints[29]._rest_length = 40;
			_constraints[29]._type = ConstraintType.BEND;

			// Dont cross your knees
			_constraints[30]._point_a = 13;
			_constraints[30]._point_b = 14;
			_constraints[30]._rest_length = 30;
			_constraints[30]._type = ConstraintType.BEND;

			for(int i = 0; i < _pos.Length; ++i)
			{
				if(i == 0)
					_pos[i]._mass = 8;
				else
					_pos[i]._mass = 3;

				_pos[i]._inv_mass = 1 / _pos[i]._mass;
				_old_pos[i] = _pos[i];
			}
		}

		public Vector2D[] points
		{
			get
			{
				Vector2D[] positions = new Vector2D[_pos.Length];
				for(int i = 0; i < _pos.Length; ++i)
					positions[i] = _pos[i]._pos;

				return positions;
			}
		}

		public int index
		{
			get{ return _index; }
		}
	}
}

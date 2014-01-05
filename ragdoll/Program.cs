using System;
using System.Drawing;
using System.Collections.Generic;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;

namespace ragdoll
{
	class Program
	{
		private static List<Skeleton>	_skeletons				= new List<Skeleton>();
		private static SnowGenerator	_snow					= new SnowGenerator();
		private static bool				_pause					= false;
		private static double			_active_time			= 0;
		private static double			_ragdoll_release_time	= 0;
		private static double			_fps_timer				= 0;
		private static double			_fps					= 0;
		private static double			_avg_fps				= 0;
		private static int				_fps_qty				= 0;

		private static void draw_info()
		{
			GraphicsManager.draw_text(new Vector2D(0,0),
@" 
 F: Show forces		B: Show bones	G: Toggle Gfx	C: Climbing mode	M: Christmas mode
 P: Pause		R: Reset		A: Add doll	Esc: Exit

 R-MouseButton: Lock particle to background
 L-MouseButton: Drag nearest ragdoll

 Current FPS:	" + _fps + @"
 Average FPS:	" + average_fps(_fps) + @"
 Ragdoll Count:	" + _skeletons.Count, Color.Chocolate);
		}

		public static double average_fps(double fps)
		{
			++_fps_qty;
			_avg_fps = Math.Round(_avg_fps + (fps - _avg_fps) / _fps_qty, 2);

			return _avg_fps;
		}

		public static void reset()
		{
			_skeletons.Clear();
			_snow = null;
			GC.Collect();
			GC.WaitForFullGCComplete();
			_skeletons.Add(new Skeleton());
			_snow	= new SnowGenerator();
		}

		public static void add_doll()
		{
			_skeletons.Add(new Skeleton());
		}

		public static void Main()
		{
			using(var game = new GameWindow())
			{
				game.Load += (sender, e) =>
				{
					
					// Setup settings, load textures, sound etc.
					game.VSync	= VSyncMode.On;
					game.Width	= 1280;
					game.Height	= 720;

					// Center window to desktop
					game.Location = new Point(	OpenTK.DisplayDevice.Default.Width/2 - game.Width/2,
												OpenTK.DisplayDevice.Default.Height/2 - game.Height/2);
			
					GL.ClearColor(0.4f, 0.4f, 0.4f, 1);
					GraphicsManager.init_text_writer(game.ClientSize, game.ClientSize);
					_skeletons.Add(new Skeleton());
				};

				game.Resize += (sender, e) =>
				{
					int offset = 0;
					GL.Viewport(0 + offset, 0 + offset, game.Width + offset, game.Height + offset);
				};

				game.UpdateFrame += (sender, e) =>
				{
					// Add game logic, input handling
					if(game.Keyboard[Key.Escape])
					{
						game.Exit();
					}
					if(game.Keyboard[Key.R])
					{
						reset();
						Nearest.release_check();
					}
					if(game.Keyboard[Key.A] && _active_time < DateTime.Now.Ticks)
					{
						for (int i = 0; i < 20; ++i)
							add_doll();
						if(_active_time < DateTime.Now.Ticks + 2000000)
							_active_time = DateTime.Now.Ticks + 2000000;
					}
					if(game.Keyboard[Key.P] && _active_time < DateTime.Now.Ticks)
					{
						_pause = !_pause;
						if(_active_time < DateTime.Now.Ticks + 2000000)
							_active_time = DateTime.Now.Ticks + 2000000;
					}
					if(game.Keyboard[Key.F] && _active_time < DateTime.Now.Ticks)
					{
						for(int i = 0; i < _skeletons.Count; ++i)
							_skeletons[i].toggle_forces();
						if(_active_time < DateTime.Now.Ticks + 2000000)
							_active_time = DateTime.Now.Ticks + 2000000;
					}
					if(game.Keyboard[Key.B] && _active_time < DateTime.Now.Ticks)
					{
						for(int i = 0; i < _skeletons.Count; ++i)
							_skeletons[i].toggle_bones();
						if(_active_time < DateTime.Now.Ticks + 2000000)
							_active_time = DateTime.Now.Ticks + 2000000;
					}
					if(game.Keyboard[Key.G] && _active_time < DateTime.Now.Ticks)
					{
						for(int i = 0; i < _skeletons.Count; ++i)
							_skeletons[i].toggle_graphics();
						if(_active_time < DateTime.Now.Ticks + 2000000)
							_active_time = DateTime.Now.Ticks + 2000000;
					}
					if(game.Keyboard[Key.C] && _active_time < DateTime.Now.Ticks)
					{
						for(int i = 0; i < _skeletons.Count; ++i)
							_skeletons[i].toggle_climbing_mode();
						if(_active_time < DateTime.Now.Ticks + 2000000)
							_active_time = DateTime.Now.Ticks + 2000000;
					}
					if(game.Keyboard[Key.M] && _active_time < DateTime.Now.Ticks)
					{
						for(int i = 0; i < _skeletons.Count; ++i)
							_skeletons[i].toggle_christmas_mode();

						_snow.toggle();

						if(_active_time < DateTime.Now.Ticks + 2000000)
							_active_time = DateTime.Now.Ticks + 2000000;
					}
					if(game.Mouse[MouseButton.Right] && _active_time < DateTime.Now.Ticks)
					{
						for(int i = 0; i < _skeletons.Count; ++i)
							_skeletons[i].pin_point();
						if(_active_time < DateTime.Now.Ticks + 2000000)
							_active_time = DateTime.Now.Ticks + 2000000;
					}
					if(game.Mouse[MouseButton.Left])
					{
						// Finds the nearest ragdoll to mouse
						Nearest.check_nearest(_skeletons, new Vector2D(game.Mouse.X, game.Mouse.Y));
						if(_ragdoll_release_time < DateTime.Now.Ticks + 2000000)
							_ragdoll_release_time = DateTime.Now.Ticks + 2000000;

						// Adds a mouse drag force.
						for(int i = 0; i < _skeletons.Count; ++i)
							_skeletons[i].mouse_force(new Vector2D(game.Mouse.X, game.Mouse.Y));
					}
					else
					{
						// Makes it possible to drag around another ragdoll
						if(_ragdoll_release_time < DateTime.Now.Ticks)
							Nearest.release_check();

						for(int i = 0; i < _skeletons.Count; ++i)
							_skeletons[i].release_point_lock();
					}

					// Update skeleton
					if(!_pause)
					{
						for(int i = 0; i < _skeletons.Count; ++i)
							_skeletons[i].update(1.0/10.0);
						_snow.update(1.0/10.0);
					}
				};

				game.RenderFrame += (sender, e) =>
				{
					// Render graphics
					GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

					GL.MatrixMode(MatrixMode.Projection);
					GL.LoadIdentity();
					GL.Ortho(0, game.Width, game.Height, 0, -1, 1);
					
					// Render ragdolls
					for(int i = 0; i < _skeletons.Count; ++i)
							_skeletons[i].draw(new Vector2D(game.Mouse.X, game.Mouse.Y));

					// Render snow
					_snow.draw();

					// Some user friendly info
					if(_fps_timer < DateTime.Now.Ticks)
					{
						_fps_timer = DateTime.Now.Ticks + 800000;
						_fps = Math.Round(game.RenderFrequency, 2);
					}

					// Render help text
					draw_info();

                    game.SwapBuffers();
				};

				// Run the game at 60 updates per second
				game.Run(60.0);
			}
		}
	}
}

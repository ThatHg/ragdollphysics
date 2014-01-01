using System;
using System.Drawing;
using OpenTK.Graphics.OpenGL;
using OpenTK;

namespace ragdoll
{
	public static class GraphicsManager
	{
		private static TextWriter _text_writer;

		public static void init_text_writer(Size client, Size area)
		{
			_text_writer = new TextWriter(client, area);
		}

		public static void draw_point(Vector2D vec, Color c, float size)
		{
			// Configure point
			GL.Enable(EnableCap.PointSmooth);
			GL.PointSize(size);

			// Draw point
			GL.Begin(PrimitiveType.Points);
				GL.Color3(c);
				GL.Vertex2(vec.x, vec.y);				
			GL.End();
		}

		public static void draw_line(Vector2D vec_1, Vector2D vec_2, Color c, float width)
		{
			// Configure line
			GL.LineWidth(width);
			GL.Color3(c);

			// Draw line
			GL.Begin(PrimitiveType.Lines);
				GL.Vertex2(vec_1.x, vec_1.y);	// line start
				GL.Vertex2(vec_2.x, vec_2.y);	// line end
			GL.End();
		}

		public static void draw_triangle(Vector2D[] vertices, Color c)
		{
			if(vertices.Length <3)
				return;

			GL.Begin(PrimitiveType.Triangles);
				GL.Color3(c);
				GL.Vertex2(vertices[0].x, vertices[0].y);
				GL.Vertex2(vertices[1].x, vertices[1].y);
				GL.Vertex2(vertices[2].x, vertices[2].y);
			GL.End();
		}

		public static void draw_text(Vector2D p, string text, Color c)
		{
			if(text.Length < 1 || _text_writer == null) 
				return;
			_text_writer.Clear();
			_text_writer.AddLine(text, new PointF((float)p.x, (float)p.y), new SolidBrush(c));
			_text_writer.Draw();
		}
	}
}

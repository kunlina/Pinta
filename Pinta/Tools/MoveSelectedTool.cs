// 
// MoveSelectedTool.cs
//  
// Author:
//       Jonathan Pobst <monkey@jpobst.com>
// 
// Copyright (c) 2010 Jonathan Pobst
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using Cairo;
using Pinta.Core;

namespace Pinta
{
	public class MoveSelectedTool : BaseTool
	{
		private PointD origin_offset;
		private bool is_dragging;
		private bool has_uncommitted;
		
		public override string Name {
			get { return "Move Selected Pixels"; }
		}
		public override string Icon {
			get { return "Tools.Move.png"; }
		}
		public override string StatusBarText {
			get { return "Drag the selection to move. Drag the nubs to scale. Drag with right mouse button to rotate."; }
		}
		public override bool Enabled {
			get { return true; }
		}

		#region Mouse Handlers
		protected override void OnMouseDown (Gtk.DrawingArea canvas, Gtk.ButtonPressEventArgs args, Cairo.PointD point)
		{
			origin_offset = point;
			is_dragging = true;
			
			if (!has_uncommitted) {
				// Copy the selection to the temp layer
				PintaCore.Layers.CreateSelectionLayer ();
				PintaCore.Layers.ShowSelectionLayer = true;
				
				using (Cairo.Context g = new Cairo.Context (PintaCore.Layers.SelectionLayer.Surface)) {
					g.AppendPath (PintaCore.Layers.SelectionPath);
					g.SetSource (PintaCore.Layers.CurrentLayer.Surface);
					g.Clip ();
					g.Paint ();
				}			
				
				Cairo.ImageSurface surf = PintaCore.Layers.CurrentLayer.Surface;
				
				using (Cairo.Context g = new Cairo.Context (surf)) {
					g.AppendPath (PintaCore.Layers.SelectionPath);
					g.Operator = Cairo.Operator.Clear;
					g.Fill ();
				}
			}
			
			has_uncommitted = true;
			
			canvas.GdkWindow.Invalidate ();
		}

		protected override void OnMouseMove (object o, Gtk.MotionNotifyEventArgs args, Cairo.PointD point)
		{
			if (!is_dragging)
				return;
			
			PointD new_offset = new PointD (point.X, point.Y);
			
			double dx = origin_offset.X - new_offset.X;
			double dy = origin_offset.Y - new_offset.Y;
			
			using (Cairo.Context g = new Cairo.Context (PintaCore.Layers.CurrentLayer.Surface)) {
				g.AppendPath (PintaCore.Layers.SelectionPath);
				g.Translate (dx, dy);
				PintaCore.Layers.SelectionPath = g.CopyPath ();
			}

			PintaCore.Layers.SelectionLayer.Offset = new PointD (PintaCore.Layers.SelectionLayer.Offset.X - dx, PintaCore.Layers.SelectionLayer.Offset.Y - dy);
			
			origin_offset = new_offset;
			
			(o as Gtk.DrawingArea).GdkWindow.Invalidate ();
		}

		protected override void OnMouseUp (Gtk.DrawingArea canvas, Gtk.ButtonReleaseEventArgs args, Cairo.PointD point)
		{
			is_dragging = false;
		}
		#endregion

		protected override void OnDeactivated ()
		{
			
			base.OnDeactivated ();
			
			Layer layer = PintaCore.Layers.SelectionLayer;
			
			using (Cairo.Context g = new Cairo.Context (PintaCore.Layers.CurrentLayer.Surface)) {
				g.Save ();

				g.SetSourceSurface (layer.Surface, (int)layer.Offset.X, (int)layer.Offset.Y);
				g.PaintWithAlpha (layer.Opacity);
					
				g.Restore ();
			}
			
			PintaCore.Layers.DestroySelectionLayer ();

			has_uncommitted = false;
			PintaCore.Chrome.DrawingArea.GdkWindow.Invalidate ();
		}
	}
}

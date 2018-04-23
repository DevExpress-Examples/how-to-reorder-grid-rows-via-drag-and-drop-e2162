using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace DevExpress.AgDataGrid.Internal {
	public class DragControllerEx : DragController {
		FrameworkElement _dropContainer;

		public DragControllerEx(FrameworkElement dropContainer, IDragableObject dragableObject, Canvas surfaceCanvas, Point startPt, Point margin)
			: base(dragableObject, surfaceCanvas, startPt, margin) {
			this._dropContainer = dropContainer;
		}

		protected override IDropableObject FindDropableObject(Point point) {
            if(CachedDropableObjects == null)
                BuildDropableObjectsCache();
			FindDropableObjectsToCache(_dropContainer);
            IDropableObject obj = base.FindDropableObject(point); 			
			return obj;
		}
	}

	public class DragObject : IDragableObject {
		AgDataGridRow row;
		AgDataGrid source;
		object dataRow;

		public DragObject(AgDataGridRow row, object dataRow, AgDataGrid source) {
			this.source = source;
			this.row = row;
			this.dataRow = dataRow;
		}

		public AgDataGrid Source { get { return this.source; } }
		public AgDataGridRow Row { get { return this.row; } }
		public object DataRow { get { return this.dataRow; } }

		public FrameworkElement CreateDragShape()
		{
			Rectangle rect = new Rectangle();
			rect.Opacity = 0.8;
			rect.Width = this.row.ActualWidth;
			rect.Height = this.row.ActualHeight;

			LinearGradientBrush gradientBrush = new LinearGradientBrush();

			gradientBrush.StartPoint = new Point(0.5, 1);
			gradientBrush.EndPoint = new Point(0.5, 0);						

			gradientBrush.GradientStops.Add(new GradientStop(){Color=Color.FromArgb(0xFF, 0xBE, 0xDA, 0xF9)});
			gradientBrush.GradientStops.Add(new GradientStop(){Color=Color.FromArgb(0xFF, 0xD3, 0xE6, 0xFB), Offset=1});

			rect.Fill = gradientBrush;
			return rect;
		}
	}

	public delegate bool CanAcceptDelegate(IDragableObject dragObject, Point position);
	public delegate void AcceptDelegate(IDragableObject dragObject, Point position);

	public class DropController : IDropableObject {
		public static bool IsIn(FrameworkElement element, Point localPt) {
			return element != null && localPt.X > 0 && localPt.Y > 0 && localPt.X < element.ActualWidth && localPt.Y < element.ActualHeight;
		}
		
		public static Point TransformSurface(Point pt, object from, object to) {
			UIElement fromElement = from as UIElement;
			UIElement toElement = to as UIElement;
			if(fromElement == null || toElement == null) return pt;
			GeneralTransform gt = toElement.TransformToVisual(fromElement);
			return gt.Transform(pt);
		}

		public DropController(AgDataGrid dataGrid, FrameworkElement dropContainer, Canvas dragSurface) {
			this._dataGrid = dataGrid;
			if(this._dataGrid != null) {
                _dataGrid.DataRowCreated += new DataRowCreatedEventHandler(OnNewDataRow);				
				this._dataGrid.MouseMove += new MouseEventHandler(OnMouseMove);
				this._dataGrid.MouseLeftButtonUp += new MouseButtonEventHandler(OnMouseLeftButtonUp);
			}
			this._dragSurface = dragSurface;
			this._dropContainer = dropContainer;
		}

		AgDataGrid _dataGrid;
		public AgDataGrid DataGrid { get { return this._dataGrid; } }
		Canvas _dragSurface;
		public Canvas DragSurface{
			get { return _dragSurface; }
			set { _dragSurface = value; }
		}
		FrameworkElement _dropContainer;
		public FrameworkElement DropContainer {
			get { return _dropContainer; }
			set { _dropContainer = value; }
		}
		DragController dragController;
		protected DragController DragController { get { return dragController; } }
		public bool IsDragging { get { return DragController != null; } }

		IList<AgDataGridRow> dataRows = new List<AgDataGridRow>();
        void OnNewDataRow(object sender, DataRowCreatedEventArgs e)
        {
			foreach(AgDataGridRow dataRow in dataRows) {
				dataRow.MouseLeftButtonDown -= new MouseButtonEventHandler(OnMouseLeftButtonDown);
			}
			dataRows.Clear();
			AgDataGridRow row = e.Row as AgDataGridRow;
			if(row != null) {
				row.MouseLeftButtonDown += new MouseButtonEventHandler(OnMouseLeftButtonDown);
			}
		}

		void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
			if(e.Handled) return;
			StopDrag();
		}

		void OnMouseMove(object sender, MouseEventArgs e) {
			if(this.DragSurface == null) return;
			if(DragController != null) { DragController.Move(e.GetPosition(this.DragSurface)); }
		}

		void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
			if(e.Handled) return;
			StartDragging(sender as AgDataGridRow, e.GetPosition(e.OriginalSource as UIElement));
		}

		protected virtual void StopDrag() {
			if(DragController == null) return;
			DragController.StopDrag();
			this.dragController = null;
			this.DataGrid.ReleaseMouseCapture();
		}


		protected virtual void StartDragging(AgDataGridRow row, Point point) {
			if(this.DataGrid == null) return;
			if(this.DragSurface == null) return;			
			
			this.dragController = new DragControllerEx(
				DropContainer,
				new DragObject(row, DataGrid.GetDataRow(row.Handle), this.DataGrid),
				this.DragSurface, 
				point,
				new Point(this.DataGrid.Margin.Left, this.DataGrid.Margin.Top));
			
			DragController.StartDragShift = AgDataGrid.DefaultStartDragShift;
			DragController.ScrollOnDragging = null;
			this.DataGrid.CaptureMouse(); 
		}

		public event AcceptDelegate DragAccept;
		public event CanAcceptDelegate CanDragAccept;

		#region IDropableObject Members
		void IDropableObject.AcceptDrag(IDragableObject dragObject, Point position) {
			if(DragAccept != null) {
				DragAccept(dragObject, position);
			}
		}

		bool IDropableObject.CanAccept(IDragableObject dragObject, Point position) {
			if(CanDragAccept != null) {
				return CanDragAccept(dragObject, position);
			}
			return false;
		}

		bool IDropableObject.CanAccept() {
			return DragAccept != null && CanDragAccept != null;
		}

		FrameworkElement IDropableObject.GetThumbObject() {
			return null;
		}

		Rect IDropableObject.GetThumbRect(Point position) {
			return new Rect(0, 0, 0, 0);
		}
		#endregion
	}
}
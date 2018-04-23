using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DevExpress.AgDataGrid.Internal;
using DevExpress.AgDataGrid;

namespace SilverlightApplication1
{
    public partial class MainPage : UserControl, IDropableObject
    {
        DropController dropController;
        public MainPage()
        {
            InitializeComponent();

            dropController = new DropController(this.dataGrid, this, this.DragSurface);
            dataGrid.DataSource = new List<DataItem>() {
				new DataItem() { Value = "Item 0", SortOrder=0 },
				new DataItem() { Value = "Item 1", SortOrder=1 },
				new DataItem() { Value = "Item 2", SortOrder=2 },
				new DataItem() { Value = "Item 3", SortOrder=3 },
				new DataItem() { Value = "Item 4", SortOrder=4 },
			};

        }

        #region IDropableObject Members


        AgDataGridRow GetRowAtPos(Point p)
        {
            AgDataGridRow[] rows = VisualTreeHelper.FindElementsInHostCoordinates(p, dataGrid).OfType<AgDataGridRow>().ToArray();
            return (rows.Length != 0) ? rows[0] : null;
        }

        private void ResortGridData()
        {
            dataGrid.BeginRefresh();
            dataGrid.Columns["SortOrder"].SortOrder = DevExpress.AgData.ColumnSortOrder.None;
            dataGrid.Columns["SortOrder"].SortOrder = DevExpress.AgData.ColumnSortOrder.Ascending;
            dataGrid.Columns["SortOrder"].Visible = false;
            dataGrid.EndRefresh();
        }
        private void ChangeRowsOrder(int targetRowVisibleIndex, int draggedRowVisibleIndex)
        {
            for (int i = 0; i < dataGrid.VisibleRowCount; i++)
            {
                if (i >= targetRowVisibleIndex && i != draggedRowVisibleIndex)
                {
                    int rowHandle = dataGrid.GetRowHandleByVisibleIndex(i);
                    ((DataItem)dataGrid.GetDataRow(rowHandle)).SortOrder += 1;
                }
            }
        }

        private void CorrectRowsOrder()
        {
            for (int i = 0; i < dataGrid.VisibleRowCount; i++)
            {
                int rowHandle = dataGrid.GetRowHandleByVisibleIndex(i);
                ((DataItem)dataGrid.GetDataRow(rowHandle)).SortOrder = i;
            }
        }

        public void AcceptDrag(IDragableObject dragObject, Point position)
        {
            DragObject obj = dragObject as DragObject;
            if (obj == null)
            {
                return;
            }

            AgDataGridRow targetRow = GetRowAtPos(position);
            if (targetRow == null)
                return;

            CorrectRowsOrder();

            int targetSortOrder = ((DataItem)targetRow.DataContext).SortOrder;
            int targetRowVisibleIndex = dataGrid.GetRowVisibleIndex(targetRow.Handle);
            int draggedRowVisibleIndex = dataGrid.GetRowVisibleIndex(obj.Row.Handle);
            ((DataItem)obj.DataRow).SortOrder = targetSortOrder;
            
            //change rows order
            ChangeRowsOrder(targetRowVisibleIndex, draggedRowVisibleIndex);

            //resort grid data
            ResortGridData();
        }


        public bool CanAccept(IDragableObject dragObject, Point position)
        {
            DragObject obj = dragObject as DragObject;
            if (this.dataGrid == obj.Source)
            {
                Point localPt = DropController.TransformSurface(position, this.dataGrid, this.DragSurface);
                if (DropController.IsIn(this.dataGrid, localPt))
                {
                    return true;
                }
            }
            return false;
        }

        public bool CanAccept()
        {
            return true;
        }

        public FrameworkElement GetThumbObject()
        {
            return null;
        }

        public Rect GetThumbRect(Point position)
        {
            return new Rect();
        }

        #endregion
    }

    public class DataItem
    {
        public string Value
        {
            get;
            set;
        }

        public int SortOrder { get; set; }
    }
}

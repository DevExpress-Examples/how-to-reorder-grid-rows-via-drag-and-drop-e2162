Imports Microsoft.VisualBasic
Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Windows
Imports System.Windows.Controls
Imports System.Windows.Media
Imports DevExpress.AgDataGrid.Internal
Imports DevExpress.AgDataGrid
Imports DevExpress.Data

Namespace SilverlightApplication1
	Partial Public Class MainPage
		Inherits UserControl
		Implements IDropableObject
		Private dropController As DropController
		Public Sub New()
			InitializeComponent()

			dropController = New DropController(Me.dataGrid, Me, Me.DragSurface)
			dataGrid.DataSource = New List(Of DataItem)(New DataItem() {New DataItem() With {.Value = "Item 0", .SortOrder = 0}, New DataItem() With {.Value = "Item 1", .SortOrder = 1}, New DataItem() With {.Value = "Item 2", .SortOrder = 2}, New DataItem() With {.Value = "Item 3", .SortOrder = 3}, New DataItem() With {.Value = "Item 4", .SortOrder = 4}})

		End Sub

#Region "IDropableObject Members"


		Private Function GetRowAtPos(ByVal p As Point) As AgDataGridRow
			Dim rows() As AgDataGridRow = VisualTreeHelper.FindElementsInHostCoordinates(p, dataGrid).OfType(Of AgDataGridRow)().ToArray()
			Return If((rows.Length <> 0), rows(0), Nothing)
		End Function

		Private Sub ResortGridData()
			dataGrid.BeginRefresh()
			dataGrid.Columns("SortOrder").SortOrder = ColumnSortOrder.None
			dataGrid.Columns("SortOrder").SortOrder = ColumnSortOrder.Ascending
			dataGrid.Columns("SortOrder").Visible = False
			dataGrid.EndRefresh()
		End Sub
		Private Sub ChangeRowsOrder(ByVal targetRowVisibleIndex As Integer, ByVal draggedRowVisibleIndex As Integer)
			For i As Integer = 0 To dataGrid.VisibleRowCount - 1
				If i >= targetRowVisibleIndex AndAlso i <> draggedRowVisibleIndex Then
					Dim rowHandle As Integer = dataGrid.GetRowHandleByVisibleIndex(i)
					CType(dataGrid.GetDataRow(rowHandle), DataItem).SortOrder += 1
				End If
			Next i
		End Sub

		Private Sub CorrectRowsOrder()
			For i As Integer = 0 To dataGrid.VisibleRowCount - 1
				Dim rowHandle As Integer = dataGrid.GetRowHandleByVisibleIndex(i)
				CType(dataGrid.GetDataRow(rowHandle), DataItem).SortOrder = i
			Next i
		End Sub

		Public Sub AcceptDrag(ByVal dragObject As IDragableObject, ByVal position As Point) Implements IDropableObject.AcceptDrag
			Dim obj As DragObject = TryCast(dragObject, DragObject)
			If obj Is Nothing Then
				Return
			End If

			Dim targetRow As AgDataGridRow = GetRowAtPos(position)
			If targetRow Is Nothing Then
				Return
			End If

			CorrectRowsOrder()

			Dim targetSortOrder As Integer = (CType(targetRow.DataContext, DataItem)).SortOrder
			Dim targetRowVisibleIndex As Integer = dataGrid.GetRowVisibleIndex(targetRow.Handle)
			Dim draggedRowVisibleIndex As Integer = dataGrid.GetRowVisibleIndex(obj.Row.Handle)
			CType(obj.DataRow, DataItem).SortOrder = targetSortOrder

			'change rows order
			ChangeRowsOrder(targetRowVisibleIndex, draggedRowVisibleIndex)

			'resort grid data
			ResortGridData()
		End Sub


		Public Function CanAccept(ByVal dragObject As IDragableObject, ByVal position As Point) As Boolean Implements IDropableObject.CanAccept
			Dim obj As DragObject = TryCast(dragObject, DragObject)
			If Me.dataGrid Is obj.Source Then
				Dim localPt As Point = dropController.TransformSurface(position, Me.dataGrid, Me.DragSurface)
				If dropController.IsIn(Me.dataGrid, localPt) Then
					Return True
				End If
			End If
			Return False
		End Function

		Public Function CanAccept() As Boolean Implements IDropableObject.CanAccept
			Return True
		End Function

		Public Function GetThumbObject() As FrameworkElement Implements IDropableObject.GetThumbObject
			Return Nothing
		End Function

		Public Function GetThumbRect(ByVal position As Point) As Rect Implements IDropableObject.GetThumbRect
			Return New Rect()
		End Function

#End Region
	End Class

	Public Class DataItem
		Private privateValue As String
		Public Property Value() As String
			Get
				Return privateValue
			End Get
			Set(ByVal value As String)
				privateValue = value
			End Set
		End Property

		Private privateSortOrder As Integer
		Public Property SortOrder() As Integer
			Get
				Return privateSortOrder
			End Get
			Set(ByVal value As Integer)
				privateSortOrder = value
			End Set
		End Property
	End Class
End Namespace

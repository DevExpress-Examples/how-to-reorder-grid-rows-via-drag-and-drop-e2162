Imports Microsoft.VisualBasic
Imports System.Collections.Generic
Imports System.Windows
Imports System.Windows.Controls
Imports System.Windows.Input
Imports System.Windows.Media
Imports System.Windows.Shapes

Namespace DevExpress.AgDataGrid.Internal
	Public Class DragControllerEx
		Inherits DragController
		Private _dropContainer As FrameworkElement

		Public Sub New(ByVal dropContainer As FrameworkElement, ByVal dragableObject As IDragableObject, ByVal surfaceCanvas As Canvas, ByVal startPt As Point, ByVal margin As Point)
			MyBase.New(dragableObject, surfaceCanvas, startPt, margin)
			Me._dropContainer = dropContainer
		End Sub

		Protected Overrides Function FindDropableObject(ByVal point As Point) As IDropableObject
			If CachedDropableObjects Is Nothing Then
				BuildDropableObjectsCache()
			End If
			FindDropableObjectsToCache(_dropContainer)
			Dim obj As IDropableObject = MyBase.FindDropableObject(point)
			Return obj
		End Function
	End Class

	Public Class DragObject
		Implements IDragableObject
		Private row_Renamed As AgDataGridRow
		Private source_Renamed As AgDataGrid
		Private dataRow_Renamed As Object

		Public Sub New(ByVal row As AgDataGridRow, ByVal dataRow As Object, ByVal source As AgDataGrid)
			Me.source_Renamed = source
			Me.row_Renamed = row
			Me.dataRow_Renamed = dataRow
		End Sub

		Public ReadOnly Property Source() As AgDataGrid
			Get
				Return Me.source_Renamed
			End Get
		End Property
		Public ReadOnly Property Row() As AgDataGridRow
			Get
				Return Me.row_Renamed
			End Get
		End Property
		Public ReadOnly Property DataRow() As Object
			Get
				Return Me.dataRow_Renamed
			End Get
		End Property

		Public Function CreateDragShape() As FrameworkElement Implements IDragableObject.CreateDragShape
			Dim rect As New Rectangle()
			rect.Opacity = 0.8
			rect.Width = Row.ActualWidth
			rect.Height = Row.ActualHeight

			Dim gradientBrush As New LinearGradientBrush()

			gradientBrush.StartPoint = New Point(0.5, 1)
			gradientBrush.EndPoint = New Point(0.5, 0)

			gradientBrush.GradientStops.Add(New GradientStop() With {.Color = Color.FromArgb(&HFF, &HBE, &HDA, &HF9)})
			gradientBrush.GradientStops.Add(New GradientStop() With {.Color = Color.FromArgb(&HFF, &HD3, &HE6, &HFB), .Offset = 1})

			rect.Fill = gradientBrush
			Return rect
		End Function
	End Class

	Public Delegate Sub CanAcceptDelegate(ByVal dragObject As IDragableObject, ByVal position As Point, ByRef cancel As Boolean)
	Public Delegate Sub AcceptDelegate(ByVal dragObject As IDragableObject, ByVal position As Point)

	Public Class DropController
		Implements IDropableObject
		Public Shared Function IsIn(ByVal element As FrameworkElement, ByVal localPt As Point) As Boolean
			Return element IsNot Nothing AndAlso localPt.X > 0 AndAlso localPt.Y > 0 AndAlso localPt.X < element.ActualWidth AndAlso localPt.Y < element.ActualHeight
		End Function

		Public Shared Function TransformSurface(ByVal pt As Point, ByVal [from] As Object, ByVal [to] As Object) As Point
			Dim fromElement As UIElement = TryCast(from, UIElement)
			Dim toElement As UIElement = TryCast([to], UIElement)
			If fromElement Is Nothing OrElse toElement Is Nothing Then
				Return pt
			End If
			Dim gt As GeneralTransform = toElement.TransformToVisual(fromElement)
			Return gt.Transform(pt)
		End Function

		Public Sub New(ByVal dataGrid As AgDataGrid, ByVal dropContainer As FrameworkElement, ByVal dragSurface As Canvas)
			Me._dataGrid = dataGrid
			If Me._dataGrid IsNot Nothing Then
				AddHandler _dataGrid.DataRowCreated, AddressOf OnNewDataRow
				AddHandler _dataGrid.MouseMove, AddressOf OnMouseMove
				AddHandler _dataGrid.MouseLeftButtonUp, AddressOf OnMouseLeftButtonUp
			End If
			Me._dragSurface = dragSurface
			Me._dropContainer = dropContainer
		End Sub

		Private _dataGrid As AgDataGrid
		Public ReadOnly Property DataGrid() As AgDataGrid
			Get
				Return Me._dataGrid
			End Get
		End Property
		Private _dragSurface As Canvas
		Public Property DragSurface() As Canvas
			Get
				Return _dragSurface
			End Get
			Set(ByVal value As Canvas)
				_dragSurface = value
			End Set
		End Property
		Private _dropContainer As FrameworkElement
		Public Property DropContainer() As FrameworkElement
			Get
				Return _dropContainer
			End Get
			Set(ByVal value As FrameworkElement)
				_dropContainer = value
			End Set
		End Property
		Private dragController_Renamed As DragController
		Protected ReadOnly Property DragController() As DragController
			Get
				Return dragController_Renamed
			End Get
		End Property
		Public ReadOnly Property IsDragging() As Boolean
			Get
				Return DragController IsNot Nothing
			End Get
		End Property

		Private dataRows As IList(Of AgDataGridRow) = New List(Of AgDataGridRow)()
		Private Sub OnNewDataRow(ByVal sender As Object, ByVal e As DataRowCreatedEventArgs)
			For Each dataRow As AgDataGridRow In dataRows
				RemoveHandler dataRow.MouseLeftButtonDown, AddressOf OnMouseLeftButtonDown
			Next dataRow
			dataRows.Clear()
			Dim row As AgDataGridRow = TryCast(e.Row, AgDataGridRow)
			If row IsNot Nothing Then
				AddHandler row.MouseLeftButtonDown, AddressOf OnMouseLeftButtonDown
			End If
		End Sub

		Private Sub OnMouseLeftButtonUp(ByVal sender As Object, ByVal e As MouseButtonEventArgs)
			If e.Handled Then
				Return
			End If
			StopDrag()
		End Sub

		Private Sub OnMouseMove(ByVal sender As Object, ByVal e As MouseEventArgs)
			If Me.DragSurface Is Nothing Then
				Return
			End If
			If DragController IsNot Nothing Then
				DragController.Move(e.GetPosition(Me.DragSurface))
			End If
		End Sub

		Private Sub OnMouseLeftButtonDown(ByVal sender As Object, ByVal e As MouseButtonEventArgs)
			If e.Handled Then
				Return
			End If
			StartDragging(TryCast(sender, AgDataGridRow), e.GetPosition(TryCast(e.OriginalSource, UIElement)))
		End Sub

		Protected Overridable Sub StopDrag()
			If DragController Is Nothing Then
				Return
			End If
			DragController.StopDrag()
			Me.dragController_Renamed = Nothing
			Me.DataGrid.ReleaseMouseCapture()
		End Sub


		Protected Overridable Sub StartDragging(ByVal row As AgDataGridRow, ByVal point As Point)
			If Me.DataGrid Is Nothing Then
				Return
			End If
			If Me.DragSurface Is Nothing Then
				Return
			End If

			Me.dragController_Renamed = New DragControllerEx(DropContainer, New DragObject(row, DataGrid.GetDataRow(row.Handle), Me.DataGrid), Me.DragSurface, point, New Point(Me.DataGrid.Margin.Left, Me.DataGrid.Margin.Top))

			DragController.StartDragShift = AgDataGrid.DefaultStartDragShift
			DragController.ScrollOnDragging = Nothing
			Me.DataGrid.CaptureMouse()
		End Sub

		Public Event DragAccept As AcceptDelegate
		Public Event CanDragAccept As CanAcceptDelegate

#Region "IDropableObject Members"
		Private Sub AcceptDrag(ByVal dragObject As IDragableObject, ByVal position As Point) Implements IDropableObject.AcceptDrag
			RaiseEvent DragAccept(dragObject, position)
		End Sub

		Private Function CanAccept(ByVal dragObject As IDragableObject, ByVal position As Point) As Boolean Implements IDropableObject.CanAccept
			If CanDragAcceptEvent IsNot Nothing Then
				Dim cancel As Boolean = False
				CanDragAcceptEvent(dragObject, position, cancel)
				Return cancel
			End If
			Return False
		End Function

		Private Function CanAccept() As Boolean Implements IDropableObject.CanAccept
			Return DragAcceptEvent IsNot Nothing AndAlso CanDragAcceptEvent IsNot Nothing
		End Function

		Private Function GetThumbObject() As FrameworkElement Implements IDropableObject.GetThumbObject
			Return Nothing
		End Function

		Private Function GetThumbRect(ByVal position As Point) As Rect Implements IDropableObject.GetThumbRect
			Return New Rect(0, 0, 0, 0)
		End Function
#End Region
	End Class
End Namespace
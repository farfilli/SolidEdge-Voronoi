
Imports System.Reflection
Imports System.Windows
Imports System.Windows.Forms.VisualStyles.VisualStyleElement.Rebar
Imports System.Windows.Shapes
Imports csDelaunay
Imports SolidEdge_Voronoi.DelaunayVoronoi

Public Class Form1

    Dim delaunay As New DelaunayTriangulator()
    Dim voronoi As New DelaunayVoronoi.Voronoi()
    Dim PointCount As Int16
    Dim surface As Graphics = CreateGraphics()

    Dim points As List(Of DelaunayVoronoi.Point)
    Dim triangulation As IEnumerable(Of DelaunayVoronoi.Triangle)
    Dim voronoiEdges As IEnumerable(Of DelaunayVoronoi.Edge)

    Dim CSVoronoi As csDelaunay.Voronoi


    Private Sub Form1_Resize(sender As Object, e As EventArgs) Handles Me.Resize

        surface = CreateGraphics()

    End Sub

    Public ReadOnly Property DiagramWidth As Double
        Get
            Return CInt(Me.Width)
        End Get
    End Property

    Public ReadOnly Property DiagramHeight As Double
        Get
            Return CInt(Me.Height)
        End Get
    End Property

    Private Sub GenerateAndDraw()

        points = delaunay.GeneratePoints(PointCount, DiagramWidth, DiagramHeight)
        'generate_random_points(PointCount, DiagramWidth, DiagramHeight)

        triangulation = delaunay.BowyerWatson(points)
        voronoiEdges = voronoi.GenerateEdgesFromDelaunay(triangulation)

        Dim tmpPoints As New List(Of Numerics.Vector2)

        For Each tmpPoint In points
            Dim tmpVector2 As Numerics.Vector2 = New Numerics.Vector2(tmpPoint.X, tmpPoint.Y)
            tmpPoints.Add(tmpVector2)
        Next

        Dim tmpBounds As Rectf = New Rectf(0, 0, DiagramWidth, DiagramHeight)
        CSVoronoi = New csDelaunay.Voronoi(tmpPoints, tmpBounds, CInt(TB_Relaxation.Text))

        Repaint()

    End Sub

    Private Sub DrawTriangulation(ByVal triangulation As IEnumerable(Of DelaunayVoronoi.Triangle))

        Dim edges = New List(Of DelaunayVoronoi.Edge)()

        For Each triangle In triangulation
            edges.Add(New DelaunayVoronoi.Edge(triangle.Vertices(0), triangle.Vertices(1)))
            edges.Add(New DelaunayVoronoi.Edge(triangle.Vertices(1), triangle.Vertices(2)))
            edges.Add(New DelaunayVoronoi.Edge(triangle.Vertices(2), triangle.Vertices(0)))
        Next

        Dim pen1 As Pen = New Pen(Color.LightSteelBlue, 0.5)

        For Each edge In edges
            surface.DrawLine(pen1, CSng(edge.Point1.X), CSng(edge.Point1.Y), CSng(edge.Point2.X), CSng(edge.Point2.Y))
        Next

    End Sub

    'Private Sub DrawVoronoi(ByVal voronoiEdges As IEnumerable(Of DelaunayVoronoi.Edge))

    '    Dim pen1 As Pen = New Pen(Color.DarkViolet, 1)

    '    For Each edge In voronoiEdges
    '        surface.DrawLine(pen1, CSng(edge.Point1.X), CSng(edge.Point1.Y), CSng(edge.Point2.X), CSng(edge.Point2.Y))
    '    Next
    'End Sub

    Private Sub DrawVoronoi2(ByVal voronoiEdges As List(Of csDelaunay.Edge))

        Dim pen1 As Pen = New Pen(Color.DarkViolet, 1)

        For Each edge In voronoiEdges
            If Not IsNothing(edge.RightVertex) And Not IsNothing(edge.LeftVertex) Then surface.DrawLine(pen1, CSng(edge.RightVertex.Coord.X), CSng(edge.RightVertex.Coord.Y), CSng(edge.LeftVertex.Coord.X), CSng(edge.LeftVertex.Coord.Y))
        Next

    End Sub
    Private Sub DrawPoints(ByVal points As IEnumerable(Of DelaunayVoronoi.Point))

        Dim pen1 As Pen = New Pen(Color.Red, 2)

        'For Each point In points
        '    surface.DrawEllipse(pen1, CSng(point.X), CSng(point.Y), 1, 1)
        'Next

        For Each point In CSVoronoi.SiteCoords
            surface.DrawEllipse(pen1, CSng(point.X), CSng(point.Y), 1, 1)
        Next

    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click

        surface.Clear(Me.BackColor)
        surface.SmoothingMode = Drawing2D.SmoothingMode.AntiAlias

        GenerateAndDraw()

    End Sub

    Private Sub TB_Points_TextChanged(sender As Object, e As EventArgs) Handles TB_Points.TextChanged
        PointCount = CInt(TB_Points.Text)
    End Sub



    Private Sub CB_Points_CheckedChanged(sender As Object, e As EventArgs) Handles CB_Points.CheckedChanged

        If Not IsNothing(points) Then
            If points.Count <> 0 And CB_Points.Checked Then
                DrawPoints(points)
            Else
                Repaint()
            End If
        End If

    End Sub

    Private Sub CB_Triangles_CheckedChanged(sender As Object, e As EventArgs) Handles CB_Triangles.CheckedChanged

        If Not IsNothing(triangulation) Then
            If triangulation.Count <> 0 And CB_Triangles.Checked Then
                DrawTriangulation(triangulation)
            Else
                Repaint()
            End If
        End If

    End Sub

    Private Sub CB_Voronoi_CheckedChanged(sender As Object, e As EventArgs) Handles CB_Voronoi.CheckedChanged

        If Not IsNothing(voronoiEdges) Then
            If voronoiEdges.Count <> 0 And CB_Voronoi.Checked Then
                DrawVoronoi2(CSVoronoi.Edges)
            Else
                Repaint()
            End If
        End If

    End Sub
    Private Sub Repaint()

        surface.Clear(Me.BackColor)

        If CB_Triangles.Checked Then DrawTriangulation(triangulation)
        If CB_Circles.Checked Then DrawCircles(CSVoronoi.Circles)
        If CB_Voronoi.Checked Then DrawVoronoi2(CSVoronoi.Edges) 'DrawVoronoi(voronoiEdges)
        If CB_Points.Checked Then DrawPoints(points)


    End Sub

    Private Sub DrawCircles(circles As List(Of Circle))

        Dim pen2 As Pen = New Pen(Color.CornflowerBlue, 0.5)

        For Each Circle In CSVoronoi.Circles

            surface.DrawEllipse(pen2, Circle.center.X - Circle.radius, Circle.center.Y - Circle.radius, Circle.radius * 2, Circle.radius * 2)

        Next

    End Sub

    Private Sub BT_DrawInEdge_Click(sender As Object, e As EventArgs) Handles BT_DrawInEdge.Click

        Dim objApp As SolidEdgeFramework.Application

        Try
            objApp = GetObject(, "SolidEdge.Application")
        Catch ex As Exception
            MsgBox("Solid Edge must be running!", MsgBoxStyle.Exclamation, "Solid Edge Voronoi")
            Exit Sub
        End Try

        Dim objPar As SolidEdgePart.PartDocument = Nothing
        Dim objPsm As SolidEdgePart.SheetMetalDocument = Nothing
        Dim objSketch As Object = Nothing

        If objApp.Documents.Count = 0 Then
            MsgBox("Ordered Part or Sheetmetal should be open!", MsgBoxStyle.Exclamation, "Solid Edge Voronoi")
            Exit Sub
        End If

        Select Case objApp.ActiveDocumentType
            Case = SolidEdgeFramework.DocumentTypeConstants.igPartDocument
                objPar = objApp.ActiveDocument
            Case = SolidEdgeFramework.DocumentTypeConstants.igSheetMetalDocument
                objPsm = objApp.ActiveDocument
            Case Else
                MsgBox("Ordered Part or Sheetmetal should be open!", MsgBoxStyle.Exclamation, "Solid Edge Voronoi")
                Exit Sub
        End Select

        Select Case objApp.ActiveDocumentType
            Case = SolidEdgeFramework.DocumentTypeConstants.igPartDocument
                If objPar.ActiveSketch Is Nothing Then
                    MsgBox("A sketch must be active!", MsgBoxStyle.Exclamation, "Solid Edge Voronoi")
                    Exit Sub
                End If
                objSketch = objPar.ActiveSketch

            Case = SolidEdgeFramework.DocumentTypeConstants.igSheetMetalDocument
                If objPsm.ActiveSketch Is Nothing Then
                    MsgBox("A sketch must be active!", MsgBoxStyle.Exclamation, "Solid Edge Voronoi")
                    Exit Sub
                End If
                objSketch = objPsm.ActiveSketch

        End Select


        'Dim refPlanes As SolidEdgePart.RefPlanes = objPar.RefPlanes
        'Dim refPlane As SolidEdgePart.RefPlane = refPlanes.Item(1)

        Dim lines2d As SolidEdgeFrameworkSupport.Lines2d = objSketch.Lines2d

        For Each edge In CSVoronoi.Edges 'voronoiEdges
            If Not IsNothing(edge.RightVertex) And Not IsNothing(edge.LeftVertex) Then lines2d.AddBy2Points(CSng(edge.RightVertex.x / 1000), CSng(edge.RightVertex.y / 1000), CSng(edge.LeftVertex.x / 1000), CSng(edge.LeftVertex.y / 1000))
        Next

    End Sub

    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles Me.Load

        Me.Text += " - Version " & Assembly.GetExecutingAssembly().GetName().Version.Major & "." & Assembly.GetExecutingAssembly().GetName().Version.Minor

    End Sub

    Private Sub TB_Relaxation_TextChanged(sender As Object, e As EventArgs) Handles TB_Relaxation.TextChanged

        If Not IsNothing(points) And Not IsNothing(TB_Relaxation.Text) Then

            If IsNumeric(TB_Relaxation.Text) Then

                'CSVoronoi.LloydRelaxation(TB_Relaxation.Text) '' Non è possibile tornare indietro, i punti originali vengono sovrascritti

                Dim tmpPoints As New List(Of Numerics.Vector2)

                For Each tmpPoint In points
                    Dim tmpVector2 As Numerics.Vector2 = New Numerics.Vector2(tmpPoint.X, tmpPoint.Y)
                    tmpPoints.Add(tmpVector2)
                Next

                Dim tmpBounds As Rectf = New Rectf(0, 0, DiagramWidth, DiagramHeight)
                CSVoronoi = New csDelaunay.Voronoi(tmpPoints, tmpBounds, CInt(TB_Relaxation.Text))

                Repaint()

            End If

        End If

    End Sub

    Private Sub CB_Circles_CheckedChanged(sender As Object, e As EventArgs) Handles CB_Circles.CheckedChanged

        If Not IsNothing(CSVoronoi) Then

            Repaint()

        End If

    End Sub


    Public Sub generate_random_points(ByVal inpt_point_count As Integer, ByVal x_coord_limit As Integer, ByVal y_coord_limit As Integer)
        'delaunay_triangle = New Planar_object_store()
        Dim temp_pt_list As List(Of Numerics.Vector2) = New List(Of Numerics.Vector2)()
        Dim point_count As Integer = inpt_point_count

        Dim rand0 As Random = New Random()

        Do

            For i As Integer = 0 To point_count - 1
                Dim temp_pt As Numerics.Vector2
                Dim rand_pt As PointF = New PointF(rand0.[Next](-x_coord_limit, x_coord_limit), rand0.[Next](-y_coord_limit, y_coord_limit))
                temp_pt = New Numerics.Vector2(rand_pt.X, rand_pt.Y)
                temp_pt_list.Add(temp_pt)
            Next

            'temp_pt_list = temp_pt_list.Distinct(New Planar_object_store.points_equality_comparer()).ToList()
            point_count = inpt_point_count - temp_pt_list.Count
        Loop While point_count <> 0

        For Each tmpPoint In temp_pt_list
            Dim tmpDela As DelaunayVoronoi.Point = New DelaunayVoronoi.Point(tmpPoint.X, tmpPoint.Y)
            points.Add(tmpDela)
        Next

        'delaunay_triangle.delaunay_points = temp_pt_list
    End Sub


End Class

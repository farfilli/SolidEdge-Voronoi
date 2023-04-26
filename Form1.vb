
Imports System.Reflection
Imports System.Windows
Imports System.Windows.Shapes
Imports SolidEdge_Voronoi.DelaunayVoronoi

Public Class Form1

    Dim delaunay As New DelaunayTriangulator()
    Dim voronoi As New Voronoi()
    Dim PointCount As Int16
    Dim surface As Graphics = CreateGraphics()

    Dim points As IEnumerable(Of DelaunayVoronoi.Point)
    Dim triangulation As IEnumerable(Of DelaunayVoronoi.Triangle)
    Dim voronoiEdges As IEnumerable(Of DelaunayVoronoi.Edge)
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
        triangulation = delaunay.BowyerWatson(points)
        voronoiEdges = voronoi.GenerateEdgesFromDelaunay(triangulation)

        Repaint()

    End Sub

    Private Sub DrawTriangulation(ByVal triangulation As IEnumerable(Of Triangle))

        Dim edges = New List(Of Edge)()

        For Each triangle In triangulation
            edges.Add(New Edge(triangle.Vertices(0), triangle.Vertices(1)))
            edges.Add(New Edge(triangle.Vertices(1), triangle.Vertices(2)))
            edges.Add(New Edge(triangle.Vertices(2), triangle.Vertices(0)))
        Next

        Dim pen1 As Pen = New Pen(Color.LightSteelBlue, 0.5)

        For Each edge In edges
            surface.DrawLine(pen1, CSng(edge.Point1.X), CSng(edge.Point1.Y), CSng(edge.Point2.X), CSng(edge.Point2.Y))
        Next

    End Sub

    Private Sub DrawVoronoi(ByVal voronoiEdges As IEnumerable(Of Edge))

        Dim pen1 As Pen = New Pen(Color.DarkViolet, 1)

        For Each edge In voronoiEdges
            surface.DrawLine(pen1, CSng(edge.Point1.X), CSng(edge.Point1.Y), CSng(edge.Point2.X), CSng(edge.Point2.Y))
        Next
    End Sub

    Private Sub DrawPoints(ByVal points As IEnumerable(Of DelaunayVoronoi.Point))

        Dim pen1 As Pen = New Pen(Color.Red, 2)

        For Each point In points
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
                DrawVoronoi(voronoiEdges)
            Else
                Repaint()
            End If
        End If

    End Sub
    Private Sub Repaint()

        surface.Clear(Me.BackColor)

        If CB_Triangles.Checked Then DrawTriangulation(triangulation)
        If CB_Voronoi.Checked Then DrawVoronoi(voronoiEdges)
        If CB_Points.Checked Then DrawPoints(points)

    End Sub

    Private Sub BT_DrawInEdge_Click(sender As Object, e As EventArgs) Handles BT_DrawInEdge.Click

        Dim objApp As SolidEdgeFramework.Application = GetObject(, "SolidEdge.Application")
        Dim objPar As SolidEdgePart.PartDocument = objApp.ActiveDocument

        Dim refPlanes As SolidEdgePart.RefPlanes = objPar.RefPlanes
        Dim refPlane As SolidEdgePart.RefPlane = refPlanes.Item(1)

        Dim objSketch = objPar.ActiveSketch
        Dim lines2d As SolidEdgeFrameworkSupport.Lines2d = objSketch.Lines2d

        For Each edge In voronoiEdges
            lines2d.AddBy2Points(CSng(edge.Point1.X / 1000), CSng(edge.Point1.Y / 1000), CSng(edge.Point2.X / 1000), CSng(edge.Point2.Y / 1000))
        Next

    End Sub

    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles Me.Load

        Me.Text += " - Version " & Assembly.GetExecutingAssembly().GetName().Version.Major & "." & Assembly.GetExecutingAssembly().GetName().Version.Minor

    End Sub

End Class

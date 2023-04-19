Imports System
Imports System.Collections.Generic
Imports System.Linq

Namespace DelaunayVoronoi
    Public Class Triangle
        Public ReadOnly Property Vertices As Point() = New Point(2) {}
        Public Property Circumcenter As Point
        Public RadiusSquared As Double

        Public ReadOnly Property TrianglesWithSharedEdge As IEnumerable(Of Triangle)
            Get
                Dim neighbors = New HashSet(Of Triangle)()

                For Each vertex In Vertices
                    Dim tmp_trianglesWithSharedEdge = vertex.AdjacentTriangles.Where(Function(o) o <> Me AndAlso SharesEdgeWith(o))
                    neighbors.UnionWith(tmp_trianglesWithSharedEdge)
                Next

                Return neighbors
            End Get
        End Property

        Public Sub New(ByVal point1 As Point, ByVal point2 As Point, ByVal point3 As Point)
            If point1 = point2 OrElse point1 = point3 OrElse point2 = point3 Then
                Throw New ArgumentException("Must be 3 distinct points")
            End If

            If Not IsCounterClockwise(point1, point2, point3) Then
                Vertices(0) = point1
                Vertices(1) = point3
                Vertices(2) = point2
            Else
                Vertices(0) = point1
                Vertices(1) = point2
                Vertices(2) = point3
            End If

            Vertices(0).AdjacentTriangles.Add(Me)
            Vertices(1).AdjacentTriangles.Add(Me)
            Vertices(2).AdjacentTriangles.Add(Me)
            UpdateCircumcircle()
        End Sub

        Private Sub UpdateCircumcircle()
            Dim p0 = Vertices(0)
            Dim p1 = Vertices(1)
            Dim p2 = Vertices(2)
            Dim dA = p0.X * p0.X + p0.Y * p0.Y
            Dim dB = p1.X * p1.X + p1.Y * p1.Y
            Dim dC = p2.X * p2.X + p2.Y * p2.Y
            Dim aux1 = (dA * (p2.Y - p1.Y) + dB * (p0.Y - p2.Y) + dC * (p1.Y - p0.Y))
            Dim aux2 = -(dA * (p2.X - p1.X) + dB * (p0.X - p2.X) + dC * (p1.X - p0.X))
            Dim div = (2 * (p0.X * (p2.Y - p1.Y) + p1.X * (p0.Y - p2.Y) + p2.X * (p1.Y - p0.Y)))

            If div = 0 Then
                Throw New DivideByZeroException()
            End If

            Dim center = New Point(aux1 / div, aux2 / div)
            Circumcenter = center
            RadiusSquared = (center.X - p0.X) * (center.X - p0.X) + (center.Y - p0.Y) * (center.Y - p0.Y)
        End Sub

        Private Function IsCounterClockwise(ByVal point1 As Point, ByVal point2 As Point, ByVal point3 As Point) As Boolean
            Dim result = (point2.X - point1.X) * (point3.Y - point1.Y) - (point3.X - point1.X) * (point2.Y - point1.Y)
            Return result > 0
        End Function

        Public Function SharesEdgeWith(ByVal triangle As Triangle) As Boolean
            Dim sharedVertices = Vertices.Where(Function(o) triangle.Vertices.Contains(o)).Count()
            Return sharedVertices = 2
        End Function

        Public Function IsPointInsideCircumcircle(ByVal point As Point) As Boolean
            Dim d_squared = (point.X - Circumcenter.X) * (point.X - Circumcenter.X) + (point.Y - Circumcenter.Y) * (point.Y - Circumcenter.Y)
            Return d_squared < RadiusSquared
        End Function

        Public Shared Operator =(ByVal Triangle1 As Triangle, ByVal triangle2 As Triangle) As Boolean
            If Triangle1.Vertices(0) = triangle2.Vertices(0) And Triangle1.Vertices(1) = triangle2.Vertices(1) And Triangle1.Vertices(2) = triangle2.Vertices(2) Then
                Return True
            Else
                Return False
            End If
        End Operator

        Public Shared Operator <>(ByVal Triangle1 As Triangle, ByVal triangle2 As Triangle) As Boolean
            If Triangle1.Vertices(0) = triangle2.Vertices(0) And Triangle1.Vertices(1) = triangle2.Vertices(1) And Triangle1.Vertices(2) = triangle2.Vertices(2) Then
                Return False
            Else
                Return True
            End If
        End Operator

    End Class
End Namespace

Imports System
Imports System.Collections.Generic
Imports System.Linq

Namespace DelaunayVoronoi
    Public Class DelaunayTriangulator
        Private Property MaxX As Double
        Private Property MaxY As Double
        Private border As IEnumerable(Of Triangle)

        Public Function GeneratePoints(ByVal amount As Integer, ByVal maxX As Double, ByVal maxY As Double) As IEnumerable(Of Point)
            maxX = maxX
            maxY = maxY
            Dim point0 = New Point(0, 0)
            Dim point1 = New Point(0, maxY)
            Dim point2 = New Point(maxX, maxY)
            Dim point3 = New Point(maxX, 0)
            Dim points = New List(Of Point)() From {
                point0,
                point1,
                point2,
                point3
            }
            Dim tri1 = New Triangle(point0, point1, point2)
            Dim tri2 = New Triangle(point0, point2, point3)
            border = New List(Of Triangle)() From {
                tri1,
                tri2
            }
            Dim random = New Random()

            For i As Integer = 0 To amount - 4 - 1
                Dim pointX = random.NextDouble() * maxX
                Dim pointY = random.NextDouble() * maxY
                points.Add(New Point(pointX, pointY))
            Next

            Return points
        End Function

        Public Function BowyerWatson(ByVal points As IEnumerable(Of Point)) As IEnumerable(Of Triangle)
            Dim triangulation = New HashSet(Of Triangle)(border)

            For Each point In points
                Dim badTriangles = FindBadTriangles(point, triangulation)
                Dim polygon = FindHoleBoundaries(badTriangles)

                For Each triangle In badTriangles

                    For Each vertex In triangle.Vertices
                        vertex.AdjacentTriangles.Remove(triangle)
                    Next
                Next

                triangulation.RemoveWhere(Function(o) badTriangles.Contains(o))

                For Each edge In polygon.Where(Function(possibleEdge) possibleEdge.Point1 <> point AndAlso possibleEdge.Point2 <> point)
                    Dim triangle = New Triangle(point, edge.Point1, edge.Point2)
                    triangulation.Add(triangle)
                Next
            Next

            Return triangulation
        End Function

        Private Function FindHoleBoundaries(ByVal badTriangles As ISet(Of Triangle)) As List(Of Edge)
            Dim edges = New List(Of Edge)()

            For Each triangle In badTriangles
                edges.Add(New Edge(triangle.Vertices(0), triangle.Vertices(1)))
                edges.Add(New Edge(triangle.Vertices(1), triangle.Vertices(2)))
                edges.Add(New Edge(triangle.Vertices(2), triangle.Vertices(0)))
            Next

            Dim grouped = edges.GroupBy(Function(o) o)
            Dim boundaryEdges = edges.GroupBy(Function(o) o).Where(Function(o) o.Count() = 1).[Select](Function(o) o.First())
            Return boundaryEdges.ToList()
        End Function

        Private Function GenerateSupraTriangle() As Triangle
            Dim margin = 500
            Dim point1 = New Point(0.5 * MaxX, -2 * MaxX - margin)
            Dim point2 = New Point(-2 * MaxY - margin, 2 * MaxY + margin)
            Dim point3 = New Point(2 * MaxX + MaxY + margin, 2 * MaxY + margin)
            Return New Triangle(point1, point2, point3)
        End Function

        Private Function FindBadTriangles(ByVal point As Point, ByVal triangles As HashSet(Of Triangle)) As ISet(Of Triangle)
            Dim badTriangles = triangles.Where(Function(o) o.IsPointInsideCircumcircle(point))
            Return New HashSet(Of Triangle)(badTriangles)
        End Function
    End Class
End Namespace


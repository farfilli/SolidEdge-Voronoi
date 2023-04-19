Imports System.Collections.Generic

Namespace DelaunayVoronoi
    Public Class Voronoi
        Public Function GenerateEdgesFromDelaunay(ByVal triangulation As IEnumerable(Of Triangle)) As IEnumerable(Of Edge)
            Dim voronoiEdges = New HashSet(Of Edge)()

            For Each triangle In triangulation

                For Each neighbor In triangle.TrianglesWithSharedEdge
                    Dim edge = New Edge(triangle.Circumcenter, neighbor.Circumcenter)
                    voronoiEdges.Add(edge)
                Next
            Next

            Return voronoiEdges
        End Function
    End Class
End Namespace


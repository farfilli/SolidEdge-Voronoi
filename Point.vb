Imports System.Collections.Generic

Namespace DelaunayVoronoi
    Public Class Point
        Private Shared _counter As Integer
        Private ReadOnly _instanceId As Integer = Math.Min(System.Threading.Interlocked.Increment(_counter), _counter - 1)
        Public ReadOnly Property X As Double
        Public ReadOnly Property Y As Double
        Public ReadOnly Property AdjacentTriangles As HashSet(Of Triangle) = New HashSet(Of Triangle)()

        Public Sub New(ByVal _x As Double, ByVal _y As Double)
            X = _x
            Y = _y
        End Sub

        Public Overrides Function ToString() As String
            Return $"{NameOf(Point)} {_instanceId} {X}@{Y}"
        End Function

        Public Shared Operator =(ByVal Point1 As Point, ByVal Point2 As Point) As Boolean
            If Point1.X = Point2.X And Point1.Y = Point2.Y Then
                Return True
            Else
                Return False
            End If
        End Operator

        Public Shared Operator <>(ByVal Point1 As Point, ByVal Point2 As Point) As Boolean
            If Point1.X = Point2.X And Point1.Y = Point2.Y Then
                Return False
            Else
                Return True
            End If
        End Operator

    End Class
End Namespace


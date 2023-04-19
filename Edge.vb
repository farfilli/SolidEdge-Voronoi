Namespace DelaunayVoronoi
    Public Class Edge
        Public ReadOnly Property Point1 As Point
        Public ReadOnly Property Point2 As Point

        Public Sub New(ByVal _point1 As Point, ByVal _point2 As Point)
            Point1 = _point1
            Point2 = _point2
        End Sub

        Public Overrides Function Equals(ByVal obj As Object) As Boolean
            If obj Is Nothing Then Return False
            If obj.[GetType]() <> [GetType]() Then Return False
            Dim edge = TryCast(obj, Edge)
            Dim samePoints = Point1 = edge.Point1 AndAlso Point2 = edge.Point2
            Dim samePointsReversed = Point1 = edge.Point2 AndAlso Point2 = edge.Point1
            Return samePoints OrElse samePointsReversed
        End Function

        Public Overrides Function GetHashCode() As Integer
            Dim hCode As Integer = CInt(Point1.X) Xor CInt(Point1.Y) Xor CInt(Point2.X) Xor CInt(Point2.Y)
            Return hCode.GetHashCode()
        End Function
    End Class
End Namespace


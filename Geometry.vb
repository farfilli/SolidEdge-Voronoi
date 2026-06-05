Imports System
Imports System.Drawing
Imports System.Collections.Generic

Public Structure Vec2
    Public X As Double
    Public Y As Double

    Public Sub New(x As Double, y As Double)
        Me.X = x
        Me.Y = y
    End Sub

    Public Shared Operator +(a As Vec2, b As Vec2) As Vec2
        Return New Vec2(a.X + b.X, a.Y + b.Y)
    End Operator

    Public Shared Operator -(a As Vec2, b As Vec2) As Vec2
        Return New Vec2(a.X - b.X, a.Y - b.Y)
    End Operator

    Public Shared Operator *(a As Vec2, s As Double) As Vec2
        Return New Vec2(a.X * s, a.Y * s)
    End Operator

    Public Shared Operator /(a As Vec2, s As Double) As Vec2
        Return New Vec2(a.X / s, a.Y / s)
    End Operator
End Structure

Public Class VoronoiCell
    Public Property Seed As Vec2
    Public Property Vertices As List(Of Vec2)
End Class

Public Module Geo2D

    Public Function Dot(a As Vec2, b As Vec2) As Double
        Return a.X * b.X + a.Y * b.Y
    End Function

    Public Function Cross(a As Vec2, b As Vec2) As Double
        Return a.X * b.Y - a.Y * b.X
    End Function

    Public Function Length(v As Vec2) As Double
        Return Math.Sqrt(v.X * v.X + v.Y * v.Y)
    End Function

    Public Function Normalize(v As Vec2) As Vec2
        Dim l = Length(v)
        If l < 0.0000001 Then Return New Vec2(0, 0)
        Return v / l
    End Function

    Public Function Distance(a As Vec2, b As Vec2) As Double
        Return Length(a - b)
    End Function

    Public Function Clamp(value As Double, minValue As Double, maxValue As Double) As Double
        If value < minValue Then Return minValue
        If value > maxValue Then Return maxValue
        Return value
    End Function

    Public Function ClampF(value As Single, minValue As Single, maxValue As Single) As Single
        If value < minValue Then Return minValue
        If value > maxValue Then Return maxValue
        Return value
    End Function

    Public Function SignedArea(poly As IList(Of Vec2)) As Double
        If poly Is Nothing OrElse poly.Count < 3 Then Return 0.0

        Dim a As Double = 0.0
        For i As Integer = 0 To poly.Count - 1
            Dim j As Integer = (i + 1) Mod poly.Count
            a += poly(i).X * poly(j).Y - poly(j).X * poly(i).Y
        Next

        Return a * 0.5
    End Function

    Public Function PolygonCentroid(poly As IList(Of Vec2)) As Vec2
        If poly Is Nothing OrElse poly.Count = 0 Then Return New Vec2(0, 0)

        Dim area2 As Double = 0.0
        Dim cx As Double = 0.0
        Dim cy As Double = 0.0

        For i As Integer = 0 To poly.Count - 1
            Dim j As Integer = (i + 1) Mod poly.Count
            Dim cross = poly(i).X * poly(j).Y - poly(j).X * poly(i).Y
            area2 += cross
            cx += (poly(i).X + poly(j).X) * cross
            cy += (poly(i).Y + poly(j).Y) * cross
        Next

        Dim area = area2 * 0.5

        If Math.Abs(area) < 0.0000001 Then
            Dim sx As Double = 0.0
            Dim sy As Double = 0.0
            For Each p In poly
                sx += p.X
                sy += p.Y
            Next
            Return New Vec2(sx / poly.Count, sy / poly.Count)
        End If

        cx /= (6.0 * area)
        cy /= (6.0 * area)

        Return New Vec2(cx, cy)
    End Function

    Public Function PointLineDistance(p As Vec2, a As Vec2, b As Vec2) As Double
        Dim ab = b - a
        Dim ap = p - a
        Dim denom = Length(ab)
        If denom < 0.0000001 Then Return Distance(p, a)
        Return Math.Abs(Cross(ab, ap)) / denom
    End Function

    Public Function IntersectLines(p1 As Vec2, p2 As Vec2, p3 As Vec2, p4 As Vec2) As Vec2
        Dim r = p2 - p1
        Dim s = p4 - p3
        Dim denom = Cross(r, s)

        If Math.Abs(denom) < 0.0000001 Then
            Return New Vec2((p2.X + p3.X) * 0.5, (p2.Y + p3.Y) * 0.5)
        End If

        Dim t = Cross((p3 - p1), s) / denom
        Return p1 + r * t
    End Function

    Public Function GetBounds(points As IList(Of Vec2)) As RectangleF
        If points Is Nothing OrElse points.Count = 0 Then
            Return RectangleF.Empty
        End If

        Dim minX As Double = points(0).X
        Dim minY As Double = points(0).Y
        Dim maxX As Double = points(0).X
        Dim maxY As Double = points(0).Y

        For i As Integer = 1 To points.Count - 1
            Dim p = points(i)
            If p.X < minX Then minX = p.X
            If p.Y < minY Then minY = p.Y
            If p.X > maxX Then maxX = p.X
            If p.Y > maxY Then maxY = p.Y
        Next

        Return New RectangleF(CSng(minX), CSng(minY), CSng(maxX - minX), CSng(maxY - minY))
    End Function

    Public Function RemoveDuplicateSequentialPoints(points As IList(Of Vec2),
                                                    Optional eps As Double = 0.0001) As List(Of Vec2)
        Dim result As New List(Of Vec2)
        If points Is Nothing OrElse points.Count = 0 Then Return result

        For Each p In points
            If result.Count = 0 OrElse Distance(result(result.Count - 1), p) > eps Then
                result.Add(p)
            End If
        Next

        If result.Count > 1 AndAlso Distance(result(0), result(result.Count - 1)) <= eps Then
            result.RemoveAt(result.Count - 1)
        End If

        Return result
    End Function

    Public Function PointInPolygon(pt As Vec2, poly As IList(Of Vec2)) As Boolean
        If poly Is Nothing OrElse poly.Count < 3 Then Return False

        Dim inside As Boolean = False
        Dim j As Integer = poly.Count - 1

        For i As Integer = 0 To poly.Count - 1
            Dim pi = poly(i)
            Dim pj = poly(j)

            Dim intersects As Boolean =
                ((pi.Y > pt.Y) <> (pj.Y > pt.Y)) AndAlso
                (pt.X < (pj.X - pi.X) * (pt.Y - pi.Y) / ((pj.Y - pi.Y) + 0.0000000001) + pi.X)

            If intersects Then inside = Not inside
            j = i
        Next

        Return inside
    End Function

    Public Function PolygonContainsPolygon(container As IList(Of Vec2), candidate As IList(Of Vec2)) As Boolean
        If container Is Nothing OrElse candidate Is Nothing Then Return False
        If container.Count < 3 OrElse candidate.Count < 3 Then Return False

        Dim testPt As Vec2 = PolygonCentroid(candidate)

        If PointInPolygon(testPt, container) Then Return True

        For Each p In candidate
            If PointInPolygon(p, container) Then Return True
        Next

        Return False
    End Function

    Public Function PointInPolygonWithHoles(pt As Vec2,
                                        outer As IList(Of Vec2),
                                        holes As IList(Of List(Of Vec2))) As Boolean
        If Not PointInPolygon(pt, outer) Then Return False

        If holes IsNot Nothing Then
            For Each h In holes
                If h IsNot Nothing AndAlso h.Count >= 3 AndAlso PointInPolygon(pt, h) Then
                    Return False
                End If
            Next
        End If

        Return True
    End Function

    Public Function GetPolylineBounds(points As IList(Of Vec2)) As RectangleF
        Return GetBounds(points)
    End Function

End Module
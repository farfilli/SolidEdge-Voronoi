Imports System
Imports System.Collections.Generic
Imports System.Drawing
Imports Clipper2Lib

Public Module VoronoiEngine

    Private Const CLIP_SCALE As Double = 1000.0

    Public Function CreateSeeds(count As Integer, bounds As RectangleF, seed As Integer) As List(Of Vec2)
        Dim rng As New Random(seed)
        Dim pts As New List(Of Vec2)

        For i As Integer = 1 To count
            Dim x = bounds.Left + rng.NextDouble() * bounds.Width
            Dim y = bounds.Top + rng.NextDouble() * bounds.Height
            pts.Add(New Vec2(x, y))
        Next

        Return pts
    End Function

    Public Function CreateSeeds(count As Integer,
                            bounds As RectangleF,
                            outer As List(Of Vec2),
                            holes As List(Of List(Of Vec2)),
                            seed As Integer) As List(Of Vec2)

        Dim rng As New Random(seed)
        Dim pts As New List(Of Vec2)

        If outer Is Nothing OrElse outer.Count < 3 Then Return pts

        Dim maxAttempts As Integer = Math.Max(count * 200, 2000)
        Dim attempts As Integer = 0

        While pts.Count < count AndAlso attempts < maxAttempts
            Dim x = bounds.Left + rng.NextDouble() * bounds.Width
            Dim y = bounds.Top + rng.NextDouble() * bounds.Height
            Dim p As New Vec2(x, y)

            If Geo2D.PointInPolygonWithHoles(p, outer, holes) Then
                pts.Add(p)
            End If

            attempts += 1
        End While

        Return pts
    End Function

    Public Function BuildCells(seeds As List(Of Vec2), bounds As RectangleF) As List(Of VoronoiCell)
        Dim cells As New List(Of VoronoiCell)

        If seeds Is Nothing OrElse seeds.Count = 0 Then Return cells

        Dim rectPoly As New List(Of Vec2) From {
            New Vec2(bounds.Left, bounds.Top),
            New Vec2(bounds.Right, bounds.Top),
            New Vec2(bounds.Right, bounds.Bottom),
            New Vec2(bounds.Left, bounds.Bottom)
        }

        For i As Integer = 0 To seeds.Count - 1
            Dim poly As New List(Of Vec2)(rectPoly)

            For j As Integer = 0 To seeds.Count - 1
                If i = j Then Continue For
                poly = ClipWithBisector(poly, seeds(i), seeds(j))
                If poly.Count = 0 Then Exit For
            Next

            poly = CleanPolygon(poly)

            If poly.Count >= 3 Then
                cells.Add(New VoronoiCell With {
                    .Seed = seeds(i),
                    .Vertices = poly
                })
            End If
        Next

        Return cells
    End Function

    Public Function BuildCells(seeds As List(Of Vec2),
                           outer As List(Of Vec2),
                           holes As List(Of List(Of Vec2))) As List(Of VoronoiCell)

        Dim cells As New List(Of VoronoiCell)
        If seeds Is Nothing OrElse seeds.Count = 0 Then Return cells
        If outer Is Nothing OrElse outer.Count < 3 Then Return cells

        For i As Integer = 0 To seeds.Count - 1
            Dim poly As New List(Of Vec2)(outer)

            For j As Integer = 0 To seeds.Count - 1
                If i = j Then Continue For
                poly = ClipWithBisector(poly, seeds(i), seeds(j))
                If poly.Count = 0 Then Exit For
            Next

            poly = CleanPolygon(poly)
            If poly.Count < 3 Then Continue For

            Dim clippedParts As List(Of List(Of Vec2)) = SubtractHolesFromPolygon(poly, holes)

            For Each part In clippedParts
                Dim clean = CleanPolygon(part)
                If clean.Count < 3 Then Continue For

                If Math.Abs(Geo2D.SignedArea(clean)) > 1.0 Then
                    cells.Add(New VoronoiCell With {
                    .Seed = seeds(i),
                    .Vertices = clean
                })
                End If
            Next
        Next

        Return cells
    End Function

    Private Function SubtractHolesFromPolygon(subject As List(Of Vec2),
                                          holes As List(Of List(Of Vec2))) As List(Of List(Of Vec2))

        Dim result As New List(Of List(Of Vec2))

        If subject Is Nothing OrElse subject.Count < 3 Then
            Return result
        End If

        Dim subj As New Paths64 From {
        ToPath64(subject)
    }

        Dim clips As New Paths64()

        If holes IsNot Nothing Then
            For Each h In holes
                If h Is Nothing OrElse h.Count < 3 Then Continue For
                clips.Add(ToPath64(h))
            Next
        End If

        If clips.Count = 0 Then
            result.Add(New List(Of Vec2)(subject))
            Return result
        End If

        Dim diff As Paths64 = Clipper.Difference(subj, clips, FillRule.NonZero)

        If diff Is Nothing OrElse diff.Count = 0 Then
            Return result
        End If

        ' NORMALIZZAZIONE FONDAMENTALE PRIMA DELL'OFFSET
        Dim normalized As Paths64 = Clipper.Union(diff, FillRule.NonZero)

        For Each p As Path64 In normalized
            Dim poly = ToVec2List(p)
            poly = Geo2D.RemoveDuplicateSequentialPoints(poly)
            poly = CleanPolygon(poly)

            If poly.Count < 3 Then Continue For
            If Math.Abs(Geo2D.SignedArea(poly)) <= 1.0 Then Continue For
            If Geo2D.SignedArea(poly) < 0.0 Then
                poly.Reverse()
            End If
            result.Add(poly)
        Next

        Return result
    End Function

    Private Function ToPath64(points As List(Of Vec2)) As Path64
        Dim path As New Path64()

        For Each p In points
            path.Add(New Point64(
            CLng(Math.Round(p.X * CLIP_SCALE)),
            CLng(Math.Round(p.Y * CLIP_SCALE))
        ))
        Next

        Return path
    End Function

    Private Function ToVec2List(path As Path64) As List(Of Vec2)
        Dim pts As New List(Of Vec2)

        For Each p As Point64 In path
            pts.Add(New Vec2(
            p.X / CLIP_SCALE,
            p.Y / CLIP_SCALE
        ))
        Next

        Return pts
    End Function

    ' Offset robusto di un poligono tramite Clipper.
    ' delta < 0 = inset (rientro), delta > 0 = espansione. In world units.
    ' Restituisce 0..N anelli puliti: niente auto-intersezioni, e se l'inset
    ' "mangia" la cella il risultato e' vuoto (nessuno spigolo che sporge).
    Public Function OffsetPolygon(poly As List(Of Vec2), delta As Double) As List(Of List(Of Vec2))
        Dim result As New List(Of List(Of Vec2))
        If poly Is Nothing OrElse poly.Count < 3 Then Return result

        If Math.Abs(delta) < 0.0000001 Then
            result.Add(New List(Of Vec2)(poly))
            Return result
        End If

        ' Normalizzo a orientazione positiva: cosi' delta<0 = inset in modo affidabile.
        Dim work As New List(Of Vec2)(poly)
        If Geo2D.SignedArea(work) < 0.0 Then work.Reverse()

        Dim subj As New Paths64 From {ToPath64(work)}

        Dim solution As Paths64 = Clipper.InflatePaths(subj,
                                                       delta * CLIP_SCALE,
                                                       JoinType.Miter,
                                                       EndType.Polygon)

        If solution Is Nothing OrElse solution.Count = 0 Then Return result

        For Each p As Path64 In solution
            Dim v = ToVec2List(p)
            v = Geo2D.RemoveDuplicateSequentialPoints(v)
            If v.Count >= 3 Then result.Add(v)
        Next

        Return result
    End Function

    Private Function CellNeedsHoleClip(cellPoly As List(Of Vec2),
                                   hole As List(Of Vec2),
                                   seed As Vec2) As Boolean

        If cellPoly Is Nothing OrElse cellPoly.Count < 3 Then Return False
        If hole Is Nothing OrElse hole.Count < 3 Then Return False

        If Geo2D.PointInPolygon(seed, hole) Then Return True

        For Each p In cellPoly
            If Geo2D.PointInPolygon(p, hole) Then Return True
        Next

        For Each hp In hole
            If Geo2D.PointInPolygon(hp, cellPoly) Then Return True
        Next

        Return False
    End Function
    Private Function ClipPolygonOutsideHoleApprox(subject As List(Of Vec2),
                                              hole As List(Of Vec2)) As List(Of Vec2)

        If subject Is Nothing OrElse subject.Count < 3 Then
            Return New List(Of Vec2)
        End If

        If hole Is Nothing OrElse hole.Count < 3 Then
            Return New List(Of Vec2)(subject)
        End If

        Dim subjectCentroid As Vec2 = Geo2D.PolygonCentroid(subject)
        Dim holeCentroid As Vec2 = Geo2D.PolygonCentroid(hole)

        Dim bestA As Vec2 = hole(0)
        Dim bestB As Vec2 = hole(1)
        Dim bestDist As Double = Double.MaxValue

        For i As Integer = 0 To hole.Count - 1
            Dim a = hole(i)
            Dim b = hole((i + 1) Mod hole.Count)
            Dim mid As New Vec2((a.X + b.X) * 0.5, (a.Y + b.Y) * 0.5)
            Dim d As Double = Geo2D.Distance(mid, subjectCentroid)

            If d < bestDist Then
                bestDist = d
                bestA = a
                bestB = b
            End If
        Next

        Return ClipWithHalfPlaneKeepingSide(subject, bestA, bestB, subjectCentroid)
    End Function

    Private Function ClipWithHalfPlaneKeepingSide(subject As List(Of Vec2),
                                              a As Vec2,
                                              b As Vec2,
                                              keepPoint As Vec2) As List(Of Vec2)

        Dim output As New List(Of Vec2)
        If subject Is Nothing OrElse subject.Count = 0 Then Return output

        Dim keepSign As Double = EdgeSide(keepPoint, a, b)

        Dim prev As Vec2 = subject(subject.Count - 1)
        Dim prevInside As Boolean = SameSideOrOn(prev, a, b, keepSign)

        For i As Integer = 0 To subject.Count - 1
            Dim curr As Vec2 = subject(i)
            Dim currInside As Boolean = SameSideOrOn(curr, a, b, keepSign)

            If currInside Then
                If Not prevInside Then
                    Dim inter As Vec2
                    If TryLineIntersectionInfinite(prev, curr, a, b, inter) Then
                        output.Add(inter)
                    End If
                End If
                output.Add(curr)

            ElseIf prevInside Then
                Dim inter As Vec2
                If TryLineIntersectionInfinite(prev, curr, a, b, inter) Then
                    output.Add(inter)
                End If
            End If

            prev = curr
            prevInside = currInside
        Next

        Return output
    End Function

    Private Function EdgeSide(p As Vec2, a As Vec2, b As Vec2) As Double
        Return (b.X - a.X) * (p.Y - a.Y) - (b.Y - a.Y) * (p.X - a.X)
    End Function

    Private Function SameSideOrOn(p As Vec2, a As Vec2, b As Vec2, refSign As Double) As Boolean
        Dim s As Double = EdgeSide(p, a, b)
        If Math.Abs(s) <= 0.000001 Then Return True
        If Math.Abs(refSign) <= 0.000001 Then Return True
        Return (s > 0 AndAlso refSign > 0) OrElse (s < 0 AndAlso refSign < 0)
    End Function

    Private Function ClipOutsideEdge(subject As List(Of Vec2),
                                 a As Vec2,
                                 b As Vec2,
                                 holeIsCCW As Boolean) As List(Of Vec2)

        Dim output As New List(Of Vec2)
        If subject Is Nothing OrElse subject.Count = 0 Then Return output

        Dim prev As Vec2 = subject(subject.Count - 1)
        Dim prevInside As Boolean = IsOutsideHoleEdge(prev, a, b, holeIsCCW)

        For i As Integer = 0 To subject.Count - 1
            Dim curr As Vec2 = subject(i)
            Dim currInside As Boolean = IsOutsideHoleEdge(curr, a, b, holeIsCCW)

            If currInside Then
                If Not prevInside Then
                    Dim inter As Vec2
                    If TryLineIntersectionInfinite(prev, curr, a, b, inter) Then
                        output.Add(inter)
                    End If
                End If
                output.Add(curr)

            ElseIf prevInside Then
                Dim inter As Vec2
                If TryLineIntersectionInfinite(prev, curr, a, b, inter) Then
                    output.Add(inter)
                End If
            End If

            prev = curr
            prevInside = currInside
        Next

        Return output
    End Function

    Private Function IsOutsideHoleEdge(p As Vec2,
                                   a As Vec2,
                                   b As Vec2,
                                   holeIsCCW As Boolean) As Boolean

        Dim cross As Double = (b.X - a.X) * (p.Y - a.Y) - (b.Y - a.Y) * (p.X - a.X)

        If holeIsCCW Then
            Return cross <= 0.000001
        Else
            Return cross >= -0.000001
        End If
    End Function

    Private Function TryLineIntersectionInfinite(p1 As Vec2,
                                             p2 As Vec2,
                                             q1 As Vec2,
                                             q2 As Vec2,
                                             ByRef inter As Vec2) As Boolean

        inter = New Vec2(0, 0)

        Dim rx As Double = p2.X - p1.X
        Dim ry As Double = p2.Y - p1.Y
        Dim sx As Double = q2.X - q1.X
        Dim sy As Double = q2.Y - q1.Y

        Dim denom As Double = rx * sy - ry * sx
        If Math.Abs(denom) < 0.0000001 Then Return False

        Dim qpx As Double = q1.X - p1.X
        Dim qpy As Double = q1.Y - p1.Y

        Dim t As Double = (qpx * sy - qpy * sx) / denom

        If t >= -0.000001 AndAlso t <= 1.000001 Then
            inter = New Vec2(p1.X + t * rx, p1.Y + t * ry)
            Return True
        End If

        Return False
    End Function

    Public Function RelaxSeeds(cells As List(Of VoronoiCell)) As List(Of Vec2)
        Dim pts As New List(Of Vec2)
        If cells Is Nothing Then Return pts

        For Each c In cells
            If c.Vertices IsNot Nothing AndAlso c.Vertices.Count >= 3 Then
                pts.Add(Geo2D.PolygonCentroid(c.Vertices))
            End If
        Next

        Return pts
    End Function

    Private Function ClipWithBisector(poly As List(Of Vec2), a As Vec2, b As Vec2) As List(Of Vec2)
        Dim result As New List(Of Vec2)
        If poly Is Nothing OrElse poly.Count = 0 Then Return result

        Dim mid As New Vec2((a.X + b.X) * 0.5, (a.Y + b.Y) * 0.5)
        Dim n As New Vec2(b.X - a.X, b.Y - a.Y)

        Dim prev As Vec2 = poly(poly.Count - 1)
        Dim prevInside As Boolean = IsInside(prev, mid, n)

        For Each curr As Vec2 In poly
            Dim currInside As Boolean = IsInside(curr, mid, n)

            If currInside Then
                If Not prevInside Then
                    result.Add(LineIntersectionWithBisector(prev, curr, mid, n))
                End If
                result.Add(curr)
            ElseIf prevInside Then
                result.Add(LineIntersectionWithBisector(prev, curr, mid, n))
            End If

            prev = curr
            prevInside = currInside
        Next

        Return result
    End Function

    Private Function IsInside(p As Vec2, mid As Vec2, n As Vec2) As Boolean
        Dim d = (p.X - mid.X) * n.X + (p.Y - mid.Y) * n.Y
        Return d <= 0.000001
    End Function

    Private Function LineIntersectionWithBisector(p1 As Vec2, p2 As Vec2, mid As Vec2, n As Vec2) As Vec2
        Dim d As New Vec2(p2.X - p1.X, p2.Y - p1.Y)
        Dim denom = d.X * n.X + d.Y * n.Y

        If Math.Abs(denom) < 0.0000001 Then
            Return p1
        End If

        Dim t = -(((p1.X - mid.X) * n.X) + ((p1.Y - mid.Y) * n.Y)) / denom
        Return New Vec2(p1.X + d.X * t, p1.Y + d.Y * t)
    End Function

    Private Function CleanPolygon(poly As List(Of Vec2)) As List(Of Vec2)
        Dim cleaned As New List(Of Vec2)
        If poly Is Nothing OrElse poly.Count = 0 Then Return cleaned

        Const eps As Double = 0.0001

        For i As Integer = 0 To poly.Count - 1
            Dim p = poly(i)

            If cleaned.Count = 0 Then
                cleaned.Add(p)
            Else
                Dim q = cleaned(cleaned.Count - 1)
                If Geo2D.Distance(p, q) > eps Then cleaned.Add(p)
            End If
        Next

        If cleaned.Count > 1 AndAlso Geo2D.Distance(cleaned(0), cleaned(cleaned.Count - 1)) <= eps Then
            cleaned.RemoveAt(cleaned.Count - 1)
        End If

        Return cleaned
    End Function

End Module
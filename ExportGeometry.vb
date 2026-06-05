Imports System
Imports System.Collections.Generic
Imports System.Drawing

Public Enum ExportSegmentKind
    Line
    Arc
    CubicBezier
End Enum

Public MustInherit Class ExportSegment2D
    Public Property Kind As ExportSegmentKind
End Class

Public Class ExportLine2D
    Inherits ExportSegment2D

    Public Property P1 As Vec2
    Public Property P2 As Vec2

    Public Sub New()
        Kind = ExportSegmentKind.Line
    End Sub

    Public Sub New(a As Vec2, b As Vec2)
        Kind = ExportSegmentKind.Line
        P1 = a
        P2 = b
    End Sub
End Class

Public Class ExportArc2D
    Inherits ExportSegment2D

    Public Property Center As Vec2
    Public Property Radius As Double
    Public Property StartPoint As Vec2
    Public Property EndPoint As Vec2
    Public Property Clockwise As Boolean

    Public Sub New()
        Kind = ExportSegmentKind.Arc
    End Sub

    Public Sub New(center As Vec2, radius As Double, startPt As Vec2, endPt As Vec2, clockwise As Boolean)
        Kind = ExportSegmentKind.Arc
        Me.Center = center
        Me.Radius = radius
        Me.StartPoint = startPt
        Me.EndPoint = endPt
        Me.Clockwise = clockwise
    End Sub
End Class

Public Class ExportCubicBezier2D
    Inherits ExportSegment2D

    Public Property P0 As Vec2
    Public Property C1 As Vec2
    Public Property C2 As Vec2
    Public Property P3 As Vec2

    Public Sub New()
        Kind = ExportSegmentKind.CubicBezier
    End Sub

    Public Sub New(p0 As Vec2, c1 As Vec2, c2 As Vec2, p3 As Vec2)
        Kind = ExportSegmentKind.CubicBezier
        Me.P0 = p0
        Me.C1 = c1
        Me.C2 = c2
        Me.P3 = p3
    End Sub
End Class

Public Class ExportPath2D
    Public Property Segments As New List(Of ExportSegment2D)
    Public Property Closed As Boolean = True

    Public Property StrokeColor As Color = Color.Black
    Public Property StrokeWidth As Double = 1.0
    Public Property FillColor As Color = Color.Transparent
End Class

Public Module ExportGeometry

    Public Function BuildExportPaths(canvas As VoronoiCanvas) As List(Of ExportPath2D)
        Dim result As New List(Of ExportPath2D)
        If canvas Is Nothing OrElse canvas.Cells Is Nothing Then Return result

        Dim cellIndex As Integer = 0

        For Each cell In canvas.Cells
            If cell Is Nothing OrElse cell.Vertices Is Nothing OrElse cell.Vertices.Count < 3 Then Continue For

            Dim p As ExportPath2D = Nothing

            Select Case canvas.RenderStyle
                Case CellRenderStyle.Straight
                    p = BuildStraightCellPath(cell)

                Case CellRenderStyle.Curved
                    If canvas.InnerCornerMode = InnerCornerStyle.Arc Then
                        p = BuildInnerArcCellPath(cell, canvas.InnerOffset, canvas.CornerTrim)
                    Else
                        p = BuildInnerBezierCellPath(cell, canvas.InnerOffset, canvas.CornerTrim, canvas.BezierBulge)
                    End If

                Case CellRenderStyle.Circle
                    p = BuildCircleAsArcPath(cell, canvas.CellScale)

                Case CellRenderStyle.Square
                    p = BuildPolygonSymbolPath(cell, canvas.CellScale, canvas.RandomRotation, 4, 0.0, canvas.SymbolCornerMode, canvas.SymbolCornerTrim, canvas.SymbolBezierBulge)

                Case CellRenderStyle.RoundedSquare
                    p = BuildPolygonSymbolPath(cell, canvas.CellScale, canvas.RandomRotation, 4, 0.0, SymbolCornerStyle.FilletArc, Math.Max(canvas.SymbolCornerTrim, 0.18F), canvas.SymbolBezierBulge)

                Case CellRenderStyle.Triangle
                    p = BuildPolygonSymbolPath(cell, canvas.CellScale, canvas.RandomRotation, 3, -Math.PI / 2.0, canvas.SymbolCornerMode, canvas.SymbolCornerTrim, canvas.SymbolBezierBulge)

                Case CellRenderStyle.Pentagon
                    p = BuildPolygonSymbolPath(cell, canvas.CellScale, canvas.RandomRotation, 5, -Math.PI / 2.0, canvas.SymbolCornerMode, canvas.SymbolCornerTrim, canvas.SymbolBezierBulge)

                Case CellRenderStyle.Hexagon
                    p = BuildPolygonSymbolPath(cell, canvas.CellScale, canvas.RandomRotation, 6, 0.0, canvas.SymbolCornerMode, canvas.SymbolCornerTrim, canvas.SymbolBezierBulge)

                Case CellRenderStyle.Octagon
                    p = BuildPolygonSymbolPath(cell, canvas.CellScale, canvas.RandomRotation, 8, 0.0, canvas.SymbolCornerMode, canvas.SymbolCornerTrim, canvas.SymbolBezierBulge)

                Case CellRenderStyle.Star
                    p = BuildStarSymbolPath(cell, canvas.CellScale, canvas.RandomRotation, 5, 0.46, -Math.PI / 2.0, canvas.SymbolCornerMode, canvas.SymbolCornerTrim, canvas.SymbolBezierBulge)

                Case CellRenderStyle.Star3
                    p = BuildStarSymbolPath(cell, canvas.CellScale, canvas.RandomRotation, 3, 0.3, -Math.PI / 2.0, canvas.SymbolCornerMode, canvas.SymbolCornerTrim, canvas.SymbolBezierBulge)

                Case CellRenderStyle.Star4
                    p = BuildStarSymbolPath(cell, canvas.CellScale, canvas.RandomRotation, 4, 0.45, -Math.PI / 4.0, canvas.SymbolCornerMode, canvas.SymbolCornerTrim, canvas.SymbolBezierBulge)

            End Select

            If p IsNot Nothing AndAlso p.Segments.Count > 0 Then
                ApplyDefaultStyle(p, canvas, cellIndex)
                result.Add(p)
                cellIndex += 1
            End If
        Next

        Return result
    End Function

    Private Sub ApplyDefaultStyle(path As ExportPath2D, canvas As VoronoiCanvas, cellIndex As Integer)
        path.StrokeColor = GetExportColor(cellIndex)
        path.StrokeWidth = canvas.InnerCurveWidth
        path.FillColor = Color.Transparent
    End Sub

    Private Function GetExportColor(index As Integer) As Color
        Dim palette As Color() = {
            Color.FromArgb(0, 188, 212),
            Color.FromArgb(0, 150, 170),
            Color.FromArgb(110, 231, 243),
            Color.FromArgb(64, 224, 208),
            Color.FromArgb(26, 205, 192),
            Color.FromArgb(167, 255, 235),
            Color.FromArgb(0, 121, 140),
            Color.FromArgb(79, 195, 247),
            Color.FromArgb(111, 0, 168),
            Color.FromArgb(142, 36, 170),
            Color.FromArgb(49, 27, 146),
            Color.FromArgb(0, 70, 112),
            Color.FromArgb(38, 198, 218),
            Color.FromArgb(0, 200, 83),
            Color.FromArgb(198, 255, 140),
            Color.FromArgb(255, 202, 40)
        }

        Return palette(index Mod palette.Length)
    End Function

    Private Function BuildStraightCellPath(cell As VoronoiCell) As ExportPath2D
        Dim path As New ExportPath2D()
        Dim pts = cell.Vertices

        For i As Integer = 0 To pts.Count - 1
            Dim a = pts(i)
            Dim b = pts((i + 1) Mod pts.Count)
            path.Segments.Add(New ExportLine2D(a, b))
        Next

        Return path
    End Function

    Private Function BuildCircleAsArcPath(cell As VoronoiCell, scaleFactor As Single) As ExportPath2D
        Dim c As Vec2 = Geo2D.PolygonCentroid(cell.Vertices)
        Dim radius As Double = GetInscribedRadius(cell.Vertices, c)
        radius *= Math.Max(0.05, Math.Min(1.5, scaleFactor))

        Dim path As New ExportPath2D()
        Dim pR As New Vec2(c.X + radius, c.Y)
        Dim pL As New Vec2(c.X - radius, c.Y)

        path.Segments.Add(New ExportArc2D(c, radius, pR, pL, False))
        path.Segments.Add(New ExportArc2D(c, radius, pL, pR, False))

        Return path
    End Function

    Private Function BuildPolygonSymbolPath(cell As VoronoiCell,
                                            scaleFactor As Single,
                                            randomRotation As Boolean,
                                            sides As Integer,
                                            angleOffset As Double,
                                            cornerMode As SymbolCornerStyle,
                                            cornerTrim As Single,
                                            bezierBulge As Single) As ExportPath2D

        Dim c As Vec2 = Geo2D.PolygonCentroid(cell.Vertices)
        Dim radius As Double = GetInscribedRadius(cell.Vertices, c)
        radius *= Math.Max(0.05, Math.Min(1.5, scaleFactor))

        Dim angle As Double = 0.0
        If randomRotation Then angle = GetStableAngleFromSeed(cell.Seed)

        Dim pts = BuildRegularPolygonPointsWorld(c, radius, sides, angle + angleOffset)

        Select Case cornerMode
            Case SymbolCornerStyle.FilletArc
                Return BuildFilletPathFromPolygon(pts, cornerTrim)
            Case SymbolCornerStyle.Bezier
                Return BuildBezierPathFromPolygon(pts, cornerTrim, bezierBulge)
            Case Else
                Return BuildPathFromPolygon(pts)
        End Select
    End Function

    Private Function BuildStarSymbolPath(cell As VoronoiCell,
                                     scaleFactor As Single,
                                     randomRotation As Boolean,
                                     pointsCount As Integer,
                                     innerRatio As Double,
                                     angleOffset As Double,
                                     cornerMode As SymbolCornerStyle,
                                     cornerTrim As Single,
                                     bezierBulge As Single) As ExportPath2D

        Dim c As Vec2 = Geo2D.PolygonCentroid(cell.Vertices)
        Dim radius As Double = GetInscribedRadius(cell.Vertices, c)
        radius *= Math.Max(0.05, Math.Min(1.5, scaleFactor))

        Dim angle As Double = 0.0
        If randomRotation Then angle = GetStableAngleFromSeed(cell.Seed)

        Dim pts = BuildStarPointsWorld(c, radius, pointsCount, innerRatio, angle + angleOffset)

        Select Case cornerMode
            Case SymbolCornerStyle.FilletArc
                Return BuildFilletPathFromPolygon(pts, Math.Min(cornerTrim, 0.22F))
            Case SymbolCornerStyle.Bezier
                Return BuildBezierPathFromPolygon(pts, Math.Min(cornerTrim, 0.22F), bezierBulge)
            Case Else
                Return BuildPathFromPolygon(pts)
        End Select
    End Function

    Private Function BuildInnerArcCellPath(cell As VoronoiCell,
                                           insetWorld As Single,
                                           cornerTrim As Single) As ExportPath2D
        Dim basePoly = GetInsetOrBasePolygon(cell.Vertices, insetWorld)
        Return BuildFilletPathFromPolygon(basePoly, cornerTrim)
    End Function

    Private Function BuildInnerBezierCellPath(cell As VoronoiCell,
                                              insetWorld As Single,
                                              cornerTrim As Single,
                                              bezierBulge As Single) As ExportPath2D
        Dim basePoly = GetInsetOrBasePolygon(cell.Vertices, insetWorld)
        Return BuildBezierPathFromPolygon(basePoly, cornerTrim, bezierBulge)
    End Function

    Private Function GetInsetOrBasePolygon(vertices As List(Of Vec2), insetWorld As Single) As List(Of Vec2)
        Dim basePoly As List(Of Vec2)

        If insetWorld <= 0.0001F Then
            basePoly = New List(Of Vec2)(vertices)
        Else
            Dim safeInset = Math.Min(CDbl(insetWorld), GetMaxUsableInset(vertices))
            If safeInset <= 0.0001 Then
                basePoly = New List(Of Vec2)(vertices)
            Else
                basePoly = BuildInsetPolygon(vertices, safeInset)
                If basePoly Is Nothing OrElse basePoly.Count < 3 Then
                    basePoly = New List(Of Vec2)(vertices)
                End If
            End If
        End If

        Return basePoly
    End Function

    Private Function BuildPathFromPolygon(points As List(Of Vec2)) As ExportPath2D
        Dim path As New ExportPath2D()
        If points Is Nothing OrElse points.Count < 3 Then Return path

        For i As Integer = 0 To points.Count - 1
            path.Segments.Add(New ExportLine2D(points(i), points((i + 1) Mod points.Count)))
        Next

        Return path
    End Function

    Private Function BuildBezierPathFromPolygon(points As List(Of Vec2),
                                                cornerTrim As Single,
                                                bezierBulge As Single) As ExportPath2D
        Dim path As New ExportPath2D()
        If points Is Nothing OrElse points.Count < 3 Then Return path

        Dim n As Integer = points.Count

        If cornerTrim <= 0.0001F Then
            Return BuildPathFromPolygon(points)
        End If

        Dim pIn(n - 1) As Vec2
        Dim pOut(n - 1) As Vec2
        Dim c1(n - 1) As Vec2
        Dim c2(n - 1) As Vec2

        Dim trimFactor As Double = Math.Max(0.0, cornerTrim)
        Dim bulgeFactor As Double = Math.Max(0.0, bezierBulge)

        For i As Integer = 0 To n - 1
            Dim prev = points((i - 1 + n) Mod n)
            Dim curr = points(i)
            Dim [next] = points((i + 1) Mod n)

            Dim dirToPrev = Geo2D.Normalize(prev - curr)
            Dim dirToNext = Geo2D.Normalize([next] - curr)

            Dim lenIn = Geo2D.Distance(prev, curr)
            Dim lenOut = Geo2D.Distance(curr, [next])
            Dim minLen = Math.Min(lenIn, lenOut)

            Dim trim As Double
            If trimFactor <= 1.0 Then
                trim = minLen * trimFactor * 0.5
            Else
                trim = minLen * (1.0 - (1.0 / (1.0 + trimFactor)))
            End If

            trim = Math.Min(trim, minLen * 0.49)

            pIn(i) = curr + dirToPrev * trim
            pOut(i) = curr + dirToNext * trim

            Dim handleLen = trim * bulgeFactor
            c1(i) = pIn(i) - dirToPrev * handleLen
            c2(i) = pOut(i) - dirToNext * handleLen
        Next

        path.Segments.Add(New ExportLine2D(pOut(n - 1), pIn(0)))

        For i As Integer = 0 To n - 1
            path.Segments.Add(New ExportCubicBezier2D(pIn(i), c1(i), c2(i), pOut(i)))
            Dim ni As Integer = (i + 1) Mod n
            path.Segments.Add(New ExportLine2D(pOut(i), pIn(ni)))
        Next

        Return path
    End Function

    Private Function BuildFilletPathFromPolygon(points As List(Of Vec2), cornerTrim As Single) As ExportPath2D
        Dim path As New ExportPath2D()
        If points Is Nothing OrElse points.Count < 3 Then Return path

        Dim n As Integer = points.Count
        Dim tanA(n - 1) As Vec2
        Dim tanB(n - 1) As Vec2
        Dim arcCenter(n - 1) As Vec2
        Dim arcRadius(n - 1) As Double
        Dim hasArc(n - 1) As Boolean

        For i As Integer = 0 To n - 1
            Dim prev = points((i - 1 + n) Mod n)
            Dim curr = points(i)
            Dim [next] = points((i + 1) Mod n)

            Dim u1 = Geo2D.Normalize(prev - curr)
            Dim u2 = Geo2D.Normalize([next] - curr)

            Dim len1 = Geo2D.Distance(prev, curr)
            Dim len2 = Geo2D.Distance(curr, [next])
            Dim minLen = Math.Min(len1, len2)

            Dim trim As Double
            If cornerTrim <= 1.0F Then
                trim = minLen * cornerTrim * 0.5
            Else
                trim = minLen * (1.0 - (1.0 / (1.0 + cornerTrim)))
            End If

            If points.Count >= 8 Then
                trim = Math.Min(trim, minLen * 0.3)
            Else
                trim = Math.Min(trim, minLen * 0.49)
            End If

            tanA(i) = curr + u1 * trim
            tanB(i) = curr + u2 * trim

            Dim dot = Math.Max(-1.0, Math.Min(1.0, Geo2D.Dot(u1, u2)))
            Dim theta = Math.Acos(dot)

            If theta < 0.01 OrElse Math.Abs(Math.PI - theta) < 0.01 Then
                hasArc(i) = False
                Continue For
            End If

            Dim r = trim * Math.Tan(theta / 2.0)
            If r <= 0.0001 Then
                hasArc(i) = False
                Continue For
            End If

            Dim bis = Geo2D.Normalize(u1 + u2)
            If Math.Abs(bis.X) < 0.000001 AndAlso Math.Abs(bis.Y) < 0.000001 Then
                hasArc(i) = False
                Continue For
            End If

            Dim distToCenter = r / Math.Sin(theta / 2.0)
            arcCenter(i) = curr + bis * distToCenter
            arcRadius(i) = r
            hasArc(i) = True
        Next

        path.Segments.Add(New ExportLine2D(tanB(n - 1), tanA(0)))

        For i As Integer = 0 To n - 1
            Dim ni As Integer = (i + 1) Mod n

            If hasArc(i) Then
                Dim cw = IsArcClockwise(arcCenter(i), tanA(i), tanB(i), points(i))
                path.Segments.Add(New ExportArc2D(arcCenter(i), arcRadius(i), tanA(i), tanB(i), cw))
            Else
                path.Segments.Add(New ExportLine2D(tanA(i), tanB(i)))
            End If

            path.Segments.Add(New ExportLine2D(tanB(i), tanA(ni)))
        Next

        Return path
    End Function

    Private Function IsArcClockwise(center As Vec2, startPt As Vec2, endPt As Vec2, vertexPt As Vec2) As Boolean
        Dim a1 = NormalizeAngleDeg(Math.Atan2(startPt.Y - center.Y, startPt.X - center.X) * 180.0 / Math.PI)
        Dim a2 = NormalizeAngleDeg(Math.Atan2(endPt.Y - center.Y, endPt.X - center.X) * 180.0 / Math.PI)
        Dim av = NormalizeAngleDeg(Math.Atan2(vertexPt.Y - center.Y, vertexPt.X - center.X) * 180.0 / Math.PI)

        Dim sweepCCW = NormalizeAngleDeg(a2 - a1)
        Dim containsCCW = AngleBelongsToSweep(a1, sweepCCW, av)

        Return Not containsCCW
    End Function

    Private Function AngleBelongsToSweep(startAngle As Double, sweep As Double, testAngle As Double) As Boolean
        startAngle = NormalizeAngleDeg(startAngle)
        testAngle = NormalizeAngleDeg(testAngle)

        If sweep >= 0 Then
            Dim delta = NormalizeAngleDeg(testAngle - startAngle)
            Return delta <= sweep + 0.001
        Else
            Dim delta = NormalizeAngleDeg(startAngle - testAngle)
            Return delta <= (-sweep) + 0.001
        End If
    End Function

    Private Function NormalizeAngleDeg(angle As Double) As Double
        While angle < 0.0
            angle += 360.0
        End While
        While angle >= 360.0
            angle -= 360.0
        End While
        Return angle
    End Function

    Private Function BuildRegularPolygonPointsWorld(center As Vec2,
                                                    radius As Double,
                                                    sides As Integer,
                                                    angleOffset As Double) As List(Of Vec2)
        Dim pts As New List(Of Vec2)
        For i As Integer = 0 To sides - 1
            Dim a = angleOffset + i * (2.0 * Math.PI / sides)
            pts.Add(New Vec2(center.X + Math.Cos(a) * radius, center.Y + Math.Sin(a) * radius))
        Next
        Return pts
    End Function

    Private Function BuildStarPointsWorld(center As Vec2,
                                          radius As Double,
                                          pointsCount As Integer,
                                          innerRatio As Double,
                                          angleOffset As Double) As List(Of Vec2)
        Dim pts As New List(Of Vec2)
        Dim total As Integer = pointsCount * 2

        For i As Integer = 0 To total - 1
            Dim rr = If(i Mod 2 = 0, radius, radius * innerRatio)
            Dim a = angleOffset + i * (Math.PI / pointsCount)
            pts.Add(New Vec2(center.X + Math.Cos(a) * rr, center.Y + Math.Sin(a) * rr))
        Next

        Return pts
    End Function

    Private Function GetStableAngleFromSeed(seed As Vec2) As Double
        Dim v = Math.Abs(seed.X * 12.9898 + seed.Y * 78.233)
        Dim frac = v - Math.Floor(v)
        Return frac * Math.PI * 2.0
    End Function

    Private Function GetInscribedRadius(vertices As List(Of Vec2), c As Vec2) As Double
        Dim minDist As Double = Double.MaxValue
        For i As Integer = 0 To vertices.Count - 1
            Dim a = vertices(i)
            Dim b = vertices((i + 1) Mod vertices.Count)
            Dim d = Geo2D.PointLineDistance(c, a, b)
            If d < minDist Then minDist = d
        Next
        Return minDist
    End Function

    Private Function GetMaxUsableInset(vertices As List(Of Vec2)) As Double
        Dim c = Geo2D.PolygonCentroid(vertices)
        Dim minDist As Double = Double.MaxValue

        For i As Integer = 0 To vertices.Count - 1
            Dim a = vertices(i)
            Dim b = vertices((i + 1) Mod vertices.Count)
            Dim d = Geo2D.PointLineDistance(c, a, b)
            If d < minDist Then minDist = d
        Next

        Return Math.Max(0.0, minDist * 0.92)
    End Function

    Private Function BuildInsetPolygon(vertices As List(Of Vec2), offset As Double) As List(Of Vec2)
        Dim result As New List(Of Vec2)
        Dim area = Geo2D.SignedArea(vertices)
        If Math.Abs(area) < 0.0000001 Then Return result

        Dim isCCW As Boolean = area > 0.0
        Dim n = vertices.Count

        For i As Integer = 0 To n - 1
            Dim a1, a2, b1, b2 As Vec2

            ShiftEdgeInward(vertices(i), vertices((i + 1) Mod n), offset, isCCW, a1, a2)
            ShiftEdgeInward(vertices((i + 1) Mod n), vertices((i + 2) Mod n), offset, isCCW, b1, b2)

            result.Add(Geo2D.IntersectLines(a1, a2, b1, b2))
        Next

        Return result
    End Function

    Private Sub ShiftEdgeInward(p1 As Vec2,
                                p2 As Vec2,
                                offset As Double,
                                isCCW As Boolean,
                                ByRef q1 As Vec2,
                                ByRef q2 As Vec2)
        Dim dir = Geo2D.Normalize(p2 - p1)
        Dim inward As Vec2

        If isCCW Then
            inward = New Vec2(-dir.Y, dir.X)
        Else
            inward = New Vec2(dir.Y, -dir.X)
        End If

        q1 = p1 + inward * offset
        q2 = p2 + inward * offset
    End Sub

End Module
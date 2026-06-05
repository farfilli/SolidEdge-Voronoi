Imports System
Imports System.Collections.Generic
Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Windows.Forms

Public Enum CellRenderStyle
    Straight
    Curved
    Circle
    Square
    RoundedSquare
    Star
    Star3
    Star4
    Triangle
    Pentagon
    Hexagon
    Octagon
End Enum

Public Enum InnerCornerStyle
    Bezier
    Arc
End Enum

Public Enum SymbolCornerStyle
    Sharp
    Bezier
    FilletArc
End Enum

Public Class CanvasSketchDomain
    Public Property Outer As List(Of Vec2) = New List(Of Vec2)
    Public Property Holes As List(Of List(Of Vec2)) = New List(Of List(Of Vec2))
End Class



Public Class VoronoiCanvas

    Inherits Control

    Public Property SketchDomains As New List(Of CanvasSketchDomain)
    Public Property ConstrainSeedsToSketchDomains As Boolean = False

    Private Structure ViewInfo
        Public Scale As Single
        Public OffsetX As Single
        Public OffsetY As Single
    End Structure

    Public Event SeedsEdited As EventHandler



    Public Property Cells As List(Of VoronoiCell) = New List(Of VoronoiCell)
    Public Property EditableSeeds As List(Of Vec2) = New List(Of Vec2)
    Public Property Domain As RectangleF = New RectangleF(0, 0, 1000, 700)

    Public Property SketchBoundaries As List(Of List(Of Vec2)) = New List(Of List(Of Vec2))
    Public Property SketchBoundaryIsHole As List(Of Boolean) = New List(Of Boolean)
    Public Property ShowSketchBoundary As Boolean = True

    Public Property FillCells As Boolean = True
    Public Property ShowOuterEdges As Boolean = True
    Public Property ShowSeeds As Boolean = True
    Public Property ShowInnerCurve As Boolean = True
    Public Property AllowSeedEditing As Boolean = True

    Public Property RenderStyle As CellRenderStyle = CellRenderStyle.Curved
    Public Property CellScale As Single = 0.82F
    Public Property RandomRotation As Boolean = True

    Public Property SymbolCornerMode As SymbolCornerStyle = SymbolCornerStyle.Sharp
    Public Property SymbolCornerTrim As Single = 0.18F
    Public Property SymbolBezierBulge As Single = 0.55F

    Public Property SeedRadius As Single = 4.0F
    Public Property HitRadius As Single = 10.0F

    Public Property InnerOffset As Single = 0.0F
    Public Property CornerTrim As Single = 0.22F
    Public Property BezierBulge As Single = 0.55F
    Public Property InnerCurveWidth As Single = 1.8F

    Public Property InnerCornerMode As InnerCornerStyle = InnerCornerStyle.Bezier

    Private dragSeedIndex As Integer = -1
    Private hoverSeedIndex As Integer = -1
    Private isDragging As Boolean = False

    Public Sub New()
        SetStyle(ControlStyles.UserPaint Or
                 ControlStyles.AllPaintingInWmPaint Or
                 ControlStyles.OptimizedDoubleBuffer Or
                 ControlStyles.ResizeRedraw, True)

        DoubleBuffered = True
        BackColor = Color.FromArgb(8, 6, 53)
        ForeColor = Color.White
    End Sub

    Protected Overrides Sub OnPaintBackground(pevent As PaintEventArgs)
    End Sub

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        e.Graphics.Clear(BackColor)
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias
        e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality

        Dim view = GetView()

        DrawBounds(e.Graphics, view)
        DrawSketchBoundary(e.Graphics, view)
        DrawCells(e.Graphics, view)
        DrawSeeds(e.Graphics, view)
    End Sub

    Private Sub DrawSketchBoundary(g As Graphics, view As ViewInfo)
        If Not ShowSketchBoundary Then Return
        If SketchBoundaries Is Nothing OrElse SketchBoundaries.Count = 0 Then Return

        For i As Integer = 0 To SketchBoundaries.Count - 1
            Dim loopPts = SketchBoundaries(i)
            If loopPts Is Nothing OrElse loopPts.Count < 2 Then Continue For

            Dim isHole As Boolean = False
            If SketchBoundaryIsHole IsNot Nothing AndAlso i < SketchBoundaryIsHole.Count Then
                isHole = SketchBoundaryIsHole(i)
            End If

            Dim strokeColor As Color = If(isHole,
            Color.FromArgb(255, 255, 120, 120),
            Color.FromArgb(255, 255, 210, 80))

            Dim pts As New List(Of PointF)
            For Each p In loopPts
                pts.Add(WorldToScreen(p, view))
            Next

            Using pen As New Pen(strokeColor, If(isHole, 1.8F, 2.4F))
                pen.LineJoin = LineJoin.Round
                pen.StartCap = LineCap.Round
                pen.EndCap = LineCap.Round

                If pts.Count >= 3 Then
                    g.DrawPolygon(pen, pts.ToArray())
                Else
                    g.DrawLines(pen, pts.ToArray())
                End If
            End Using

            For Each sp In pts
                Using b As New SolidBrush(strokeColor)
                    g.FillEllipse(b, sp.X - 2.4F, sp.Y - 2.4F, 4.8F, 4.8F)
                End Using
            Next
        Next
    End Sub

    Private Sub DrawBounds(g As Graphics, view As ViewInfo)
        Dim p1 = WorldToScreen(New Vec2(Domain.Left, Domain.Top), view)
        Dim p2 = WorldToScreen(New Vec2(Domain.Right, Domain.Bottom), view)

        Dim rc As New RectangleF(
            p1.X,
            p1.Y,
            p2.X - p1.X,
            p2.Y - p1.Y
        )

        Using pen As New Pen(Color.FromArgb(110, 89, 214, 230), 1.0F)
            g.DrawRectangle(pen, rc.X, rc.Y, rc.Width, rc.Height)
        End Using
    End Sub

    Private Sub DrawCells(g As Graphics, view As ViewInfo)
        If Cells Is Nothing OrElse Cells.Count = 0 Then Return

        Using outerPen As New Pen(Color.FromArgb(210, 180, 245, 240), 1.2F)
            For i As Integer = 0 To Cells.Count - 1
                Dim cell = Cells(i)
                If cell.Vertices Is Nothing OrElse cell.Vertices.Count < 3 Then Continue For

                Dim outerPts(cell.Vertices.Count - 1) As PointF
                For k As Integer = 0 To cell.Vertices.Count - 1
                    outerPts(k) = WorldToScreen(cell.Vertices(k), view)
                Next

                If FillCells Then
                    Using br As New SolidBrush(GetCellColor(i, 42))
                        g.FillPolygon(br, outerPts)
                    End Using
                End If

                Select Case RenderStyle
                    Case CellRenderStyle.Straight
                        If ShowOuterEdges Then
                            g.DrawPolygon(outerPen, outerPts)
                        End If

                    Case CellRenderStyle.Curved
                        If ShowOuterEdges Then
                            g.DrawPolygon(Pens.DimGray, outerPts)
                        End If

                        If ShowInnerCurve Then
                            Using path As GraphicsPath = BuildInnerRoundedPath(cell.Vertices, view, InnerOffset, CornerTrim, BezierBulge)
                                If path IsNot Nothing Then
                                    Using innerPen As New Pen(GetCellColor(i, 235), InnerCurveWidth)
                                        innerPen.LineJoin = LineJoin.Round
                                        innerPen.StartCap = LineCap.Round
                                        innerPen.EndCap = LineCap.Round
                                        g.DrawPath(innerPen, path)
                                    End Using
                                End If
                            End Using
                        End If

                    Case Else
                        If ShowOuterEdges Then
                            g.DrawPolygon(Pens.DimGray, outerPts)
                        End If

                        Using path As GraphicsPath = BuildSymbolPath(cell, view, RenderStyle, CellScale, RandomRotation)
                            If path IsNot Nothing Then
                                Using symbolPen As New Pen(GetCellColor(i, 240), InnerCurveWidth)
                                    symbolPen.LineJoin = LineJoin.Round
                                    symbolPen.StartCap = LineCap.Round
                                    symbolPen.EndCap = LineCap.Round
                                    g.DrawPath(symbolPen, path)
                                End Using
                            End If
                        End Using
                End Select
            Next
        End Using
    End Sub

    Private Function BuildSymbolPath(cell As VoronoiCell,
                                     view As ViewInfo,
                                     style As CellRenderStyle,
                                     scaleFactor As Single,
                                     randomRotation As Boolean) As GraphicsPath

        If cell Is Nothing OrElse cell.Vertices Is Nothing OrElse cell.Vertices.Count < 3 Then Return Nothing

        Dim c As Vec2 = Geo2D.PolygonCentroid(cell.Vertices)
        Dim radius As Double = GetInscribedRadius(cell.Vertices, c)
        If radius <= 0.0001 Then Return Nothing

        radius *= Math.Max(0.05, Math.Min(1.5, scaleFactor))

        Dim angle As Double = 0.0
        If randomRotation Then
            angle = GetStableAngleFromSeed(cell.Seed)
        End If

        Dim center As PointF = WorldToScreen(c, view)
        Dim radiusPx As Single = CSng(radius * view.Scale)

        Select Case style
            Case CellRenderStyle.Circle
                Return BuildCirclePath(center, radiusPx)

            Case CellRenderStyle.Square
                Return BuildCorneredSymbolPath(BuildRegularPolygonPoints(center, radiusPx, 4, angle))

            Case CellRenderStyle.RoundedSquare
                Return BuildCorneredSymbolPath(BuildRegularPolygonPoints(center, radiusPx, 4, angle))

            Case CellRenderStyle.Triangle
                Return BuildCorneredSymbolPath(BuildRegularPolygonPoints(center, radiusPx, 3, angle - Math.PI / 2.0))

            Case CellRenderStyle.Pentagon
                Return BuildCorneredSymbolPath(BuildRegularPolygonPoints(center, radiusPx, 5, angle - Math.PI / 2.0))

            Case CellRenderStyle.Hexagon
                Return BuildCorneredSymbolPath(BuildRegularPolygonPoints(center, radiusPx, 6, angle))

            Case CellRenderStyle.Octagon
                Return BuildCorneredSymbolPath(BuildRegularPolygonPoints(center, radiusPx, 8, angle))

            Case CellRenderStyle.Star
                Return BuildCorneredSymbolPath(BuildStarPoints(center, radiusPx, 5, 0.46F, angle - Math.PI / 2.0))

            Case CellRenderStyle.Star3
                Return BuildCorneredSymbolPath(BuildStarPoints(center, radiusPx, 3, 0.3F, angle - Math.PI / 2.0))
            Case CellRenderStyle.Star4
                Return BuildCorneredSymbolPath(BuildStarPoints(center, radiusPx, 4, 0.45F, angle - Math.PI / 4.0))

            Case Else
                Return Nothing
        End Select
    End Function

    Private Function BuildCorneredSymbolPath(points As List(Of PointF)) As GraphicsPath
        If points Is Nothing OrElse points.Count < 3 Then Return Nothing

        Select Case SymbolCornerMode
            Case SymbolCornerStyle.Bezier
                Return BuildBezierClosedPath(points, SymbolCornerTrim, SymbolBezierBulge)

            Case SymbolCornerStyle.FilletArc
                Return BuildFilletArcClosedPath(points, SymbolCornerTrim)

            Case Else
                Dim path As New GraphicsPath()
                path.AddPolygon(points.ToArray())
                Return path
        End Select
    End Function

    Private Function BuildCirclePath(center As PointF, radius As Single) As GraphicsPath
        Dim path As New GraphicsPath()
        path.AddEllipse(center.X - radius, center.Y - radius, radius * 2, radius * 2)
        Return path
    End Function

    Private Function BuildRegularPolygonPoints(center As PointF,
                                               radius As Single,
                                               sides As Integer,
                                               angleOffset As Double) As List(Of PointF)
        Dim pts As New List(Of PointF)
        If sides < 3 Then Return pts

        For i As Integer = 0 To sides - 1
            Dim a = angleOffset + i * (2.0 * Math.PI / sides)
            pts.Add(New PointF(
                center.X + CSng(Math.Cos(a) * radius),
                center.Y + CSng(Math.Sin(a) * radius)
            ))
        Next

        Return pts
    End Function

    Private Function BuildStarPoints(center As PointF,
                                     radius As Single,
                                     pointsCount As Integer,
                                     innerRatio As Single,
                                     angleOffset As Double) As List(Of PointF)
        Dim pts As New List(Of PointF)
        If pointsCount < 3 Then Return pts

        Dim total As Integer = pointsCount * 2

        For i As Integer = 0 To total - 1
            Dim rr As Single = If(i Mod 2 = 0, radius, radius * innerRatio)
            Dim a = angleOffset + i * (Math.PI / pointsCount)

            pts.Add(New PointF(
                center.X + CSng(Math.Cos(a) * rr),
                center.Y + CSng(Math.Sin(a) * rr)
            ))
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

    Private Function BuildInnerRoundedPath(vertices As List(Of Vec2),
                                           view As ViewInfo,
                                           insetWorld As Single,
                                           cornerTrim As Single,
                                           bezierBulge As Single) As GraphicsPath

        If vertices Is Nothing OrElse vertices.Count < 3 Then Return Nothing

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

        If basePoly Is Nothing OrElse basePoly.Count < 3 Then Return Nothing

        Dim screenPts As New List(Of PointF)
        For Each p In basePoly
            screenPts.Add(WorldToScreen(p, view))
        Next

        If InnerCornerMode = InnerCornerStyle.Arc Then
            Return BuildFilletArcClosedPath(screenPts, cornerTrim)
        Else
            Return BuildBezierClosedPath(screenPts, cornerTrim, bezierBulge)
        End If
    End Function

    Private Function GetMaxUsableInset(vertices As List(Of Vec2)) As Double
        If vertices Is Nothing OrElse vertices.Count < 3 Then Return 0.0

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
        If vertices Is Nothing OrElse vertices.Count < 3 Then Return result

        Dim area = Geo2D.SignedArea(vertices)
        If Math.Abs(area) < 0.0000001 Then Return result

        Dim isCCW As Boolean = area > 0.0
        Dim n = vertices.Count

        For i As Integer = 0 To n - 1
            Dim a1, a2, b1, b2 As Vec2

            ShiftEdgeInward(vertices(i),
                            vertices((i + 1) Mod n),
                            offset,
                            isCCW,
                            a1,
                            a2)

            ShiftEdgeInward(vertices((i + 1) Mod n),
                            vertices((i + 2) Mod n),
                            offset,
                            isCCW,
                            b1,
                            b2)

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

    Private Function BuildBezierClosedPath(points As List(Of PointF),
                                           cornerTrim As Single,
                                           bezierBulge As Single) As GraphicsPath

        Dim path As New GraphicsPath()

        If points Is Nothing OrElse points.Count < 3 Then Return path

        Dim n As Integer = points.Count

        If cornerTrim <= 0.0001F Then
            path.AddPolygon(points.ToArray())
            Return path
        End If

        Dim pIn(n - 1) As PointF
        Dim pOut(n - 1) As PointF
        Dim c1(n - 1) As PointF
        Dim c2(n - 1) As PointF

        Dim trimFactor As Single = Math.Max(0.0F, cornerTrim)
        Dim bulgeFactor As Single = Math.Max(0.0F, bezierBulge)

        For i As Integer = 0 To n - 1
            Dim prev As PointF = points((i - 1 + n) Mod n)
            Dim curr As PointF = points(i)
            Dim [next] As PointF = points((i + 1) Mod n)

            Dim dirToPrev As PointF = NormalizePoint(New PointF(prev.X - curr.X, prev.Y - curr.Y))
            Dim dirToNext As PointF = NormalizePoint(New PointF([next].X - curr.X, [next].Y - curr.Y))

            Dim lenIn As Single = DistancePoint(prev, curr)
            Dim lenOut As Single = DistancePoint(curr, [next])
            Dim minLen As Single = Math.Min(lenIn, lenOut)

            Dim trim As Single
            If trimFactor <= 1.0F Then
                trim = minLen * trimFactor * 0.5F
            Else
                trim = minLen * (1.0F - CSng(1.0 / (1.0 + trimFactor)))
            End If

            trim = Math.Min(trim, minLen * 0.49F)

            pIn(i) = New PointF(curr.X + dirToPrev.X * trim, curr.Y + dirToPrev.Y * trim)
            pOut(i) = New PointF(curr.X + dirToNext.X * trim, curr.Y + dirToNext.Y * trim)

            Dim handleLen As Single = trim * bulgeFactor

            c1(i) = New PointF(pIn(i).X - dirToPrev.X * handleLen, pIn(i).Y - dirToPrev.Y * handleLen)
            c2(i) = New PointF(pOut(i).X - dirToNext.X * handleLen, pOut(i).Y - dirToNext.Y * handleLen)
        Next

        path.StartFigure()
        path.AddLine(pOut(n - 1), pIn(0))

        For i As Integer = 0 To n - 1
            path.AddBezier(pIn(i), c1(i), c2(i), pOut(i))
            Dim ni As Integer = (i + 1) Mod n
            path.AddLine(pOut(i), pIn(ni))
        Next

        path.CloseFigure()
        Return path
    End Function

    Private Function BuildFilletArcClosedPath(points As List(Of PointF),
                                              cornerTrim As Single) As GraphicsPath

        Dim path As New GraphicsPath()
        If points Is Nothing OrElse points.Count < 3 Then Return path

        Dim n As Integer = points.Count

        If cornerTrim <= 0.0001F Then
            path.AddPolygon(points.ToArray())
            Return path
        End If

        Dim tanA(n - 1) As PointF
        Dim tanB(n - 1) As PointF
        Dim arcCenter(n - 1) As PointF
        Dim arcRadius(n - 1) As Single
        Dim hasArc(n - 1) As Boolean

        For i As Integer = 0 To n - 1
            Dim prev As PointF = points((i - 1 + n) Mod n)
            Dim curr As PointF = points(i)
            Dim [next] As PointF = points((i + 1) Mod n)

            Dim u1 As PointF = NormalizePoint(New PointF(prev.X - curr.X, prev.Y - curr.Y))
            Dim u2 As PointF = NormalizePoint(New PointF([next].X - curr.X, [next].Y - curr.Y))

            Dim len1 As Single = DistancePoint(prev, curr)
            Dim len2 As Single = DistancePoint(curr, [next])
            Dim minLen As Single = Math.Min(len1, len2)

            Dim trim As Single
            If cornerTrim <= 1.0F Then
                trim = minLen * cornerTrim * 0.5F
            Else
                trim = minLen * (1.0F - CSng(1.0 / (1.0 + cornerTrim)))
            End If

            If points.Count >= 8 Then
                trim = Math.Min(trim, minLen * 0.3F)
            Else
                trim = Math.Min(trim, minLen * 0.49F)
            End If

            tanA(i) = New PointF(curr.X + u1.X * trim, curr.Y + u1.Y * trim)
            tanB(i) = New PointF(curr.X + u2.X * trim, curr.Y + u2.Y * trim)

            Dim dot As Double = Math.Max(-1.0, Math.Min(1.0, u1.X * u2.X + u1.Y * u2.Y))
            Dim theta As Double = Math.Acos(dot)

            If theta < 0.01 OrElse Math.Abs(Math.PI - theta) < 0.01 Then
                hasArc(i) = False
                Continue For
            End If

            Dim r As Single = CSng(trim * Math.Tan(theta / 2.0))
            If r <= 0.0001F Then
                hasArc(i) = False
                Continue For
            End If

            Dim bis As PointF = NormalizePoint(New PointF(u1.X + u2.X, u1.Y + u2.Y))
            If Math.Abs(bis.X) < 0.0001F AndAlso Math.Abs(bis.Y) < 0.0001F Then
                hasArc(i) = False
                Continue For
            End If

            Dim distToCenter As Single = CSng(r / Math.Sin(theta / 2.0))
            Dim center As New PointF(curr.X + bis.X * distToCenter, curr.Y + bis.Y * distToCenter)

            arcCenter(i) = center
            arcRadius(i) = r
            hasArc(i) = True
        Next

        path.StartFigure()
        path.AddLine(tanB(n - 1), tanA(0))

        For i As Integer = 0 To n - 1
            If hasArc(i) Then
                AddFilletArc(path, arcCenter(i), arcRadius(i), tanA(i), tanB(i), points(i))
            Else
                path.AddLine(tanA(i), tanB(i))
            End If

            Dim ni As Integer = (i + 1) Mod n
            path.AddLine(tanB(i), tanA(ni))
        Next

        path.CloseFigure()
        Return path
    End Function

    Private Sub AddFilletArc(path As GraphicsPath,
                             center As PointF,
                             radius As Single,
                             startPt As PointF,
                             endPt As PointF,
                             vertexPt As PointF)

        Dim a1 As Single = CSng(Math.Atan2(startPt.Y - center.Y, startPt.X - center.X) * 180.0 / Math.PI)
        Dim a2 As Single = CSng(Math.Atan2(endPt.Y - center.Y, endPt.X - center.X) * 180.0 / Math.PI)
        Dim av As Single = CSng(Math.Atan2(vertexPt.Y - center.Y, vertexPt.X - center.X) * 180.0 / Math.PI)

        Dim sweep1 As Single = NormalizeAngle(a2 - a1)
        Dim sweep2 As Single = sweep1 - 360.0F

        Dim contains1 As Boolean = AngleBelongsToSweep(a1, sweep1, av)
        Dim contains2 As Boolean = AngleBelongsToSweep(a1, sweep2, av)

        Dim rect As New RectangleF(center.X - radius, center.Y - radius, radius * 2.0F, radius * 2.0F)

        If contains1 AndAlso Not contains2 Then
            path.AddArc(rect, a1, sweep1)
        ElseIf contains2 AndAlso Not contains1 Then
            path.AddArc(rect, a1, sweep2)
        Else
            If Math.Abs(sweep1) < Math.Abs(sweep2) Then
                path.AddArc(rect, a1, sweep1)
            Else
                path.AddArc(rect, a1, sweep2)
            End If
        End If
    End Sub

    Private Function AngleBelongsToSweep(startAngle As Single,
                                         sweep As Single,
                                         testAngle As Single) As Boolean
        startAngle = NormalizeAngle(startAngle)
        testAngle = NormalizeAngle(testAngle)

        If sweep >= 0 Then
            Dim delta As Single = NormalizeAngle(testAngle - startAngle)
            Return delta <= sweep + 0.001F
        Else
            Dim delta As Single = NormalizeAngle(startAngle - testAngle)
            Return delta <= (-sweep) + 0.001F
        End If
    End Function

    Private Function NormalizeAngle(angle As Single) As Single
        While angle < 0.0F
            angle += 360.0F
        End While
        While angle >= 360.0F
            angle -= 360.0F
        End While
        Return angle
    End Function

    Private Function GetView() As ViewInfo
        Dim pad As Single = 20.0F

        If ClientSize.Width <= 0 OrElse ClientSize.Height <= 0 Then
            Return New ViewInfo With {.Scale = 1.0F, .OffsetX = 0, .OffsetY = 0}
        End If

        Dim sx = CSng((ClientSize.Width - 2 * pad) / Domain.Width)
        Dim sy = CSng((ClientSize.Height - 2 * pad) / Domain.Height)
        Dim scale = Math.Min(sx, sy)

        Dim drawW = CSng(Domain.Width * scale)
        Dim drawH = CSng(Domain.Height * scale)

        Dim ox = (ClientSize.Width - drawW) / 2.0F
        Dim oy = (ClientSize.Height - drawH) / 2.0F

        Return New ViewInfo With {
            .Scale = scale,
            .OffsetX = ox,
            .OffsetY = oy
        }
    End Function

    Private Function WorldToScreen(p As Vec2, view As ViewInfo) As PointF
        Return New PointF(
            view.OffsetX + CSng((p.X - Domain.Left) * view.Scale),
            view.OffsetY + CSng((p.Y - Domain.Top) * view.Scale)
        )
    End Function

    Private Function ScreenToWorld(p As Point) As Vec2
        Dim view = GetView()
        Dim x = Domain.Left + ((p.X - view.OffsetX) / view.Scale)
        Dim y = Domain.Top + ((p.Y - view.OffsetY) / view.Scale)
        Return New Vec2(x, y)
    End Function

    Private Function HitTestSeed(screenPt As Point) As Integer
        If EditableSeeds Is Nothing OrElse EditableSeeds.Count = 0 Then Return -1

        Dim view = GetView()
        Dim bestIndex As Integer = -1
        Dim bestDist As Double = Double.MaxValue

        For i As Integer = 0 To EditableSeeds.Count - 1
            Dim sp = WorldToScreen(EditableSeeds(i), view)
            Dim dx = sp.X - screenPt.X
            Dim dy = sp.Y - screenPt.Y
            Dim d2 = dx * dx + dy * dy

            If d2 <= HitRadius * HitRadius AndAlso d2 < bestDist Then
                bestDist = d2
                bestIndex = i
            End If
        Next

        Return bestIndex
    End Function

    Private Function ClampToDomain(p As Vec2) As Vec2
        Dim x = Geo2D.Clamp(p.X, Domain.Left, Domain.Right)
        Dim y = Geo2D.Clamp(p.Y, Domain.Top, Domain.Bottom)
        Return New Vec2(x, y)
    End Function

    Private Function GetCellColor(index As Integer, alpha As Integer) As Color
        Dim palette As Color() = {
            Color.FromArgb(alpha, 0, 188, 212),
            Color.FromArgb(alpha, 0, 150, 170),
            Color.FromArgb(alpha, 110, 231, 243),
            Color.FromArgb(alpha, 64, 224, 208),
            Color.FromArgb(alpha, 26, 205, 192),
            Color.FromArgb(alpha, 167, 255, 235),
            Color.FromArgb(alpha, 0, 121, 140),
            Color.FromArgb(alpha, 79, 195, 247),
            Color.FromArgb(alpha, 111, 0, 168),
            Color.FromArgb(alpha, 142, 36, 170),
            Color.FromArgb(alpha, 49, 27, 146),
            Color.FromArgb(alpha, 0, 70, 112),
            Color.FromArgb(alpha, 38, 198, 218),
            Color.FromArgb(alpha, 0, 200, 83),
            Color.FromArgb(alpha, 198, 255, 140),
            Color.FromArgb(alpha, 255, 202, 40)
        }

        Return palette(index Mod palette.Length)
    End Function

    Private Function NormalizePoint(v As PointF) As PointF
        Dim l As Single = CSng(Math.Sqrt(v.X * v.X + v.Y * v.Y))
        If l < 0.0001F Then Return New PointF(0.0F, 0.0F)
        Return New PointF(v.X / l, v.Y / l)
    End Function

    Private Function DistancePoint(a As PointF, b As PointF) As Single
        Dim dx As Single = a.X - b.X
        Dim dy As Single = a.Y - b.Y
        Return CSng(Math.Sqrt(dx * dx + dy * dy))
    End Function

    Private Sub DrawSeeds(g As Graphics, view As ViewInfo)
        If Not ShowSeeds OrElse EditableSeeds Is Nothing Then Return

        For i As Integer = 0 To EditableSeeds.Count - 1
            Dim sp = WorldToScreen(EditableSeeds(i), view)

            Dim r As Single = SeedRadius
            Dim fillColor As Color = Color.FromArgb(255, 0, 188, 212)
            Dim borderColor As Color = Color.FromArgb(255, 8, 6, 53)

            If i = hoverSeedIndex Then
                r += 1.5F
                fillColor = Color.FromArgb(255, 110, 231, 243)
            End If

            If i = dragSeedIndex Then
                r += 2.5F
                fillColor = Color.FromArgb(255, 167, 255, 235)
                borderColor = Color.White
            End If

            Using b As New SolidBrush(fillColor)
                g.FillEllipse(b, sp.X - r, sp.Y - r, r * 2, r * 2)
            End Using

            Using p As New Pen(borderColor, 1.2F)
                g.DrawEllipse(p, sp.X - r, sp.Y - r, r * 2, r * 2)
            End Using
        Next
    End Sub

    Protected Overrides Sub OnMouseDown(e As MouseEventArgs)
        MyBase.OnMouseDown(e)
        If Not AllowSeedEditing Then Return

        Dim idx = HitTestSeed(e.Location)

        If e.Button = MouseButtons.Left Then
            If idx >= 0 Then
                dragSeedIndex = idx
                isDragging = True
                Capture = True
                Invalidate()
                Return
            End If

            If (ModifierKeys And Keys.Control) = Keys.Control Then
                EditableSeeds.Add(ClampToDomain(ScreenToWorld(e.Location)))
                RaiseEvent SeedsEdited(Me, EventArgs.Empty)
                Invalidate()
            End If
        ElseIf e.Button = MouseButtons.Right Then
            If idx >= 0 AndAlso EditableSeeds.Count > 3 Then
                EditableSeeds.RemoveAt(idx)
                hoverSeedIndex = -1
                dragSeedIndex = -1
                RaiseEvent SeedsEdited(Me, EventArgs.Empty)
                Invalidate()
            End If
        End If
    End Sub

    Protected Overrides Sub OnMouseMove(e As MouseEventArgs)
        MyBase.OnMouseMove(e)
        If Not AllowSeedEditing Then Return

        hoverSeedIndex = HitTestSeed(e.Location)

        If isDragging AndAlso dragSeedIndex >= 0 AndAlso dragSeedIndex < EditableSeeds.Count Then
            EditableSeeds(dragSeedIndex) = ClampToDomain(ScreenToWorld(e.Location))
            RaiseEvent SeedsEdited(Me, EventArgs.Empty)
        End If

        Cursor = If(hoverSeedIndex >= 0 OrElse isDragging, Cursors.SizeAll, Cursors.Default)
        Invalidate()
    End Sub

    Protected Overrides Sub OnMouseUp(e As MouseEventArgs)
        MyBase.OnMouseUp(e)

        If isDragging Then
            isDragging = False
            dragSeedIndex = -1
            Capture = False
            Invalidate()
        End If
    End Sub

    Protected Overrides Sub OnMouseDoubleClick(e As MouseEventArgs)
        MyBase.OnMouseDoubleClick(e)
        If Not AllowSeedEditing Then Return

        EditableSeeds.Add(ClampToDomain(ScreenToWorld(e.Location)))
        RaiseEvent SeedsEdited(Me, EventArgs.Empty)
        Invalidate()
    End Sub
End Class
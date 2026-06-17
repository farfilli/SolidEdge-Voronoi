'Imports System
'Imports System.Collections.Generic
'Imports System.Drawing
'Imports System.Runtime.InteropServices
'Imports SolidEdgeFrameworkSupport

'Public Module SolidEdgeExporter

'    Public Class SketchBoundaryLoop
'        Public Property Points As List(Of Vec2) = New List(Of Vec2)
'        Public Property Bounds As RectangleF
'        Public Property SignedArea As Double
'        Public Property AbsoluteArea As Double
'        Public Property NestingLevel As Integer
'        Public Property ParentIndex As Integer = -1
'        Public Property IsHole As Boolean
'        Public Property Children As List(Of Integer) = New List(Of Integer)
'    End Class

'    Private Class BoundaryPiece
'        Public Property Points As List(Of Vec2) = New List(Of Vec2)
'    End Class

'    Public Function TryReadActiveSketchBoundaries(ByRef loops As List(Of SketchBoundaryLoop),
'                                                  ByRef totalBounds As RectangleF,
'                                                  ByRef errorMessage As String) As Boolean
'        Dim app As Object = Nothing
'        Dim doc As Object = Nothing
'        Dim profile As Object = Nothing
'        Dim lines2d As Object = Nothing
'        Dim arcs2d As Object = Nothing
'        Dim circles2d As Object = Nothing

'        loops = New List(Of SketchBoundaryLoop)
'        totalBounds = RectangleF.Empty
'        errorMessage = Nothing

'        Try
'            app = Marshal.GetActiveObject("SolidEdge.Application")
'            doc = app.ActiveDocument

'            If doc Is Nothing OrElse doc.ActiveSketch Is Nothing Then
'                errorMessage = "No active sketch in Solid Edge."
'                Return False
'            End If

'            profile = doc.ActiveSketch
'            lines2d = profile.Lines2d
'            arcs2d = profile.Arcs2d
'            circles2d = profile.Circles2d

'            Dim pieces As New List(Of BoundaryPiece)

'            If lines2d IsNot Nothing Then
'                For i As Integer = 1 To CInt(lines2d.Count)
'                    Dim ln = lines2d.Item(i)

'                    Dim x1 As Double = 0.0
'                    Dim y1 As Double = 0.0
'                    ln.GetStartPoint(x1, y1)

'                    Dim x2 As Double = 0.0
'                    Dim y2 As Double = 0.0
'                    ln.GetEndPoint(x2, y2)

'                    Dim p1 As New Vec2(x1 * 1000.0, -y1 * 1000.0)
'                    Dim p2 As New Vec2(x2 * 1000.0, -y2 * 1000.0)

'                    pieces.Add(New BoundaryPiece With {
'                        .Points = New List(Of Vec2) From {p1, p2}
'                    })
'                Next
'            End If

'            If arcs2d IsNot Nothing Then
'                For i As Integer = 1 To CInt(arcs2d.Count)
'                    Dim arc = arcs2d.Item(i)

'                    Dim xc As Double = 0.0
'                    Dim yc As Double = 0.0
'                    arc.GetCenterPoint(xc, yc)

'                    Dim x1 As Double = 0.0
'                    Dim y1 As Double = 0.0
'                    arc.GetStartPoint(x1, y1)

'                    Dim x2 As Double = 0.0
'                    Dim y2 As Double = 0.0
'                    arc.GetEndPoint(x2, y2)

'                    Dim xm As Double = Nothing
'                    Dim ym As Double = Nothing
'                    Dim zm As Double = Nothing
'                    'Dim KeyPointType As SolidEdgeConstants.KeyPointType = SolidEdgeConstants.KeyPointType.igKeyPointMiddle
'                    'Dim HandleType As SolidEdgeConstants.HandleType = Nothing
'                    'Dim arc2d As Arc2d = arc

'                    arc.GetKeyPoint(3, xm, ym, zm, 32, Nothing)

'                    Dim center As New Vec2(xc * 1000.0, -yc * 1000.0)
'                    Dim startPt As New Vec2(x1 * 1000.0, -y1 * 1000.0)
'                    Dim endPt As New Vec2(x2 * 1000.0, -y2 * 1000.0)
'                    Dim midPt As New Vec2(xm * 1000.0, -ym * 1000.0)

'                    Dim pts = SampleArcThroughPoint(center, startPt, midPt, endPt, 24)
'                    If pts.Count >= 2 Then
'                        pieces.Add(New BoundaryPiece With {.Points = pts})
'                    End If
'                Next
'            End If

'            If circles2d IsNot Nothing Then
'                For i As Integer = 1 To CInt(circles2d.Count)
'                    Dim c = circles2d.Item(i)

'                    Dim x As Double = 0.0
'                    Dim y As Double = 0.0
'                    c.GetCenterPoint(x, y)

'                    Dim center As New Vec2(x * 1000.0, -y * 1000.0)
'                    Dim radius As Double = CDbl(c.Radius) * 1000.0

'                    Dim pts = SampleCircle(center, radius, 72)
'                    If pts.Count >= 3 Then
'                        pieces.Add(New BoundaryPiece With {.Points = pts})
'                    End If
'                Next
'            End If

'            If pieces.Count = 0 Then
'                errorMessage = "No readable geometry found in the active sketch."
'                Return False
'            End If

'            Dim rawLoops = OrderPiecesIntoClosedBoundaries(pieces, 0.5)

'            If rawLoops Is Nothing OrElse rawLoops.Count = 0 Then
'                errorMessage = "Unable to reconstruct closed profiles from the active sketch."
'                Return False
'            End If

'            For Each raw In rawLoops
'                Dim clean = Geo2D.RemoveDuplicateSequentialPoints(raw)

'                If clean.Count >= 2 AndAlso Geo2D.Distance(clean(0), clean(clean.Count - 1)) <= 0.5 Then
'                    clean.RemoveAt(clean.Count - 1)
'                End If

'                If clean.Count >= 3 Then
'                    Dim lp As New SketchBoundaryLoop()
'                    lp.Points = clean
'                    lp.Bounds = Geo2D.GetBounds(clean)
'                    lp.SignedArea = Geo2D.SignedArea(clean)
'                    loops.Add(lp)
'                End If
'            Next

'            If loops.Count = 0 Then
'                errorMessage = "No valid closed loop found."
'                Return False
'            End If

'            ClassifyLoops(loops)

'            totalBounds = GetCombinedBounds(loops)
'            Return True

'        Catch ex As Exception
'            errorMessage = "Error reading Solid Edge profiles: " & ex.Message
'            Return False

'        Finally
'            circles2d = Nothing
'            arcs2d = Nothing
'            lines2d = Nothing
'            profile = Nothing
'            doc = Nothing
'            app = Nothing
'        End Try
'    End Function

'    Private Sub ClassifyLoops(loops As List(Of SketchBoundaryLoop))
'        If loops Is Nothing OrElse loops.Count = 0 Then Exit Sub

'        For i As Integer = 0 To loops.Count - 1
'            loops(i).AbsoluteArea = Math.Abs(loops(i).SignedArea)
'            loops(i).NestingLevel = 0
'            loops(i).ParentIndex = -1
'            loops(i).IsHole = False
'            loops(i).Children.Clear()
'        Next

'        For i As Integer = 0 To loops.Count - 1
'            Dim bestParent As Integer = -1
'            Dim bestParentArea As Double = Double.MaxValue

'            For j As Integer = 0 To loops.Count - 1
'                If i = j Then Continue For

'                If Not BoundsContains(loops(j).Bounds, loops(i).Bounds) Then Continue For

'                If Geo2D.PolygonContainsPolygon(loops(j).Points, loops(i).Points) Then
'                    If loops(j).AbsoluteArea < bestParentArea Then
'                        bestParentArea = loops(j).AbsoluteArea
'                        bestParent = j
'                    End If
'                End If
'            Next

'            loops(i).ParentIndex = bestParent
'        Next

'        For i As Integer = 0 To loops.Count - 1
'            Dim level As Integer = 0
'            Dim p As Integer = loops(i).ParentIndex

'            While p >= 0
'                level += 1
'                p = loops(p).ParentIndex
'            End While

'            loops(i).NestingLevel = level
'            loops(i).IsHole = (level Mod 2 = 1)
'        Next

'        For i As Integer = 0 To loops.Count - 1
'            Dim p = loops(i).ParentIndex
'            If p >= 0 Then
'                loops(p).Children.Add(i)
'            End If
'        Next
'    End Sub

'    Private Function BoundsContains(outer As RectangleF, inner As RectangleF, Optional eps As Single = 0.01F) As Boolean
'        Return inner.Left >= outer.Left - eps AndAlso
'           inner.Top >= outer.Top - eps AndAlso
'           inner.Right <= outer.Right + eps AndAlso
'           inner.Bottom <= outer.Bottom + eps
'    End Function

'    Private Function SampleArcThroughPoint(center As Vec2,
'                                       startPt As Vec2,
'                                       midPt As Vec2,
'                                       endPt As Vec2,
'                                       steps As Integer) As List(Of Vec2)

'        Dim pts As New List(Of Vec2)
'        Dim r As Double = Geo2D.Distance(center, startPt)
'        If r <= 0.0001 Then Return pts

'        Dim a1 As Double = Math.Atan2(startPt.Y - center.Y, startPt.X - center.X)
'        Dim am As Double = Math.Atan2(midPt.Y - center.Y, midPt.X - center.X)
'        Dim a2 As Double = Math.Atan2(endPt.Y - center.Y, endPt.X - center.X)

'        Dim sweepCCW As Double = NormalizeAngleRad(a2 - a1)
'        Dim sweepCW As Double = sweepCCW - Math.PI * 2.0

'        Dim midCCW As Boolean = AngleBelongsToSweepRad(a1, sweepCCW, am)

'        Dim sweep As Double = If(midCCW, sweepCCW, sweepCW)

'        For i As Integer = 0 To steps
'            Dim t As Double = i / CDbl(steps)
'            Dim a As Double = a1 + sweep * t
'            pts.Add(New Vec2(center.X + Math.Cos(a) * r,
'                         center.Y + Math.Sin(a) * r))
'        Next

'        Return Geo2D.RemoveDuplicateSequentialPoints(pts)
'    End Function

'    Private Function NormalizeAngleRad(angle As Double) As Double
'        While angle < 0.0
'            angle += Math.PI * 2.0
'        End While
'        While angle >= Math.PI * 2.0
'            angle -= Math.PI * 2.0
'        End While
'        Return angle
'    End Function

'    Private Function AngleBelongsToSweepRad(startAngle As Double,
'                                        sweep As Double,
'                                        testAngle As Double) As Boolean
'        startAngle = NormalizeAngleRad(startAngle)
'        testAngle = NormalizeAngleRad(testAngle)

'        If sweep >= 0 Then
'            Dim delta = NormalizeAngleRad(testAngle - startAngle)
'            Return delta <= sweep + 0.000001
'        Else
'            Dim delta = NormalizeAngleRad(startAngle - testAngle)
'            Return delta <= (-sweep) + 0.000001
'        End If
'    End Function

'    Private Function SampleCircle(center As Vec2,
'                                  radius As Double,
'                                  steps As Integer) As List(Of Vec2)

'        Dim pts As New List(Of Vec2)
'        If radius <= 0.0001 Then Return pts

'        For i As Integer = 0 To steps - 1
'            Dim a As Double = (i / CDbl(steps)) * Math.PI * 2.0
'            pts.Add(New Vec2(center.X + Math.Cos(a) * radius,
'                             center.Y + Math.Sin(a) * radius))
'        Next

'        Return Geo2D.RemoveDuplicateSequentialPoints(pts)
'    End Function

'    Private Function OrderPiecesIntoClosedBoundaries(pieces As List(Of BoundaryPiece),
'                                                     tolerance As Double) As List(Of List(Of Vec2))

'        Dim remaining As New List(Of BoundaryPiece)(pieces)
'        Dim loops As New List(Of List(Of Vec2))

'        Do While remaining.Count > 0
'            Dim loopPts As New List(Of Vec2)(remaining(0).Points)
'            remaining.RemoveAt(0)

'            Dim extended As Boolean = True

'            Do While extended AndAlso remaining.Count > 0
'                extended = False

'                Dim head As Vec2 = loopPts(0)
'                Dim tail As Vec2 = loopPts(loopPts.Count - 1)

'                For i As Integer = 0 To remaining.Count - 1
'                    Dim pts = remaining(i).Points
'                    Dim firstPt = pts(0)
'                    Dim lastPt = pts(pts.Count - 1)

'                    If Geo2D.Distance(tail, firstPt) <= tolerance Then
'                        For k As Integer = 1 To pts.Count - 1
'                            loopPts.Add(pts(k))
'                        Next
'                        remaining.RemoveAt(i)
'                        extended = True
'                        Exit For

'                    ElseIf Geo2D.Distance(tail, lastPt) <= tolerance Then
'                        pts = New List(Of Vec2)(pts)
'                        pts.Reverse()
'                        For k As Integer = 1 To pts.Count - 1
'                            loopPts.Add(pts(k))
'                        Next
'                        remaining.RemoveAt(i)
'                        extended = True
'                        Exit For

'                    ElseIf Geo2D.Distance(head, lastPt) <= tolerance Then
'                        Dim merged As New List(Of Vec2)(pts)
'                        For k As Integer = 1 To loopPts.Count - 1
'                            merged.Add(loopPts(k))
'                        Next
'                        loopPts = merged
'                        remaining.RemoveAt(i)
'                        extended = True
'                        Exit For

'                    ElseIf Geo2D.Distance(head, firstPt) <= tolerance Then
'                        pts = New List(Of Vec2)(pts)
'                        pts.Reverse()
'                        Dim merged As New List(Of Vec2)(pts)
'                        For k As Integer = 1 To loopPts.Count - 1
'                            merged.Add(loopPts(k))
'                        Next
'                        loopPts = merged
'                        remaining.RemoveAt(i)
'                        extended = True
'                        Exit For
'                    End If
'                Next
'            Loop

'            If loopPts.Count >= 3 Then
'                If Geo2D.Distance(loopPts(0), loopPts(loopPts.Count - 1)) > tolerance Then
'                    loopPts.Add(loopPts(0))
'                End If
'                loops.Add(loopPts)
'            End If
'        Loop

'        Return loops
'    End Function

'    Private Function GetCombinedBounds(loops As List(Of SketchBoundaryLoop)) As RectangleF
'        If loops Is Nothing OrElse loops.Count = 0 Then Return RectangleF.Empty

'        Dim first As Boolean = True
'        Dim minX As Single = 0.0F
'        Dim minY As Single = 0.0F
'        Dim maxX As Single = 0.0F
'        Dim maxY As Single = 0.0F

'        For Each lp In loops
'            If lp Is Nothing Then Continue For
'            If lp.Bounds.IsEmpty Then Continue For

'            If first Then
'                minX = lp.Bounds.Left
'                minY = lp.Bounds.Top
'                maxX = lp.Bounds.Right
'                maxY = lp.Bounds.Bottom
'                first = False
'            Else
'                If lp.Bounds.Left < minX Then minX = lp.Bounds.Left
'                If lp.Bounds.Top < minY Then minY = lp.Bounds.Top
'                If lp.Bounds.Right > maxX Then maxX = lp.Bounds.Right
'                If lp.Bounds.Bottom > maxY Then maxY = lp.Bounds.Bottom
'            End If
'        Next

'        If first Then Return RectangleF.Empty
'        Return RectangleF.FromLTRB(minX, minY, maxX, maxY)
'    End Function

'    Public Sub ExportToActivePartSketch(paths As IEnumerable(Of ExportPath2D))
'        Dim app As Object = Nothing
'        Dim doc As Object = Nothing
'        Dim profile As Object = Nothing
'        Dim lines2d As Object = Nothing
'        Dim arcs2d As Object = Nothing
'        Dim bSplineCurves2d As Object = Nothing

'        Try
'            app = Marshal.GetActiveObject("SolidEdge.Application")
'            doc = app.ActiveDocument

'            If doc.ActiveSketch Is Nothing Then
'                MsgBox("A sketch must be active!", MsgBoxStyle.Exclamation, "Solid Edge Voronoi")
'                Exit Sub
'            End If
'            profile = doc.ActiveSketch

'            app.ScreenUpdating = False

'            lines2d = profile.Lines2d
'            arcs2d = profile.Arcs2d
'            bSplineCurves2d = profile.BSplineCurves2d

'            For Each path In paths
'                For Each seg In path.Segments
'                    If TypeOf seg Is ExportLine2D Then
'                        Dim ln = DirectCast(seg, ExportLine2D)
'                        Dim p1 = FlipX(ln.P1)
'                        Dim p2 = FlipX(ln.P2)

'                        lines2d.AddBy2Points(
'                            x1:=p1.X / 1000.0,
'                            y1:=p1.Y / 1000.0,
'                            x2:=p2.X / 1000.0,
'                            y2:=p2.Y / 1000.0)

'                    ElseIf TypeOf seg Is ExportArc2D Then
'                        Dim a = DirectCast(seg, ExportArc2D)

'                        Dim c = FlipX(a.Center)
'                        Dim s = FlipX(a.StartPoint)
'                        Dim en = FlipX(a.EndPoint)

'                        Dim sx = s.X / 1000.0
'                        Dim sy = s.Y / 1000.0
'                        Dim ex = en.X / 1000.0
'                        Dim ey = en.Y / 1000.0
'                        Dim cx = c.X / 1000.0
'                        Dim cy = c.Y / 1000.0

'                        Dim isClockwise As Boolean = Not a.Clockwise

'                        If isClockwise Then
'                            arcs2d.AddByCenterStartEnd(
'                                xCenter:=cx,
'                                yCenter:=cy,
'                                xStart:=ex,
'                                yStart:=ey,
'                                xEnd:=sx,
'                                yEnd:=sy)
'                        Else
'                            arcs2d.AddByCenterStartEnd(
'                                xCenter:=cx,
'                                yCenter:=cy,
'                                xStart:=sx,
'                                yStart:=sy,
'                                xEnd:=ex,
'                                yEnd:=ey)
'                        End If

'                    ElseIf TypeOf seg Is ExportCubicBezier2D Then
'                        Dim bz = DirectCast(seg, ExportCubicBezier2D)

'                        Dim p0 = FlipX(bz.P0)
'                        Dim c1 = FlipX(bz.C1)
'                        Dim c2 = FlipX(bz.C2)
'                        Dim p3 = FlipX(bz.P3)

'                        Dim poles() As Double = {
'                            p0.X / 1000.0, p0.Y / 1000.0,
'                            c1.X / 1000.0, c1.Y / 1000.0,
'                            c2.X / 1000.0, c2.Y / 1000.0,
'                            p3.X / 1000.0, p3.Y / 1000.0
'                        }

'                        Dim knots() As Double = {
'                            0.0, 0.0, 0.0, 0.0,
'                            1.0, 1.0, 1.0, 1.0
'                        }

'                        bSplineCurves2d.Add(3, 4, poles, knots)
'                    End If
'                Next
'            Next

'            app.ScreenUpdating = True

'        Catch ex As Exception
'            Throw New Exception("Error exporting to Solid Edge: " & ex.Message, ex)

'        Finally
'            bSplineCurves2d = Nothing
'            arcs2d = Nothing
'            lines2d = Nothing
'            profile = Nothing
'            doc = Nothing
'            app = Nothing
'        End Try
'    End Sub

'    Private Function FlipX(p As Vec2) As Vec2
'        Return New Vec2(p.X, -p.Y)
'    End Function

'End Module

Imports System
Imports System.Collections.Generic
Imports System.Drawing
Imports System.Runtime.InteropServices
Imports SolidEdgeFrameworkSupport

Public Module SolidEdgeExporter

    Public Class SketchBoundaryLoop
        Public Property Points As List(Of Vec2) = New List(Of Vec2)
        Public Property Bounds As RectangleF
        Public Property SignedArea As Double
        Public Property AbsoluteArea As Double
        Public Property NestingLevel As Integer
        Public Property ParentIndex As Integer = -1
        Public Property IsHole As Boolean
        Public Property Children As List(Of Integer) = New List(Of Integer)
    End Class

    Private Class BoundaryPiece
        Public Property Points As List(Of Vec2) = New List(Of Vec2)
    End Class

    Private Function TryReadBoundariesFromProfileObject(ByVal profile As Object,
                                                        ByVal sourceLabel As String,
                                                        ByRef loops As List(Of SketchBoundaryLoop),
                                                        ByRef totalBounds As RectangleF,
                                                        ByRef errorMessage As String) As Boolean
        Dim lines2d As Object = Nothing
        Dim arcs2d As Object = Nothing
        Dim circles2d As Object = Nothing

        loops = New List(Of SketchBoundaryLoop)
        totalBounds = RectangleF.Empty
        errorMessage = Nothing

        Try
            If profile Is Nothing Then
                errorMessage = "Source not available: " & sourceLabel & "."
                Return False
            End If

            lines2d = profile.Lines2d
            arcs2d = profile.Arcs2d
            circles2d = profile.Circles2d

            Dim pieces As New List(Of BoundaryPiece)

            If lines2d IsNot Nothing Then
                For i As Integer = 1 To CInt(lines2d.Count)
                    Dim ln = lines2d.Item(i)

                    Dim x1 As Double = 0.0
                    Dim y1 As Double = 0.0
                    ln.GetStartPoint(x1, y1)

                    Dim x2 As Double = 0.0
                    Dim y2 As Double = 0.0
                    ln.GetEndPoint(x2, y2)

                    Dim p1 As New Vec2(x1 * 1000.0, -y1 * 1000.0)
                    Dim p2 As New Vec2(x2 * 1000.0, -y2 * 1000.0)

                    pieces.Add(New BoundaryPiece With {
                        .Points = New List(Of Vec2) From {p1, p2}
                    })
                Next
            End If

            If arcs2d IsNot Nothing Then
                For i As Integer = 1 To CInt(arcs2d.Count)
                    Dim arc = arcs2d.Item(i)

                    Dim xc As Double = 0.0
                    Dim yc As Double = 0.0
                    arc.GetCenterPoint(xc, yc)

                    Dim x1 As Double = 0.0
                    Dim y1 As Double = 0.0
                    arc.GetStartPoint(x1, y1)

                    Dim x2 As Double = 0.0
                    Dim y2 As Double = 0.0
                    arc.GetEndPoint(x2, y2)

                    Dim xm As Double = 0.0
                    Dim ym As Double = 0.0
                    Dim zm As Double = 0.0
                    arc.GetKeyPoint(3, xm, ym, zm, 32, Nothing)

                    Dim center As New Vec2(xc * 1000.0, -yc * 1000.0)
                    Dim startPt As New Vec2(x1 * 1000.0, -y1 * 1000.0)
                    Dim endPt As New Vec2(x2 * 1000.0, -y2 * 1000.0)
                    Dim midPt As New Vec2(xm * 1000.0, -ym * 1000.0)

                    Dim pts = SampleArcThroughPoint(center, startPt, midPt, endPt, 24)
                    If pts.Count >= 2 Then
                        pieces.Add(New BoundaryPiece With {.Points = pts})
                    End If
                Next
            End If

            If circles2d IsNot Nothing Then
                For i As Integer = 1 To CInt(circles2d.Count)
                    Dim c = circles2d.Item(i)

                    Dim x As Double = 0.0
                    Dim y As Double = 0.0
                    c.GetCenterPoint(x, y)

                    Dim center As New Vec2(x * 1000.0, -y * 1000.0)
                    Dim radius As Double = CDbl(c.Radius) * 1000.0

                    Dim pts = SampleCircle(center, radius, 72)
                    If pts.Count >= 3 Then
                        pieces.Add(New BoundaryPiece With {.Points = pts})
                    End If
                Next
            End If

            If pieces.Count = 0 Then
                errorMessage = "No readable geometry found in " & sourceLabel & "."
                Return False
            End If

            Dim rawLoops = OrderPiecesIntoClosedBoundaries(pieces, 0.5)

            If rawLoops Is Nothing OrElse rawLoops.Count = 0 Then
                errorMessage = "Unable to reconstruct closed profiles from " & sourceLabel & "."
                Return False
            End If

            For Each raw In rawLoops
                Dim clean = Geo2D.RemoveDuplicateSequentialPoints(raw)

                If clean.Count >= 2 AndAlso Geo2D.Distance(clean(0), clean(clean.Count - 1)) <= 0.5 Then
                    clean.RemoveAt(clean.Count - 1)
                End If

                If clean.Count >= 3 Then
                    Dim lp As New SketchBoundaryLoop()
                    lp.Points = clean
                    lp.Bounds = Geo2D.GetBounds(clean)
                    lp.SignedArea = Geo2D.SignedArea(clean)
                    loops.Add(lp)
                End If
            Next

            If loops.Count = 0 Then
                errorMessage = "No valid closed loop found in " & sourceLabel & "."
                Return False
            End If

            ClassifyLoops(loops)
            totalBounds = GetCombinedBounds(loops)
            Return True

        Catch ex As Exception
            errorMessage = "Error reading Solid Edge profiles from " & sourceLabel & ": " & ex.Message
            Return False

        Finally
            circles2d = Nothing
            arcs2d = Nothing
            lines2d = Nothing
        End Try
    End Function

    Public Function TryReadActiveSketchBoundaries(ByRef loops As List(Of SketchBoundaryLoop),
                                                  ByRef totalBounds As RectangleF,
                                                  ByRef errorMessage As String) As Boolean
        Dim app As Object = Nothing
        Dim doc As Object = Nothing
        Dim profile As Object = Nothing

        loops = New List(Of SketchBoundaryLoop)
        totalBounds = RectangleF.Empty
        errorMessage = Nothing

        Try
            app = Marshal.GetActiveObject("SolidEdge.Application")
            doc = app.ActiveDocument

            If doc Is Nothing OrElse doc.ActiveSketch Is Nothing Then
                errorMessage = "No active sketch in Solid Edge."
                Return False
            End If

            profile = doc.ActiveSketch
            Return TryReadBoundariesFromProfileObject(profile, "the active sketch", loops, totalBounds, errorMessage)

        Catch ex As Exception
            errorMessage = "Error reading Solid Edge profiles: " & ex.Message
            Return False

        Finally
            profile = Nothing
            doc = Nothing
            app = Nothing
        End Try
    End Function

    Public Function TryReadBlockDefaultViewBoundaries(ByRef loops As List(Of SketchBoundaryLoop),
                                                      ByRef totalBounds As RectangleF,
                                                      ByRef errorMessage As String,
                                                      Optional ByVal blockName As String = Nothing) As Boolean
        Dim app As Object = Nothing
        Dim doc As Object = Nothing
        Dim blocks As Object = Nothing
        Dim blk As Object = Nothing
        Dim profile As Object = Nothing

        loops = New List(Of SketchBoundaryLoop)
        totalBounds = RectangleF.Empty
        errorMessage = Nothing

        Try
            app = Marshal.GetActiveObject("SolidEdge.Application")
            doc = app.ActiveDocument

            If doc Is Nothing Then
                errorMessage = "No active Solid Edge document."
                Return False
            End If

            blocks = doc.Blocks

            If blocks Is Nothing OrElse CInt(blocks.Count) <= 0 Then
                errorMessage = "No blocks found in the active document."
                Return False
            End If

            If String.IsNullOrWhiteSpace(blockName) Then
                blk = blocks.Item(1)
            Else
                For i As Integer = 1 To CInt(blocks.Count)
                    Dim candidate = blocks.Item(i)
                    Dim candidateName As String = ""

                    Try
                        candidateName = CStr(candidate.Name)
                    Catch
                    End Try

                    If String.Equals(candidateName, blockName, StringComparison.OrdinalIgnoreCase) Then
                        blk = candidate
                        Exit For
                    End If
                Next
            End If

            If blk Is Nothing Then
                If String.IsNullOrWhiteSpace(blockName) Then
                    errorMessage = "Unable to access the first block."
                Else
                    errorMessage = "Block not found: " & blockName
                End If
                Return False
            End If

            profile = blk.DefaultView

            If profile Is Nothing Then
                errorMessage = "The selected block has no DefaultView."
                Return False
            End If

            Dim sourceLabel As String = "the selected block DefaultView"
            Try
                sourceLabel = "block '" & CStr(blk.Name) & "' DefaultView"
            Catch
            End Try

            Return TryReadBoundariesFromProfileObject(profile, sourceLabel, loops, totalBounds, errorMessage)

        Catch ex As Exception
            errorMessage = "Error reading block geometry from Solid Edge: " & ex.Message
            Return False

        Finally
            profile = Nothing
            blk = Nothing
            blocks = Nothing
            doc = Nothing
            app = Nothing
        End Try
    End Function

    Private Sub ClassifyLoops(loops As List(Of SketchBoundaryLoop))
        If loops Is Nothing OrElse loops.Count = 0 Then Exit Sub

        For i As Integer = 0 To loops.Count - 1
            loops(i).AbsoluteArea = Math.Abs(loops(i).SignedArea)
            loops(i).NestingLevel = 0
            loops(i).ParentIndex = -1
            loops(i).IsHole = False
            loops(i).Children.Clear()
        Next

        For i As Integer = 0 To loops.Count - 1
            Dim bestParent As Integer = -1
            Dim bestParentArea As Double = Double.MaxValue

            For j As Integer = 0 To loops.Count - 1
                If i = j Then Continue For

                If Not BoundsContains(loops(j).Bounds, loops(i).Bounds) Then Continue For

                If Geo2D.PolygonContainsPolygon(loops(j).Points, loops(i).Points) Then
                    If loops(j).AbsoluteArea < bestParentArea Then
                        bestParentArea = loops(j).AbsoluteArea
                        bestParent = j
                    End If
                End If
            Next

            loops(i).ParentIndex = bestParent
        Next

        For i As Integer = 0 To loops.Count - 1
            Dim level As Integer = 0
            Dim p As Integer = loops(i).ParentIndex

            While p >= 0
                level += 1
                p = loops(p).ParentIndex
            End While

            loops(i).NestingLevel = level
            loops(i).IsHole = (level Mod 2 = 1)
        Next

        For i As Integer = 0 To loops.Count - 1
            Dim p = loops(i).ParentIndex
            If p >= 0 Then
                loops(p).Children.Add(i)
            End If
        Next
    End Sub

    Private Function BoundsContains(outer As RectangleF, inner As RectangleF, Optional eps As Single = 0.01F) As Boolean
        Return inner.Left >= outer.Left - eps AndAlso
               inner.Top >= outer.Top - eps AndAlso
               inner.Right <= outer.Right + eps AndAlso
               inner.Bottom <= outer.Bottom + eps
    End Function

    Private Function SampleArcThroughPoint(center As Vec2,
                                           startPt As Vec2,
                                           midPt As Vec2,
                                           endPt As Vec2,
                                           steps As Integer) As List(Of Vec2)

        Dim pts As New List(Of Vec2)
        Dim r As Double = Geo2D.Distance(center, startPt)
        If r <= 0.0001 Then Return pts

        Dim a1 As Double = Math.Atan2(startPt.Y - center.Y, startPt.X - center.X)
        Dim am As Double = Math.Atan2(midPt.Y - center.Y, midPt.X - center.X)
        Dim a2 As Double = Math.Atan2(endPt.Y - center.Y, endPt.X - center.X)

        Dim sweepCCW As Double = NormalizeAngleRad(a2 - a1)
        Dim sweepCW As Double = sweepCCW - Math.PI * 2.0

        Dim midCCW As Boolean = AngleBelongsToSweepRad(a1, sweepCCW, am)

        Dim sweep As Double = If(midCCW, sweepCCW, sweepCW)

        For i As Integer = 0 To steps
            Dim t As Double = i / CDbl(steps)
            Dim a As Double = a1 + sweep * t
            pts.Add(New Vec2(center.X + Math.Cos(a) * r,
                             center.Y + Math.Sin(a) * r))
        Next

        Return Geo2D.RemoveDuplicateSequentialPoints(pts)
    End Function

    Private Function NormalizeAngleRad(angle As Double) As Double
        While angle < 0.0
            angle += Math.PI * 2.0
        End While
        While angle >= Math.PI * 2.0
            angle -= Math.PI * 2.0
        End While
        Return angle
    End Function

    Private Function AngleBelongsToSweepRad(startAngle As Double,
                                            sweep As Double,
                                            testAngle As Double) As Boolean
        startAngle = NormalizeAngleRad(startAngle)
        testAngle = NormalizeAngleRad(testAngle)

        If sweep >= 0 Then
            Dim delta = NormalizeAngleRad(testAngle - startAngle)
            Return delta <= sweep + 0.000001
        Else
            Dim delta = NormalizeAngleRad(startAngle - testAngle)
            Return delta <= (-sweep) + 0.000001
        End If
    End Function

    Private Function SampleCircle(center As Vec2,
                                  radius As Double,
                                  steps As Integer) As List(Of Vec2)

        Dim pts As New List(Of Vec2)
        If radius <= 0.0001 Then Return pts

        For i As Integer = 0 To steps - 1
            Dim a As Double = (i / CDbl(steps)) * Math.PI * 2.0
            pts.Add(New Vec2(center.X + Math.Cos(a) * radius,
                             center.Y + Math.Sin(a) * radius))
        Next

        Return Geo2D.RemoveDuplicateSequentialPoints(pts)
    End Function

    Private Function OrderPiecesIntoClosedBoundaries(pieces As List(Of BoundaryPiece),
                                                     tolerance As Double) As List(Of List(Of Vec2))

        Dim remaining As New List(Of BoundaryPiece)(pieces)
        Dim loops As New List(Of List(Of Vec2))

        Do While remaining.Count > 0
            Dim loopPts As New List(Of Vec2)(remaining(0).Points)
            remaining.RemoveAt(0)

            Dim extended As Boolean = True

            Do While extended AndAlso remaining.Count > 0
                extended = False

                Dim head As Vec2 = loopPts(0)
                Dim tail As Vec2 = loopPts(loopPts.Count - 1)

                For i As Integer = 0 To remaining.Count - 1
                    Dim pts = remaining(i).Points
                    Dim firstPt = pts(0)
                    Dim lastPt = pts(pts.Count - 1)

                    If Geo2D.Distance(tail, firstPt) <= tolerance Then
                        For k As Integer = 1 To pts.Count - 1
                            loopPts.Add(pts(k))
                        Next
                        remaining.RemoveAt(i)
                        extended = True
                        Exit For

                    ElseIf Geo2D.Distance(tail, lastPt) <= tolerance Then
                        pts = New List(Of Vec2)(pts)
                        pts.Reverse()
                        For k As Integer = 1 To pts.Count - 1
                            loopPts.Add(pts(k))
                        Next
                        remaining.RemoveAt(i)
                        extended = True
                        Exit For

                    ElseIf Geo2D.Distance(head, lastPt) <= tolerance Then
                        Dim merged As New List(Of Vec2)(pts)
                        For k As Integer = 1 To loopPts.Count - 1
                            merged.Add(loopPts(k))
                        Next
                        loopPts = merged
                        remaining.RemoveAt(i)
                        extended = True
                        Exit For

                    ElseIf Geo2D.Distance(head, firstPt) <= tolerance Then
                        pts = New List(Of Vec2)(pts)
                        pts.Reverse()
                        Dim merged As New List(Of Vec2)(pts)
                        For k As Integer = 1 To loopPts.Count - 1
                            merged.Add(loopPts(k))
                        Next
                        loopPts = merged
                        remaining.RemoveAt(i)
                        extended = True
                        Exit For
                    End If
                Next
            Loop

            If loopPts.Count >= 3 Then
                If Geo2D.Distance(loopPts(0), loopPts(loopPts.Count - 1)) > tolerance Then
                    loopPts.Add(loopPts(0))
                End If
                loops.Add(loopPts)
            End If
        Loop

        Return loops
    End Function

    Private Function GetCombinedBounds(loops As List(Of SketchBoundaryLoop)) As RectangleF
        If loops Is Nothing OrElse loops.Count = 0 Then Return RectangleF.Empty

        Dim first As Boolean = True
        Dim minX As Single = 0.0F
        Dim minY As Single = 0.0F
        Dim maxX As Single = 0.0F
        Dim maxY As Single = 0.0F

        For Each lp In loops
            If lp Is Nothing Then Continue For
            If lp.Bounds.IsEmpty Then Continue For

            If first Then
                minX = lp.Bounds.Left
                minY = lp.Bounds.Top
                maxX = lp.Bounds.Right
                maxY = lp.Bounds.Bottom
                first = False
            Else
                If lp.Bounds.Left < minX Then minX = lp.Bounds.Left
                If lp.Bounds.Top < minY Then minY = lp.Bounds.Top
                If lp.Bounds.Right > maxX Then maxX = lp.Bounds.Right
                If lp.Bounds.Bottom > maxY Then maxY = lp.Bounds.Bottom
            End If
        Next

        If first Then Return RectangleF.Empty
        Return RectangleF.FromLTRB(minX, minY, maxX, maxY)
    End Function

    Public Sub ExportToActivePartSketch(paths As IEnumerable(Of ExportPath2D))
        Dim app As Object = Nothing
        Dim doc As Object = Nothing
        Dim profile As Object = Nothing
        Dim lines2d As Object = Nothing
        Dim arcs2d As Object = Nothing
        Dim bSplineCurves2d As Object = Nothing

        Try
            app = Marshal.GetActiveObject("SolidEdge.Application")
            doc = app.ActiveDocument

            If doc.ActiveSketch Is Nothing Then
                MsgBox("A sketch must be active!", MsgBoxStyle.Exclamation, "Solid Edge Voronoi")
                Exit Sub
            End If
            profile = doc.ActiveSketch

            app.ScreenUpdating = False

            lines2d = profile.Lines2d
            arcs2d = profile.Arcs2d
            bSplineCurves2d = profile.BSplineCurves2d

            For Each path In paths
                For Each seg In path.Segments
                    If TypeOf seg Is ExportLine2D Then
                        Dim ln = DirectCast(seg, ExportLine2D)
                        Dim p1 = FlipX(ln.P1)
                        Dim p2 = FlipX(ln.P2)

                        lines2d.AddBy2Points(
                            x1:=p1.X / 1000.0,
                            y1:=p1.Y / 1000.0,
                            x2:=p2.X / 1000.0,
                            y2:=p2.Y / 1000.0)

                    ElseIf TypeOf seg Is ExportArc2D Then
                        Dim a = DirectCast(seg, ExportArc2D)

                        Dim c = FlipX(a.Center)
                        Dim s = FlipX(a.StartPoint)
                        Dim en = FlipX(a.EndPoint)

                        Dim sx = s.X / 1000.0
                        Dim sy = s.Y / 1000.0
                        Dim ex = en.X / 1000.0
                        Dim ey = en.Y / 1000.0
                        Dim cx = c.X / 1000.0
                        Dim cy = c.Y / 1000.0

                        Dim isClockwise As Boolean = Not a.Clockwise

                        If isClockwise Then
                            arcs2d.AddByCenterStartEnd(
                                xCenter:=cx,
                                yCenter:=cy,
                                xStart:=ex,
                                yStart:=ey,
                                xEnd:=sx,
                                yEnd:=sy)
                        Else
                            arcs2d.AddByCenterStartEnd(
                                xCenter:=cx,
                                yCenter:=cy,
                                xStart:=sx,
                                yStart:=sy,
                                xEnd:=ex,
                                yEnd:=ey)
                        End If

                    ElseIf TypeOf seg Is ExportCubicBezier2D Then
                        Dim bz = DirectCast(seg, ExportCubicBezier2D)

                        Dim p0 = FlipX(bz.P0)
                        Dim c1 = FlipX(bz.C1)
                        Dim c2 = FlipX(bz.C2)
                        Dim p3 = FlipX(bz.P3)

                        Dim poles() As Double = {
                            p0.X / 1000.0, p0.Y / 1000.0,
                            c1.X / 1000.0, c1.Y / 1000.0,
                            c2.X / 1000.0, c2.Y / 1000.0,
                            p3.X / 1000.0, p3.Y / 1000.0
                        }

                        Dim knots() As Double = {
                            0.0, 0.0, 0.0, 0.0,
                            1.0, 1.0, 1.0, 1.0
                        }

                        bSplineCurves2d.Add(3, 4, poles, knots)
                    End If
                Next
            Next

            app.ScreenUpdating = True

        Catch ex As Exception
            Throw New Exception("Error exporting to Solid Edge: " & ex.Message, ex)

        Finally
            bSplineCurves2d = Nothing
            arcs2d = Nothing
            lines2d = Nothing
            profile = Nothing
            doc = Nothing
            app = Nothing
        End Try
    End Sub

    Private Function FlipX(p As Vec2) As Vec2
        Return New Vec2(p.X, -p.Y)
    End Function

End Module
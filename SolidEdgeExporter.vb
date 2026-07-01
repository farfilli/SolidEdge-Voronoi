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
        Dim ellipses2d As Object = Nothing

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

            Try
                ellipses2d = profile.Ellipses2d
            Catch
                ellipses2d = Nothing
            End Try

            If ellipses2d IsNot Nothing Then
                For i As Integer = 1 To CInt(ellipses2d.Count)
                    Dim el = ellipses2d.Item(i)

                    Dim xc As Double = 0.0, yc As Double = 0.0
                    el.GetCenterPoint(xc, yc)

                    ' GetMajorAxis/GetMinorAxis restituiscono i VETTORI semiasse
                    ' (relativi al centro), non punti assoluti. I vettori vanno solo
                    ' scalati e riflessi in Y (niente sottrazione del centro).
                    Dim xa As Double = 0.0, ya As Double = 0.0
                    el.GetMajorAxis(xa, ya)
                    Dim xb As Double = 0.0, yb As Double = 0.0
                    el.GetMinorAxis(xb, yb)

                    Dim centerW As New Vec2(xc * 1000.0, -yc * 1000.0)
                    Dim majVx As Double = xa * 1000.0
                    Dim majVy As Double = -ya * 1000.0
                    Dim minVx As Double = xb * 1000.0
                    Dim minVy As Double = -yb * 1000.0

                    Dim rMajor As Double = Math.Sqrt(majVx * majVx + majVy * majVy)
                    Dim rMinor As Double = Math.Sqrt(minVx * minVx + minVy * minVy)
                    If rMajor <= 0.0001 Then Continue For
                    Dim rot As Double = Math.Atan2(majVy, majVx)

                    ' L'ellisse del profilo viene campionata nel contorno del dominio
                    ' (il dominio Voronoi e' comunque poligonale, come per i cerchi).
                    Dim pts = ExportGeometry.SampleEllipse(centerW, rMajor, rMinor, rot, 96)
                    If pts.Count >= 3 Then
                        pieces.Add(New BoundaryPiece With {.Points = pts})
                    End If
                Next
            End If

            Dim ellipticalArcs2d As Object = Nothing
            Try
                ellipticalArcs2d = profile.EllipticalArcs2d
            Catch
                ellipticalArcs2d = Nothing
            End Try

            If ellipticalArcs2d IsNot Nothing Then
                For i As Integer = 1 To CInt(ellipticalArcs2d.Count)
                    Dim ea = ellipticalArcs2d.Item(i)

                    Dim xc As Double = 0.0, yc As Double = 0.0
                    ea.GetCenterPoint(xc, yc)
                    Dim xa As Double = 0.0, ya As Double = 0.0
                    ea.GetMajorAxis(xa, ya)
                    Dim xb As Double = 0.0, yb As Double = 0.0
                    ea.GetMinorAxis(xb, yb)

                    Dim startA As Double = CDbl(ea.StartAngle)
                    Dim sweepA As Double = CDbl(ea.SweepAngle)
                    Dim orient As Integer = 0
                    Try
                        orient = CInt(ea.Orientation)
                    Catch
                        orient = 0
                    End Try

                    Dim centerW As New Vec2(xc * 1000.0, -yc * 1000.0)
                    Dim majW As New Vec2(xa * 1000.0, -ya * 1000.0)
                    Dim minW As New Vec2(xb * 1000.0, -yb * 1000.0)

                    ' Arco aperto: campionato come pezzo di contorno (come gli archi).
                    Dim pts = ExportGeometry.SampleEllipticalArc(centerW, majW, minW, startA, sweepA, orient)
                    If pts.Count >= 2 Then
                        pieces.Add(New BoundaryPiece With {.Points = pts})
                    End If
                Next
            End If

            Dim bsplines2d As Object = Nothing
            Try
                bsplines2d = profile.BSplineCurves2d
            Catch
                bsplines2d = Nothing
            End Try

            If bsplines2d IsNot Nothing Then
                For i As Integer = 1 To CInt(bsplines2d.Count)
                    Dim bc = bsplines2d.Item(i)

                    Dim closed As Boolean = False
                    Try
                        closed = CBool(bc.IsTangentiallyClosedCurve)
                    Catch
                        closed = False
                    End Try

                    Dim nc As Integer = 0
                    Try
                        nc = CInt(bc.NodeCount)
                    Catch
                        nc = 0
                    End Try

                    Dim nodes As New List(Of Vec2)
                    For k As Integer = 1 To nc
                        Dim x As Double = 0.0, y As Double = 0.0
                        Try
                            bc.GetNode(k, x, y)
                        Catch
                            Exit For
                        End Try
                        nodes.Add(New Vec2(x * 1000.0, -y * 1000.0))
                    Next

                    If nodes.Count >= 2 Then
                        Dim pts = ExportGeometry.SampleBSpline(nodes, closed)
                        If closed AndAlso pts.Count >= 3 Then
                            pts.Add(pts(0))   ' chiude l'anello per la ricostruzione del dominio
                        End If
                        If pts.Count >= 2 Then
                            pieces.Add(New BoundaryPiece With {.Points = pts})
                        End If
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
            ellipses2d = Nothing
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

    ' Legge TUTTI i blocchi del documento come definizioni a primitive
    ' (linee/archi/cerchi nativi) piu' il nome di ciascun blocco.
    Public Function TryReadAllBlocksAsPrimitives(ByRef definitions As List(Of BlockDefinition),
                                                 ByRef errorMessage As String) As Boolean
        Dim app As Object = Nothing
        Dim doc As Object = Nothing
        Dim blocks As Object = Nothing

        definitions = New List(Of BlockDefinition)
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

            For i As Integer = 1 To CInt(blocks.Count)
                Dim blk As Object = blocks.Item(i)
                If blk Is Nothing Then Continue For

                Dim name As String = ""
                Try
                    name = CStr(blk.Name)
                Catch
                End Try

                Dim view As Object = Nothing
                Try
                    view = blk.DefaultView
                Catch
                End Try
                If view Is Nothing Then Continue For

                ' Punto base/origine del blocco (su cui SE applica scala e rotazione
                ' dell'occorrenza). Stessa convenzione della geometria: mm, Y ribaltata.
                Dim gx As Double = 0.0
                Dim gy As Double = 0.0
                Try
                    blk.GetOrigin(gx, gy)
                Catch
                    Try
                        view.GetOrigin(gx, gy)
                    Catch
                    End Try
                End Try

                Dim entities As List(Of ExportPath2D) = ReadProfilePrimitives(view)
                If entities IsNot Nothing AndAlso entities.Count > 0 Then
                    definitions.Add(New BlockDefinition With {
                        .Name = name,
                        .Entities = entities,
                        .BaseOrigin = New Vec2(gx * 1000.0, -gy * 1000.0)
                    })
                End If
            Next

            If definitions.Count = 0 Then
                errorMessage = "No readable block geometry found."
                Return False
            End If

            Return True

        Catch ex As Exception
            errorMessage = "Error reading blocks from Solid Edge: " & ex.Message
            Return False

        Finally
            blocks = Nothing
            doc = Nothing
            app = Nothing
        End Try
    End Function

    ' Legge la geometria di un profilo/vista come entita' a primitive native.
    ' Ogni linea/arco e' un'entita' aperta; ogni cerchio un'entita' chiusa fatta
    ' di due semicerchi. Coordinate in mm con Y ribaltata (come gli altri reader).
    Private Function ReadProfilePrimitives(profile As Object) As List(Of ExportPath2D)
        Dim entities As New List(Of ExportPath2D)

        Dim lines2d As Object = Nothing
        Dim arcs2d As Object = Nothing
        Dim circles2d As Object = Nothing
        Dim ellipses2d As Object = Nothing
        Dim ellipticalArcs2d As Object = Nothing
        Dim bsplines2d As Object = Nothing

        Try
            lines2d = profile.Lines2d
        Catch
        End Try
        Try
            arcs2d = profile.Arcs2d
        Catch
        End Try
        Try
            circles2d = profile.Circles2d
        Catch
        End Try
        Try
            ellipses2d = profile.Ellipses2d
        Catch
        End Try
        Try
            ellipticalArcs2d = profile.EllipticalArcs2d
        Catch
        End Try
        Try
            bsplines2d = profile.BSplineCurves2d
        Catch
        End Try

        If lines2d IsNot Nothing Then
            For i As Integer = 1 To CInt(lines2d.Count)
                Dim ln = lines2d.Item(i)

                Dim x1 As Double = 0.0
                Dim y1 As Double = 0.0
                ln.GetStartPoint(x1, y1)

                Dim x2 As Double = 0.0
                Dim y2 As Double = 0.0
                ln.GetEndPoint(x2, y2)

                Dim e As New ExportPath2D()
                e.Closed = False
                e.Segments.Add(New ExportLine2D(New Vec2(x1 * 1000.0, -y1 * 1000.0),
                                                New Vec2(x2 * 1000.0, -y2 * 1000.0)))
                entities.Add(e)
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

                Dim center As New Vec2(xc * 1000.0, -yc * 1000.0)
                Dim sp As New Vec2(x1 * 1000.0, -y1 * 1000.0)
                Dim ep As New Vec2(x2 * 1000.0, -y2 * 1000.0)
                Dim rad As Double = Geo2D.Distance(center, sp)

                Dim orientation As Integer = 1
                Try
                    orientation = CInt(arc.Orientation)
                Catch
                End Try

                ' Angoli nel nostro frame (Y in basso, punti gia' ribaltati).
                Dim aaS As Double = Math.Atan2(sp.Y - center.Y, sp.X - center.X) * 180.0 / Math.PI
                Dim aE As Double = Math.Atan2(ep.Y - center.Y, ep.X - center.X) * 180.0 / Math.PI

                ' SE Orientation: 0 = CW, 1 = CCW (frame SE, Y in alto). Il flip Y
                ' inverte il verso: SE-CW => verso positivo (orario) nel nostro frame.
                Dim sweep As Double
                If orientation = 0 Then
                    sweep = NormPos360(aE - aaS)
                Else
                    sweep = NormPos360(aE - aaS) - 360.0
                End If

                Dim arcSeg As New ExportArc2D(center, rad, sp, ep, orientation = 0)
                arcSeg.SweepDeg = sweep

                Dim e As New ExportPath2D()
                e.Closed = False
                e.Segments.Add(arcSeg)
                entities.Add(e)
            Next
        End If

        If circles2d IsNot Nothing Then
            For i As Integer = 1 To CInt(circles2d.Count)
                Dim cir = circles2d.Item(i)

                Dim xc As Double = 0.0
                Dim yc As Double = 0.0
                cir.GetCenterPoint(xc, yc)

                Dim rad As Double = CDbl(cir.Radius) * 1000.0
                Dim center As New Vec2(xc * 1000.0, -yc * 1000.0)

                Dim e As New ExportPath2D()
                e.Closed = True
                e.Segments.Add(New ExportCircle2D(center, rad))
                entities.Add(e)
            Next
        End If

        If ellipses2d IsNot Nothing Then
            For i As Integer = 1 To CInt(ellipses2d.Count)
                Dim el = ellipses2d.Item(i)

                Dim xc As Double = 0.0, yc As Double = 0.0
                el.GetCenterPoint(xc, yc)

                ' Assi maggiore/minore come VETTORI semiasse (relativi al centro).
                Dim xa As Double = 0.0, ya As Double = 0.0
                el.GetMajorAxis(xa, ya)
                Dim xb As Double = 0.0, yb As Double = 0.0
                el.GetMinorAxis(xb, yb)

                Dim orient As Integer = 0
                Try
                    orient = CInt(el.Orientation)
                Catch
                    orient = 0
                End Try

                Dim centerW As New Vec2(xc * 1000.0, -yc * 1000.0)
                Dim majVx As Double = xa * 1000.0
                Dim majVy As Double = -ya * 1000.0
                Dim minVx As Double = xb * 1000.0
                Dim minVy As Double = -yb * 1000.0

                Dim rMajor As Double = Math.Sqrt(majVx * majVx + majVy * majVy)
                Dim rMinor As Double = Math.Sqrt(minVx * minVx + minVy * minVy)
                If rMajor <= 0.0001 Then Continue For
                Dim rot As Double = Math.Atan2(majVy, majVx)

                Dim e As New ExportPath2D()
                e.Closed = True
                e.Segments.Add(New ExportEllipse2D(centerW, rMajor, rMinor, rot, orient))
                entities.Add(e)
            Next
        End If

        If ellipticalArcs2d IsNot Nothing Then
            For i As Integer = 1 To CInt(ellipticalArcs2d.Count)
                Dim ea = ellipticalArcs2d.Item(i)

                Dim xc As Double = 0.0, yc As Double = 0.0
                ea.GetCenterPoint(xc, yc)
                Dim xa As Double = 0.0, ya As Double = 0.0
                ea.GetMajorAxis(xa, ya)
                Dim xb As Double = 0.0, yb As Double = 0.0
                ea.GetMinorAxis(xb, yb)

                Dim startA As Double = CDbl(ea.StartAngle)
                Dim sweepA As Double = CDbl(ea.SweepAngle)
                Dim orient As Integer = 0
                Try
                    orient = CInt(ea.Orientation)
                Catch
                    orient = 0
                End Try

                Dim centerW As New Vec2(xc * 1000.0, -yc * 1000.0)
                Dim majW As New Vec2(xa * 1000.0, -ya * 1000.0)
                Dim minW As New Vec2(xb * 1000.0, -yb * 1000.0)

                If majW.X * majW.X + majW.Y * majW.Y <= 0.0001 Then Continue For

                ' Arco aperto: entita' nativa che conserva i valori SE originali.
                Dim e As New ExportPath2D()
                e.Closed = False
                e.Segments.Add(New ExportEllipticalArc2D(centerW, majW, minW, startA, sweepA, orient))
                entities.Add(e)
            Next
        End If

        If bsplines2d IsNot Nothing Then
            For i As Integer = 1 To CInt(bsplines2d.Count)
                Dim bc = bsplines2d.Item(i)

                Dim closed As Boolean = False
                Try
                    closed = CBool(bc.IsTangentiallyClosedCurve)
                Catch
                    closed = False
                End Try

                Dim nc As Integer = 0
                Try
                    nc = CInt(bc.NodeCount)
                Catch
                    nc = 0
                End Try

                Dim nodes As New List(Of Vec2)
                ' ASSUNZIONE: GetNode usa indici 1-based (come le collection SE).
                ' Se i nodi risultassero shiftati/mancanti, provare 0..nc-1.
                For k As Integer = 1 To nc
                    Dim x As Double = 0.0, y As Double = 0.0
                    Try
                        bc.GetNode(k, x, y)
                    Catch
                        Exit For
                    End Try
                    nodes.Add(New Vec2(x * 1000.0, -y * 1000.0))
                Next

                If nodes.Count >= 2 Then
                    Dim e As New ExportPath2D()
                    e.Closed = closed
                    e.Segments.Add(New ExportBSpline2D(nodes, closed))
                    entities.Add(e)
                End If
            Next
        End If

        lines2d = Nothing
        arcs2d = Nothing
        circles2d = Nothing
        ellipses2d = Nothing
        ellipticalArcs2d = Nothing
        bsplines2d = Nothing

        Return entities
    End Function

    ' Normalizza un angolo in gradi nell'intervallo [0, 360).
    Private Function NormPos360(a As Double) As Double
        Dim r As Double = a Mod 360.0
        If r < 0.0 Then r += 360.0
        Return r
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
        Dim ellipses2d As Object = Nothing
        Dim circles2d As Object = Nothing
        Dim ellipticalArcs2d As Object = Nothing

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
            ellipses2d = profile.Ellipses2d
            circles2d = profile.Circles2d
            ellipticalArcs2d = profile.EllipticalArcs2d

            For Each path In paths
                EmitPathGeometry(path, lines2d, arcs2d, bSplineCurves2d, ellipses2d, circles2d, ellipticalArcs2d)
            Next

            app.ScreenUpdating = True

        Catch ex As Exception
            Throw New Exception("Error exporting to Solid Edge: " & ex.Message, ex)

        Finally
            ellipticalArcs2d = Nothing
            circles2d = Nothing
            ellipses2d = Nothing
            bSplineCurves2d = Nothing
            arcs2d = Nothing
            lines2d = Nothing
            profile = Nothing
            doc = Nothing
            app = Nothing
        End Try
    End Sub

    ' Export per-cella: le celle a blocco diventano occorrenze native (BlockOccurrences.Add),
    ' le altre vengono emesse come geometria (linee/archi/spline) come prima.
    Public Sub ExportToActivePartSketch(geoms As List(Of CellGeometry), Optional blockDefs As List(Of BlockDefinition) = Nothing)
        Dim app As Object = Nothing
        Dim doc As Object = Nothing
        Dim profile As Object = Nothing
        Dim lines2d As Object = Nothing
        Dim arcs2d As Object = Nothing
        Dim bSplineCurves2d As Object = Nothing
        Dim ellipses2d As Object = Nothing
        Dim circles2d As Object = Nothing
        Dim ellipticalArcs2d As Object = Nothing
        Dim blockOccurrences As Object = Nothing

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
            ellipses2d = profile.Ellipses2d
            circles2d = profile.Circles2d
            ellipticalArcs2d = profile.EllipticalArcs2d

            Try
                blockOccurrences = profile.BlockOccurrences
            Catch
                blockOccurrences = Nothing
            End Try

            ' Mappa nome->definizione (per creare i blocchi mancanti) e set dei
            ' nomi gia' verificati/creati in questa esportazione.
            Dim defByName As New Dictionary(Of String, BlockDefinition)(StringComparer.OrdinalIgnoreCase)
            If blockDefs IsNot Nothing Then
                For Each bd In blockDefs
                    If bd IsNot Nothing AndAlso Not String.IsNullOrEmpty(bd.Name) AndAlso Not defByName.ContainsKey(bd.Name) Then
                        defByName.Add(bd.Name, bd)
                    End If
                Next
            End If
            Dim ensured As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)

            For Each cg In geoms
                If cg Is Nothing Then Continue For

                If cg.HasBlock AndAlso Not String.IsNullOrEmpty(cg.BlockName) AndAlso blockOccurrences IsNot Nothing Then
                    ' Se il blocco non e' ancora presente nel documento, crealo dalle
                    ' entita' native prima di inserire l'occorrenza.
                    If Not ensured.Contains(cg.BlockName) Then
                        Dim def As BlockDefinition = Nothing
                        defByName.TryGetValue(cg.BlockName, def)
                        EnsureBlockDefinition(doc, cg.BlockName, def,
                                              lines2d, arcs2d, bSplineCurves2d, ellipses2d, circles2d, ellipticalArcs2d)
                        ensured.Add(cg.BlockName)
                    End If

                    blockOccurrences.Add(
                        BlockName:=cg.BlockName,
                        xOrigin:=cg.BlockOriginX,
                        yOrigin:=cg.BlockOriginY,
                        Scale:=cg.BlockScale,
                        Rotation:=cg.BlockRotation)
                Else
                    For Each path In cg.StyledPaths
                        EmitPathGeometry(path, lines2d, arcs2d, bSplineCurves2d, ellipses2d, circles2d, ellipticalArcs2d)
                    Next
                End If
            Next

            app.ScreenUpdating = True

        Catch ex As Exception
            Throw New Exception("Error exporting to Solid Edge: " & ex.Message, ex)

        Finally
            blockOccurrences = Nothing
            ellipticalArcs2d = Nothing
            circles2d = Nothing
            ellipses2d = Nothing
            bSplineCurves2d = Nothing
            arcs2d = Nothing
            lines2d = Nothing
            profile = Nothing
            doc = Nothing
            app = Nothing
        End Try
    End Sub

    ' Crea la definizione del blocco in Solid Edge se non gia' presente.
    ' Passi: (1) verifica nome nella collection Blocks; (2) disegna la geometria
    ' NATIVA (de-normalizzata) nel profilo raccogliendo gli oggetti creati;
    ' (3) li aggiunge al SelectSet; (4) Blocks.Add(name, x, y, True, True).
    ' NOTA: gli accessi doc.Blocks / doc.SelectSet e Blocks.Add sono gli accessori
    ' SE piu' probabili; se al test qualcuno risultasse diverso, va corretto qui.
    Private Function EnsureBlockDefinition(doc As Object, name As String, def As BlockDefinition,
                                           lines2d As Object, arcs2d As Object, bSplineCurves2d As Object,
                                           ellipses2d As Object, circles2d As Object, ellipticalArcs2d As Object) As Boolean
        Try
            ' (1) gia' presente?
            If BlockNameExists(doc, name) Then Return True
            If def Is Nothing OrElse def.Entities Is Nothing OrElse def.Entities.Count = 0 Then Return False
            If def.NativeRadius <= 0.000001 Then Return False

            ' (2) geometria nativa disegnata nel profilo, raccogliendo gli oggetti.
            Dim created As New List(Of Object)
            For Each pth In def.Entities
                Dim nativePath = ExportGeometry.DenormalizeBlockPath(pth, def)
                EmitPathGeometry(nativePath, lines2d, arcs2d, bSplineCurves2d, ellipses2d, circles2d, ellipticalArcs2d, created)
            Next
            If created.Count = 0 Then Return False

            ' (3) selezione della sola geometria appena creata.
            Dim ss As Object = Nothing
            Try
                ss = doc.SelectSet
            Catch
            End Try
            If ss Is Nothing Then Return False
            Try : ss.RemoveAll() : Catch : End Try
            For Each o In created
                Try : ss.Add(o) : Catch : End Try
            Next

            ' (4) creazione blocco. XOrigin/YOrigin = BaseOrigin in metri SE
            ' (FlipX + /1000, stessa convenzione della geometria). Optional entrambi True.
            Dim xo As Double = def.BaseOrigin.X / 1000.0
            Dim yo As Double = -def.BaseOrigin.Y / 1000.0
            doc.Blocks.Add(name, xo, yo, True, True)
            Return True

        Catch
            ' Se la creazione fallisce, l'occorrenza successiva dara' errore come prima:
            ' non blocchiamo l'intera esportazione.
            Return False
        End Try
    End Function

    Private Function BlockNameExists(doc As Object, name As String) As Boolean
        Try
            Dim blocks As Object = doc.Blocks
            If blocks Is Nothing Then Return False
            Dim n As Integer = CInt(blocks.Count)
            For i As Integer = 1 To n
                Dim blk As Object = blocks.Item(i)
                If blk Is Nothing Then Continue For
                Dim nm As String = ""
                Try : nm = CStr(blk.Name) : Catch : End Try
                If String.Equals(nm, name, StringComparison.OrdinalIgnoreCase) Then Return True
            Next
        Catch
        End Try
        Return False
    End Function

    ' Emissione di un singolo path come geometria 2D nello sketch attivo.
    Private Sub EmitPathGeometry(path As ExportPath2D,
                                 lines2d As Object,
                                 arcs2d As Object,
                                 bSplineCurves2d As Object,
                                 ellipses2d As Object,
                                 circles2d As Object,
                                 ellipticalArcs2d As Object,
                                 Optional created As List(Of Object) = Nothing)
        If path Is Nothing OrElse path.Segments Is Nothing Then Return

        For Each seg In path.Segments
            Dim madeObj As Object = Nothing
            If TypeOf seg Is ExportLine2D Then
                Dim ln = DirectCast(seg, ExportLine2D)
                Dim p1 = FlipX(ln.P1)
                Dim p2 = FlipX(ln.P2)

                madeObj = lines2d.AddBy2Points(
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

                ' SE disegna l'arco CCW da start a end (Y in alto). Il FlipX inverte
                ' il verso rispetto al nostro frame: SweepDeg>0 (orario a video) = CCW
                ' in SE, quindi nessuno scambio; altrimenti scambio start/end.
                Dim swapEnds As Boolean
                If Not Double.IsNaN(a.SweepDeg) Then
                    ' Verificato su SE: per gli archi da blocco lo swap va con SweepDeg > 0.
                    swapEnds = (a.SweepDeg > 0.0)
                Else
                    swapEnds = (Not a.Clockwise)
                End If

                If swapEnds Then
                    madeObj = arcs2d.AddByCenterStartEnd(
                        xCenter:=cx,
                        yCenter:=cy,
                        xStart:=ex,
                        yStart:=ey,
                        xEnd:=sx,
                        yEnd:=sy)
                Else
                    madeObj = arcs2d.AddByCenterStartEnd(
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

                madeObj = bSplineCurves2d.Add(3, 4, poles, knots)

            ElseIf TypeOf seg Is ExportEllipse2D Then
                Dim el = DirectCast(seg, ExportEllipse2D)

                ' Centro (punto) e asse maggiore (VETTORE relativo al centro),
                ' riportati nel frame SE (Y in alto) e in metri. Coerente con la
                ' lettura: GetMajorAxis e' un vettore semiasse, quindi anche
                ' AddByCenter riceve il vettore, non un punto assoluto.
                Dim cf = FlipX(el.Center)
                Dim majVxW As Double = Math.Cos(el.RotationRad) * el.RadiusMajor
                Dim majVyW As Double = Math.Sin(el.RotationRad) * el.RadiusMajor
                ' Vettore: solo riflessione in Y (nessun centro), poi in metri.
                Dim majSEx As Double = majVxW / 1000.0
                Dim majSEy As Double = -majVyW / 1000.0
                Dim ratio As Double = If(el.RadiusMajor <> 0.0, el.RadiusMinor / el.RadiusMajor, 1.0)

                madeObj = ellipses2d.AddByCenter(
                    cf.X / 1000.0,
                    cf.Y / 1000.0,
                    majSEx,
                    majSEy,
                    ratio,
                    el.Orientation)

            ElseIf TypeOf seg Is ExportCircle2D Then
                Dim ci = DirectCast(seg, ExportCircle2D)
                Dim cf = FlipX(ci.Center)
                madeObj = circles2d.AddByCenterRadius(
                    cf.X / 1000.0,
                    cf.Y / 1000.0,
                    ci.Radius / 1000.0)

            ElseIf TypeOf seg Is ExportEllipticalArc2D Then
                Dim ea = DirectCast(seg, ExportEllipticalArc2D)
                Dim cf = FlipX(ea.Center)
                ' Vettore asse maggiore: solo riflessione in Y, poi in metri.
                Dim mvf = FlipX(ea.MajorAxis)
                Dim rMaj As Double = Math.Sqrt(ea.MajorAxis.X * ea.MajorAxis.X + ea.MajorAxis.Y * ea.MajorAxis.Y)
                Dim rMin As Double = Math.Sqrt(ea.MinorAxis.X * ea.MinorAxis.X + ea.MinorAxis.Y * ea.MinorAxis.Y)
                Dim ratio As Double = If(rMaj <> 0.0, rMin / rMaj, 1.0)

                ' Stesso sweep effettivo usato nel campionamento: orient=1 -> +sweep,
                ' altrimenti -sweep. Cosi' AddByCenter percorre l'arco dal lato corto
                ' (con End=start+sweep "fisso" gli archi orient=0 uscivano dal lato
                ' lungo, ~360-sweep, quasi ellissi intere).
                Dim effSweep As Double = If(ea.Orientation = 1, ea.SweepAngle, -ea.SweepAngle)

                madeObj = ellipticalArcs2d.AddByCenter(
                    cf.X / 1000.0,
                    cf.Y / 1000.0,
                    mvf.X / 1000.0,
                    mvf.Y / 1000.0,
                    ratio,
                    ea.Orientation,
                    ea.StartAngle,
                    ea.StartAngle + effSweep)

            ElseIf TypeOf seg Is ExportBSpline2D Then
                Dim bs = DirectCast(seg, ExportBSpline2D)
                Dim cnt As Integer = bs.Nodes.Count
                If cnt >= 2 Then
                    ' Punti di interpolazione: ribaltati in Y e in metri, x,y interleaved.
                    Dim arr(cnt * 2 - 1) As Double
                    For k As Integer = 0 To cnt - 1
                        Dim pf = FlipX(bs.Nodes(k))
                        arr(k * 2) = pf.X / 1000.0
                        arr(k * 2 + 1) = pf.Y / 1000.0
                    Next

                    ' ArraySize = numero di PUNTI (cnt). L'array contiene cnt*2 double
                    ' (x,y interleaved). Passando cnt*2 SE leggeva il doppio degli
                    ' elementi -> DISP_E_BADINDEX (indice non valido).
                    ' Order = 3 come l'uso esistente di .Add in questo file; se la
                    ' curva risultasse troppo "spigolosa", provare 4.
                    madeObj = bSplineCurves2d.AddByPointsWithCloseOption(3, cnt, arr, bs.ClosedCurve)
                End If
            End If
            If created IsNot Nothing AndAlso madeObj IsNot Nothing Then created.Add(madeObj)
        Next
    End Sub

    Private Function FlipX(p As Vec2) As Vec2
        Return New Vec2(p.X, -p.Y)
    End Function

End Module
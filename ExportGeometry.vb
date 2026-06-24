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

    ' Sweep con segno in gradi, nel frame Y-in-basso (positivo = orario a video).
    ' NaN => arco non specificato: i consumatori usano l'arco minore (raccordi,
    ' semicerchi dei cerchi). Impostato per gli archi importati dai blocchi.
    Public Property SweepDeg As Double = Double.NaN

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

' Risultato della generazione UNICA della geometria per una singola cella.
' Tiene insieme l'indice di cella (per i colori), la cella d'origine (per fill
' e bordo nel canvas), lo stile effettivo (per scegliere penna e gate
' ShowInnerCurve) e i path stilizzati in world-space. La maggior parte degli
' stili produce un solo path; BlockSymbol puo' produrne piu' d'uno.
' Definizione di un blocco importato da Solid Edge: nome (per ripiazzarlo come
' occorrenza) + geometria nativa come entita' indipendenti (linee/archi/cerchi),
' in spazio locale normalizzato attorno all'origine (~raggio 1). Ogni entita' e'
' un ExportPath2D a se' (cosi' niente segmenti spuri di collegamento tra loop).
Public Class BlockDefinition
    Public Property Name As String = ""
    Public Property Entities As New List(Of ExportPath2D)

    ' Riferimenti nativi (prima della normalizzazione), in mm nel nostro frame
    ' Y-in-basso: centro del bounding box e raggio massimo. Servono a calcolare
    ' origine e scala dell'occorrenza nativa in Solid Edge.
    Public Property NativeCenter As Vec2 = New Vec2(0, 0)
    Public Property NativeRadius As Double = 0.0

    ' Punto base/origine del blocco in SE (su cui SE applica scala e rotazione
    ' dell'occorrenza), in mm nel nostro frame Y-in-basso.
    Public Property BaseOrigin As Vec2 = New Vec2(0, 0)
End Class

Public Class CellGeometry
    Public Property CellIndex As Integer
    Public Property Cell As VoronoiCell
    Public Property EffectiveStyle As CellRenderStyle
    Public Property StyledPaths As New List(Of ExportPath2D)

    ' Piazzamento come occorrenza di blocco nativo Solid Edge (solo celle BlockSymbol).
    ' Origine in metri SE (Y verso l'alto), scala adimensionale, rotazione in radianti.
    Public Property HasBlock As Boolean = False
    Public Property BlockName As String = ""
    Public Property BlockOriginX As Double = 0.0
    Public Property BlockOriginY As Double = 0.0
    Public Property BlockScale As Double = 1.0
    Public Property BlockRotation As Double = 0.0
End Class

Public Module ExportGeometry

    'Public Function BuildExportPaths(canvas As VoronoiCanvas) As List(Of ExportPath2D)
    '    Dim result As New List(Of ExportPath2D)
    '    If canvas Is Nothing OrElse canvas.Cells Is Nothing Then Return result

    '    Dim cellIndex As Integer = 0

    '    For Each cell In canvas.Cells
    '        If cell Is Nothing OrElse cell.Vertices Is Nothing OrElse cell.Vertices.Count < 3 Then Continue For

    '        Dim p As ExportPath2D = Nothing
    '        Dim effectiveStyle As CellRenderStyle = GetEffectiveRenderStyle(canvas, cellIndex)

    '        Select Case effectiveStyle
    '            Case CellRenderStyle.Straight
    '                p = BuildStraightCellPath(cell)

    '            Case CellRenderStyle.Curved
    '                If canvas.InnerCornerMode = InnerCornerStyle.Arc Then
    '                    p = BuildInnerArcCellPath(cell, canvas.InnerOffset, canvas.CornerTrim)
    '                Else
    '                    p = BuildInnerBezierCellPath(cell, canvas.InnerOffset, canvas.CornerTrim, canvas.BezierBulge)
    '                End If

    '            Case CellRenderStyle.Circle
    '                p = BuildCircleAsArcPath(cell, canvas.CellScale)

    '            Case CellRenderStyle.Square
    '                p = BuildPolygonSymbolPath(cell, canvas.CellScale, canvas.RandomRotation, cellIndex, 4, 0.0, canvas.SymbolCornerMode, canvas.SymbolCornerTrim, canvas.SymbolBezierBulge, canvas)

    '            Case CellRenderStyle.RoundedSquare
    '                p = BuildPolygonSymbolPath(cell, canvas.CellScale, canvas.RandomRotation, cellIndex, 4, 0.0, SymbolCornerStyle.FilletArc, Math.Max(canvas.SymbolCornerTrim, 0.18F), canvas.SymbolBezierBulge, canvas)

    '            Case CellRenderStyle.Triangle
    '                p = BuildPolygonSymbolPath(cell, canvas.CellScale, canvas.RandomRotation, cellIndex, 3, -Math.PI / 2.0, canvas.SymbolCornerMode, canvas.SymbolCornerTrim, canvas.SymbolBezierBulge, canvas)

    '            Case CellRenderStyle.Pentagon
    '                p = BuildPolygonSymbolPath(cell, canvas.CellScale, canvas.RandomRotation, cellIndex, 5, -Math.PI / 2.0, canvas.SymbolCornerMode, canvas.SymbolCornerTrim, canvas.SymbolBezierBulge, canvas)

    '            Case CellRenderStyle.Hexagon
    '                p = BuildPolygonSymbolPath(cell, canvas.CellScale, canvas.RandomRotation, cellIndex, 6, 0.0, canvas.SymbolCornerMode, canvas.SymbolCornerTrim, canvas.SymbolBezierBulge, canvas)

    '            Case CellRenderStyle.Octagon
    '                p = BuildPolygonSymbolPath(cell, canvas.CellScale, canvas.RandomRotation, cellIndex, 8, 0.0, canvas.SymbolCornerMode, canvas.SymbolCornerTrim, canvas.SymbolBezierBulge, canvas)

    '            Case CellRenderStyle.Star
    '                p = BuildStarSymbolPath(cell, canvas.CellScale, canvas.RandomRotation, cellIndex, 5, 0.46, -Math.PI / 2.0, canvas.SymbolCornerMode, canvas.SymbolCornerTrim, canvas.SymbolBezierBulge, canvas)

    '            Case CellRenderStyle.Star3
    '                p = BuildStarSymbolPath(cell, canvas.CellScale, canvas.RandomRotation, cellIndex, 3, 0.3, -Math.PI / 2.0, canvas.SymbolCornerMode, canvas.SymbolCornerTrim, canvas.SymbolBezierBulge, canvas)

    '            Case CellRenderStyle.Star4
    '                p = BuildStarSymbolPath(cell, canvas.CellScale, canvas.RandomRotation, cellIndex, 4, 0.45, -Math.PI / 4.0, canvas.SymbolCornerMode, canvas.SymbolCornerTrim, canvas.SymbolBezierBulge, canvas)

    '            Case CellRenderStyle.BlockSymbol
    '                Dim blockPaths = BuildBlockSymbolPaths(cell, canvas.BlockSymbolLoops, canvas.CellScale, canvas.RandomRotation, cellIndex, canvas)

    '                If blockPaths IsNot Nothing AndAlso blockPaths.Count > 0 Then
    '                    For Each bp In blockPaths
    '                        ApplyDefaultStyle(bp, canvas, cellIndex)
    '                        result.Add(bp)
    '                    Next

    '                    cellIndex += 1
    '                End If

    '                Continue For

    '        End Select

    '        If p IsNot Nothing AndAlso p.Segments.Count > 0 Then
    '            ApplyDefaultStyle(p, canvas, cellIndex)
    '            result.Add(p)
    '            cellIndex += 1
    '        End If
    '    Next

    '    Return result
    'End Function


    ' ====================================================================
    ' GENERAZIONE UNICA DELLA GEOMETRIA (world-space).
    ' Sia il canvas (preview) sia gli exporter consumano questo risultato.
    ' L'indice di cella usato per stile/scala/angolo/colore e' l'indice di
    ' loop grezzo su canvas.Cells: coincide con quello usato dal canvas per
    ' il fill e per CellScales/SeedStyleKeys, cosi' preview ed export restano
    ' allineati anche in presenza di celle degeneri saltate.
    ' ====================================================================
    Public Function BuildCellGeometry(canvas As VoronoiCanvas) As List(Of CellGeometry)
        Dim result As New List(Of CellGeometry)
        If canvas Is Nothing OrElse canvas.Cells Is Nothing Then Return result

        For cellIndex As Integer = 0 To canvas.Cells.Count - 1
            Dim cell = canvas.Cells(cellIndex)
            If cell Is Nothing OrElse cell.Vertices Is Nothing OrElse cell.Vertices.Count < 3 Then Continue For

            Dim effectiveStyle As CellRenderStyle = GetEffectiveRenderStyle(canvas, cellIndex)
            Dim effectiveScale As Single = GetEffectiveCellScale(canvas, cellIndex)

            Dim paths As New List(Of ExportPath2D)

            Select Case effectiveStyle
                Case CellRenderStyle.Straight
                    paths.Add(BuildStraightCellPath(cell))

                Case CellRenderStyle.Curved
                    For Each lp In GetInsetLoops(cell.Vertices, canvas.InnerOffset)
                        Select Case canvas.VertexMode
                            Case SymbolCornerStyle.FilletArc
                                paths.Add(BuildFilletPathFromPolygon(lp, canvas.VertexTrim))
                            Case SymbolCornerStyle.Bezier
                                paths.Add(BuildBezierPathFromPolygon(lp, canvas.VertexTrim, canvas.VertexSplineBulge))
                            Case Else ' Sharp
                                paths.Add(BuildPathFromPolygon(lp))
                        End Select
                    Next

                Case CellRenderStyle.Circle
                    paths.Add(BuildCircleAsArcPath(cell, effectiveScale))

                Case CellRenderStyle.Square
                    paths.Add(BuildPolygonSymbolPath(cell, effectiveScale, canvas.RandomRotation, cellIndex, 4, 0.0, canvas.VertexMode, canvas.VertexTrim, canvas.VertexSplineBulge, canvas))

                Case CellRenderStyle.RoundedSquare
                    paths.Add(BuildPolygonSymbolPath(cell, effectiveScale, canvas.RandomRotation, cellIndex, 4, 0.0, SymbolCornerStyle.FilletArc, Math.Max(canvas.VertexTrim, 0.18F), canvas.VertexSplineBulge, canvas))

                Case CellRenderStyle.Triangle
                    paths.Add(BuildPolygonSymbolPath(cell, effectiveScale, canvas.RandomRotation, cellIndex, 3, -Math.PI / 2.0, canvas.VertexMode, canvas.VertexTrim, canvas.VertexSplineBulge, canvas))

                Case CellRenderStyle.Pentagon
                    paths.Add(BuildPolygonSymbolPath(cell, effectiveScale, canvas.RandomRotation, cellIndex, 5, -Math.PI / 2.0, canvas.VertexMode, canvas.VertexTrim, canvas.VertexSplineBulge, canvas))

                Case CellRenderStyle.Hexagon
                    paths.Add(BuildPolygonSymbolPath(cell, effectiveScale, canvas.RandomRotation, cellIndex, 6, 0.0, canvas.VertexMode, canvas.VertexTrim, canvas.VertexSplineBulge, canvas))

                Case CellRenderStyle.Octagon
                    paths.Add(BuildPolygonSymbolPath(cell, effectiveScale, canvas.RandomRotation, cellIndex, 8, 0.0, canvas.VertexMode, canvas.VertexTrim, canvas.VertexSplineBulge, canvas))

                Case CellRenderStyle.Star
                    paths.Add(BuildStarSymbolPath(cell, effectiveScale, canvas.RandomRotation, cellIndex, 5, 0.46, -Math.PI / 2.0, canvas.VertexMode, canvas.VertexTrim, canvas.VertexSplineBulge, canvas))

                Case CellRenderStyle.Star3
                    paths.Add(BuildStarSymbolPath(cell, effectiveScale, canvas.RandomRotation, cellIndex, 3, 0.3, -Math.PI / 2.0, canvas.VertexMode, canvas.VertexTrim, canvas.VertexSplineBulge, canvas))

                Case CellRenderStyle.Star4
                    paths.Add(BuildStarSymbolPath(cell, effectiveScale, canvas.RandomRotation, cellIndex, 4, 0.45, -Math.PI / 4.0, canvas.VertexMode, canvas.VertexTrim, canvas.VertexSplineBulge, canvas))

                Case CellRenderStyle.BlockSymbol
                    Dim blockPaths = BuildBlockSymbolPaths(cell, canvas.BlockSymbols, effectiveScale, canvas.RandomRotation, cellIndex, canvas)
                    If blockPaths IsNot Nothing Then
                        paths.AddRange(blockPaths)
                    End If
            End Select

            Dim cg As New CellGeometry With {
                .CellIndex = cellIndex,
                .Cell = cell,
                .EffectiveStyle = effectiveStyle
            }

            For Each pth In paths
                If pth IsNot Nothing AndAlso pth.Segments.Count > 0 Then
                    ApplyDefaultStyle(pth, canvas, cellIndex)
                    cg.StyledPaths.Add(pth)
                End If
            Next

            If effectiveStyle = CellRenderStyle.BlockSymbol Then
                SetBlockPlacement(cg, cell, canvas.BlockSymbols, effectiveScale, canvas.RandomRotation, cellIndex, canvas)
            End If

            If cg.StyledPaths.Count > 0 Then
                result.Add(cg)
            End If
        Next

        Return result
    End Function

    ' Proiettore per gli exporter: lista piatta dei soli path stilizzati.
    Public Function BuildExportPaths(canvas As VoronoiCanvas) As List(Of ExportPath2D)
        Dim result As New List(Of ExportPath2D)
        For Each cg In BuildCellGeometry(canvas)
            result.AddRange(cg.StyledPaths)
        Next
        Return result
    End Function

    Private Function GetEffectiveCellScale(canvas As VoronoiCanvas, cellIndex As Integer) As Single
        If canvas Is Nothing Then Return 0.82F

        Dim value As Single = canvas.CellScale

        If canvas.CellScales IsNot Nothing AndAlso cellIndex >= 0 AndAlso cellIndex < canvas.CellScales.Count Then
            value = canvas.CellScales(cellIndex)
        End If

        Return CSng(Math.Max(0.05F, Math.Min(1.5F, value)))
    End Function

    ' Offset di rotazione manuale per-cella (radianti), additivo sull'angolo base.
    Private Function GetEffectiveCellRotation(canvas As VoronoiCanvas, cellIndex As Integer) As Double
        If canvas Is Nothing Then Return 0.0
        If canvas.CellRotations IsNot Nothing AndAlso cellIndex >= 0 AndAlso cellIndex < canvas.CellRotations.Count Then
            Return canvas.CellRotations(cellIndex)
        End If
        Return 0.0
    End Function

    Private Function BuildBlockSymbolPaths(cell As VoronoiCell,
                                           blocks As List(Of BlockDefinition),
                                           scaleFactor As Single,
                                           randomRotation As Boolean,
                                           cellIndex As Integer,
                                           canvas As VoronoiCanvas) As List(Of ExportPath2D)

        Dim result As New List(Of ExportPath2D)()
        If cell Is Nothing OrElse cell.Vertices Is Nothing OrElse cell.Vertices.Count < 3 Then Return result
        If blocks Is Nothing OrElse blocks.Count = 0 Then Return result

        ' Scelta stabile del blocco per questa cella (piazzamento casuale ma ripetibile).
        Dim def As BlockDefinition = blocks(GetStableBlockIndex(canvas, cellIndex, blocks.Count))
        If def Is Nothing OrElse def.Entities Is Nothing OrElse def.Entities.Count = 0 Then Return result

        Dim maxR As Double = def.NativeRadius
        If maxR <= 0.000001 Then Return result

        ' Scala dal raggio del cerchio equivalente all'area della cella.
        Dim radius As Double = BlockRadiusFromArea(cell, scaleFactor)

        ' Ancoraggio: il punto base del blocco coincide col seed della cella.
        Dim anchor As Vec2 = cell.Seed
        Dim baseNx As Double = (def.BaseOrigin.X - def.NativeCenter.X) / maxR
        Dim baseNy As Double = (def.BaseOrigin.Y - def.NativeCenter.Y) / maxR

        Dim angle As Double = 0.0
        If randomRotation Then angle = GetStableAngleFromKey(canvas, cellIndex)
        angle += GetEffectiveCellRotation(canvas, cellIndex)

        Dim cosA As Double = Math.Cos(angle)
        Dim sinA As Double = Math.Sin(angle)

        For Each entity In def.Entities
            If entity Is Nothing OrElse entity.Segments Is Nothing OrElse entity.Segments.Count = 0 Then Continue For
            Dim te = TransformBlockEntity(entity, anchor, baseNx, baseNy, radius, cosA, sinA)
            If te.Segments.Count > 0 Then result.Add(te)
        Next

        Return result
    End Function

    ' Raggio di scala del blocco in funzione dell'area della cella: raggio del
    ' cerchio di pari area, moltiplicato per il fattore di scala (per-cella/slider).
    Private Function BlockRadiusFromArea(cell As VoronoiCell, scaleFactor As Single) As Double
        Dim area As Double = Math.Abs(Geo2D.SignedArea(cell.Vertices))
        Dim rArea As Double = Math.Sqrt(area / Math.PI)
        Return rArea * Math.Max(0.05, Math.Min(1.5, scaleFactor))
    End Function

    ' Calcola il piazzamento dell'occorrenza nativa in SE per una cella a blocco,
    ' coerente con come BuildBlockSymbolPaths disegna l'anteprima.
    Private Sub SetBlockPlacement(cg As CellGeometry,
                                  cell As VoronoiCell,
                                  blocks As List(Of BlockDefinition),
                                  scaleFactor As Single,
                                  randomRotation As Boolean,
                                  cellIndex As Integer,
                                  canvas As VoronoiCanvas)
        If cell Is Nothing OrElse cell.Vertices Is Nothing OrElse cell.Vertices.Count < 3 Then Return
        If blocks Is Nothing OrElse blocks.Count = 0 Then Return

        Dim def As BlockDefinition = blocks(GetStableBlockIndex(canvas, cellIndex, blocks.Count))
        If def Is Nothing OrElse String.IsNullOrEmpty(def.Name) OrElse def.NativeRadius <= 0.000001 Then Return

        Dim maxR As Double = def.NativeRadius
        Dim radius As Double = BlockRadiusFromArea(cell, scaleFactor)

        Dim angle As Double = 0.0
        If randomRotation Then angle = GetStableAngleFromKey(canvas, cellIndex)
        angle += GetEffectiveCellRotation(canvas, cellIndex)

        ' Il punto base del blocco viene posizionato sul seed della cella.
        Dim anchor As Vec2 = cell.Seed

        cg.HasBlock = True
        cg.BlockName = def.Name
        ' Metri SE, con Y ribaltata indietro (SE ha Y verso l'alto).
        cg.BlockOriginX = anchor.X / 1000.0
        cg.BlockOriginY = -anchor.Y / 1000.0
        cg.BlockScale = radius / maxR
        ' Rotazione in SE: il flip Y inverte il verso. In GRADI (SE interpreta il
        ' parametro Rotation di BlockOccurrences.Add in gradi, non in radianti).
        cg.BlockRotation = -angle * 180.0 / Math.PI
    End Sub

    ' Indice di blocco stabile per cella: usa la chiave per-seme se presente.
    Private Function GetStableBlockIndex(canvas As VoronoiCanvas, cellIndex As Integer, count As Integer) As Integer
        If count <= 1 Then Return 0

        Dim key As Integer = cellIndex * 104729 + 17
        If canvas IsNot Nothing AndAlso canvas.SeedStyleKeys IsNot Nothing _
           AndAlso cellIndex >= 0 AndAlso cellIndex < canvas.SeedStyleKeys.Count Then
            key = canvas.SeedStyleKeys(cellIndex)
        End If

        Return Math.Abs(key) Mod count
    End Function

    ' Trasforma un'entita' di blocco (spazio locale ~raggio 1) nello spazio mondo:
    ' rotazione + scala (radius) + traslazione sul centro cella. Gli archi restano archi.
    Private Function TransformBlockEntity(entity As ExportPath2D,
                                          anchor As Vec2,
                                          baseNx As Double,
                                          baseNy As Double,
                                          r As Double,
                                          cosA As Double,
                                          sinA As Double) As ExportPath2D
        Dim outp As New ExportPath2D()
        outp.Closed = entity.Closed

        For Each seg In entity.Segments
            If TypeOf seg Is ExportLine2D Then
                Dim ln = DirectCast(seg, ExportLine2D)
                outp.Segments.Add(New ExportLine2D(MapBlockPoint(ln.P1, anchor, baseNx, baseNy, r, cosA, sinA),
                                                   MapBlockPoint(ln.P2, anchor, baseNx, baseNy, r, cosA, sinA)))

            ElseIf TypeOf seg Is ExportArc2D Then
                Dim a = DirectCast(seg, ExportArc2D)
                Dim na As New ExportArc2D(MapBlockPoint(a.Center, anchor, baseNx, baseNy, r, cosA, sinA),
                                          a.Radius * r,
                                          MapBlockPoint(a.StartPoint, anchor, baseNx, baseNy, r, cosA, sinA),
                                          MapBlockPoint(a.EndPoint, anchor, baseNx, baseNy, r, cosA, sinA),
                                          a.Clockwise)
                na.SweepDeg = a.SweepDeg
                outp.Segments.Add(na)
            End If
        Next

        Return outp
    End Function

    ' Mappa un punto normalizzato del blocco nel mondo: porta il punto base
    ' (baseNx,baseNy) sull'origine, ruota, scala e ancora su 'anchor' (il seed).
    Private Function MapBlockPoint(p As Vec2, anchor As Vec2, baseNx As Double, baseNy As Double, r As Double, cosA As Double, sinA As Double) As Vec2
        Dim dx As Double = p.X - baseNx
        Dim dy As Double = p.Y - baseNy
        Dim rx As Double = dx * cosA - dy * sinA
        Dim ry As Double = dx * sinA + dy * cosA
        Return New Vec2(anchor.X + rx * r, anchor.Y + ry * r)
    End Function

    ' Normalizza in place una definizione di blocco attorno all'origine (~raggio 1),
    ' cosi' che, moltiplicata per il raggio inscritto della cella, riempia la cella.
    Public Sub NormalizeBlockInPlace(def As BlockDefinition)
        If def Is Nothing OrElse def.Entities Is Nothing OrElse def.Entities.Count = 0 Then Return

        ' Punti rappresentativi: estremi delle linee e campionamento del percorso
        ' reale degli archi (cosi' un arco a raggio grande non gonfia l'ingombro).
        Dim pts As New List(Of Vec2)
        For Each e In def.Entities
            For Each seg In e.Segments
                If TypeOf seg Is ExportLine2D Then
                    Dim ln = DirectCast(seg, ExportLine2D)
                    pts.Add(ln.P1)
                    pts.Add(ln.P2)
                ElseIf TypeOf seg Is ExportArc2D Then
                    AccumulateArcPoints(DirectCast(seg, ExportArc2D), pts)
                End If
            Next
        Next

        If pts.Count = 0 Then Return

        Dim minX As Double = pts(0).X, maxX As Double = pts(0).X
        Dim minY As Double = pts(0).Y, maxY As Double = pts(0).Y
        For Each p In pts
            If p.X < minX Then minX = p.X
            If p.X > maxX Then maxX = p.X
            If p.Y < minY Then minY = p.Y
            If p.Y > maxY Then maxY = p.Y
        Next

        Dim cx As Double = (minX + maxX) / 2.0
        Dim cy As Double = (minY + maxY) / 2.0

        Dim maxR As Double = 0.0
        For Each p In pts
            maxR = Math.Max(maxR, DistTo(p, cx, cy))
        Next

        If maxR <= 0.000001 Then Return

        ' Salvo i riferimenti nativi per il piazzamento dell'occorrenza in SE.
        def.NativeCenter = New Vec2(cx, cy)
        def.NativeRadius = maxR

        For Each e In def.Entities
            Dim ns As New List(Of ExportSegment2D)
            For Each seg In e.Segments
                If TypeOf seg Is ExportLine2D Then
                    Dim ln = DirectCast(seg, ExportLine2D)
                    ns.Add(New ExportLine2D(NormBlockPoint(ln.P1, cx, cy, maxR), NormBlockPoint(ln.P2, cx, cy, maxR)))
                ElseIf TypeOf seg Is ExportArc2D Then
                    Dim a = DirectCast(seg, ExportArc2D)
                    Dim na As New ExportArc2D(NormBlockPoint(a.Center, cx, cy, maxR),
                                              a.Radius / maxR,
                                              NormBlockPoint(a.StartPoint, cx, cy, maxR),
                                              NormBlockPoint(a.EndPoint, cx, cy, maxR),
                                              a.Clockwise)
                    na.SweepDeg = a.SweepDeg
                    ns.Add(na)
                End If
            Next
            e.Segments = ns
        Next
    End Sub

    ' Aggiunge punti rappresentativi di un arco. Per archi parziali (SweepDeg noto)
    ' campiona il percorso reale; per i semicerchi dei cerchi (SweepDeg = NaN) usa
    ' centro +/- raggio, che per un cerchio e' l'ingombro corretto.
    Private Sub AccumulateArcPoints(a As ExportArc2D, pts As List(Of Vec2))
        If Double.IsNaN(a.SweepDeg) Then
            pts.Add(New Vec2(a.Center.X - a.Radius, a.Center.Y - a.Radius))
            pts.Add(New Vec2(a.Center.X + a.Radius, a.Center.Y + a.Radius))
            Return
        End If

        Dim aStart As Double = Math.Atan2(a.StartPoint.Y - a.Center.Y, a.StartPoint.X - a.Center.X)
        Dim sweepRad As Double = a.SweepDeg * Math.PI / 180.0

        Const steps As Integer = 8
        For i As Integer = 0 To steps
            Dim t As Double = i / CDbl(steps)
            Dim ang As Double = aStart + sweepRad * t
            pts.Add(New Vec2(a.Center.X + a.Radius * Math.Cos(ang),
                             a.Center.Y + a.Radius * Math.Sin(ang)))
        Next
    End Sub

    Private Sub AccumulateBounds(p As Vec2, ByRef hasAny As Boolean,
                                 ByRef minX As Double, ByRef minY As Double,
                                 ByRef maxX As Double, ByRef maxY As Double)
        If Not hasAny Then
            minX = p.X : maxX = p.X : minY = p.Y : maxY = p.Y
            hasAny = True
        Else
            If p.X < minX Then minX = p.X
            If p.X > maxX Then maxX = p.X
            If p.Y < minY Then minY = p.Y
            If p.Y > maxY Then maxY = p.Y
        End If
    End Sub

    Private Function DistTo(p As Vec2, cx As Double, cy As Double) As Double
        Dim dx = p.X - cx
        Dim dy = p.Y - cy
        Return Math.Sqrt(dx * dx + dy * dy)
    End Function

    Private Function NormBlockPoint(p As Vec2, cx As Double, cy As Double, maxR As Double) As Vec2
        Return New Vec2((p.X - cx) / maxR, (p.Y - cy) / maxR)
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
                                            cellIndex As Integer,
                                            sides As Integer,
                                            angleOffset As Double,
                                            cornerMode As SymbolCornerStyle,
                                            cornerTrim As Single,
                                            bezierBulge As Single,
                                            canvas As VoronoiCanvas) As ExportPath2D

        Dim c As Vec2 = Geo2D.PolygonCentroid(cell.Vertices)
        Dim radius As Double = GetInscribedRadius(cell.Vertices, c)
        radius *= Math.Max(0.05, Math.Min(1.5, scaleFactor))

        Dim angle As Double = 0.0
        If randomRotation Then angle = GetStableAngleFromKey(canvas, cellIndex)
        angle += GetEffectiveCellRotation(canvas, cellIndex)

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
                                         cellIndex As Integer,
                                         pointsCount As Integer,
                                         innerRatio As Double,
                                         angleOffset As Double,
                                         cornerMode As SymbolCornerStyle,
                                         cornerTrim As Single,
                                         bezierBulge As Single,
                                         canvas As VoronoiCanvas) As ExportPath2D

        Dim c As Vec2 = Geo2D.PolygonCentroid(cell.Vertices)
        Dim radius As Double = GetInscribedRadius(cell.Vertices, c)
        radius *= Math.Max(0.05, Math.Min(1.5, scaleFactor))

        Dim angle As Double = 0.0
        If randomRotation Then angle = GetStableAngleFromKey(canvas, cellIndex)
        angle += GetEffectiveCellRotation(canvas, cellIndex)

        Dim pts = BuildStarPointsWorld(c, radius, pointsCount, innerRatio, angle + angleOffset)

        Select Case cornerMode
            Case SymbolCornerStyle.FilletArc
                Return BuildFilletPathFromPolygon(pts, cornerTrim)
            Case SymbolCornerStyle.Bezier
                Return BuildBezierPathFromPolygon(pts, cornerTrim, bezierBulge)
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

    ' Contorno celle a spigolo vivo: poligono di offset senza raccordi.
    Private Function BuildInnerSharpCellPath(cell As VoronoiCell,
                                             insetWorld As Single) As ExportPath2D
        Dim basePoly = GetInsetOrBasePolygon(cell.Vertices, insetWorld)
        Return BuildPathFromPolygon(basePoly)
    End Function

    ' Anelli di offset interno della cella, puliti (via Clipper).
    ' Offset ~0 => poligono base. Puo' restituire piu' anelli o nessuno.
    Private Function GetInsetLoops(vertices As List(Of Vec2), insetWorld As Single) As List(Of List(Of Vec2))
        If vertices Is Nothing OrElse vertices.Count < 3 Then Return New List(Of List(Of Vec2))()

        If insetWorld <= 0.0001F Then
            Return New List(Of List(Of Vec2)) From {New List(Of Vec2)(vertices)}
        End If

        Return VoronoiEngine.OffsetPolygon(vertices, -CDbl(insetWorld))
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

    Private Function GetStableAngleFromKey(canvas As VoronoiCanvas, cellIndex As Integer) As Double
        Dim key As Integer = 0

        If canvas IsNot Nothing AndAlso
           canvas.SeedStyleKeys IsNot Nothing AndAlso
           cellIndex >= 0 AndAlso
           cellIndex < canvas.SeedStyleKeys.Count Then
            key = canvas.SeedStyleKeys(cellIndex)
        End If

        Dim v As Double = Math.Abs(key * 0.61803398875)
        Dim frac As Double = v - Math.Floor(v)
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

    Private Function GetEffectiveRenderStyle(canvas As VoronoiCanvas, cell As VoronoiCell) As CellRenderStyle
        If canvas.RenderStyle <> CellRenderStyle.Random Then Return canvas.RenderStyle
        Return GetStableRandomSymbolStyle(cell)
    End Function

    Private Function GetStableRandomSymbolStyle(cell As VoronoiCell) As CellRenderStyle
        Dim styles As CellRenderStyle() = {
            CellRenderStyle.Circle,
            CellRenderStyle.Square,
            CellRenderStyle.RoundedSquare,
            CellRenderStyle.Triangle,
            CellRenderStyle.Pentagon,
            CellRenderStyle.Hexagon,
            CellRenderStyle.Octagon,
            CellRenderStyle.Star,
            CellRenderStyle.Star3,
            CellRenderStyle.Star4
        }

        Dim idx As Integer = GetStableStyleIndex(cell.Seed, styles.Length)
        Return styles(idx)
    End Function

    Private Function GetStableStyleIndex(seed As Vec2, count As Integer) As Integer
        If count <= 0 Then Return 0

        Dim v As Double = Math.Abs(seed.X * 91.173 + seed.Y * 167.413)
        Dim frac As Double = v - Math.Floor(v)
        Dim idx As Integer = CInt(Math.Floor(frac * count))

        If idx < 0 Then idx = 0
        If idx >= count Then idx = count - 1

        Return idx
    End Function

    Private Function GetStableRandomSymbolStyle(canvas As VoronoiCanvas, cellIndex As Integer) As CellRenderStyle
        Dim styles As CellRenderStyle() = {
            CellRenderStyle.Circle,
            CellRenderStyle.Square,
            CellRenderStyle.RoundedSquare,
            CellRenderStyle.Triangle,
            CellRenderStyle.Pentagon,
            CellRenderStyle.Hexagon,
            CellRenderStyle.Octagon,
            CellRenderStyle.Star,
            CellRenderStyle.Star3,
            CellRenderStyle.Star4
        }

        If styles.Length = 0 Then Return CellRenderStyle.Circle

        Dim key As Integer = 0
        If canvas.SeedStyleKeys IsNot Nothing AndAlso cellIndex >= 0 AndAlso cellIndex < canvas.SeedStyleKeys.Count Then
            key = canvas.SeedStyleKeys(cellIndex)
        End If

        Dim idx As Integer = Math.Abs(key) Mod styles.Length
        Return styles(idx)
    End Function

    Private Function GetEffectiveRenderStyle(canvas As VoronoiCanvas, cellIndex As Integer) As CellRenderStyle
        If canvas.RenderStyle <> CellRenderStyle.Random Then Return canvas.RenderStyle
        Return GetStableRandomSymbolStyle(canvas, cellIndex)
    End Function

End Module
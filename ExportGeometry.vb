Imports System
Imports System.Collections.Generic
Imports System.Drawing

Public Enum ExportSegmentKind
    Line
    Arc
    CubicBezier
    Ellipse
    Circle
    EllipticalArc
    BSpline
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

' Ellisse completa in world-space. Center = centro; RadiusMajor/RadiusMinor =
' semiassi; RotationRad = angolo dell'asse maggiore (frame Y-in-basso, come il
' resto). Orientation conserva il valore grezzo Geom2dOrientationConstants letto
' da SE, da ripassare ad AddByCenter in export (per un'ellisse completa e'
' ininfluente sulla forma).
Public Class ExportEllipse2D
    Inherits ExportSegment2D

    Public Property Center As Vec2
    Public Property RadiusMajor As Double
    Public Property RadiusMinor As Double
    Public Property RotationRad As Double
    Public Property Orientation As Integer = 0

    Public Sub New()
        Kind = ExportSegmentKind.Ellipse
    End Sub

    Public Sub New(center As Vec2, rMajor As Double, rMinor As Double, rotationRad As Double, orientation As Integer)
        Kind = ExportSegmentKind.Ellipse
        Me.Center = center
        Me.RadiusMajor = rMajor
        Me.RadiusMinor = rMinor
        Me.RotationRad = rotationRad
        Me.Orientation = orientation
    End Sub
End Class

' Cerchio completo in world-space.
Public Class ExportCircle2D
    Inherits ExportSegment2D

    Public Property Center As Vec2
    Public Property Radius As Double

    Public Sub New()
        Kind = ExportSegmentKind.Circle
    End Sub

    Public Sub New(center As Vec2, radius As Double)
        Kind = ExportSegmentKind.Circle
        Me.Center = center
        Me.Radius = radius
    End Sub
End Class

' Arco di ellisse in world-space. I due semiassi sono memorizzati come VETTORI
' (modulo = semiasse), cosi' campionando P(t)=C+cos(t)*Major+sin(t)*Minor sia la
' riflessione in Y sia l'orientazione si gestiscono da sole. StartAngle/SweepAngle
' sono i valori parametrici originali di SE (radianti), conservati per il
' round-trip nativo in export. Orientation = Geom2dOrientationConstants grezzo.
Public Class ExportEllipticalArc2D
    Inherits ExportSegment2D

    Public Property Center As Vec2
    Public Property MajorAxis As Vec2
    Public Property MinorAxis As Vec2
    Public Property StartAngle As Double
    Public Property SweepAngle As Double
    Public Property Orientation As Integer = 0

    Public Sub New()
        Kind = ExportSegmentKind.EllipticalArc
    End Sub

    Public Sub New(center As Vec2, major As Vec2, minor As Vec2,
                   startAngle As Double, sweepAngle As Double, orientation As Integer)
        Kind = ExportSegmentKind.EllipticalArc
        Me.Center = center
        Me.MajorAxis = major
        Me.MinorAxis = minor
        Me.StartAngle = startAngle
        Me.SweepAngle = sweepAngle
        Me.Orientation = orientation
    End Sub
End Class

' Curva B-spline in world-space. Memorizziamo i NODI (punti di interpolazione
' attraverso cui passa la curva, come restituiti da GetNode) e il flag di
' chiusura tangenziale. In anteprima/SVG/DXF la curva viene approssimata con una
' spline di Catmull-Rom che passa per i nodi; in export verso SE si ri-emettono i
' nodi nativi (AddByPointsWithCloseOption), che ricostruisce la curva esatta.
Public Class ExportBSpline2D
    Inherits ExportSegment2D

    Public Property Nodes As New List(Of Vec2)
    Public Property ClosedCurve As Boolean = False

    Public Sub New()
        Kind = ExportSegmentKind.BSpline
    End Sub

    Public Sub New(nodes As List(Of Vec2), closedCurve As Boolean)
        Kind = ExportSegmentKind.BSpline
        Me.Nodes = nodes
        Me.ClosedCurve = closedCurve
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
        Dim radius As Double = BlockRadiusFromArea(cell, scaleFactor, canvas)

        ' Ancoraggio: il punto base del blocco coincide col seed della cella.
        Dim anchor As Vec2 = Geo2D.PolygonCentroid(cell.Vertices)
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
    Private Function BlockRadiusFromArea(cell As VoronoiCell, scaleFactor As Single, canvas As VoronoiCanvas) As Double
        Dim area As Double = Math.Abs(Geo2D.SignedArea(cell.Vertices))

        ' Tetto di sicurezza: una cella non puo' sensatamente essere piu' grande
        ' del dominio. Celle patologiche (es. artefatti di clipping con fori
        ' circolari) possono pero' avere area enorme/degenere: senza limite il
        ' blocco verrebbe scalato a dismisura e un suo arco genererebbe un raggio
        ' in pixel sterminato => OutOfMemory in GDI+. Limitiamo l'area a quella
        ' del dominio (sempre finita e ragionevole).
        If canvas IsNot Nothing Then
            Dim dom = canvas.Domain
            Dim domArea As Double = Math.Abs(CDbl(dom.Width) * CDbl(dom.Height))
            If domArea > 0.0 AndAlso area > domArea Then area = domArea
        End If

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
        Dim radius As Double = BlockRadiusFromArea(cell, scaleFactor, canvas)

        Dim angle As Double = 0.0
        If randomRotation Then angle = GetStableAngleFromKey(canvas, cellIndex)
        angle += GetEffectiveCellRotation(canvas, cellIndex)

        ' Il punto base del blocco viene posizionato sul seed della cella.
        Dim anchor As Vec2 = Geo2D.PolygonCentroid(cell.Vertices)

        cg.HasBlock = True
        cg.BlockName = def.Name
        ' Metri SE, con Y ribaltata indietro (SE ha Y verso l'alto).
        cg.BlockOriginX = anchor.X / 1000.0
        cg.BlockOriginY = -anchor.Y / 1000.0
        cg.BlockScale = radius / maxR
        ' Rotazione occorrenza in RADIANTI (SE usa radianti). Il segno e' -angle:
        ' il nostro frame Y-in-basso e SE Y-in-alto hanno verso opposto.
        cg.BlockRotation = -angle
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

            ElseIf TypeOf seg Is ExportEllipse2D Then
                Dim el = DirectCast(seg, ExportEllipse2D)
                Dim ang As Double = Math.Atan2(sinA, cosA)
                outp.Segments.Add(New ExportEllipse2D(
                    MapBlockPoint(el.Center, anchor, baseNx, baseNy, r, cosA, sinA),
                    el.RadiusMajor * r,
                    el.RadiusMinor * r,
                    el.RotationRad + ang,
                    el.Orientation))

            ElseIf TypeOf seg Is ExportCircle2D Then
                Dim ci = DirectCast(seg, ExportCircle2D)
                outp.Segments.Add(New ExportCircle2D(
                    MapBlockPoint(ci.Center, anchor, baseNx, baseNy, r, cosA, sinA),
                    ci.Radius * r))

            ElseIf TypeOf seg Is ExportEllipticalArc2D Then
                Dim ea = DirectCast(seg, ExportEllipticalArc2D)
                outp.Segments.Add(New ExportEllipticalArc2D(
                    MapBlockPoint(ea.Center, anchor, baseNx, baseNy, r, cosA, sinA),
                    MapBlockVector(ea.MajorAxis, r, cosA, sinA),
                    MapBlockVector(ea.MinorAxis, r, cosA, sinA),
                    ea.StartAngle, ea.SweepAngle, ea.Orientation))

            ElseIf TypeOf seg Is ExportBSpline2D Then
                Dim bs = DirectCast(seg, ExportBSpline2D)
                Dim mapped As New List(Of Vec2)
                For Each nd In bs.Nodes
                    mapped.Add(MapBlockPoint(nd, anchor, baseNx, baseNy, r, cosA, sinA))
                Next
                outp.Segments.Add(New ExportBSpline2D(mapped, bs.ClosedCurve))
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

    ' Come MapBlockPoint ma per un VETTORE (direzione/asse): solo rotazione e
    ' scala, niente traslazione ne' punto base.
    Private Function MapBlockVector(v As Vec2, r As Double, cosA As Double, sinA As Double) As Vec2
        Dim rx As Double = v.X * cosA - v.Y * sinA
        Dim ry As Double = v.X * sinA + v.Y * cosA
        Return New Vec2(rx * r, ry * r)
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
                ElseIf TypeOf seg Is ExportEllipse2D Then
                    AccumulateEllipsePoints(DirectCast(seg, ExportEllipse2D), pts)
                ElseIf TypeOf seg Is ExportCircle2D Then
                    Dim ci = DirectCast(seg, ExportCircle2D)
                    pts.Add(New Vec2(ci.Center.X - ci.Radius, ci.Center.Y - ci.Radius))
                    pts.Add(New Vec2(ci.Center.X + ci.Radius, ci.Center.Y + ci.Radius))
                ElseIf TypeOf seg Is ExportEllipticalArc2D Then
                    Dim ea = DirectCast(seg, ExportEllipticalArc2D)
                    pts.AddRange(SampleEllipticalArc(ea.Center, ea.MajorAxis, ea.MinorAxis, ea.StartAngle, ea.SweepAngle, ea.Orientation))
                ElseIf TypeOf seg Is ExportBSpline2D Then
                    Dim bs = DirectCast(seg, ExportBSpline2D)
                    pts.AddRange(bs.Nodes)
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
                    Dim ns0 = NormBlockPoint(a.StartPoint, cx, cy, maxR)
                    Dim ne0 = NormBlockPoint(a.EndPoint, cx, cy, maxR)
                    Dim nr As Double = a.Radius / maxR

                    ' Un arco con raggio enorme rispetto all'ingombro del blocco e'
                    ' di fatto rettilineo: tenerlo come arco genererebbe, una volta
                    ' scalato, un raggio in pixel spropositato (OOM in GDI+) e archi
                    ' degeneri in export. Lo declassiamo a segmento.
                    If Double.IsNaN(nr) OrElse Double.IsInfinity(nr) OrElse nr > 200.0 Then
                        ns.Add(New ExportLine2D(ns0, ne0))
                    Else
                        Dim na As New ExportArc2D(NormBlockPoint(a.Center, cx, cy, maxR),
                                                  nr,
                                                  ns0,
                                                  ne0,
                                                  a.Clockwise)
                        na.SweepDeg = a.SweepDeg
                        ns.Add(na)
                    End If
                ElseIf TypeOf seg Is ExportEllipse2D Then
                    Dim el = DirectCast(seg, ExportEllipse2D)
                    ns.Add(New ExportEllipse2D(NormBlockPoint(el.Center, cx, cy, maxR),
                                               el.RadiusMajor / maxR,
                                               el.RadiusMinor / maxR,
                                               el.RotationRad,
                                               el.Orientation))
                ElseIf TypeOf seg Is ExportCircle2D Then
                    Dim ci = DirectCast(seg, ExportCircle2D)
                    ns.Add(New ExportCircle2D(NormBlockPoint(ci.Center, cx, cy, maxR), ci.Radius / maxR))
                ElseIf TypeOf seg Is ExportEllipticalArc2D Then
                    Dim ea = DirectCast(seg, ExportEllipticalArc2D)
                    ns.Add(New ExportEllipticalArc2D(
                        NormBlockPoint(ea.Center, cx, cy, maxR),
                        New Vec2(ea.MajorAxis.X / maxR, ea.MajorAxis.Y / maxR),
                        New Vec2(ea.MinorAxis.X / maxR, ea.MinorAxis.Y / maxR),
                        ea.StartAngle, ea.SweepAngle, ea.Orientation))
                ElseIf TypeOf seg Is ExportBSpline2D Then
                    Dim bs = DirectCast(seg, ExportBSpline2D)
                    Dim nn As New List(Of Vec2)
                    For Each nd In bs.Nodes
                        nn.Add(NormBlockPoint(nd, cx, cy, maxR))
                    Next
                    ns.Add(New ExportBSpline2D(nn, bs.ClosedCurve))
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

    ' Campiona un'ellisse completa in 'steps' punti (frame world). L'eventuale
    ' chiusura del loop e' lasciata al chiamante.
    Public Function SampleEllipse(center As Vec2,
                                  rMajor As Double,
                                  rMinor As Double,
                                  rotationRad As Double,
                                  steps As Integer) As List(Of Vec2)
        Dim pts As New List(Of Vec2)
        If steps < 8 Then steps = 8
        Dim ca As Double = Math.Cos(rotationRad)
        Dim sa As Double = Math.Sin(rotationRad)
        For i As Integer = 0 To steps - 1
            Dim t As Double = (i / CDbl(steps)) * 2.0 * Math.PI
            Dim ex As Double = rMajor * Math.Cos(t)
            Dim ey As Double = rMinor * Math.Sin(t)
            pts.Add(New Vec2(center.X + ex * ca - ey * sa,
                             center.Y + ex * sa + ey * ca))
        Next
        Return pts
    End Function

    Private Sub AccumulateEllipsePoints(el As ExportEllipse2D, pts As List(Of Vec2))
        pts.AddRange(SampleEllipse(el.Center, el.RadiusMajor, el.RadiusMinor, el.RotationRad, 16))
    End Sub

    ' Campiona un arco di ellisse: P(t) = Center + cos(t)*Major + sin(t)*Minor.
    ' SweepAngle e' una MAGNITUDINE; il verso e' dato da Orientation. Nel nostro
    ' frame (Y ribaltato) il segno risulta invertito: orient=1 -> +sweep,
    ' orient<>1 -> -sweep. Restituisce una polilinea aperta.
    Public Function SampleEllipticalArc(center As Vec2,
                                        major As Vec2,
                                        minor As Vec2,
                                        startAngle As Double,
                                        sweepAngle As Double,
                                        orientation As Integer) As List(Of Vec2)
        Dim effSweep As Double = If(orientation = 1, sweepAngle, -sweepAngle)
        Dim pts As New List(Of Vec2)
        Dim steps As Integer = Math.Max(8, CInt(Math.Ceiling(64.0 * Math.Abs(effSweep) / (2.0 * Math.PI))))
        For i As Integer = 0 To steps
            Dim t As Double = startAngle + effSweep * (i / CDbl(steps))
            Dim ct As Double = Math.Cos(t)
            Dim st As Double = Math.Sin(t)
            pts.Add(New Vec2(center.X + ct * major.X + st * minor.X,
                             center.Y + ct * major.Y + st * minor.Y))
        Next
        Return pts
    End Function

    ' Campiona una B-spline interpolante con una SPLINE CUBICA C2 GLOBALE che passa
    ' per i nodi: condizioni NATURALI per le curve aperte, PERIODICHE per quelle
    ' chiuse. Parametrizzazione a lunghezza di corda. Cosi' la forma (tangenti e
    ' curvatura fra i nodi) aderisce alla B-spline di Solid Edge, senza i "gomiti"
    ' della precedente interpolazione locale. Polilinea fitta; per le chiuse il
    ' punto finale NON duplica l'iniziale (chiusura gestita dal flag Closed).
    Public Function SampleBSpline(nodes As List(Of Vec2), closedCurve As Boolean) As List(Of Vec2)
        Dim outPts As New List(Of Vec2)
        If nodes Is Nothing OrElse nodes.Count = 0 Then Return outPts

        ' SE chiude la curva ripetendo il primo punto come ultimo nodo; inoltre
        ' possono esserci nodi coincidenti. Rimuovendoli si evita un segmento di
        ' lunghezza nulla che, nella spline, produrrebbe una "frustata" vicino alla
        ' giunzione (derivate seconde divergenti). RemoveDuplicateSequentialPoints
        ' toglie anche il punto di chiusura ripetuto (first == last).
        Dim bb = Geo2D.GetBounds(nodes)
        Dim diag As Double = Math.Sqrt(bb.Width * bb.Width + bb.Height * bb.Height)
        Dim eps As Double = Math.Max(diag * 0.000001, 0.000000001)
        Dim cleanNodes = Geo2D.RemoveDuplicateSequentialPoints(nodes, eps)

        If cleanNodes.Count = 0 Then Return outPts
        If cleanNodes.Count = 1 Then
            outPts.Add(cleanNodes(0))
            Return outPts
        End If
        If cleanNodes.Count = 2 Then
            outPts.Add(cleanNodes(0))
            outPts.Add(cleanNodes(1))
            Return outPts
        End If

        Dim n As Integer = cleanNodes.Count
        Dim segPerSpan As Integer = 18
        Const epsLen As Double = 0.000001

        Dim vx(n - 1) As Double
        Dim vy(n - 1) As Double
        For i As Integer = 0 To n - 1
            vx(i) = cleanNodes(i).X
            vy(i) = cleanNodes(i).Y
        Next

        If closedCurve Then
            ' lunghezze dei segmenti i -> (i+1) mod n
            Dim h(n - 1) As Double
            For i As Integer = 0 To n - 1
                h(i) = Math.Max(Geo2D.Distance(cleanNodes(i), cleanNodes((i + 1) Mod n)), epsLen)
            Next

            Dim mx = PeriodicSplineM(h, vx)
            Dim my = PeriodicSplineM(h, vy)

            For i As Integer = 0 To n - 1
                Dim ni As Integer = (i + 1) Mod n
                For s As Integer = 0 To segPerSpan - 1
                    Dim tq As Double = h(i) * (s / CDbl(segPerSpan))
                    Dim x = EvalCubicScalar(tq, 0.0, h(i), vx(i), vx(ni), mx(i), mx(ni))
                    Dim y = EvalCubicScalar(tq, 0.0, h(i), vy(i), vy(ni), my(i), my(ni))
                    outPts.Add(New Vec2(x, y))
                Next
            Next
        Else
            ' lunghezze dei segmenti i -> i+1 (n-1 segmenti)
            Dim h(n - 2) As Double
            For i As Integer = 0 To n - 2
                h(i) = Math.Max(Geo2D.Distance(cleanNodes(i), cleanNodes(i + 1)), epsLen)
            Next

            Dim mx = NaturalSplineM(h, vx)
            Dim my = NaturalSplineM(h, vy)

            For i As Integer = 0 To n - 2
                For s As Integer = 0 To segPerSpan - 1
                    Dim tq As Double = h(i) * (s / CDbl(segPerSpan))
                    Dim x = EvalCubicScalar(tq, 0.0, h(i), vx(i), vx(i + 1), mx(i), mx(i + 1))
                    Dim y = EvalCubicScalar(tq, 0.0, h(i), vy(i), vy(i + 1), my(i), my(i + 1))
                    outPts.Add(New Vec2(x, y))
                Next
            Next
            outPts.Add(cleanNodes(n - 1))
        End If

        Return outPts
    End Function

    ' Valore di un tratto di spline cubica su [ta,tb] con derivate seconde Ma,Mb.
    Private Function EvalCubicScalar(tq As Double, ta As Double, tb As Double,
                                     va As Double, vb As Double,
                                     ma As Double, mb As Double) As Double
        Dim h As Double = tb - ta
        If h <= 0.0000001 Then Return va
        Dim A As Double = (tb - tq) / h
        Dim B As Double = (tq - ta) / h
        Return A * va + B * vb + ((A * A * A - A) * ma + (B * B * B - B) * mb) * (h * h) / 6.0
    End Function

    ' Derivate seconde per spline cubica NATURALE (M0 = M(n-1) = 0).
    ' h(0..n-2) = lunghezze segmenti; v(0..n-1) = valori nodali.
    Private Function NaturalSplineM(h As Double(), v As Double()) As Double()
        Dim n As Integer = v.Length
        Dim m(n - 1) As Double
        If n < 3 Then Return m

        Dim a(n - 1) As Double
        Dim b(n - 1) As Double
        Dim c(n - 1) As Double
        Dim d(n - 1) As Double

        b(0) = 1.0 : c(0) = 0.0 : d(0) = 0.0
        a(n - 1) = 0.0 : b(n - 1) = 1.0 : d(n - 1) = 0.0

        For i As Integer = 1 To n - 2
            a(i) = h(i - 1)
            c(i) = h(i)
            b(i) = 2.0 * (h(i - 1) + h(i))
            d(i) = 6.0 * ((v(i + 1) - v(i)) / h(i) - (v(i) - v(i - 1)) / h(i - 1))
        Next

        Return SolveThomas(a, b, c, d)
    End Function

    ' Derivate seconde per spline cubica PERIODICA (M(n) = M(0)).
    ' h(0..n-1) = lunghezze segmenti i->(i+1)mod n; v(0..n-1) = valori nodali.
    Private Function PeriodicSplineM(h As Double(), v As Double()) As Double()
        Dim n As Integer = v.Length
        Dim m(n - 1) As Double
        If n < 3 Then Return m

        Dim a(n - 1) As Double
        Dim b(n - 1) As Double
        Dim c(n - 1) As Double
        Dim d(n - 1) As Double

        For i As Integer = 0 To n - 1
            Dim hm As Double = h((i - 1 + n) Mod n)
            Dim hi As Double = h(i)
            a(i) = hm
            c(i) = hi
            b(i) = 2.0 * (hm + hi)
            Dim vp As Double = v((i + 1) Mod n)
            Dim vmm As Double = v((i - 1 + n) Mod n)
            d(i) = 6.0 * ((vp - v(i)) / hi - (v(i) - vmm) / hm)
        Next

        ' corner: alpha (riga0,col n-1) = h(n-1); beta (riga n-1,col0) = h(n-1)
        Dim alpha As Double = h(n - 1)
        Dim beta As Double = h(n - 1)
        a(0) = 0.0
        c(n - 1) = 0.0

        Return SolveCyclic(a, b, c, d, alpha, beta)
    End Function

    ' Risolutore tridiagonale (Thomas). a=sub, b=diag, c=super, d=termine noto.
    Private Function SolveThomas(a As Double(), b As Double(), c As Double(), d As Double()) As Double()
        Dim n As Integer = b.Length
        Dim cp(n - 1) As Double
        Dim dp(n - 1) As Double
        Dim x(n - 1) As Double

        cp(0) = c(0) / b(0)
        dp(0) = d(0) / b(0)
        For i As Integer = 1 To n - 1
            Dim mden As Double = b(i) - a(i) * cp(i - 1)
            If Math.Abs(mden) < 0.0000000001 Then mden = 0.0000000001
            cp(i) = c(i) / mden
            dp(i) = (d(i) - a(i) * dp(i - 1)) / mden
        Next

        x(n - 1) = dp(n - 1)
        For i As Integer = n - 2 To 0 Step -1
            x(i) = dp(i) - cp(i) * x(i + 1)
        Next
        Return x
    End Function

    ' Risolutore tridiagonale CICLICO (Sherman-Morrison). alpha = angolo alto-dx
    ' (riga0,col n-1), beta = angolo basso-sx (riga n-1,col0).
    Private Function SolveCyclic(a As Double(), b As Double(), c As Double(), d As Double(),
                                 alpha As Double, beta As Double) As Double()
        Dim n As Integer = b.Length
        Dim m(n - 1) As Double
        If n < 2 Then Return SolveThomas(a, b, c, d)

        Dim gamma As Double = -b(0)
        If Math.Abs(gamma) < 0.0000000001 Then gamma = -0.0000000001

        Dim bb(n - 1) As Double
        For i As Integer = 0 To n - 1
            bb(i) = b(i)
        Next
        bb(0) = b(0) - gamma
        bb(n - 1) = b(n - 1) - alpha * beta / gamma

        Dim x = SolveThomas(a, bb, c, d)

        Dim u(n - 1) As Double
        u(0) = gamma
        u(n - 1) = alpha
        Dim z = SolveThomas(a, bb, c, u)

        Dim denom As Double = 1.0 + z(0) + beta * z(n - 1) / gamma
        If Math.Abs(denom) < 0.0000000001 Then denom = 0.0000000001
        Dim fact As Double = (x(0) + beta * x(n - 1) / gamma) / denom

        For i As Integer = 0 To n - 1
            m(i) = x(i) - fact * z(i)
        Next
        Return m
    End Function

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

    ' Colore di palette per una cella (stesso usato per le geometrie stilizzate).
    ' Esposto per consentire agli exporter di colorare riempimenti coerentemente.
    Public Function GetExportCellColor(index As Integer) As Color
        Return GetExportColor(index)
    End Function

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


    ' Anelli di offset interno della cella, puliti (via Clipper).
    ' Offset ~0 => poligono base. Puo' restituire piu' anelli o nessuno.
    Private Function GetInsetLoops(vertices As List(Of Vec2), insetWorld As Single) As List(Of List(Of Vec2))
        If vertices Is Nothing OrElse vertices.Count < 3 Then Return New List(Of List(Of Vec2))()

        If insetWorld <= 0.0001F Then
            Return New List(Of List(Of Vec2)) From {New List(Of Vec2)(vertices)}
        End If

        Return VoronoiEngine.OffsetPolygon(vertices, -CDbl(insetWorld))
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

    ' ===== Riempimento: costruzione degli anelli chiusi di una cella =====
    ' I blocchi memorizzano ogni linea/arco come path APERTO separato; per
    ' riempire correttamente bisogna prima concatenare i segmenti che condividono
    ' gli estremi in anelli chiusi. Le primitive gia' chiuse (ellisse, cerchio,
    ' spline chiusa) diventano subito un anello. Il risultato (in world) va poi
    ' riempito con regola even-odd, cosi' i profili interni diventano fori.
    Public Function BuildCellFillLoops(cg As CellGeometry) As List(Of List(Of Vec2))
        Dim loops As New List(Of List(Of Vec2))
        If cg Is Nothing OrElse cg.StyledPaths Is Nothing OrElse cg.StyledPaths.Count = 0 Then Return loops

        Dim flats As New List(Of List(Of Vec2))
        Dim closedHint As New List(Of Boolean)
        Dim minX As Double = Double.MaxValue, minY As Double = Double.MaxValue
        Dim maxX As Double = Double.MinValue, maxY As Double = Double.MinValue

        For Each sp In cg.StyledPaths
            Dim poly = FlattenStyledPath(sp)
            If poly Is Nothing OrElse poly.Count < 2 Then Continue For
            flats.Add(poly)
            closedHint.Add(sp.Closed)
            For Each p In poly
                If p.X < minX Then minX = p.X
                If p.Y < minY Then minY = p.Y
                If p.X > maxX Then maxX = p.X
                If p.Y > maxY Then maxY = p.Y
            Next
        Next

        If flats.Count = 0 Then Return loops

        Dim diag As Double = Math.Sqrt((maxX - minX) * (maxX - minX) + (maxY - minY) * (maxY - minY))
        Dim tol As Double = Math.Max(diag * 0.0005, 0.000001)
        Dim closeTol As Double = Math.Max(diag * 0.002, tol)

        Dim openPieces As New List(Of List(Of Vec2))
        For i As Integer = 0 To flats.Count - 1
            Dim poly = flats(i)
            Dim selfClosed As Boolean = closedHint(i) OrElse Geo2D.Distance(poly(0), poly(poly.Count - 1)) <= closeTol
            If selfClosed AndAlso poly.Count >= 3 Then
                loops.Add(poly)
            Else
                openPieces.Add(poly)
            End If
        Next

        loops.AddRange(ChainOpenPieces(openPieces, tol, closeTol))
        Return loops
    End Function

    Private Function ChainOpenPieces(pieces As List(Of List(Of Vec2)), tol As Double, closeTol As Double) As List(Of List(Of Vec2))
        Dim loops As New List(Of List(Of Vec2))
        Dim remaining As New List(Of List(Of Vec2))(pieces)

        Do While remaining.Count > 0
            Dim cur As New List(Of Vec2)(remaining(0))
            remaining.RemoveAt(0)

            Dim extended As Boolean = True
            Do While extended AndAlso remaining.Count > 0
                extended = False
                Dim head As Vec2 = cur(0)
                Dim tail As Vec2 = cur(cur.Count - 1)

                For i As Integer = 0 To remaining.Count - 1
                    Dim pc = remaining(i)
                    Dim f As Vec2 = pc(0)
                    Dim l As Vec2 = pc(pc.Count - 1)

                    If Geo2D.Distance(tail, f) <= tol Then
                        For k As Integer = 1 To pc.Count - 1
                            cur.Add(pc(k))
                        Next
                        remaining.RemoveAt(i) : extended = True : Exit For
                    ElseIf Geo2D.Distance(tail, l) <= tol Then
                        Dim r As New List(Of Vec2)(pc) : r.Reverse()
                        For k As Integer = 1 To r.Count - 1
                            cur.Add(r(k))
                        Next
                        remaining.RemoveAt(i) : extended = True : Exit For
                    ElseIf Geo2D.Distance(head, l) <= tol Then
                        Dim merged As New List(Of Vec2)(pc)
                        For k As Integer = 1 To cur.Count - 1
                            merged.Add(cur(k))
                        Next
                        cur = merged
                        remaining.RemoveAt(i) : extended = True : Exit For
                    ElseIf Geo2D.Distance(head, f) <= tol Then
                        Dim r As New List(Of Vec2)(pc) : r.Reverse()
                        Dim merged As New List(Of Vec2)(r)
                        For k As Integer = 1 To cur.Count - 1
                            merged.Add(cur(k))
                        Next
                        cur = merged
                        remaining.RemoveAt(i) : extended = True : Exit For
                    End If
                Next
            Loop

            ' Si aggiunge come anello solo se chiuso: i tratti aperti (decorazioni)
            ' restano comunque tracciati a parte, quindi non riempirli e' corretto.
            If cur.Count >= 3 AndAlso Geo2D.Distance(cur(0), cur(cur.Count - 1)) <= closeTol Then
                loops.Add(cur)
            End If
        Loop

        Return loops
    End Function

    Private Function FlattenStyledPath(sp As ExportPath2D) As List(Of Vec2)
        Dim pts As New List(Of Vec2)
        If sp Is Nothing OrElse sp.Segments Is Nothing Then Return pts

        For Each seg In sp.Segments
            Dim segPts = FlattenSegment(seg)
            For Each p In segPts
                If pts.Count = 0 OrElse Geo2D.Distance(pts(pts.Count - 1), p) > 0.000001 Then
                    pts.Add(p)
                End If
            Next
        Next
        Return pts
    End Function

    Private Function FlattenSegment(seg As ExportSegment2D) As List(Of Vec2)
        Dim pts As New List(Of Vec2)

        If TypeOf seg Is ExportLine2D Then
            Dim ln = DirectCast(seg, ExportLine2D)
            pts.Add(ln.P1)
            pts.Add(ln.P2)

        ElseIf TypeOf seg Is ExportArc2D Then
            pts.AddRange(SampleArc2D(DirectCast(seg, ExportArc2D)))

        ElseIf TypeOf seg Is ExportCubicBezier2D Then
            Dim bz = DirectCast(seg, ExportCubicBezier2D)
            Dim steps As Integer = 18
            For i As Integer = 0 To steps
                Dim t As Double = i / CDbl(steps)
                pts.Add(EvalCubicBezier(bz.P0, bz.C1, bz.C2, bz.P3, t))
            Next

        ElseIf TypeOf seg Is ExportEllipse2D Then
            Dim el = DirectCast(seg, ExportEllipse2D)
            pts.AddRange(SampleEllipse(el.Center, el.RadiusMajor, el.RadiusMinor, el.RotationRad, 96))

        ElseIf TypeOf seg Is ExportCircle2D Then
            Dim ci = DirectCast(seg, ExportCircle2D)
            Dim steps As Integer = 72
            For i As Integer = 0 To steps
                Dim a As Double = (i / CDbl(steps)) * Math.PI * 2.0
                pts.Add(New Vec2(ci.Center.X + Math.Cos(a) * ci.Radius, ci.Center.Y + Math.Sin(a) * ci.Radius))
            Next

        ElseIf TypeOf seg Is ExportEllipticalArc2D Then
            Dim ea = DirectCast(seg, ExportEllipticalArc2D)
            pts.AddRange(SampleEllipticalArc(ea.Center, ea.MajorAxis, ea.MinorAxis, ea.StartAngle, ea.SweepAngle, ea.Orientation))

        ElseIf TypeOf seg Is ExportBSpline2D Then
            Dim bs = DirectCast(seg, ExportBSpline2D)
            pts.AddRange(SampleBSpline(bs.Nodes, bs.ClosedCurve))
        End If

        Return pts
    End Function

    Private Function SampleArc2D(arc As ExportArc2D) As List(Of Vec2)
        Dim pts As New List(Of Vec2)
        Dim c = arc.Center
        Dim a1 As Double = Math.Atan2(arc.StartPoint.Y - c.Y, arc.StartPoint.X - c.X)

        Dim sweepDeg As Double
        If Not Double.IsNaN(arc.SweepDeg) Then
            sweepDeg = arc.SweepDeg
        Else
            Dim a2 As Double = Math.Atan2(arc.EndPoint.Y - c.Y, arc.EndPoint.X - c.X)
            sweepDeg = (a2 - a1) * 180.0 / Math.PI
            While sweepDeg <= -180.0
                sweepDeg += 360.0
            End While
            While sweepDeg > 180.0
                sweepDeg -= 360.0
            End While
        End If

        Dim sweepRad As Double = sweepDeg * Math.PI / 180.0
        Dim steps As Integer = Math.Max(2, CInt(Math.Ceiling(Math.Abs(sweepDeg) / 6.0)))
        For i As Integer = 0 To steps
            Dim a As Double = a1 + sweepRad * (i / CDbl(steps))
            pts.Add(New Vec2(c.X + Math.Cos(a) * arc.Radius, c.Y + Math.Sin(a) * arc.Radius))
        Next
        Return pts
    End Function

    Private Function EvalCubicBezier(p0 As Vec2, c1 As Vec2, c2 As Vec2, p3 As Vec2, t As Double) As Vec2
        Dim u As Double = 1.0 - t
        Dim x = u * u * u * p0.X + 3.0 * u * u * t * c1.X + 3.0 * u * t * t * c2.X + t * t * t * p3.X
        Dim y = u * u * u * p0.Y + 3.0 * u * u * t * c1.Y + 3.0 * u * t * t * c2.Y + t * t * t * p3.Y
        Return New Vec2(x, y)
    End Function

End Module
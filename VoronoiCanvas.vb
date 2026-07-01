Imports System
Imports System.Collections.Generic
Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Windows.Forms

Public Enum CellRenderStyle
    Straight
    Curved
    Random
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
    BlockSymbol
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


    ' Blocchi importati da Solid Edge, come definizioni a primitive (linee/archi).
    ' Lo stile BlockSymbol ne piazza uno per cella, scelto in modo stabile.
    Public Property BlockSymbols As New List(Of BlockDefinition)

    Private Structure ViewInfo
        Public Scale As Single
        Public OffsetX As Single
        Public OffsetY As Single
    End Structure

    Public Event SeedsEdited As EventHandler



    Public Property Cells As List(Of VoronoiCell) = New List(Of VoronoiCell)
    Public Property EditableSeeds As List(Of Vec2) = New List(Of Vec2)
    Public Property SeedStyleKeys As List(Of Integer) = New List(Of Integer)
    Public Property Domain As RectangleF = New RectangleF(0, 0, 1000, 700)

    Public Property SketchBoundaries As List(Of List(Of Vec2)) = New List(Of List(Of Vec2))
    Public Property SketchBoundaryIsHole As List(Of Boolean) = New List(Of Boolean)
    Public Property ShowSketchBoundary As Boolean = True

    Public Property FillCells As Boolean = True
    Public Property FillSymbols As Boolean = False
    Public Property ShowOuterEdges As Boolean = True
    Public Property ShowSeeds As Boolean = True
    Public Property ShowInnerCurve As Boolean = True
    Public Property AllowSeedEditing As Boolean = True

    Public Property RenderStyle As CellRenderStyle = CellRenderStyle.Curved
    Public Property CellScale As Single = 0.82F
    Public Property RandomRotation As Boolean = True


    ' --- Sistema vertici UNICO (sostituisce InnerCornerMode/SymbolCornerMode) ---
    ' Una sola modalita' applicata sia ai simboli sia al contorno celle (Curved):
    '   Sharp     = spigolo vivo
    '   FilletArc = raggiatura con arco
    '   Bezier    = curva spline
    ' VertexTrim e' un fattore relativo al lato (come la vecchia trim); per la
    ' spline indica quanto la curva parte lontano dallo spigolo. La bombatura
    ' della spline e' fissa (VertexSplineBulge).
    Public Property VertexMode As SymbolCornerStyle = SymbolCornerStyle.Bezier
    Public Property VertexTrim As Single = 0.22F
    Public Property VertexSplineBulge As Single = 0.55F

    Public Property SeedRadius As Single = 4.0F
    Public Property HitRadius As Single = 10.0F

    Public Property InnerOffset As Single = 0.0F
    Public Property InnerCurveWidth As Single = 1.8F


    Private dragSeedIndex As Integer = -1
    Private hoverSeedIndex As Integer = -1
    Private isDragging As Boolean = False


    Public Event SeedScalesEdited As EventHandler
    Public Event SeedRotationsEdited As EventHandler

    Public Property CellScales As List(Of Single) = New List(Of Single)
    Public Property MinCellScale As Single = 0.05F
    Public Property MaxCellScale As Single = 1.5F
    Public Property MouseWheelScaleStep As Single = 0.02F

    ' Offset di rotazione per-cella (radianti), additivo sull'angolo base.
    Public Property CellRotations As List(Of Single) = New List(Of Single)
    Public Property MouseWheelRotateStepDeg As Single = 5.0F

    ' Offset per-cella del simbolo/blocco (CTRL + rotella): cicla tra i simboli/blocchi.
    Public Property CellSymbolOffsets As List(Of Integer) = New List(Of Integer)
    Public Event SeedSymbolOffsetsEdited As EventHandler




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

        EnsureCellScaleCount(Cells.Count)
        EnsureCellRotationCount(Cells.Count)
        EnsureCellSymbolOffsetCount(Cells.Count)

        ' Geometria stilizzata dalla fonte UNICA (world-space): stesso
        ' risultato che useranno gli exporter.
        Dim geoms = ExportGeometry.BuildCellGeometry(Me)

        ' Livello 1 + 2: fill e bordo esterno dal poligono grezzo della cella.
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

                If ShowOuterEdges Then
                    If GetEffectiveRenderStyle(i) = CellRenderStyle.Straight Then
                        g.DrawPolygon(outerPen, outerPts)
                    Else
                        g.DrawPolygon(Pens.DimGray, outerPts)
                    End If
                End If
            Next
        End Using

        ' Livello 3: geometria stilizzata convertita in pixel.
        ' Straight coincide col bordo gia' disegnato; Curved e' soggetto al
        ' toggle ShowInnerCurve; i simboli sono sempre attivi.
        For Each cg In geoms
            If cg.EffectiveStyle = CellRenderStyle.Straight Then Continue For
            If cg.EffectiveStyle = CellRenderStyle.Curved AndAlso Not ShowInnerCurve Then Continue For

            ' Path schermo di tutti i sotto-profili della cella.
            Dim cellPaths As New List(Of GraphicsPath)
            Dim cellSegs As New List(Of ExportPath2D)
            For Each sp In cg.StyledPaths
                Dim p = ToScreenPath(sp, view)
                If p IsNot Nothing Then
                    cellPaths.Add(p)
                    cellSegs.Add(sp)
                End If
            Next
            If cellPaths.Count = 0 Then Continue For

            ' FILL: anelli chiusi della cella (segmenti concatenati) in UN solo
            ' path con regola even-odd, cosi' i profili interni diventano fori e
            ' anche i contorni "articolati" (fatti di tanti segmenti) si riempiono.
            If FillSymbols Then
                Try
                    Dim loops = ExportGeometry.BuildCellFillLoops(cg)
                    If loops.Count > 0 Then
                        Using composite As New GraphicsPath()
                            composite.FillMode = FillMode.Alternate
                            For Each lp In loops
                                If lp.Count < 3 Then Continue For
                                Dim scr(lp.Count - 1) As PointF
                                Dim okPts As Boolean = True
                                For k As Integer = 0 To lp.Count - 1
                                    Dim spt = WorldToScreen(lp(k), view)
                                    If Not IsSanePt(spt) Then
                                        okPts = False
                                        Exit For
                                    End If
                                    scr(k) = spt
                                Next
                                If okPts Then composite.AddPolygon(scr)
                            Next
                            Dim fc = ExportGeometry.GetExportCellColor(cg.CellIndex)
                            Using br As New SolidBrush(Color.FromArgb(235, fc.R, fc.G, fc.B))
                                g.FillPath(br, composite)
                            End Using
                        End Using
                    End If
                Catch
                    ' Geometria non riempibile: si ignora il fill.
                End Try
            End If

            ' STROKE: ogni sotto-profilo col proprio bordo.
            For k As Integer = 0 To cellPaths.Count - 1
                Dim sp = cellSegs(k)
                Using pen As New Pen(sp.StrokeColor, CSng(sp.StrokeWidth))
                    pen.LineJoin = LineJoin.Round
                    pen.StartCap = LineCap.Round
                    pen.EndCap = LineCap.Round
                    Try
                        g.DrawPath(pen, cellPaths(k))
                    Catch
                        ' Geometria degenere sfuggita ai filtri: si salta.
                    End Try
                End Using
            Next

            For Each p In cellPaths
                p.Dispose()
            Next
        Next
    End Sub

    ' Soglia minima di lunghezza disegnabile (pixel). Elementi piu' corti di
    ' questa vengono saltati: tracciare un segmento/arco di lunghezza ~nulla con
    ' line cap arrotondati provoca una OutOfMemoryException in GDI+ (bug noto dei
    ' cap non-Flat). Sotto questa soglia l'elemento e' comunque invisibile.
    Private Const MinDrawLenPx As Single = 0.3F

    Private Function ScreenDist(a As PointF, b As PointF) As Single
        Dim dx As Single = a.X - b.X
        Dim dy As Single = a.Y - b.Y
        Return CSng(Math.Sqrt(dx * dx + dy * dy))
    End Function

    ' Converte un ExportPath2D world-space in un GraphicsPath in pixel.
    Private Function ToScreenPath(wp As ExportPath2D, view As ViewInfo) As GraphicsPath
        If wp Is Nothing OrElse wp.Segments Is Nothing OrElse wp.Segments.Count = 0 Then Return Nothing

        Dim path As New GraphicsPath()
        path.StartFigure()
        Dim added As Boolean = False

        For Each seg In wp.Segments
            If TypeOf seg Is ExportLine2D Then
                Dim ln = DirectCast(seg, ExportLine2D)
                Dim a = WorldToScreen(ln.P1, view)
                Dim b = WorldToScreen(ln.P2, view)
                If IsSanePt(a) AndAlso IsSanePt(b) AndAlso ScreenDist(a, b) >= MinDrawLenPx Then
                    path.AddLine(a, b)
                    added = True
                End If

            ElseIf TypeOf seg Is ExportCubicBezier2D Then
                Dim bz = DirectCast(seg, ExportCubicBezier2D)
                Dim p0 = WorldToScreen(bz.P0, view)
                Dim k1 = WorldToScreen(bz.C1, view)
                Dim k2 = WorldToScreen(bz.C2, view)
                Dim p3 = WorldToScreen(bz.P3, view)
                If IsSanePt(p0) AndAlso IsSanePt(k1) AndAlso IsSanePt(k2) AndAlso IsSanePt(p3) Then
                    ' Lunghezza stimata col poligono di controllo: se ~0, salta.
                    Dim approx As Single = ScreenDist(p0, k1) + ScreenDist(k1, k2) + ScreenDist(k2, p3)
                    If approx >= MinDrawLenPx Then
                        path.AddBezier(p0, k1, k2, p3)
                        added = True
                    End If
                End If

            ElseIf TypeOf seg Is ExportArc2D Then
                If AddWorldArc(path, DirectCast(seg, ExportArc2D), view) Then added = True

            ElseIf TypeOf seg Is ExportEllipse2D Then
                Dim el = DirectCast(seg, ExportEllipse2D)
                Dim wpts = ExportGeometry.SampleEllipse(el.Center, el.RadiusMajor, el.RadiusMinor, el.RotationRad, 48)
                Dim spts As New List(Of PointF)
                Dim ok As Boolean = True
                Dim mnx As Single = Single.MaxValue, mny As Single = Single.MaxValue
                Dim mxx As Single = Single.MinValue, mxy As Single = Single.MinValue
                For Each wp2 In wpts
                    Dim sp = WorldToScreen(wp2, view)
                    If Not IsSanePt(sp) Then
                        ok = False
                        Exit For
                    End If
                    spts.Add(sp)
                    If sp.X < mnx Then mnx = sp.X
                    If sp.Y < mny Then mny = sp.Y
                    If sp.X > mxx Then mxx = sp.X
                    If sp.Y > mxy Then mxy = sp.Y
                Next
                ' Estensione minima: evita il bug GDI+ dei cap su figure ~nulle.
                If ok AndAlso spts.Count >= 3 AndAlso (mxx - mnx) + (mxy - mny) >= MinDrawLenPx Then
                    spts.Add(spts(0))
                    path.AddLines(spts.ToArray())
                    added = True
                End If

            ElseIf TypeOf seg Is ExportCircle2D Then
                Dim ci = DirectCast(seg, ExportCircle2D)
                Dim cen = WorldToScreen(ci.Center, view)
                Dim rPx As Single = CSng(ci.Radius * view.Scale)
                If IsSanePt(cen) AndAlso rPx > 0.0F AndAlso (rPx * 2.0F) >= MinDrawLenPx Then
                    path.AddEllipse(cen.X - rPx, cen.Y - rPx, rPx * 2.0F, rPx * 2.0F)
                    added = True
                End If

            ElseIf TypeOf seg Is ExportEllipticalArc2D Then
                Dim ea = DirectCast(seg, ExportEllipticalArc2D)
                Dim wpts = ExportGeometry.SampleEllipticalArc(ea.Center, ea.MajorAxis, ea.MinorAxis, ea.StartAngle, ea.SweepAngle, ea.Orientation)
                Dim spts As New List(Of PointF)
                Dim ok As Boolean = True
                Dim mnx As Single = Single.MaxValue, mny As Single = Single.MaxValue
                Dim mxx As Single = Single.MinValue, mxy As Single = Single.MinValue
                For Each wp2 In wpts
                    Dim sp = WorldToScreen(wp2, view)
                    If Not IsSanePt(sp) Then
                        ok = False
                        Exit For
                    End If
                    spts.Add(sp)
                    If sp.X < mnx Then mnx = sp.X
                    If sp.Y < mny Then mny = sp.Y
                    If sp.X > mxx Then mxx = sp.X
                    If sp.Y > mxy Then mxy = sp.Y
                Next
                If ok AndAlso spts.Count >= 2 AndAlso (mxx - mnx) + (mxy - mny) >= MinDrawLenPx Then
                    path.AddLines(spts.ToArray())
                    added = True
                End If

            ElseIf TypeOf seg Is ExportBSpline2D Then
                Dim bs = DirectCast(seg, ExportBSpline2D)
                Dim wpts = ExportGeometry.SampleBSpline(bs.Nodes, bs.ClosedCurve)
                Dim spts As New List(Of PointF)
                Dim ok As Boolean = True
                Dim mnx As Single = Single.MaxValue, mny As Single = Single.MaxValue
                Dim mxx As Single = Single.MinValue, mxy As Single = Single.MinValue
                For Each wp2 In wpts
                    Dim sp = WorldToScreen(wp2, view)
                    If Not IsSanePt(sp) Then
                        ok = False
                        Exit For
                    End If
                    spts.Add(sp)
                    If sp.X < mnx Then mnx = sp.X
                    If sp.Y < mny Then mny = sp.Y
                    If sp.X > mxx Then mxx = sp.X
                    If sp.Y > mxy Then mxy = sp.Y
                Next
                If ok AndAlso spts.Count >= 2 AndAlso (mxx - mnx) + (mxy - mny) >= MinDrawLenPx Then
                    path.AddLines(spts.ToArray())
                    If bs.ClosedCurve AndAlso spts.Count >= 3 Then
                        path.AddLine(spts(spts.Count - 1), spts(0))
                    End If
                    added = True
                End If
            End If
        Next

        If wp.Closed Then path.CloseFigure()
        Return path
    End Function

    ' Disegna un arco world-space su GraphicsPath. Ritorna True se ha aggiunto
    ' qualcosa di disegnabile, False se l'arco e' stato scartato (degenere). Il
    ' centro e' noto dal builder: per gli archi importati si usa lo sweep reale
    ' con segno, per i raccordi/semicerchi l'arco minore.
    Private Function AddWorldArc(path As GraphicsPath, arc As ExportArc2D, view As ViewInfo) As Boolean
        Dim c = WorldToScreen(arc.Center, view)
        Dim s = WorldToScreen(arc.StartPoint, view)
        Dim ept = WorldToScreen(arc.EndPoint, view)
        Dim rPx As Single = CSng(arc.Radius * view.Scale)

        ' Coordinate non finite o assurdamente fuori scala => spazzatura: scarta.
        If Not (IsSanePt(c) AndAlso IsSanePt(s) AndAlso IsSanePt(ept)) Then Return False

        ' Raggio sproporzionato o ~0 => l'arco e' di fatto un segmento.
        Dim maxRPx As Single = CSng((Math.Max(ClientSize.Width, ClientSize.Height) + 1) * 8)
        If Single.IsNaN(rPx) OrElse Single.IsInfinity(rPx) OrElse rPx <= 0.01F OrElse rPx > maxRPx Then
            If ScreenDist(s, ept) >= MinDrawLenPx Then
                path.AddLine(s, ept)
                Return True
            End If
            Return False
        End If

        Dim startAngle As Double = Math.Atan2(s.Y - c.Y, s.X - c.X) * 180.0 / Math.PI

        Dim sweep As Double
        If Not Double.IsNaN(arc.SweepDeg) Then
            sweep = arc.SweepDeg
        Else
            Dim endAngle As Double = Math.Atan2(ept.Y - c.Y, ept.X - c.X) * 180.0 / Math.PI
            sweep = endAngle - startAngle
            While sweep <= -180.0
                sweep += 360.0
            End While
            While sweep > 180.0
                sweep -= 360.0
            End While
        End If

        If Double.IsNaN(startAngle) OrElse Double.IsNaN(sweep) OrElse Double.IsInfinity(sweep) Then
            If ScreenDist(s, ept) >= MinDrawLenPx Then
                path.AddLine(s, ept)
                Return True
            End If
            Return False
        End If

        ' Lunghezza dell'arco in pixel = r * |sweep(rad)|. Se trascurabile lo si
        ' SALTA: un arco di lunghezza ~nulla con cap arrotondati manda GDI+ in
        ' OutOfMemoryException. E' la causa del crash con blocchi che hanno archi
        ' a sweep minuscolo, scalati in celle piccole.
        Dim arcLenPx As Double = rPx * Math.Abs(sweep) * Math.PI / 180.0
        If arcLenPx < MinDrawLenPx Then Return False

        Dim rect As New RectangleF(c.X - rPx, c.Y - rPx, rPx * 2.0F, rPx * 2.0F)
        path.AddArc(rect, CSng(startAngle), CSng(sweep))
        Return True
    End Function

    ' Vero se il punto e' finito (no NaN / Infinito) E entro un limite molto
    ' largo rispetto al canvas. Coordinate oltre questo limite indicano
    ' geometria degenere che farebbe esplodere GDI+ in fase di disegno.
    Private Function IsSanePt(p As PointF) As Boolean
        If Single.IsNaN(p.X) OrElse Single.IsInfinity(p.X) OrElse
           Single.IsNaN(p.Y) OrElse Single.IsInfinity(p.Y) Then Return False
        Dim lim As Single = CSng((Math.Max(ClientSize.Width, ClientSize.Height) + 1) * 50)
        Return Math.Abs(p.X) <= lim AndAlso Math.Abs(p.Y) <= lim
    End Function



    Private Sub EnsureCellScaleCount(requiredCount As Integer)
        If CellScales Is Nothing Then
            CellScales = New List(Of Single)()
        End If

        While CellScales.Count < requiredCount
            CellScales.Add(ClampCellScale(CellScale))
        End While

        While CellScales.Count > requiredCount
            CellScales.RemoveAt(CellScales.Count - 1)
        End While
    End Sub

    Private Function ClampCellScale(value As Single) As Single
        Return CSng(Math.Max(MinCellScale, Math.Min(MaxCellScale, value)))
    End Function

    Private Function GetEffectiveCellScale(cellIndex As Integer) As Single
        If CellScales IsNot Nothing AndAlso cellIndex >= 0 AndAlso cellIndex < CellScales.Count Then
            Return ClampCellScale(CellScales(cellIndex))
        End If

        Return ClampCellScale(CellScale)
    End Function

    Private Sub EnsureCellRotationCount(requiredCount As Integer)
        If CellRotations Is Nothing Then
            CellRotations = New List(Of Single)()
        End If

        While CellRotations.Count < requiredCount
            CellRotations.Add(0.0F)
        End While

        While CellRotations.Count > requiredCount
            CellRotations.RemoveAt(CellRotations.Count - 1)
        End While
    End Sub

    Private Sub EnsureCellSymbolOffsetCount(requiredCount As Integer)
        If CellSymbolOffsets Is Nothing Then
            CellSymbolOffsets = New List(Of Integer)()
        End If

        While CellSymbolOffsets.Count < requiredCount
            CellSymbolOffsets.Add(0)
        End While

        While CellSymbolOffsets.Count > requiredCount
            CellSymbolOffsets.RemoveAt(CellSymbolOffsets.Count - 1)
        End While
    End Sub

    Private Function IsSymbolStyle(style As CellRenderStyle) As Boolean
        Return style <> CellRenderStyle.Straight AndAlso style <> CellRenderStyle.Curved
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

    'Protected Overrides Sub OnMouseDown(e As MouseEventArgs)
    '    MyBase.OnMouseDown(e)
    '    If Not AllowSeedEditing Then Return

    '    Dim idx = HitTestSeed(e.Location)

    '    If e.Button = MouseButtons.Left Then
    '        If idx >= 0 Then
    '            dragSeedIndex = idx
    '            isDragging = True
    '            Capture = True
    '            Invalidate()
    '            Return
    '        End If

    '        If (ModifierKeys And Keys.Control) = Keys.Control Then
    '            EditableSeeds.Add(ClampToDomain(ScreenToWorld(e.Location)))
    '            RaiseEvent SeedsEdited(Me, EventArgs.Empty)
    '            Invalidate()
    '        End If
    '    ElseIf e.Button = MouseButtons.Right Then
    '        If idx >= 0 AndAlso EditableSeeds.Count > 3 Then
    '            EditableSeeds.RemoveAt(idx)
    '            hoverSeedIndex = -1
    '            dragSeedIndex = -1
    '            RaiseEvent SeedsEdited(Me, EventArgs.Empty)
    '            Invalidate()
    '        End If
    '    End If
    'End Sub


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
                EnsureCellScaleCount(EditableSeeds.Count)
                CellScales(EditableSeeds.Count - 1) = ClampCellScale(CellScale)
                RaiseEvent SeedsEdited(Me, EventArgs.Empty)
                Invalidate()
            End If

        ElseIf e.Button = MouseButtons.Right Then
            If idx >= 0 AndAlso EditableSeeds.Count > 3 Then
                EditableSeeds.RemoveAt(idx)

                If CellScales IsNot Nothing AndAlso idx < CellScales.Count Then
                    CellScales.RemoveAt(idx)
                End If

                If SeedStyleKeys IsNot Nothing AndAlso idx < SeedStyleKeys.Count Then
                    SeedStyleKeys.RemoveAt(idx)
                End If

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

    'Protected Overrides Sub OnMouseDoubleClick(e As MouseEventArgs)
    '    MyBase.OnMouseDoubleClick(e)
    '    If Not AllowSeedEditing Then Return

    '    EditableSeeds.Add(ClampToDomain(ScreenToWorld(e.Location)))
    '    RaiseEvent SeedsEdited(Me, EventArgs.Empty)
    '    Invalidate()
    'End Sub

    Protected Overrides Sub OnMouseDoubleClick(e As MouseEventArgs)
        MyBase.OnMouseDoubleClick(e)
        If Not AllowSeedEditing Then Return

        EditableSeeds.Add(ClampToDomain(ScreenToWorld(e.Location)))
        EnsureCellScaleCount(EditableSeeds.Count)
        CellScales(EditableSeeds.Count - 1) = ClampCellScale(CellScale)

        RaiseEvent SeedsEdited(Me, EventArgs.Empty)
        Invalidate()
    End Sub


    Protected Overrides Sub OnMouseWheel(e As MouseEventArgs)
        MyBase.OnMouseWheel(e)
        If Not AllowSeedEditing Then Return

        Dim idx As Integer = HitTestSeed(e.Location)
        hoverSeedIndex = idx

        If idx < 0 Then
            Invalidate()
            Return
        End If

        Dim effectiveStyle As CellRenderStyle = GetEffectiveRenderStyle(idx)
        If Not IsSymbolStyle(effectiveStyle) Then
            Invalidate()
            Return
        End If

        Dim wheelSteps As Single = CSng(e.Delta) / 120.0F
        If Math.Abs(wheelSteps) < 0.001F Then Return

        If (ModifierKeys And Keys.Control) = Keys.Control Then
            ' CTRL + rotella: cambia SOLO questa cella con il simbolo/blocco
            ' successivo (o precedente) in sequenza. Persistente come scala/rotazione.
            EnsureCellSymbolOffsetCount(EditableSeeds.Count)
            Dim stepN As Integer = If(wheelSteps > 0, 1, -1)
            CellSymbolOffsets(idx) = CellSymbolOffsets(idx) + stepN
            RaiseEvent SeedSymbolOffsetsEdited(Me, EventArgs.Empty)
            Invalidate()
            Return
        End If

        If (ModifierKeys And Keys.Shift) = Keys.Shift Then
            ' SHIFT + rotella: ruota il simbolo della cella (offset additivo).
            EnsureCellRotationCount(EditableSeeds.Count)
            Dim stepRad As Single = CSng(MouseWheelRotateStepDeg * Math.PI / 180.0)
            CellRotations(idx) = CellRotations(idx) + stepRad * wheelSteps
            RaiseEvent SeedRotationsEdited(Me, EventArgs.Empty)
            Invalidate()
            Return
        End If

        EnsureCellScaleCount(EditableSeeds.Count)

        Dim oldScale As Single = GetEffectiveCellScale(idx)
        Dim newScale As Single = ClampCellScale(oldScale + MouseWheelScaleStep * wheelSteps)

        If Math.Abs(newScale - oldScale) < 0.0001F Then Return

        CellScales(idx) = newScale
        RaiseEvent SeedScalesEdited(Me, EventArgs.Empty)
        Invalidate()
    End Sub


    Private Function GetEffectiveRenderStyle(cell As VoronoiCell) As CellRenderStyle
        If RenderStyle <> CellRenderStyle.Random Then Return RenderStyle
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

    Private Function GetStableRandomSymbolStyleByIndex(cellIndex As Integer) As CellRenderStyle
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
        If SeedStyleKeys IsNot Nothing AndAlso cellIndex >= 0 AndAlso cellIndex < SeedStyleKeys.Count Then
            key = SeedStyleKeys(cellIndex)
        End If

        Dim idx As Integer = Math.Abs(key) Mod styles.Length
        Return styles(idx)
    End Function

    Private Function GetEffectiveRenderStyle(cellIndex As Integer) As CellRenderStyle
        If RenderStyle <> CellRenderStyle.Random Then Return RenderStyle
        Return GetStableRandomSymbolStyleByIndex(cellIndex)
    End Function

End Class
Imports System
Imports System.Collections.Generic
Imports System.Drawing
Imports System.Globalization
Imports System.IO
Imports System.Linq
Imports System.Text

Public Module SvgExporter

    Public Sub SaveSvg(filePath As String, paths As IEnumerable(Of ExportPath2D))
        Dim allPts As New List(Of Vec2)

        For Each path In paths
            For Each seg In path.Segments
                If TypeOf seg Is ExportLine2D Then
                    Dim ln = DirectCast(seg, ExportLine2D)
                    allPts.Add(ln.P1)
                    allPts.Add(ln.P2)

                ElseIf TypeOf seg Is ExportArc2D Then
                    Dim a = DirectCast(seg, ExportArc2D)
                    allPts.Add(a.StartPoint)
                    allPts.Add(a.EndPoint)
                    allPts.Add(New Vec2(a.Center.X - a.Radius, a.Center.Y - a.Radius))
                    allPts.Add(New Vec2(a.Center.X + a.Radius, a.Center.Y + a.Radius))

                ElseIf TypeOf seg Is ExportCubicBezier2D Then
                    Dim b = DirectCast(seg, ExportCubicBezier2D)
                    allPts.Add(b.P0)
                    allPts.Add(b.C1)
                    allPts.Add(b.C2)
                    allPts.Add(b.P3)
                End If
            Next
        Next

        If allPts.Count = 0 Then Exit Sub

        Dim minX = allPts.Min(Function(p) p.X)
        Dim minY = allPts.Min(Function(p) p.Y)
        Dim maxX = allPts.Max(Function(p) p.X)
        Dim maxY = allPts.Max(Function(p) p.Y)

        Dim w = maxX - minX
        Dim h = maxY - minY

        Dim sb As New StringBuilder()
        sb.AppendLine("<?xml version=""1.0"" encoding=""UTF-8""?>")
        sb.AppendLine($"<svg xmlns=""http://www.w3.org/2000/svg"" version=""1.1"" viewBox=""{F(minX)} {F(minY)} {F(w)} {F(h)}"">")

        For Each path In paths
            Dim d = BuildPathData(path)
            Dim strokeColor = ColorToSvg(path.StrokeColor)
            Dim fillColor = FillToSvg(path.FillColor)
            Dim strokeWidth = F(path.StrokeWidth)

            sb.AppendLine($"  <path d=""{d}"" fill=""{fillColor}"" stroke=""{strokeColor}"" stroke-width=""{strokeWidth}"" stroke-linejoin=""round"" stroke-linecap=""round"" />")
        Next

        sb.AppendLine("</svg>")

        File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8)
    End Sub

    ' Esporta l'intera scena visibile sul canvas secondo le checkbox attive:
    ' contorno sketch, riempimento celle, bordi esterni, simboli/curve interne,
    ' seed. Coordinate in world-space (Y verso il basso, come SVG e come il canvas).
    Public Sub SaveSvgFull(filePath As String, canvas As VoronoiCanvas)
        If canvas Is Nothing Then Exit Sub

        Dim geoms = ExportGeometry.BuildCellGeometry(canvas)

        ' Bounding box da tutto cio' che verra' effettivamente disegnato.
        Dim has As Boolean = False
        Dim minX As Double = 0, minY As Double = 0, maxX As Double = 0, maxY As Double = 0

        For Each cg In geoms
            If cg.Cell IsNot Nothing AndAlso cg.Cell.Vertices IsNot Nothing Then
                For Each p In cg.Cell.Vertices
                    AccBounds(p, has, minX, minY, maxX, maxY)
                Next
            End If
            For Each sp In cg.StyledPaths
                For Each q In PathExtentPoints(sp)
                    AccBounds(q, has, minX, minY, maxX, maxY)
                Next
            Next
        Next

        If canvas.ShowSeeds AndAlso canvas.EditableSeeds IsNot Nothing Then
            For Each p In canvas.EditableSeeds
                AccBounds(p, has, minX, minY, maxX, maxY)
            Next
        End If

        If canvas.ShowSketchBoundary AndAlso canvas.SketchBoundaries IsNot Nothing Then
            For Each loopPts In canvas.SketchBoundaries
                If loopPts Is Nothing Then Continue For
                For Each p In loopPts
                    AccBounds(p, has, minX, minY, maxX, maxY)
                Next
            Next
        End If

        ' Il riempimento del dominio/profilo fa parte del disegno: includilo nei bounds.
        If canvas.ShowDomainFill Then
            If canvas.SketchDomains IsNot Nothing AndAlso canvas.SketchDomains.Count > 0 Then
                For Each d In canvas.SketchDomains
                    If d Is Nothing OrElse d.Outer Is Nothing Then Continue For
                    For Each p In d.Outer
                        AccBounds(p, has, minX, minY, maxX, maxY)
                    Next
                Next
            Else
                AccBounds(New Vec2(canvas.Domain.Left, canvas.Domain.Top), has, minX, minY, maxX, maxY)
                AccBounds(New Vec2(canvas.Domain.Right, canvas.Domain.Bottom), has, minX, minY, maxX, maxY)
            End If
        End If

        If Not has Then Exit Sub

        Dim diag As Double = Math.Sqrt((maxX - minX) * (maxX - minX) + (maxY - minY) * (maxY - minY))
        If diag <= 0.0 Then diag = 1.0

        Dim pad As Double = diag * 0.02
        minX -= pad : minY -= pad : maxX += pad : maxY += pad
        Dim w As Double = maxX - minX
        Dim h As Double = maxY - minY

        Dim edgeW As Double = Math.Max(CDbl(canvas.InnerCurveWidth), diag * 0.0008)
        Dim seedR As Double = Math.Max(diag * 0.005, edgeW)

        Dim sb As New StringBuilder()
        sb.AppendLine("<?xml version=""1.0"" encoding=""UTF-8""?>")
        sb.AppendLine($"<svg xmlns=""http://www.w3.org/2000/svg"" version=""1.1"" viewBox=""{F(minX)} {F(minY)} {F(w)} {F(h)}"">")

        ' 0) Domain fill: SOLO il "dentro" (profili sketch o rettangolo del
        '    dominio) col colore del canvas. Lo sfondo del tema NON viene
        '    esportato: fuori dal profilo l'SVG resta trasparente, e i fori
        '    bucano davvero (fill-rule even-odd).
        If canvas.ShowDomainFill Then
            Dim bgIn As String = ColorToSvg(canvas.DomainFillColor)

            sb.AppendLine($"  <g id=""domainfill"" stroke=""none"">")

            If canvas.SketchDomains IsNot Nothing AndAlso canvas.SketchDomains.Count > 0 Then
                For Each d In canvas.SketchDomains
                    If d Is Nothing OrElse d.Outer Is Nothing OrElse d.Outer.Count < 3 Then Continue For

                    Dim fd As New StringBuilder()
                    fd.Append($"M {F(d.Outer(0).X)} {F(d.Outer(0).Y)} ")
                    For k As Integer = 1 To d.Outer.Count - 1
                        fd.Append($"L {F(d.Outer(k).X)} {F(d.Outer(k).Y)} ")
                    Next
                    fd.Append("Z")

                    If d.Holes IsNot Nothing Then
                        For Each hle In d.Holes
                            If hle Is Nothing OrElse hle.Count < 3 Then Continue For
                            fd.Append($" M {F(hle(0).X)} {F(hle(0).Y)} ")
                            For k As Integer = 1 To hle.Count - 1
                                fd.Append($"L {F(hle(k).X)} {F(hle(k).Y)} ")
                            Next
                            fd.Append("Z")
                        Next
                    End If

                    sb.AppendLine($"    <path d=""{fd}"" fill=""{bgIn}"" fill-rule=""evenodd"" />")
                Next
            Else
                Dim dm = canvas.Domain
                sb.AppendLine($"    <rect x=""{F(dm.Left)}"" y=""{F(dm.Top)}"" width=""{F(dm.Width)}"" height=""{F(dm.Height)}"" fill=""{bgIn}"" />")
            End If

            sb.AppendLine("  </g>")
        End If

        ' 1) Contorno sketch (giallo = esterno, rosso = foro).
        If canvas.ShowSketchBoundary AndAlso canvas.SketchBoundaries IsNot Nothing AndAlso canvas.SketchBoundaries.Count > 0 Then
            sb.AppendLine("  <g id=""sketch"" fill=""none"" stroke-linejoin=""round"" stroke-linecap=""round"">")
            For i As Integer = 0 To canvas.SketchBoundaries.Count - 1
                Dim loopPts = canvas.SketchBoundaries(i)
                If loopPts Is Nothing OrElse loopPts.Count < 2 Then Continue For
                Dim isHole As Boolean = canvas.SketchBoundaryIsHole IsNot Nothing AndAlso
                                        i < canvas.SketchBoundaryIsHole.Count AndAlso
                                        canvas.SketchBoundaryIsHole(i)
                Dim col As String = If(isHole, "#FF7878", "#FFD250")
                Dim wdt As String = F(edgeW * If(isHole, 1.2, 1.6))
                sb.AppendLine($"    <polygon points=""{PolyPoints(loopPts)}"" stroke=""{col}"" stroke-width=""{wdt}"" />")
            Next
            sb.AppendLine("  </g>")
        End If

        ' 2) Riempimento celle (colore di palette, semitrasparente).
        If canvas.FillCells Then
            sb.AppendLine("  <g id=""fill"" stroke=""none"" fill-opacity=""0.165"">")
            For Each cg In geoms
                If cg.Cell Is Nothing OrElse cg.Cell.Vertices Is Nothing OrElse cg.Cell.Vertices.Count < 3 Then Continue For
                Dim col As String = ColorToSvg(ExportGeometry.GetExportCellColor(cg.CellIndex))
                sb.AppendLine($"    <polygon points=""{PolyPoints(cg.Cell.Vertices)}"" fill=""{col}"" />")
            Next
            sb.AppendLine("  </g>")
        End If

        ' 3) Bordi esterni delle celle (Straight: tinta chiara; altri: grigio).
        If canvas.ShowOuterEdges Then
            sb.AppendLine("  <g id=""edges"" fill=""none"" stroke-linejoin=""round"">")
            For Each cg In geoms
                If cg.Cell Is Nothing OrElse cg.Cell.Vertices Is Nothing OrElse cg.Cell.Vertices.Count < 3 Then Continue For
                Dim col As String = If(cg.EffectiveStyle = CellRenderStyle.Straight, "#B4F5F0", "#696969")
                sb.AppendLine($"    <polygon points=""{PolyPoints(cg.Cell.Vertices)}"" stroke=""{col}"" stroke-width=""{F(edgeW)}"" />")
            Next
            sb.AppendLine("  </g>")
        End If

        ' 4) Geometria stilizzata (simboli / curve interne). Straight e' gia' reso
        '    dai bordi esterni; Curved e' soggetto al toggle ShowInnerCurve.
        sb.AppendLine("  <g id=""symbols"" stroke-linejoin=""round"" stroke-linecap=""round"">")
        For Each cg In geoms
            If cg.EffectiveStyle = CellRenderStyle.Straight Then Continue For
            If cg.EffectiveStyle = CellRenderStyle.Curved AndAlso Not canvas.ShowInnerCurve Then Continue For

            If canvas.FillSymbols Then
                ' Riempimento: anelli chiusi della cella (segmenti concatenati) in
                ' un unico path con fill-rule even-odd (profili interni = fori,
                ' contorni articolati riempiti). Lo stroke resta dalle primitive.
                Dim loops = ExportGeometry.BuildCellFillLoops(cg)
                If loops.Count > 0 Then
                    Dim fillD As New StringBuilder()
                    For Each lp In loops
                        If lp.Count < 3 Then Continue For
                        If fillD.Length > 0 Then fillD.Append(" ")
                        fillD.Append($"M {F(lp(0).X)} {F(lp(0).Y)} ")
                        For k As Integer = 1 To lp.Count - 1
                            fillD.Append($"L {F(lp(k).X)} {F(lp(k).Y)} ")
                        Next
                        fillD.Append("Z")
                    Next
                    If fillD.Length > 0 Then
                        Dim col As String = ColorToSvg(ExportGeometry.GetExportCellColor(cg.CellIndex))
                        sb.AppendLine($"    <path d=""{fillD}"" fill=""{col}"" fill-opacity=""0.92"" fill-rule=""evenodd"" stroke=""none"" />")
                    End If
                End If
                ' Bordi delle primitive originali.
                For Each sp In cg.StyledPaths
                    Dim d = BuildPathData(sp)
                    If String.IsNullOrEmpty(d) Then Continue For
                    sb.AppendLine($"    <path d=""{d}"" fill=""none"" stroke=""{ColorToSvg(sp.StrokeColor)}"" stroke-width=""{F(sp.StrokeWidth)}"" />")
                Next
            Else
                For Each sp In cg.StyledPaths
                    Dim d = BuildPathData(sp)
                    If String.IsNullOrEmpty(d) Then Continue For
                    sb.AppendLine($"    <path d=""{d}"" fill=""{FillToSvg(sp.FillColor)}"" stroke=""{ColorToSvg(sp.StrokeColor)}"" stroke-width=""{F(sp.StrokeWidth)}"" />")
                Next
            End If
        Next
        sb.AppendLine("  </g>")

        ' 5) Seed.
        If canvas.ShowSeeds AndAlso canvas.EditableSeeds IsNot Nothing AndAlso canvas.EditableSeeds.Count > 0 Then
            sb.AppendLine($"  <g id=""seeds"" fill=""#00BCD4"" stroke=""#080635"" stroke-width=""{F(seedR * 0.3)}"">")
            For Each p In canvas.EditableSeeds
                sb.AppendLine($"    <circle cx=""{F(p.X)}"" cy=""{F(p.Y)}"" r=""{F(seedR)}"" />")
            Next
            sb.AppendLine("  </g>")
        End If

        sb.AppendLine("</svg>")
        File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8)
    End Sub

    Private Function PolyPoints(pts As IEnumerable(Of Vec2)) As String
        Dim sb As New StringBuilder()
        For Each p In pts
            sb.Append($"{F(p.X)},{F(p.Y)} ")
        Next
        Return sb.ToString().Trim()
    End Function

    Private Sub AccBounds(p As Vec2, ByRef has As Boolean,
                          ByRef minX As Double, ByRef minY As Double,
                          ByRef maxX As Double, ByRef maxY As Double)
        If Double.IsNaN(p.X) OrElse Double.IsInfinity(p.X) OrElse
           Double.IsNaN(p.Y) OrElse Double.IsInfinity(p.Y) Then Exit Sub
        If Not has Then
            minX = p.X : maxX = p.X : minY = p.Y : maxY = p.Y : has = True
        Else
            If p.X < minX Then minX = p.X
            If p.Y < minY Then minY = p.Y
            If p.X > maxX Then maxX = p.X
            If p.Y > maxY Then maxY = p.Y
        End If
    End Sub

    Private Function PathExtentPoints(path As ExportPath2D) As List(Of Vec2)
        Dim r As New List(Of Vec2)
        If path Is Nothing OrElse path.Segments Is Nothing Then Return r
        For Each seg In path.Segments
            If TypeOf seg Is ExportLine2D Then
                Dim ln = DirectCast(seg, ExportLine2D)
                r.Add(ln.P1) : r.Add(ln.P2)
            ElseIf TypeOf seg Is ExportArc2D Then
                Dim a = DirectCast(seg, ExportArc2D)
                r.Add(a.StartPoint) : r.Add(a.EndPoint)
                r.Add(New Vec2(a.Center.X - a.Radius, a.Center.Y - a.Radius))
                r.Add(New Vec2(a.Center.X + a.Radius, a.Center.Y + a.Radius))
            ElseIf TypeOf seg Is ExportCubicBezier2D Then
                Dim b = DirectCast(seg, ExportCubicBezier2D)
                r.Add(b.P0) : r.Add(b.C1) : r.Add(b.C2) : r.Add(b.P3)
            ElseIf TypeOf seg Is ExportEllipse2D Then
                Dim el = DirectCast(seg, ExportEllipse2D)
                r.AddRange(ExportGeometry.SampleEllipse(el.Center, el.RadiusMajor, el.RadiusMinor, el.RotationRad, 16))
            ElseIf TypeOf seg Is ExportCircle2D Then
                Dim ci = DirectCast(seg, ExportCircle2D)
                r.Add(New Vec2(ci.Center.X - ci.Radius, ci.Center.Y - ci.Radius))
                r.Add(New Vec2(ci.Center.X + ci.Radius, ci.Center.Y + ci.Radius))
            ElseIf TypeOf seg Is ExportEllipticalArc2D Then
                Dim ea = DirectCast(seg, ExportEllipticalArc2D)
                r.AddRange(ExportGeometry.SampleEllipticalArc(ea.Center, ea.MajorAxis, ea.MinorAxis, ea.StartAngle, ea.SweepAngle, ea.Orientation))
            ElseIf TypeOf seg Is ExportBSpline2D Then
                Dim bs = DirectCast(seg, ExportBSpline2D)
                r.AddRange(bs.Nodes)
            End If
        Next
        Return r
    End Function

    Private Function BuildPathData(path As ExportPath2D) As String
        Dim sb As New StringBuilder()
        Dim firstMove As Boolean = True

        For Each seg In path.Segments
            If TypeOf seg Is ExportLine2D Then
                Dim s = DirectCast(seg, ExportLine2D)
                If firstMove Then
                    sb.Append($"M {F(s.P1.X)} {F(s.P1.Y)} ")
                    firstMove = False
                End If
                sb.Append($"L {F(s.P2.X)} {F(s.P2.Y)} ")

            ElseIf TypeOf seg Is ExportArc2D Then
                Dim s = DirectCast(seg, ExportArc2D)
                If firstMove Then
                    sb.Append($"M {F(s.StartPoint.X)} {F(s.StartPoint.Y)} ")
                    firstMove = False
                End If

                Dim largeArc As Integer = 0
                Dim sweepFlag As Integer = If(s.Clockwise, 0, 1)
                If Not Double.IsNaN(s.SweepDeg) Then
                    ' Arco da blocco: estensione e verso reali (Y in basso, come SVG).
                    largeArc = If(Math.Abs(s.SweepDeg) > 180.0, 1, 0)
                    sweepFlag = If(s.SweepDeg > 0.0, 1, 0)
                End If
                sb.Append($"A {F(s.Radius)} {F(s.Radius)} 0 {largeArc} {sweepFlag} {F(s.EndPoint.X)} {F(s.EndPoint.Y)} ")

            ElseIf TypeOf seg Is ExportCubicBezier2D Then
                Dim s = DirectCast(seg, ExportCubicBezier2D)
                If firstMove Then
                    sb.Append($"M {F(s.P0.X)} {F(s.P0.Y)} ")
                    firstMove = False
                End If

                sb.Append($"C {F(s.C1.X)} {F(s.C1.Y)} {F(s.C2.X)} {F(s.C2.Y)} {F(s.P3.X)} {F(s.P3.Y)} ")

            ElseIf TypeOf seg Is ExportEllipse2D Then
                Dim s = DirectCast(seg, ExportEllipse2D)
                Dim dirx As Double = Math.Cos(s.RotationRad)
                Dim diry As Double = Math.Sin(s.RotationRad)
                Dim p0x As Double = s.Center.X + dirx * s.RadiusMajor
                Dim p0y As Double = s.Center.Y + diry * s.RadiusMajor
                Dim p1x As Double = s.Center.X - dirx * s.RadiusMajor
                Dim p1y As Double = s.Center.Y - diry * s.RadiusMajor
                Dim rotDeg As Double = s.RotationRad * 180.0 / Math.PI

                ' Un'ellisse completa = due semi-archi ellittici. SVG supporta
                ' nativamente rx ry e la rotazione dell'asse x.
                If firstMove Then
                    sb.Append($"M {F(p0x)} {F(p0y)} ")
                    firstMove = False
                Else
                    sb.Append($"L {F(p0x)} {F(p0y)} ")
                End If
                sb.Append($"A {F(s.RadiusMajor)} {F(s.RadiusMinor)} {F(rotDeg)} 0 1 {F(p1x)} {F(p1y)} ")
                sb.Append($"A {F(s.RadiusMajor)} {F(s.RadiusMinor)} {F(rotDeg)} 0 1 {F(p0x)} {F(p0y)} ")

            ElseIf TypeOf seg Is ExportCircle2D Then
                Dim s = DirectCast(seg, ExportCircle2D)
                Dim p0x As Double = s.Center.X + s.Radius
                Dim p0y As Double = s.Center.Y
                Dim p1x As Double = s.Center.X - s.Radius
                Dim p1y As Double = s.Center.Y
                If firstMove Then
                    sb.Append($"M {F(p0x)} {F(p0y)} ")
                    firstMove = False
                Else
                    sb.Append($"L {F(p0x)} {F(p0y)} ")
                End If
                sb.Append($"A {F(s.Radius)} {F(s.Radius)} 0 0 1 {F(p1x)} {F(p1y)} ")
                sb.Append($"A {F(s.Radius)} {F(s.Radius)} 0 0 1 {F(p0x)} {F(p0y)} ")

            ElseIf TypeOf seg Is ExportEllipticalArc2D Then
                Dim s = DirectCast(seg, ExportEllipticalArc2D)
                Dim arcPts = ExportGeometry.SampleEllipticalArc(s.Center, s.MajorAxis, s.MinorAxis, s.StartAngle, s.SweepAngle, s.Orientation)
                For k As Integer = 0 To arcPts.Count - 1
                    If k = 0 Then
                        If firstMove Then
                            sb.Append($"M {F(arcPts(k).X)} {F(arcPts(k).Y)} ")
                            firstMove = False
                        Else
                            sb.Append($"L {F(arcPts(k).X)} {F(arcPts(k).Y)} ")
                        End If
                    Else
                        sb.Append($"L {F(arcPts(k).X)} {F(arcPts(k).Y)} ")
                    End If
                Next

            ElseIf TypeOf seg Is ExportBSpline2D Then
                Dim s = DirectCast(seg, ExportBSpline2D)
                Dim curvePts = ExportGeometry.SampleBSpline(s.Nodes, s.ClosedCurve)
                For k As Integer = 0 To curvePts.Count - 1
                    If k = 0 Then
                        If firstMove Then
                            sb.Append($"M {F(curvePts(k).X)} {F(curvePts(k).Y)} ")
                            firstMove = False
                        Else
                            sb.Append($"L {F(curvePts(k).X)} {F(curvePts(k).Y)} ")
                        End If
                    Else
                        sb.Append($"L {F(curvePts(k).X)} {F(curvePts(k).Y)} ")
                    End If
                Next
            End If
        Next

        If path.Closed Then sb.Append("Z")
        Return sb.ToString().Trim()
    End Function

    Private Function ColorToSvg(c As Color) As String
        Return $"#{c.R:X2}{c.G:X2}{c.B:X2}"
    End Function

    Private Function FillToSvg(c As Color) As String
        If c.A = 0 Then Return "none"
        Return $"#{c.R:X2}{c.G:X2}{c.B:X2}"
    End Function

    Private Function F(v As Double) As String
        Return v.ToString("0.###", CultureInfo.InvariantCulture)
    End Function

End Module
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
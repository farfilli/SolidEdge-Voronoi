Imports System
Imports System.Collections.Generic
Imports System.Globalization
Imports System.IO
Imports System.Text

Public Module DxfExporter

    Public Sub SaveDxf(filePath As String, paths As IEnumerable(Of ExportPath2D))
        Dim sb As New StringBuilder()

        sb.AppendLine("0")
        sb.AppendLine("SECTION")
        sb.AppendLine("2")
        sb.AppendLine("HEADER")
        sb.AppendLine("0")
        sb.AppendLine("ENDSEC")

        sb.AppendLine("0")
        sb.AppendLine("SECTION")
        sb.AppendLine("2")
        sb.AppendLine("ENTITIES")

        For Each path In paths
            For Each seg In path.Segments
                If TypeOf seg Is ExportLine2D Then
                    WriteLineEntity(sb, DirectCast(seg, ExportLine2D))
                ElseIf TypeOf seg Is ExportArc2D Then
                    WriteArcEntity(sb, DirectCast(seg, ExportArc2D))
                ElseIf TypeOf seg Is ExportCubicBezier2D Then
                    WriteSplineEntity(sb, DirectCast(seg, ExportCubicBezier2D))
                End If
            Next
        Next

        sb.AppendLine("0")
        sb.AppendLine("ENDSEC")
        sb.AppendLine("0")
        sb.AppendLine("EOF")

        File.WriteAllText(filePath, sb.ToString(), Encoding.ASCII)
    End Sub

    Private Sub WriteLineEntity(sb As StringBuilder, ln As ExportLine2D)
        Dim p1 = FlipX(ln.P1)
        Dim p2 = FlipX(ln.P2)

        sb.AppendLine("0")
        sb.AppendLine("LINE")
        sb.AppendLine("8")
        sb.AppendLine("0")
        sb.AppendLine("10")
        sb.AppendLine(F(p1.X))
        sb.AppendLine("20")
        sb.AppendLine(F(p1.Y))
        sb.AppendLine("30")
        sb.AppendLine("0")
        sb.AppendLine("11")
        sb.AppendLine(F(p2.X))
        sb.AppendLine("21")
        sb.AppendLine(F(p2.Y))
        sb.AppendLine("31")
        sb.AppendLine("0")
    End Sub

    Private Sub WriteArcEntity(sb As StringBuilder, arc As ExportArc2D)
        Dim c = FlipX(arc.Center)
        Dim s = FlipX(arc.StartPoint)
        Dim e = FlipX(arc.EndPoint)

        Dim startAngle = NormalizeAngleDeg(Math.Atan2(s.Y - c.Y, s.X - c.X) * 180.0 / Math.PI)
        Dim endAngle = NormalizeAngleDeg(Math.Atan2(e.Y - c.Y, e.X - c.X) * 180.0 / Math.PI)

        ' DXF disegna l'arco CCW da group50 a group51 (frame CAD, Y in alto).
        ' Il FlipX inverte il verso rispetto al nostro frame schermo.
        Dim doSwap As Boolean
        If Not Double.IsNaN(arc.SweepDeg) Then
            ' SweepDeg>0 = orario a video = CCW in CAD: nessuno scambio.
            doSwap = (arc.SweepDeg < 0.0)
        Else
            doSwap = (Not arc.Clockwise)
        End If

        If doSwap Then
            Dim tmp = startAngle
            startAngle = endAngle
            endAngle = tmp
        End If

        sb.AppendLine("0")
        sb.AppendLine("ARC")
        sb.AppendLine("8")
        sb.AppendLine("0")
        sb.AppendLine("10")
        sb.AppendLine(F(c.X))
        sb.AppendLine("20")
        sb.AppendLine(F(c.Y))
        sb.AppendLine("30")
        sb.AppendLine("0")
        sb.AppendLine("40")
        sb.AppendLine(F(arc.Radius))
        sb.AppendLine("50")
        sb.AppendLine(F(startAngle))
        sb.AppendLine("51")
        sb.AppendLine(F(endAngle))
    End Sub

    Private Sub WriteSplineEntity(sb As StringBuilder, bz As ExportCubicBezier2D)
        Dim fitPoints As New List(Of Vec2)

        Dim steps As Integer = 16
        For i As Integer = 0 To steps
            Dim t As Double = i / CDbl(steps)
            Dim p As Vec2 = EvaluateCubicBezier(bz.P0, bz.C1, bz.C2, bz.P3, t)
            fitPoints.Add(FlipX(p))
        Next

        sb.AppendLine("0")
        sb.AppendLine("SPLINE")

        sb.AppendLine("8")
        sb.AppendLine("0")

        sb.AppendLine("100")
        sb.AppendLine("AcDbEntity")
        sb.AppendLine("100")
        sb.AppendLine("AcDbSpline")

        sb.AppendLine("70")
        sb.AppendLine("1024")

        sb.AppendLine("71")
        sb.AppendLine("3")

        sb.AppendLine("72")
        sb.AppendLine("0")

        sb.AppendLine("73")
        sb.AppendLine("0")

        sb.AppendLine("74")
        sb.AppendLine(fitPoints.Count.ToString(CultureInfo.InvariantCulture))

        sb.AppendLine("44")
        sb.AppendLine("0")

        sb.AppendLine("210")
        sb.AppendLine("0")
        sb.AppendLine("220")
        sb.AppendLine("0")
        sb.AppendLine("230")
        sb.AppendLine("1")

        For Each p In fitPoints
            sb.AppendLine("11")
            sb.AppendLine(F(p.X))
            sb.AppendLine("21")
            sb.AppendLine(F(p.Y))
            sb.AppendLine("31")
            sb.AppendLine("0")
        Next
    End Sub

    Private Function EvaluateCubicBezier(p0 As Vec2, c1 As Vec2, c2 As Vec2, p3 As Vec2, t As Double) As Vec2
        Dim u As Double = 1.0 - t
        Dim tt As Double = t * t
        Dim uu As Double = u * u
        Dim uuu As Double = uu * u
        Dim ttt As Double = tt * t

        Dim x As Double =
        uuu * p0.X +
        3.0 * uu * t * c1.X +
        3.0 * u * tt * c2.X +
        ttt * p3.X

        Dim y As Double =
        uuu * p0.Y +
        3.0 * uu * t * c1.Y +
        3.0 * u * tt * c2.Y +
        ttt * p3.Y

        Return New Vec2(x, y)
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

    Private Function F(v As Double) As String
        Return v.ToString("0.######", CultureInfo.InvariantCulture)
    End Function

    Private Function FlipX(p As Vec2) As Vec2
        Return New Vec2(p.X, -p.Y)
    End Function

End Module
' ============================================================================
'  DIAGNOSTICA TEMPORANEA BLOCCHI  (v3 - include ellissi/cerchi/archi ellittici/B-spline)
'
'  USO (come prima):
'  1) Incolla questo Module nel progetto (sostituendo l'eventuale versione v1).
'  2) In MainForm.ReadBlockDefaultView_Click, al posto del loop:
'
'         For Each d In defs
'             ExportGeometry.NormalizeBlockInPlace(d)
'         Next
'
'     metti:
'
'         Dim diag As String = BlockDiag.DescribeBlocks(defs, "RAW (pre-normalize)")
'         For Each d In defs
'             ExportGeometry.NormalizeBlockInPlace(d)
'         Next
'         diag &= Environment.NewLine & BlockDiag.DescribeBlocks(defs, "NORMALIZED")
'         Dim diagPath = System.IO.Path.Combine(
'             Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "block_diag.txt")
'         System.IO.File.WriteAllText(diagPath, diag)
'         MessageBox.Show("Diagnostica scritta in: " & diagPath)
'
'  3) Premi "Read Solid Edge Blocks" col documento del blocco-stella aperto e
'     inviami block_diag.txt.
' ============================================================================

Imports System
Imports System.Collections.Generic
Imports System.Text

Public Module BlockDiag

    Public Function DescribeBlocks(blocks As List(Of BlockDefinition), title As String) As String
        Dim sb As New StringBuilder()
        sb.AppendLine("==================== " & title & " ====================")

        If blocks Is Nothing Then
            sb.AppendLine("(lista nulla)")
            Return sb.ToString()
        End If

        sb.AppendLine("Numero blocchi: " & blocks.Count)

        For bi As Integer = 0 To blocks.Count - 1
            Dim d = blocks(bi)
            sb.AppendLine("")
            sb.AppendLine(String.Format("[{0}] Name='{1}'  entita'={2}",
                                        bi, d.Name, If(d.Entities Is Nothing, 0, d.Entities.Count)))
            sb.AppendLine(String.Format("    NativeCenter=({0:0.###},{1:0.###})  NativeRadius={2:0.###}  BaseOrigin=({3:0.###},{4:0.###})",
                                        d.NativeCenter.X, d.NativeCenter.Y, d.NativeRadius,
                                        d.BaseOrigin.X, d.BaseOrigin.Y))

            Dim nLine = 0, nArc = 0, nBez = 0, nEll = 0, nCir = 0, nEArc = 0, nBspl = 0

            If d.Entities IsNot Nothing Then
                For Each e In d.Entities
                    If e Is Nothing OrElse e.Segments Is Nothing Then Continue For
                    For Each seg In e.Segments
                        If TypeOf seg Is ExportLine2D Then
                            nLine += 1

                        ElseIf TypeOf seg Is ExportArc2D Then
                            nArc += 1
                            Dim a = DirectCast(seg, ExportArc2D)
                            sb.AppendLine(String.Format("    ARC      r={0:0.###}  sweep={1}  cen=({2:0.##},{3:0.##})",
                                a.Radius, If(Double.IsNaN(a.SweepDeg), "NaN", a.SweepDeg.ToString("0.##")),
                                a.Center.X, a.Center.Y))

                        ElseIf TypeOf seg Is ExportCubicBezier2D Then
                            nBez += 1

                        ElseIf TypeOf seg Is ExportEllipse2D Then
                            nEll += 1
                            Dim el = DirectCast(seg, ExportEllipse2D)
                            sb.AppendLine(String.Format("    ELLIPSE  cen=({0:0.##},{1:0.##})  rMaj={2:0.###} rMin={3:0.###} rotDeg={4:0.##}",
                                el.Center.X, el.Center.Y, el.RadiusMajor, el.RadiusMinor, el.RotationRad * 180.0 / Math.PI))

                        ElseIf TypeOf seg Is ExportCircle2D Then
                            nCir += 1
                            Dim ci = DirectCast(seg, ExportCircle2D)
                            sb.AppendLine(String.Format("    CIRCLE   cen=({0:0.##},{1:0.##})  r={2:0.###}",
                                ci.Center.X, ci.Center.Y, ci.Radius))

                        ElseIf TypeOf seg Is ExportEllipticalArc2D Then
                            nEArc += 1
                            Dim ea = DirectCast(seg, ExportEllipticalArc2D)
                            Dim rMaj = Math.Sqrt(ea.MajorAxis.X * ea.MajorAxis.X + ea.MajorAxis.Y * ea.MajorAxis.Y)
                            Dim rMin = Math.Sqrt(ea.MinorAxis.X * ea.MinorAxis.X + ea.MinorAxis.Y * ea.MinorAxis.Y)
                            sb.AppendLine(String.Format("    EARC     cen=({0:0.##},{1:0.##})  rMaj={2:0.###} rMin={3:0.###}",
                                ea.Center.X, ea.Center.Y, rMaj, rMin))
                            sb.AppendLine(String.Format("             major=({0:0.###},{1:0.###}) minor=({2:0.###},{3:0.###})  orient={4}",
                                ea.MajorAxis.X, ea.MajorAxis.Y, ea.MinorAxis.X, ea.MinorAxis.Y, ea.Orientation))
                            sb.AppendLine(String.Format("             start={0:0.####} rad ({1:0.##} deg)   sweep={2:0.####} rad ({3:0.##} deg)",
                                ea.StartAngle, ea.StartAngle * 180.0 / Math.PI,
                                ea.SweepAngle, ea.SweepAngle * 180.0 / Math.PI))

                        ElseIf TypeOf seg Is ExportBSpline2D Then
                            nBspl += 1
                            Dim bs = DirectCast(seg, ExportBSpline2D)
                            Dim cnt = If(bs.Nodes Is Nothing, 0, bs.Nodes.Count)
                            sb.AppendLine(String.Format("    BSPLINE  nodi={0}  closed(tang)={1}", cnt, bs.ClosedCurve))
                            If bs.Nodes IsNot Nothing Then
                                For k As Integer = 0 To bs.Nodes.Count - 1
                                    sb.AppendLine(String.Format("             node[{0}]=({1:0.####},{2:0.####})", k, bs.Nodes(k).X, bs.Nodes(k).Y))
                                Next
                            End If
                        End If
                    Next
                Next
            End If

            sb.AppendLine(String.Format("    => linee={0} archi={1} bezier={2} ellissi={3} cerchi={4} archiEllittici={5} bspline={6}",
                                        nLine, nArc, nBez, nEll, nCir, nEArc, nBspl))
        Next

        Return sb.ToString()
    End Function

End Module
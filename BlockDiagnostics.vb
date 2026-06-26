' ============================================================================
'  DIAGNOSTICA TEMPORANEA BLOCCHI  (rimuovere dopo aver trovato il problema)
'
'  USO:
'  1) Incolla l'intero Module qui sotto in un file nuovo del progetto
'     (es. BlockDiagnostics.vb), oppure incolla le due funzioni dentro
'     un Module gia' esistente (es. ExportGeometry).
'
'  2) In MainForm, dentro ReadBlockDefaultView_Click, sostituisci il blocco:
'
'         For Each d In defs
'             ExportGeometry.NormalizeBlockInPlace(d)
'         Next
'
'     con QUESTO (dumpa grezzo + normalizzato su Desktop\block_diag.txt):
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
'  3) Premi "Read Solid Edge Blocks" con APERTO il documento che contiene
'     il blocco problematico, poi inviami il contenuto di block_diag.txt.
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
            sb.AppendLine(String.Format("    NativeCenter=({0:0.######},{1:0.######})  NativeRadius={2:0.######}  BaseOrigin=({3:0.######},{4:0.######})",
                                        d.NativeCenter.X, d.NativeCenter.Y, d.NativeRadius,
                                        d.BaseOrigin.X, d.BaseOrigin.Y))

            Dim nLine As Integer = 0
            Dim nArc As Integer = 0
            Dim minX As Double = Double.MaxValue, minY As Double = Double.MaxValue
            Dim maxX As Double = Double.MinValue, maxY As Double = Double.MinValue
            Dim worstR As Double = 0.0
            Dim nonFinite As Integer = 0

            If d.Entities IsNot Nothing Then
                For Each e In d.Entities
                    If e Is Nothing OrElse e.Segments Is Nothing Then Continue For
                    For Each seg In e.Segments
                        If TypeOf seg Is ExportLine2D Then
                            nLine += 1
                            Dim ln = DirectCast(seg, ExportLine2D)
                            Acc(ln.P1, minX, minY, maxX, maxY, nonFinite)
                            Acc(ln.P2, minX, minY, maxX, maxY, nonFinite)

                        ElseIf TypeOf seg Is ExportArc2D Then
                            nArc += 1
                            Dim a = DirectCast(seg, ExportArc2D)
                            If Not (Double.IsNaN(a.Radius) OrElse Double.IsInfinity(a.Radius)) Then
                                worstR = Math.Max(worstR, Math.Abs(a.Radius))
                            Else
                                nonFinite += 1
                            End If
                            Acc(a.Center, minX, minY, maxX, maxY, nonFinite)
                            Acc(a.StartPoint, minX, minY, maxX, maxY, nonFinite)
                            Acc(a.EndPoint, minX, minY, maxX, maxY, nonFinite)
                            sb.AppendLine(String.Format("    ARC  r={0}  sweep={1}  cen=({2:0.###},{3:0.###})  s=({4:0.###},{5:0.###})  e=({6:0.###},{7:0.###})  cw={8}",
                                FmtNum(a.Radius),
                                If(Double.IsNaN(a.SweepDeg), "NaN", a.SweepDeg.ToString("0.###")),
                                a.Center.X, a.Center.Y,
                                a.StartPoint.X, a.StartPoint.Y,
                                a.EndPoint.X, a.EndPoint.Y,
                                a.Clockwise))

                        ElseIf TypeOf seg Is ExportCubicBezier2D Then
                            sb.AppendLine("    BEZIER (inatteso in un blocco)")
                        End If
                    Next
                Next
            End If

            Dim extent As Double = 0.0
            If maxX >= minX AndAlso maxY >= minY Then
                extent = Math.Sqrt((maxX - minX) * (maxX - minX) + (maxY - minY) * (maxY - minY))
            End If

            sb.AppendLine(String.Format("    => linee={0}  archi={1}  bbox=({2:0.###},{3:0.###})-({4:0.###},{5:0.###})  diag={6:0.###}  |r|max={7}  nonFinite={8}",
                                        nLine, nArc, minX, minY, maxX, maxY, extent, FmtNum(worstR), nonFinite))

            ' Segnale d'allarme principale: arco col raggio molto piu' grande
            ' dell'ingombro del blocco => dopo lo scaling diventa enorme.
            If extent > 0.0000001 AndAlso worstR / extent > 50.0 Then
                sb.AppendLine(String.Format("    !!! ATTENZIONE: |r|max/diag = {0:0.#} (arco quasi rettilineo: candidato OOM)", worstR / extent))
            End If
        Next

        Return sb.ToString()
    End Function

    Private Function FmtNum(v As Double) As String
        If Double.IsNaN(v) Then Return "NaN"
        If Double.IsInfinity(v) Then Return "Inf"
        Return v.ToString("0.######")
    End Function

    Private Sub Acc(p As Vec2,
                    ByRef minX As Double, ByRef minY As Double,
                    ByRef maxX As Double, ByRef maxY As Double,
                    ByRef nonFinite As Integer)
        If Double.IsNaN(p.X) OrElse Double.IsInfinity(p.X) OrElse
           Double.IsNaN(p.Y) OrElse Double.IsInfinity(p.Y) Then
            nonFinite += 1
            Return
        End If
        If p.X < minX Then minX = p.X
        If p.Y < minY Then minY = p.Y
        If p.X > maxX Then maxX = p.X
        If p.Y > maxY Then maxY = p.Y
    End Sub

End Module

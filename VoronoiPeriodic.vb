' ============================================================
'  VoronoiPeriodic.vb - Costruzione Voronoi con periodicita' X/Y
'  (topologia cilindrica o toroidale) tramite semi fantasma.
'
'  Modulo autonomo: non tocca VoronoiEngine. Con periodicX = periodicY =
'  False e fullCells = True funziona da costruttore "celle intere" generico
'  (usato anche per i domini sketch: nessun taglio su profilo e fori).
' Ogni seme viene clippato
'  anche contro le repliche traslate di +/- periodo degli altri semi
'  (e contro le proprie repliche, che limitano la cella a mezzo periodo).
'
'  fullCells = True: la cella NON viene tagliata sul bordo periodico e
'  sborda dal rettangolo; avvolgendo lo sketch sul cilindro la parte
'  eccedente atterra esattamente sul lato opposto (continuita' perfetta,
'  stili e simboli inclusi, senza spezzare i path).
'  fullCells = False: celle tagliate al rettangolo (le due meta' sul
'  bordo combaciano una volta avvolte, ma i raccordi curvi trattano il
'  taglio come uno spigolo).
' ============================================================

Imports System
Imports System.Collections.Generic
Imports System.Drawing

Public Module VoronoiPeriodic

    Public Function BuildCellsPeriodic(seeds As List(Of Vec2),
                                       bounds As RectangleF,
                                       periodicX As Boolean,
                                       periodicY As Boolean,
                                       fullCells As Boolean) As List(Of VoronoiCell)

        Dim cells As New List(Of VoronoiCell)
        If seeds Is Nothing OrElse seeds.Count = 0 Then Return cells

        Dim W As Double = bounds.Width
        Dim H As Double = bounds.Height

        Dim dxs As Double() = If(periodicX, New Double() {-W, 0.0, W}, New Double() {0.0})
        Dim dys As Double() = If(periodicY, New Double() {-H, 0.0, H}, New Double() {0.0})

        ' Poligono di partenza: il rettangolo, gonfiato quando le celle devono
        ' restare intere. Su un asse periodico il bisettore col proprio fantasma
        ' limita comunque la cella a mezzo periodo; su un asse non periodico le
        ' celle del bordo sono illimitate e vengono chiuse su un margine
        ' adattivo (~1.5 diametri medi di cella oltre il dominio).
        Dim adaptiveMargin As Double = 0.0
        If fullCells Then
            adaptiveMargin = 1.5 * Math.Sqrt(Math.Max(1.0, W * H) / Math.Max(1, seeds.Count))
        End If

        Dim mx As Double = 0.0
        Dim my As Double = 0.0
        If fullCells Then
            mx = If(periodicX, W * 0.55, adaptiveMargin)
            my = If(periodicY, H * 0.55, adaptiveMargin)
        End If

        Dim startPoly As New List(Of Vec2) From {
            New Vec2(bounds.Left - mx, bounds.Top - my),
            New Vec2(bounds.Right + mx, bounds.Top - my),
            New Vec2(bounds.Right + mx, bounds.Bottom + my),
            New Vec2(bounds.Left - mx, bounds.Bottom + my)
        }

        ' Un bisettore puo' tagliare il poligono di partenza solo se il seme
        ' replicato dista meno del doppio della sua diagonale.
        Dim diag As Double = Math.Sqrt((W + 2 * mx) * (W + 2 * mx) + (H + 2 * my) * (H + 2 * my))
        Dim pruneDist As Double = 2.0 * diag

        For i As Integer = 0 To seeds.Count - 1
            Dim poly As New List(Of Vec2)(startPoly)

            For j As Integer = 0 To seeds.Count - 1
                For Each dx As Double In dxs
                    For Each dy As Double In dys
                        If j = i AndAlso dx = 0.0 AndAlso dy = 0.0 Then Continue For

                        Dim other As New Vec2(seeds(j).X + dx, seeds(j).Y + dy)
                        If Geo2D.Distance(seeds(i), other) > pruneDist Then Continue For

                        poly = ClipWithBisector(poly, seeds(i), other)
                        If poly.Count = 0 Then Exit For
                    Next
                    If poly.Count = 0 Then Exit For
                Next
                If poly.Count = 0 Then Exit For
            Next

            poly = CleanPolygon(poly)

            If poly.Count >= 3 Then
                cells.Add(New VoronoiCell With {
                    .Seed = seeds(i),
                    .Vertices = poly
                })
            End If
        Next

        Return cells
    End Function

    ' Riporta i semi dentro il rettangolo sugli assi periodici (dopo il relax:
    ' il centroide di una cella che sborda puo' cadere fuori dal dominio).
    Public Sub WrapSeedsIntoBounds(seeds As List(Of Vec2),
                                   bounds As RectangleF,
                                   periodicX As Boolean,
                                   periodicY As Boolean)
        If seeds Is Nothing Then Return

        For i As Integer = 0 To seeds.Count - 1
            Dim x As Double = seeds(i).X
            Dim y As Double = seeds(i).Y

            If periodicX AndAlso bounds.Width > 0.0001F Then
                x = WrapCoord(x, bounds.Left, bounds.Width)
            End If

            If periodicY AndAlso bounds.Height > 0.0001F Then
                y = WrapCoord(y, bounds.Top, bounds.Height)
            End If

            seeds(i) = New Vec2(x, y)
        Next
    End Sub

    Private Function WrapCoord(v As Double, origin As Double, period As Double) As Double
        Dim t As Double = (v - origin) Mod period
        If t < 0.0 Then t += period
        Return origin + t
    End Function

    ' --- Clip di Sutherland-Hodgman contro il semipiano del bisettore ---

    Private Function ClipWithBisector(poly As List(Of Vec2), a As Vec2, b As Vec2) As List(Of Vec2)
        Dim result As New List(Of Vec2)
        If poly Is Nothing OrElse poly.Count = 0 Then Return result

        Dim mid As New Vec2((a.X + b.X) * 0.5, (a.Y + b.Y) * 0.5)
        Dim n As New Vec2(b.X - a.X, b.Y - a.Y)

        Dim prev As Vec2 = poly(poly.Count - 1)
        Dim prevInside As Boolean = IsInside(prev, mid, n)

        For Each curr As Vec2 In poly
            Dim currInside As Boolean = IsInside(curr, mid, n)

            If currInside Then
                If Not prevInside Then
                    result.Add(LineIntersectionWithBisector(prev, curr, mid, n))
                End If
                result.Add(curr)
            ElseIf prevInside Then
                result.Add(LineIntersectionWithBisector(prev, curr, mid, n))
            End If

            prev = curr
            prevInside = currInside
        Next

        Return result
    End Function

    Private Function IsInside(p As Vec2, mid As Vec2, n As Vec2) As Boolean
        Dim d = (p.X - mid.X) * n.X + (p.Y - mid.Y) * n.Y
        Return d <= 0.000001
    End Function

    Private Function LineIntersectionWithBisector(p1 As Vec2, p2 As Vec2, mid As Vec2, n As Vec2) As Vec2
        Dim d As New Vec2(p2.X - p1.X, p2.Y - p1.Y)
        Dim denom = d.X * n.X + d.Y * n.Y

        If Math.Abs(denom) < 0.0000001 Then
            Return p1
        End If

        Dim t = -(((p1.X - mid.X) * n.X) + ((p1.Y - mid.Y) * n.Y)) / denom
        Return New Vec2(p1.X + d.X * t, p1.Y + d.Y * t)
    End Function

    Private Function CleanPolygon(poly As List(Of Vec2)) As List(Of Vec2)
        Dim cleaned As New List(Of Vec2)
        If poly Is Nothing OrElse poly.Count = 0 Then Return cleaned

        Const eps As Double = 0.0001

        For i As Integer = 0 To poly.Count - 1
            Dim p = poly(i)

            If cleaned.Count = 0 Then
                cleaned.Add(p)
            Else
                Dim q = cleaned(cleaned.Count - 1)
                If Geo2D.Distance(p, q) > eps Then cleaned.Add(p)
            End If
        Next

        If cleaned.Count > 1 AndAlso Geo2D.Distance(cleaned(0), cleaned(cleaned.Count - 1)) <= eps Then
            cleaned.RemoveAt(cleaned.Count - 1)
        End If

        Return cleaned
    End Function

End Module
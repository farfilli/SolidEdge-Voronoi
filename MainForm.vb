Imports System
Imports System.Collections.Generic
Imports System.Drawing
Imports System.IO
Imports System.Linq
Imports System.Text
Imports System.Windows.Forms

Public Class MainForm
    Inherits Form

    Private ReadOnly canvas As New VoronoiCanvas()

    Private ReadOnly sidebar As New Panel()
    Private ReadOnly sideLayout As New TableLayoutPanel()

    Private ReadOnly numCells As New NumericUpDown()
    Private ReadOnly numSeed As New NumericUpDown()
    Private ReadOnly numRelax As New NumericUpDown()

    Private ReadOnly cmbStyle As New ComboBox()
    Private ReadOnly cmbInnerCorner As New ComboBox()
    Private ReadOnly cmbSymbolCorner As New ComboBox()

    Private ReadOnly numCellScale As New NumericUpDown()

    Private ReadOnly numInnerOffset As New NumericUpDown()
    Private ReadOnly numCornerTrim As New NumericUpDown()
    Private ReadOnly numBezierBulge As New NumericUpDown()
    Private ReadOnly numCurveWidth As New NumericUpDown()
    Private ReadOnly numSymbolCornerTrim As New NumericUpDown()
    Private ReadOnly numSymbolBezierBulge As New NumericUpDown()

    Private ReadOnly chkFill As New CheckBox()
    Private ReadOnly chkOuter As New CheckBox()
    Private ReadOnly chkSeeds As New CheckBox()
    Private ReadOnly chkInner As New CheckBox()
    Private ReadOnly chkRandomRotation As New CheckBox()

    Private ReadOnly btnGenerate As New Button()
    Private ReadOnly btnShuffle As New Button()

    Private ReadOnly btnExportSvg As New Button()
    Private ReadOnly btnExportDxf As New Button()
    Private ReadOnly btnToSolidEdge As New Button()

    Private ReadOnly btnReadSketchProfile As New Button()

    Private ReadOnly domain As RectangleF = New RectangleF(0, 0, 1000, 700)
    Private currentWorldDomain As RectangleF
    Private lockSketchViewDomain As Boolean = False

    Private currentSeeds As New List(Of Vec2)

    Private currentSketchBoundaries As New List(Of List(Of Vec2))
    Private currentSketchDomains As New List(Of SketchDomainRegion)
    Private useSketchDomains As Boolean = False



    Private Class SketchDomainRegion
        Public Property Outer As List(Of Vec2) = New List(Of Vec2)
        Public Property Holes As List(Of List(Of Vec2)) = New List(Of List(Of Vec2))
        Public Property Bounds As RectangleF = RectangleF.Empty
    End Class

    Public Sub New()
        Text = "Solid Edge Voronoi Generator - v1.0"
        StartPosition = FormStartPosition.CenterScreen
        Width = 1550
        Height = 920
        MinimumSize = New Size(1200, 760)

        Icon = My.Resources.SE_Voronoi

        ConfigureControls()
        BuildSidebar()

        canvas.Dock = DockStyle.Fill
        currentWorldDomain = domain
        canvas.Domain = currentWorldDomain
        canvas.BackColor = Color.FromArgb(8, 6, 53)

        Controls.Add(canvas)
        Controls.Add(sidebar)

        AddHandler btnGenerate.Click, AddressOf GenerateRandomDiagram
        AddHandler btnShuffle.Click, AddressOf ShuffleSeed
        AddHandler btnReadSketchProfile.Click, AddressOf ReadSketchProfile_Click

        AddHandler btnExportSvg.Click, AddressOf ExportSvg_Click
        AddHandler btnExportDxf.Click, AddressOf ExportDxf_Click
        AddHandler btnToSolidEdge.Click, AddressOf ExportToSolidEdge_Click

        AddHandler chkFill.CheckedChanged, AddressOf RefreshCanvasOptions
        AddHandler chkOuter.CheckedChanged, AddressOf RefreshCanvasOptions
        AddHandler chkSeeds.CheckedChanged, AddressOf RefreshCanvasOptions
        AddHandler chkInner.CheckedChanged, AddressOf RefreshCanvasOptions
        AddHandler chkRandomRotation.CheckedChanged, AddressOf RefreshCanvasOptions

        AddHandler cmbStyle.SelectedIndexChanged, AddressOf RefreshCanvasOptions
        AddHandler cmbInnerCorner.SelectedIndexChanged, AddressOf RefreshCanvasOptions
        AddHandler cmbSymbolCorner.SelectedIndexChanged, AddressOf RefreshCanvasOptions

        AddHandler numCellScale.ValueChanged, AddressOf RefreshCanvasOptions
        AddHandler numInnerOffset.ValueChanged, AddressOf RefreshCanvasOptions
        AddHandler numCornerTrim.ValueChanged, AddressOf RefreshCanvasOptions
        AddHandler numBezierBulge.ValueChanged, AddressOf RefreshCanvasOptions
        AddHandler numCurveWidth.ValueChanged, AddressOf RefreshCanvasOptions
        AddHandler numSymbolCornerTrim.ValueChanged, AddressOf RefreshCanvasOptions
        AddHandler numSymbolBezierBulge.ValueChanged, AddressOf RefreshCanvasOptions

        AddHandler canvas.SeedsEdited, AddressOf Canvas_SeedsEdited

        GenerateRandomDiagram(Nothing, EventArgs.Empty)
    End Sub

    Private Sub BuildSidebar()
        sidebar.Dock = DockStyle.Left
        sidebar.Width = 280
        sidebar.BackColor = Color.FromArgb(245, 247, 250)
        sidebar.Padding = New Padding(6)

        sideLayout.Dock = DockStyle.Fill
        sideLayout.AutoScroll = True
        sideLayout.ColumnCount = 1
        sideLayout.RowCount = 0
        sideLayout.BackColor = sidebar.BackColor
        sideLayout.AutoSize = True
        sideLayout.GrowStyle = TableLayoutPanelGrowStyle.AddRows
        sideLayout.Padding = New Padding(0)
        sideLayout.Margin = New Padding(0)

        sidebar.Controls.Add(sideLayout)

        AddRowTitle("Cell Style")
        AddRowControl(cmbStyle)

        AddRowTitle("Corner Mode (Inner)")
        AddRowControl(cmbInnerCorner)

        AddRowTitle("Corner Mode (Symbols)")
        AddRowControl(cmbSymbolCorner)

        AddRowTitle("Cell Count")
        AddRowControl(numCells)

        AddRowTitle("Random Seed")
        AddRowControl(numSeed)

        AddRowTitle("Relax")
        AddRowControl(numRelax)

        AddRowTitle("Cell Scale")
        AddRowControl(numCellScale)

        AddRowTitle("Inner Offset")
        AddRowControl(numInnerOffset)

        AddRowTitle("Inner Trim")
        AddRowControl(numCornerTrim)

        AddRowTitle("Inner Bezier Bulge")
        AddRowControl(numBezierBulge)

        AddRowTitle("Symbol Trim")
        AddRowControl(numSymbolCornerTrim)

        AddRowTitle("Symbol Bezier Bulge")
        AddRowControl(numSymbolBezierBulge)

        AddRowTitle("Curve Width")
        AddRowControl(numCurveWidth)

        AddRowControl(chkFill)
        AddRowControl(chkOuter)
        AddRowControl(chkSeeds)
        AddRowControl(chkInner)
        AddRowControl(chkRandomRotation)

        AddRowControl(btnGenerate, 34)
        AddRowControl(btnShuffle, 34)

        AddRowTitle("Sketch")
        AddRowControl(btnReadSketchProfile, 30)

        AddRowTitle("Export")
        AddRowControl(btnExportSvg, 30)
        AddRowControl(btnExportDxf, 30)
        AddRowControl(btnToSolidEdge, 30)
    End Sub

    Private Sub AddRowTitle(text As String)
        sideLayout.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        Dim lbl As New Label With {
            .Text = text,
            .ForeColor = Color.FromArgb(30, 40, 55),
            .AutoSize = False,
            .Height = 18,
            .Width = 235,
            .Margin = New Padding(3, 4, 3, 1),
            .TextAlign = ContentAlignment.BottomLeft
        }
        sideLayout.Controls.Add(lbl)
    End Sub

    Private Sub AddRowControl(ctrl As Control, Optional forcedHeight As Integer = 28)
        sideLayout.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        ctrl.Width = 235
        ctrl.Height = forcedHeight
        ctrl.Margin = New Padding(3, 0, 3, 3)
        sideLayout.Controls.Add(ctrl)
    End Sub

    Private Sub ConfigureControls()
        cmbStyle.DropDownStyle = ComboBoxStyle.DropDownList
        cmbStyle.Items.AddRange([Enum].GetNames(GetType(CellRenderStyle)))
        cmbStyle.SelectedItem = CellRenderStyle.Curved.ToString()

        cmbInnerCorner.DropDownStyle = ComboBoxStyle.DropDownList
        cmbInnerCorner.Items.AddRange([Enum].GetNames(GetType(InnerCornerStyle)))
        cmbInnerCorner.SelectedItem = InnerCornerStyle.Bezier.ToString()

        cmbSymbolCorner.DropDownStyle = ComboBoxStyle.DropDownList
        cmbSymbolCorner.Items.AddRange([Enum].GetNames(GetType(SymbolCornerStyle)))
        cmbSymbolCorner.SelectedItem = SymbolCornerStyle.Sharp.ToString()

        numCells.Minimum = 5
        numCells.Maximum = 500
        numCells.Value = 80

        numSeed.Minimum = 0
        numSeed.Maximum = Integer.MaxValue
        numSeed.Value = 12345

        numRelax.Minimum = 0
        numRelax.Maximum = 10
        numRelax.Value = 1

        numCellScale.Minimum = 0.05D
        numCellScale.Maximum = 1.5D
        numCellScale.DecimalPlaces = 2
        numCellScale.Increment = 0.02D
        numCellScale.Value = 0.82D

        numInnerOffset.Minimum = 0
        numInnerOffset.Maximum = 200
        numInnerOffset.DecimalPlaces = 1
        numInnerOffset.Increment = 1D
        numInnerOffset.Value = 0D

        numCornerTrim.Minimum = 0D
        numCornerTrim.Maximum = 3D
        numCornerTrim.DecimalPlaces = 2
        numCornerTrim.Increment = 0.05D
        numCornerTrim.Value = 0.22D

        numBezierBulge.Minimum = 0D
        numBezierBulge.Maximum = 3D
        numBezierBulge.DecimalPlaces = 2
        numBezierBulge.Increment = 0.05D
        numBezierBulge.Value = 0.55D

        numSymbolCornerTrim.Minimum = 0D
        numSymbolCornerTrim.Maximum = 3D
        numSymbolCornerTrim.DecimalPlaces = 2
        numSymbolCornerTrim.Increment = 0.05D
        numSymbolCornerTrim.Value = 0.18D

        numSymbolBezierBulge.Minimum = 0D
        numSymbolBezierBulge.Maximum = 3D
        numSymbolBezierBulge.DecimalPlaces = 2
        numSymbolBezierBulge.Increment = 0.05D
        numSymbolBezierBulge.Value = 0.55D

        numCurveWidth.Minimum = 1D
        numCurveWidth.Maximum = 12D
        numCurveWidth.DecimalPlaces = 1
        numCurveWidth.Increment = 0.2D
        numCurveWidth.Value = 1.8D

        chkFill.Text = "Fill cells"
        chkFill.Checked = True
        chkFill.ForeColor = Color.FromArgb(30, 40, 55)

        chkOuter.Text = "Show outer edges"
        chkOuter.Checked = True
        chkOuter.ForeColor = Color.FromArgb(30, 40, 55)

        chkSeeds.Text = "Show seeds"
        chkSeeds.Checked = True
        chkSeeds.ForeColor = Color.FromArgb(30, 40, 55)

        chkInner.Text = "Show inner curve"
        chkInner.Checked = True
        chkInner.ForeColor = Color.FromArgb(30, 40, 55)

        chkRandomRotation.Text = "Random symbol rotation"
        chkRandomRotation.Checked = True
        chkRandomRotation.ForeColor = Color.FromArgb(30, 40, 55)

        chkFill.Margin = New Padding(3, 1, 3, 1)
        chkOuter.Margin = New Padding(3, 1, 3, 1)
        chkSeeds.Margin = New Padding(3, 1, 3, 1)
        chkInner.Margin = New Padding(3, 1, 3, 1)
        chkRandomRotation.Margin = New Padding(3, 1, 3, 4)

        btnGenerate.Text = "Generate"
        btnGenerate.UseVisualStyleBackColor = False
        btnGenerate.BackColor = Color.FromArgb(0, 188, 212)
        btnGenerate.ForeColor = Color.FromArgb(8, 6, 53)
        btnGenerate.FlatStyle = FlatStyle.Flat

        btnShuffle.Text = "New Seed"
        btnShuffle.UseVisualStyleBackColor = False
        btnShuffle.BackColor = Color.White
        btnShuffle.ForeColor = Color.FromArgb(30, 40, 55)
        btnShuffle.FlatStyle = FlatStyle.Flat

        btnReadSketchProfile.Text = "Leggi profilo sketch"
        btnReadSketchProfile.UseVisualStyleBackColor = False
        btnReadSketchProfile.BackColor = Color.White
        btnReadSketchProfile.ForeColor = Color.FromArgb(30, 40, 55)
        btnReadSketchProfile.FlatStyle = FlatStyle.Flat

        btnExportSvg.Text = "Export SVG"
        btnExportSvg.UseVisualStyleBackColor = False
        btnExportSvg.BackColor = Color.White
        btnExportSvg.ForeColor = Color.FromArgb(30, 40, 55)
        btnExportSvg.FlatStyle = FlatStyle.Flat

        btnExportDxf.Text = "Export DXF"
        btnExportDxf.UseVisualStyleBackColor = False
        btnExportDxf.BackColor = Color.White
        btnExportDxf.ForeColor = Color.FromArgb(30, 40, 55)
        btnExportDxf.FlatStyle = FlatStyle.Flat

        btnToSolidEdge.Text = "To Solid Edge"
        btnToSolidEdge.UseVisualStyleBackColor = False
        btnToSolidEdge.BackColor = Color.FromArgb(0, 188, 212)
        btnToSolidEdge.ForeColor = Color.FromArgb(8, 6, 53)
        btnToSolidEdge.FlatStyle = FlatStyle.Flat
    End Sub

    Private Sub ShuffleSeed(sender As Object, e As EventArgs)
        If numSeed.Value < numSeed.Maximum Then
            numSeed.Value += 1
        Else
            numSeed.Value = 0
        End If

        GenerateRandomDiagram(sender, e)
    End Sub

    Private Sub GenerateRandomDiagram(sender As Object, e As EventArgs)
        If useSketchDomains AndAlso currentSketchDomains IsNot Nothing AndAlso currentSketchDomains.Count > 0 Then
            GenerateDiagramFromSketchDomains()
            Return
        End If

        lockSketchViewDomain = False
        currentWorldDomain = domain
        canvas.Domain = currentWorldDomain

        currentSeeds = VoronoiEngine.CreateSeeds(CInt(numCells.Value), domain, CInt(numSeed.Value))

        For i As Integer = 1 To CInt(numRelax.Value)
            Dim tmpCells = VoronoiEngine.BuildCells(currentSeeds, domain)
            currentSeeds = VoronoiEngine.RelaxSeeds(tmpCells)
        Next

        BuildFromCurrentSeeds()
    End Sub

    Private Sub GenerateDiagramFromSketchDomains()
        Dim allCells As New List(Of VoronoiCell)
        Dim allSeeds As New List(Of Vec2)

        If currentSketchDomains Is Nothing OrElse currentSketchDomains.Count = 0 Then Return

        Dim totalOuterArea As Double = 0.0
        For Each d In currentSketchDomains
            totalOuterArea += Math.Abs(Geo2D.SignedArea(d.Outer))
        Next

        If totalOuterArea <= 0.0001 Then Return

        Dim requestedCount As Integer = CInt(numCells.Value)
        Dim seedBase As Integer = CInt(numSeed.Value)

        For i As Integer = 0 To currentSketchDomains.Count - 1
            Dim d = currentSketchDomains(i)
            Dim area As Double = Math.Abs(Geo2D.SignedArea(d.Outer))
            Dim quota As Integer = CInt(Math.Round(requestedCount * (area / totalOuterArea)))

            If i = currentSketchDomains.Count - 1 Then
                quota = requestedCount - allSeeds.Count
            End If

            quota = Math.Max(0, quota)
            If quota = 0 Then Continue For

            Dim seeds = VoronoiEngine.CreateSeeds(quota, d.Bounds, d.Outer, d.Holes, seedBase + i * 997)

            For r As Integer = 1 To CInt(numRelax.Value)
                Dim tmpCells = VoronoiEngine.BuildCells(seeds, d.Outer, d.Holes)
                seeds = VoronoiEngine.RelaxSeeds(tmpCells)
                seeds = FilterSeedsInsideDomain(seeds, d)
                If seeds.Count = 0 Then Exit For
            Next

            If seeds.Count = 0 Then Continue For

            Dim cells = VoronoiEngine.BuildCells(seeds, d.Outer, d.Holes)

            allSeeds.AddRange(seeds)
            allCells.AddRange(cells)
        Next

        currentSeeds = allSeeds
        canvas.Domain = currentWorldDomain
        canvas.Cells = allCells
        canvas.EditableSeeds = New List(Of Vec2)(allSeeds)

        ApplyOptions()
        canvas.Invalidate()
    End Sub

    Private Sub Canvas_SeedsEdited(sender As Object, e As EventArgs)
        currentSeeds = New List(Of Vec2)(canvas.EditableSeeds)
        BuildFromCurrentSeeds()
    End Sub

    Private Sub BuildFromCurrentSeeds()
        If useSketchDomains AndAlso lockSketchViewDomain Then
            canvas.Domain = currentWorldDomain
        Else
            canvas.Domain = domain
        End If

        If useSketchDomains AndAlso currentSketchDomains IsNot Nothing AndAlso currentSketchDomains.Count > 0 Then
            Dim allCells As New List(Of VoronoiCell)
            Dim allSeeds As New List(Of Vec2)

            For Each d In currentSketchDomains
                Dim seedsInDomain = FilterSeedsInsideDomain(currentSeeds, d)
                If seedsInDomain.Count = 0 Then Continue For

                Dim cells = VoronoiEngine.BuildCells(seedsInDomain, d.Outer, d.Holes)
                allSeeds.AddRange(seedsInDomain)
                allCells.AddRange(cells)
            Next

            currentSeeds = allSeeds
            canvas.Cells = allCells
            canvas.EditableSeeds = New List(Of Vec2)(allSeeds)

        Else
            Dim cells = VoronoiEngine.BuildCells(currentSeeds, domain)
            canvas.Cells = cells
            canvas.EditableSeeds = New List(Of Vec2)(currentSeeds)
        End If

        ApplyOptions()
        canvas.Invalidate()
    End Sub

    Private Function FilterSeedsInsideDomain(seeds As IEnumerable(Of Vec2), d As SketchDomainRegion) As List(Of Vec2)
        Dim result As New List(Of Vec2)
        If seeds Is Nothing OrElse d Is Nothing Then Return result
        If d.Outer Is Nothing OrElse d.Outer.Count < 3 Then Return result

        For Each p In seeds
            If Geo2D.PointInPolygonWithHoles(p, d.Outer, d.Holes) Then
                result.Add(p)
            End If
        Next

        Return result
    End Function

    Private Sub RefreshCanvasOptions(sender As Object, e As EventArgs)
        ApplyOptions()
        canvas.Invalidate()
    End Sub

    Private Sub ApplyOptions()
        canvas.FillCells = chkFill.Checked
        canvas.ShowOuterEdges = chkOuter.Checked
        canvas.ShowSeeds = chkSeeds.Checked
        canvas.ShowInnerCurve = chkInner.Checked
        canvas.RandomRotation = chkRandomRotation.Checked

        canvas.RenderStyle = CType([Enum].Parse(GetType(CellRenderStyle), cmbStyle.SelectedItem.ToString()), CellRenderStyle)
        canvas.InnerCornerMode = CType([Enum].Parse(GetType(InnerCornerStyle), cmbInnerCorner.SelectedItem.ToString()), InnerCornerStyle)
        canvas.SymbolCornerMode = CType([Enum].Parse(GetType(SymbolCornerStyle), cmbSymbolCorner.SelectedItem.ToString()), SymbolCornerStyle)

        canvas.CellScale = CSng(numCellScale.Value)
        canvas.InnerOffset = CSng(numInnerOffset.Value)
        canvas.CornerTrim = CSng(numCornerTrim.Value)
        canvas.BezierBulge = CSng(numBezierBulge.Value)
        canvas.SymbolCornerTrim = CSng(numSymbolCornerTrim.Value)
        canvas.SymbolBezierBulge = CSng(numSymbolBezierBulge.Value)
        canvas.InnerCurveWidth = CSng(numCurveWidth.Value)

        Dim isCurved As Boolean = (canvas.RenderStyle = CellRenderStyle.Curved)
        Dim isSymbol As Boolean = canvas.RenderStyle <> CellRenderStyle.Curved AndAlso canvas.RenderStyle <> CellRenderStyle.Straight
        Dim isSymbolBezier As Boolean = (canvas.SymbolCornerMode = SymbolCornerStyle.Bezier)

        cmbInnerCorner.Enabled = isCurved
        numInnerOffset.Enabled = isCurved
        numCornerTrim.Enabled = isCurved
        numBezierBulge.Enabled = isCurved AndAlso canvas.InnerCornerMode = InnerCornerStyle.Bezier
        chkInner.Enabled = isCurved

        cmbSymbolCorner.Enabled = isSymbol
        numCellScale.Enabled = isSymbol
        chkRandomRotation.Enabled = isSymbol
        numSymbolCornerTrim.Enabled = isSymbol AndAlso canvas.SymbolCornerMode <> SymbolCornerStyle.Sharp
        numSymbolBezierBulge.Enabled = isSymbol AndAlso isSymbolBezier
    End Sub

    Private Sub ReadSketchProfile_Click(sender As Object, e As EventArgs)
        Try
            Dim loops As List(Of SolidEdgeExporter.SketchBoundaryLoop) = Nothing
            Dim bounds As RectangleF = RectangleF.Empty
            Dim err As String = Nothing

            If Not SolidEdgeExporter.TryReadActiveSketchBoundaries(loops, bounds, err) Then
                MessageBox.Show(err, "Reading profile from Solid Edge sketch", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            If loops Is Nothing OrElse loops.Count = 0 Then
                MessageBox.Show("No loops found in the sketch.", "Reading profile from Solid Edge sketch", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If


            If Not bounds.IsEmpty AndAlso bounds.Width > 0 AndAlso bounds.Height > 0 Then
                Dim pad As Single = 40.0F
                currentWorldDomain = New RectangleF(bounds.Left - pad,
                                        bounds.Top - pad,
                                        bounds.Width + pad * 2.0F,
                                        bounds.Height + pad * 2.0F)
            Else
                currentWorldDomain = domain
            End If

            canvas.Domain = currentWorldDomain
            lockSketchViewDomain = True



            currentSketchBoundaries = New List(Of List(Of Vec2))()
            currentSketchDomains = New List(Of SketchDomainRegion)()

            Dim holeFlags As New List(Of Boolean)
            Dim loopIndexMap As New Dictionary(Of Integer, SolidEdgeExporter.SketchBoundaryLoop)

            For i As Integer = 0 To loops.Count - 1
                Dim lp = loops(i)
                currentSketchBoundaries.Add(New List(Of Vec2)(lp.Points))
                holeFlags.Add(lp.IsHole)
                loopIndexMap(i) = lp
            Next

            For i As Integer = 0 To loops.Count - 1
                Dim lp = loops(i)
                If lp.IsHole Then Continue For

                Dim region As New SketchDomainRegion()
                region.Outer = New List(Of Vec2)(lp.Points)

                If lp.Children IsNot Nothing Then
                    For Each childIndex In lp.Children
                        If childIndex >= 0 AndAlso childIndex < loops.Count Then
                            Dim child = loops(childIndex)
                            If child IsNot Nothing AndAlso child.IsHole Then
                                region.Holes.Add(New List(Of Vec2)(child.Points))
                            End If
                        End If
                    Next
                End If

                region.Bounds = Geo2D.GetBounds(region.Outer)
                currentSketchDomains.Add(region)
            Next

            canvas.SketchBoundaries = currentSketchBoundaries
            canvas.SketchBoundaryIsHole = holeFlags
            canvas.SketchDomains = ConvertToCanvasDomains(currentSketchDomains)
            canvas.ConstrainSeedsToSketchDomains = True
            canvas.ShowSketchBoundary = True

            canvas.Cells = New List(Of VoronoiCell)()
            canvas.EditableSeeds = New List(Of Vec2)()

            useSketchDomains = (currentSketchDomains.Count > 0)

            ApplyOptions()
            canvas.Invalidate()

            If useSketchDomains Then
                GenerateDiagramFromSketchDomains()
            Else
                MessageBox.Show("No valid external domain found in the sketch.", "Reading profile from Solid Edge sketch", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            End If

        Catch ex As Exception
            MessageBox.Show("Error while reading the sketch profile: " & ex.Message,
                            "Reading profile from Solid Edge sketch",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error)
        End Try
    End Sub

    Private Function ConvertToCanvasDomains(domains As List(Of SketchDomainRegion)) As List(Of CanvasSketchDomain)
        Dim result As New List(Of CanvasSketchDomain)

        If domains Is Nothing Then Return result

        For Each d In domains
            Dim cd As New CanvasSketchDomain()
            cd.Outer = New List(Of Vec2)(d.Outer)

            If d.Holes IsNot Nothing Then
                For Each h In d.Holes
                    cd.Holes.Add(New List(Of Vec2)(h))
                Next
            End If

            result.Add(cd)
        Next

        Return result
    End Function

    Private Sub ExportSvg_Click(sender As Object, e As EventArgs)
        Try
            Dim paths = ExportGeometry.BuildExportPaths(canvas)
            If paths Is Nothing OrElse paths.Count = 0 Then
                MessageBox.Show("No geometry to export.", "Export SVG", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If

            Using dlg As New SaveFileDialog()
                dlg.Title = "Save SVG"
                dlg.Filter = "SVG file (*.svg)|*.svg"
                dlg.DefaultExt = "svg"
                dlg.AddExtension = True
                dlg.FileName = "voronoi.svg"

                If dlg.ShowDialog(Me) = DialogResult.OK Then
                    SvgExporter.SaveSvg(dlg.FileName, paths)
                    MessageBox.Show("SVG saved successfully.", "Export SVG", MessageBoxButtons.OK, MessageBoxIcon.Information)
                End If
            End Using

        Catch ex As Exception
            MessageBox.Show("Error exporting SVG:" & Environment.NewLine & ex.Message,
                            "Export SVG",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub ExportDxf_Click(sender As Object, e As EventArgs)
        Try
            Dim paths = ExportGeometry.BuildExportPaths(canvas)
            If paths Is Nothing OrElse paths.Count = 0 Then
                MessageBox.Show("No geometry to export.", "Export DXF", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If

            Using dlg As New SaveFileDialog()
                dlg.Title = "Save DXF"
                dlg.Filter = "DXF file (*.dxf)|*.dxf"
                dlg.DefaultExt = "dxf"
                dlg.AddExtension = True
                dlg.FileName = "voronoi.dxf"

                If dlg.ShowDialog(Me) = DialogResult.OK Then
                    DxfExporter.SaveDxf(dlg.FileName, paths)
                    MessageBox.Show("DXF saved successfully.", "Export DXF", MessageBoxButtons.OK, MessageBoxIcon.Information)
                End If
            End Using

        Catch ex As Exception
            MessageBox.Show("Error exporting DXF:" & Environment.NewLine & ex.Message,
                        "Export DXF",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub ExportToSolidEdge_Click(sender As Object, e As EventArgs)
        Try
            Dim paths = ExportGeometry.BuildExportPaths(canvas)
            If paths Is Nothing OrElse paths.Count = 0 Then
                MessageBox.Show("No geometry to export.", "Solid Edge", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If

            SolidEdgeExporter.ExportToActivePartSketch(paths)

            MessageBox.Show("Geometry sent to Solid Edge in the sketch.", "Solid Edge", MessageBoxButtons.OK, MessageBoxIcon.Information)

        Catch ex As Exception
            MessageBox.Show("Error exporting to Solid Edge:" & Environment.NewLine & ex.Message,
                            "Solid Edge",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error)
        End Try
    End Sub
End Class
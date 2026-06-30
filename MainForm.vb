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
    Private ReadOnly cmbVertexMode As New ComboBox()

    Private ReadOnly numCellScale As New NumericUpDown()

    Private ReadOnly numInnerOffset As New NumericUpDown()
    Private ReadOnly numVertexTrim As New NumericUpDown()
    Private ReadOnly numCurveWidth As New NumericUpDown()

    Private ReadOnly chkFill As New CheckBox()
    Private ReadOnly chkFillSymbols As New CheckBox()
    Private ReadOnly chkOuter As New CheckBox()
    Private ReadOnly chkSeeds As New CheckBox()
    Private ReadOnly chkInner As New CheckBox()
    Private ReadOnly chkRandomRotation As New CheckBox()
    Private ReadOnly chkExportAsBlocks As New CheckBox()

    Private ReadOnly btnGenerate As New Button()
    Private ReadOnly btnShuffle As New Button()

    Private ReadOnly btnExportSvg As New Button()
    Private ReadOnly btnExportDxf As New Button()
    Private ReadOnly btnToSolidEdge As New Button()

    Private ReadOnly btnReadSketchProfile As New Button()
    Private ReadOnly btnReadBlockDefaultView As New Button()
    Private ReadOnly btnSaveBlocks As New Button()
    Private ReadOnly btnLoadBlocks As New Button()
    Private ReadOnly btnClearBlocks As New Button()
    Private currentBlockSymbols As New List(Of BlockDefinition)()

    Private ReadOnly domain As RectangleF = New RectangleF(0, 0, 1000, 700)
    Private currentWorldDomain As RectangleF
    Private lockSketchViewDomain As Boolean = False

    Private currentSeeds As New List(Of Vec2)
    Private currentSeedStyleKeys As New List(Of Integer)
    Private currentSeedCellScales As New List(Of Single)
    Private currentSeedCellRotations As New List(Of Single)
    Private lastCellScale As Single = 0.82F

    Private currentSketchBoundaries As New List(Of List(Of Vec2))
    Private currentSketchDomains As New List(Of SketchDomainRegion)
    Private useSketchDomains As Boolean = False



    Private Class SketchDomainRegion
        Public Property Outer As List(Of Vec2) = New List(Of Vec2)
        Public Property Holes As List(Of List(Of Vec2)) = New List(Of List(Of Vec2))
        Public Property Bounds As RectangleF = RectangleF.Empty
    End Class

    Public Sub New()
        Text = "Solid Edge Voronoi Generator - v1.1"
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
        AddHandler btnReadBlockDefaultView.Click, AddressOf ReadBlockDefaultView_Click
        AddHandler btnSaveBlocks.Click, AddressOf SaveBlocks_Click
        AddHandler btnLoadBlocks.Click, AddressOf LoadBlocks_Click
        AddHandler btnClearBlocks.Click, AddressOf ClearBlocks_Click

        AddHandler btnExportSvg.Click, AddressOf ExportSvg_Click
        AddHandler btnExportDxf.Click, AddressOf ExportDxf_Click
        AddHandler btnToSolidEdge.Click, AddressOf ExportToSolidEdge_Click

        AddHandler chkFill.CheckedChanged, AddressOf RefreshCanvasOptions
        AddHandler chkFillSymbols.CheckedChanged, AddressOf RefreshCanvasOptions
        AddHandler chkOuter.CheckedChanged, AddressOf RefreshCanvasOptions
        AddHandler chkSeeds.CheckedChanged, AddressOf RefreshCanvasOptions
        AddHandler chkInner.CheckedChanged, AddressOf RefreshCanvasOptions
        AddHandler chkRandomRotation.CheckedChanged, AddressOf RefreshCanvasOptions

        AddHandler cmbStyle.SelectedIndexChanged, AddressOf RefreshCanvasOptions
        AddHandler cmbVertexMode.SelectedIndexChanged, AddressOf RefreshCanvasOptions

        AddHandler numCellScale.ValueChanged, AddressOf RefreshCanvasOptions
        AddHandler numInnerOffset.ValueChanged, AddressOf RefreshCanvasOptions
        AddHandler numVertexTrim.ValueChanged, AddressOf RefreshCanvasOptions
        AddHandler numCurveWidth.ValueChanged, AddressOf RefreshCanvasOptions

        AddHandler canvas.SeedsEdited, AddressOf Canvas_SeedsEdited
        AddHandler canvas.SeedScalesEdited, AddressOf Canvas_SeedScalesEdited
        AddHandler canvas.SeedRotationsEdited, AddressOf Canvas_SeedRotationsEdited

        AddHandler numCells.ValueChanged, AddressOf GenerationParameterChanged
        AddHandler numSeed.ValueChanged, AddressOf GenerationParameterChanged
        AddHandler numRelax.ValueChanged, AddressOf GenerationParameterChanged

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

        AddDoubleRow("Modalità vertice", cmbVertexMode,
             "Dimensione vertice", numVertexTrim)

        AddDoubleRow("Cell Count", numCells,
             "Random Seed", numSeed)

        AddDoubleRow("Relax", numRelax,
             "Cell Scale", numCellScale)

        AddDoubleRow("Inner Offset", numInnerOffset,
             "Curve Width", numCurveWidth)

        AddRowControl(chkFill)
        AddRowControl(chkFillSymbols)
        AddRowControl(chkOuter)
        AddRowControl(chkSeeds)
        AddRowControl(chkInner)
        AddRowControl(chkRandomRotation)

        AddRowControl(btnGenerate, 34)
        AddRowControl(btnShuffle, 34)

        AddRowTitle("Sketch")
        AddRowControl(btnReadSketchProfile, 30)
        AddRowControl(btnReadBlockDefaultView, 30)
        AddRowControl(btnLoadBlocks, 30)
        AddRowControl(btnSaveBlocks, 30)
        AddRowControl(btnClearBlocks, 30)

        AddRowTitle("Export")
        AddRowControl(btnExportSvg, 30)
        AddRowControl(btnExportDxf, 30)
        AddRowControl(chkExportAsBlocks)
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


    Private Sub AddDoubleRow(leftTitle As String,
                         leftCtrl As Control,
                         rightTitle As String,
                         rightCtrl As Control,
                         Optional controlHeight As Integer = 28)

        sideLayout.RowStyles.Add(New RowStyle(SizeType.AutoSize))

        Dim host As New TableLayoutPanel With {
        .ColumnCount = 2,
        .RowCount = 1,
        .Width = 235,
        .AutoSize = True,
        .Margin = New Padding(3, 0, 3, 3),
        .Padding = New Padding(0)
    }

        host.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50.0F))
        host.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50.0F))

        Dim leftPanel = BuildLabeledControlPanel(leftTitle, leftCtrl, controlHeight)
        Dim rightPanel = BuildLabeledControlPanel(rightTitle, rightCtrl, controlHeight)

        host.Controls.Add(leftPanel, 0, 0)
        host.Controls.Add(rightPanel, 1, 0)

        sideLayout.Controls.Add(host)
    End Sub

    Private Function BuildLabeledControlPanel(title As String,
                                          ctrl As Control,
                                          forcedHeight As Integer) As Control

        Dim panel As New TableLayoutPanel With {
        .ColumnCount = 1,
        .RowCount = 2,
        .Dock = DockStyle.Fill,
        .AutoSize = True,
        .Margin = New Padding(0),
        .Padding = New Padding(0)
    }

        panel.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        panel.RowStyles.Add(New RowStyle(SizeType.AutoSize))

        Dim lbl As New Label With {
        .Text = title,
        .ForeColor = Color.FromArgb(30, 40, 55),
        .AutoSize = False,
        .Height = 18,
        .Dock = DockStyle.Top,
        .Margin = New Padding(0, 4, 4, 1),
        .TextAlign = ContentAlignment.BottomLeft
    }

        ctrl.Dock = DockStyle.Top
        ctrl.Height = forcedHeight
        ctrl.Margin = New Padding(0, 0, 4, 0)

        panel.Controls.Add(lbl, 0, 0)
        panel.Controls.Add(ctrl, 0, 1)

        Return panel
    End Function

    Private Sub ConfigureControls()
        cmbStyle.DropDownStyle = ComboBoxStyle.DropDownList
        cmbStyle.Items.AddRange([Enum].GetNames(GetType(CellRenderStyle)))
        cmbStyle.SelectedItem = CellRenderStyle.Curved.ToString()

        cmbVertexMode.DropDownStyle = ComboBoxStyle.DropDownList
        cmbVertexMode.Items.AddRange(New String() {"Spigolo vivo", "Raggiatura con arco", "Curva spline"})
        cmbVertexMode.SelectedIndex = 2

        numCells.Minimum = 5
        numCells.Maximum = 500
        numCells.Value = 80

        numSeed.Minimum = 0
        numSeed.Maximum = Integer.MaxValue
        numSeed.Value = 12345

        numRelax.Minimum = 0
        numRelax.Maximum = 30
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

        numVertexTrim.Minimum = 0D
        numVertexTrim.Maximum = 3D
        numVertexTrim.DecimalPlaces = 2
        numVertexTrim.Increment = 0.05D
        numVertexTrim.Value = 0.22D

        numCurveWidth.Minimum = 1D
        numCurveWidth.Maximum = 12D
        numCurveWidth.DecimalPlaces = 1
        numCurveWidth.Increment = 0.2D
        numCurveWidth.Value = 1.8D

        chkFill.Text = "Fill cells"
        chkFill.Checked = True
        chkFill.ForeColor = Color.FromArgb(30, 40, 55)

        chkFillSymbols.Text = "Fill symbols"
        chkFillSymbols.Checked = False
        chkFillSymbols.ForeColor = Color.FromArgb(30, 40, 55)

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

        chkExportAsBlocks.Text = "To SE as blocks (occurrences)"
        chkExportAsBlocks.Checked = False
        chkExportAsBlocks.ForeColor = Color.FromArgb(30, 40, 55)
        chkExportAsBlocks.Margin = New Padding(3, 1, 3, 1)

        chkFill.Margin = New Padding(3, 1, 3, 1)
        chkFillSymbols.Margin = New Padding(3, 1, 3, 1)
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

        btnReadSketchProfile.Text = "Read Solid Edge Sketch Profile"
        btnReadSketchProfile.UseVisualStyleBackColor = False
        btnReadSketchProfile.BackColor = Color.White
        btnReadSketchProfile.ForeColor = Color.FromArgb(30, 40, 55)
        btnReadSketchProfile.FlatStyle = FlatStyle.Flat

        btnReadBlockDefaultView.Text = "Read Solid Edge Blocks"
        btnReadBlockDefaultView.UseVisualStyleBackColor = False
        btnReadBlockDefaultView.BackColor = Color.White
        btnReadBlockDefaultView.ForeColor = Color.FromArgb(30, 40, 55)
        btnReadBlockDefaultView.FlatStyle = FlatStyle.Flat

        btnLoadBlocks.Text = "Load Blocks from File"
        btnLoadBlocks.UseVisualStyleBackColor = False
        btnLoadBlocks.BackColor = Color.White
        btnLoadBlocks.ForeColor = Color.FromArgb(30, 40, 55)
        btnLoadBlocks.FlatStyle = FlatStyle.Flat

        btnSaveBlocks.Text = "Save Blocks to File"
        btnSaveBlocks.UseVisualStyleBackColor = False
        btnSaveBlocks.BackColor = Color.White
        btnSaveBlocks.ForeColor = Color.FromArgb(30, 40, 55)
        btnSaveBlocks.FlatStyle = FlatStyle.Flat

        btnClearBlocks.Text = "Clear Blocks"
        btnClearBlocks.UseVisualStyleBackColor = False
        btnClearBlocks.BackColor = Color.White
        btnClearBlocks.ForeColor = Color.FromArgb(30, 40, 55)
        btnClearBlocks.FlatStyle = FlatStyle.Flat

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

    'Private Sub GenerateRandomDiagram(sender As Object, e As EventArgs)
    '    If useSketchDomains AndAlso currentSketchDomains IsNot Nothing AndAlso currentSketchDomains.Count > 0 Then
    '        GenerateDiagramFromSketchDomains()
    '        Return
    '    End If

    '    currentSeeds = VoronoiEngine.CreateSeeds(CInt(numCells.Value), domain, CInt(numSeed.Value))
    '    RebuildSeedStyleKeys(currentSeeds.Count, CInt(numSeed.Value))

    '    For i As Integer = 1 To CInt(numRelax.Value)
    '        Dim tmpCells = VoronoiEngine.BuildCells(currentSeeds, domain)
    '        currentSeeds = VoronoiEngine.RelaxSeeds(tmpCells)
    '    Next

    '    BuildFromCurrentSeeds()
    'End Sub

    Private Sub GenerateRandomDiagram(sender As Object, e As EventArgs)
        If useSketchDomains AndAlso currentSketchDomains IsNot Nothing AndAlso currentSketchDomains.Count > 0 Then
            GenerateDiagramFromSketchDomains()
            Return
        End If

        currentSeeds = VoronoiEngine.CreateSeeds(CInt(numCells.Value), domain, CInt(numSeed.Value))
        RebuildSeedStyleKeys(currentSeeds.Count, CInt(numSeed.Value))
        RebuildSeedCellScales(currentSeeds.Count, CSng(numCellScale.Value))
        RebuildSeedCellRotations(currentSeeds.Count)
        lastCellScale = CSng(numCellScale.Value)

        For i As Integer = 1 To CInt(numRelax.Value)
            Dim tmpCells = VoronoiEngine.BuildCells(currentSeeds, domain)
            currentSeeds = VoronoiEngine.RelaxSeeds(tmpCells)
        Next

        EnsureSeedStyleKeyCount(currentSeeds.Count, CInt(numSeed.Value))
        EnsureSeedCellScaleCount(currentSeeds.Count, CSng(numCellScale.Value))
        EnsureSeedCellRotationCount(currentSeeds.Count)

        BuildFromCurrentSeeds()
    End Sub

    'Private Sub GenerateDiagramFromSketchDomains()
    '    Dim allCells As New List(Of VoronoiCell)
    '    Dim allSeeds As New List(Of Vec2)
    '    Dim allStyleKeys As New List(Of Integer)

    '    If currentSketchDomains Is Nothing OrElse currentSketchDomains.Count = 0 Then Return

    '    Dim totalOuterArea As Double = 0.0
    '    For Each d In currentSketchDomains
    '        totalOuterArea += Math.Abs(Geo2D.SignedArea(d.Outer))
    '    Next
    '    If totalOuterArea <= 0.0001 Then Return

    '    Dim requestedCount As Integer = CInt(numCells.Value)
    '    Dim seedBase As Integer = CInt(numSeed.Value)

    '    For i As Integer = 0 To currentSketchDomains.Count - 1
    '        Dim d = currentSketchDomains(i)
    '        Dim area As Double = Math.Abs(Geo2D.SignedArea(d.Outer))
    '        Dim quota As Integer = CInt(Math.Round(requestedCount * (area / totalOuterArea)))

    '        If i = currentSketchDomains.Count - 1 Then
    '            quota = requestedCount - allSeeds.Count
    '        End If

    '        quota = Math.Max(0, quota)
    '        If quota = 0 Then Continue For

    '        Dim regionSeed As Integer = seedBase + i * 997
    '        Dim seeds = VoronoiEngine.CreateSeeds(quota, d.Bounds, d.Outer, d.Holes, regionSeed)

    '        Dim regionStyleKeys As New List(Of Integer)
    '        Dim rng As New Random(regionSeed Xor &H51F15E)
    '        For k As Integer = 0 To seeds.Count - 1
    '            regionStyleKeys.Add(rng.Next())
    '        Next

    '        For r As Integer = 1 To CInt(numRelax.Value)
    '            Dim tmpCells = VoronoiEngine.BuildCells(seeds, d.Outer, d.Holes)
    '            seeds = VoronoiEngine.RelaxSeeds(tmpCells)

    '            Dim filteredSeeds As New List(Of Vec2)
    '            Dim filteredKeys As New List(Of Integer)

    '            For k As Integer = 0 To seeds.Count - 1
    '                If Geo2D.PointInPolygonWithHoles(seeds(k), d.Outer, d.Holes) Then
    '                    filteredSeeds.Add(seeds(k))
    '                    If k < regionStyleKeys.Count Then
    '                        filteredKeys.Add(regionStyleKeys(k))
    '                    End If
    '                End If
    '            Next

    '            seeds = filteredSeeds
    '            regionStyleKeys = filteredKeys
    '        Next

    '        Dim cells = VoronoiEngine.BuildCells(seeds, d.Outer, d.Holes)

    '        allSeeds.AddRange(seeds)
    '        allCells.AddRange(cells)
    '        allStyleKeys.AddRange(regionStyleKeys)
    '    Next

    '    currentSeeds = allSeeds
    '    currentSeedStyleKeys = allStyleKeys

    '    canvas.Cells = allCells
    '    canvas.EditableSeeds = New List(Of Vec2)(allSeeds)
    '    canvas.SeedStyleKeys = New List(Of Integer)(currentSeedStyleKeys)

    '    ApplyOptions()
    '    canvas.Invalidate()
    'End Sub

    Private Sub GenerateDiagramFromSketchDomains()
        Dim allCells As New List(Of VoronoiCell)
        Dim allSeeds As New List(Of Vec2)
        Dim allStyleKeys As New List(Of Integer)
        Dim allScales As New List(Of Single)
        Dim allRotations As New List(Of Single)

        If currentSketchDomains Is Nothing OrElse currentSketchDomains.Count = 0 Then Return

        Dim totalOuterArea As Double = 0.0
        For Each d In currentSketchDomains
            totalOuterArea += Math.Abs(Geo2D.SignedArea(d.Outer))
        Next
        If totalOuterArea <= 0.0001 Then Return

        Dim requestedCount As Integer = CInt(numCells.Value)
        Dim seedBase As Integer = CInt(numSeed.Value)
        Dim defaultScale As Single = CSng(numCellScale.Value)
        lastCellScale = defaultScale

        For i As Integer = 0 To currentSketchDomains.Count - 1
            Dim d = currentSketchDomains(i)
            Dim area As Double = Math.Abs(Geo2D.SignedArea(d.Outer))
            Dim quota As Integer = CInt(Math.Round(requestedCount * (area / totalOuterArea)))

            If i = currentSketchDomains.Count - 1 Then
                quota = requestedCount - allSeeds.Count
            End If

            quota = Math.Max(0, quota)
            If quota = 0 Then Continue For

            Dim regionSeed As Integer = seedBase + i * 997
            Dim seeds = VoronoiEngine.CreateSeeds(quota, d.Bounds, d.Outer, d.Holes, regionSeed)

            Dim regionStyleKeys As New List(Of Integer)
            Dim regionScales As New List(Of Single)
            Dim regionRotations As New List(Of Single)
            Dim rng As New Random(regionSeed Xor &H51F15E)

            For k As Integer = 0 To seeds.Count - 1
                regionStyleKeys.Add(rng.Next())
                regionScales.Add(defaultScale)
                regionRotations.Add(0.0F)
            Next

            For r As Integer = 1 To CInt(numRelax.Value)
                Dim tmpCells = VoronoiEngine.BuildCells(seeds, d.Outer, d.Holes)
                seeds = VoronoiEngine.RelaxSeeds(tmpCells)

                Dim filteredSeeds As New List(Of Vec2)
                Dim filteredKeys As New List(Of Integer)
                Dim filteredScales As New List(Of Single)
                Dim filteredRotations As New List(Of Single)

                For k As Integer = 0 To seeds.Count - 1
                    If Geo2D.PointInPolygonWithHoles(seeds(k), d.Outer, d.Holes) Then
                        filteredSeeds.Add(seeds(k))

                        If k < regionStyleKeys.Count Then
                            filteredKeys.Add(regionStyleKeys(k))
                        End If

                        If k < regionScales.Count Then
                            filteredScales.Add(regionScales(k))
                        End If

                        If k < regionRotations.Count Then
                            filteredRotations.Add(regionRotations(k))
                        End If
                    End If
                Next

                seeds = filteredSeeds
                regionStyleKeys = filteredKeys
                regionScales = filteredScales
                regionRotations = filteredRotations
            Next

            Dim cells = VoronoiEngine.BuildCells(seeds, d.Outer, d.Holes)

            allSeeds.AddRange(seeds)
            allCells.AddRange(cells)
            allStyleKeys.AddRange(regionStyleKeys)
            allScales.AddRange(regionScales)
            allRotations.AddRange(regionRotations)
        Next

        currentSeeds = allSeeds
        currentSeedStyleKeys = allStyleKeys
        currentSeedCellScales = allScales
        currentSeedCellRotations = allRotations

        canvas.Cells = allCells
        canvas.EditableSeeds = New List(Of Vec2)(allSeeds)
        canvas.SeedStyleKeys = New List(Of Integer)(currentSeedStyleKeys)
        canvas.CellScales = New List(Of Single)(currentSeedCellScales)
        canvas.CellRotations = New List(Of Single)(currentSeedCellRotations)

        ApplyOptions()
        canvas.Invalidate()
    End Sub

    'Private Sub Canvas_SeedsEdited(sender As Object, e As EventArgs)
    '    currentSeeds = New List(Of Vec2)(canvas.EditableSeeds)
    '    EnsureSeedStyleKeyCount(currentSeeds.Count, CInt(numSeed.Value))
    '    BuildFromCurrentSeeds()
    'End Sub

    Private Sub Canvas_SeedsEdited(sender As Object, e As EventArgs)
        currentSeeds = New List(Of Vec2)(canvas.EditableSeeds)
        currentSeedCellScales = New List(Of Single)(canvas.CellScales)
        currentSeedCellRotations = New List(Of Single)(canvas.CellRotations)

        EnsureSeedStyleKeyCount(currentSeeds.Count, CInt(numSeed.Value))
        EnsureSeedCellScaleCount(currentSeeds.Count, CSng(numCellScale.Value))
        EnsureSeedCellRotationCount(currentSeeds.Count)

        BuildFromCurrentSeeds()
    End Sub

    Private Sub Canvas_SeedScalesEdited(sender As Object, e As EventArgs)
        currentSeedCellScales = New List(Of Single)(canvas.CellScales)
        EnsureSeedCellScaleCount(currentSeeds.Count, CSng(numCellScale.Value))
        canvas.CellScales = New List(Of Single)(currentSeedCellScales)
        canvas.Invalidate()
    End Sub

    Private Sub Canvas_SeedRotationsEdited(sender As Object, e As EventArgs)
        currentSeedCellRotations = New List(Of Single)(canvas.CellRotations)
        EnsureSeedCellRotationCount(currentSeeds.Count)
        canvas.CellRotations = New List(Of Single)(currentSeedCellRotations)
        canvas.Invalidate()
    End Sub

    'Private Sub BuildFromCurrentSeeds()
    '    If useSketchDomains AndAlso lockSketchViewDomain Then
    '        canvas.Domain = currentWorldDomain
    '    Else
    '        canvas.Domain = domain
    '    End If

    '    If useSketchDomains AndAlso currentSketchDomains IsNot Nothing AndAlso currentSketchDomains.Count > 0 Then
    '        Dim allCells As New List(Of VoronoiCell)
    '        Dim allSeeds As New List(Of Vec2)

    '        For Each d In currentSketchDomains
    '            Dim seedsInDomain = FilterSeedsInsideDomain(currentSeeds, d)
    '            If seedsInDomain.Count = 0 Then Continue For

    '            Dim cells = VoronoiEngine.BuildCells(seedsInDomain, d.Outer, d.Holes)
    '            allSeeds.AddRange(seedsInDomain)
    '            allCells.AddRange(cells)
    '        Next

    '        currentSeeds = allSeeds
    '        canvas.Cells = allCells
    '        canvas.EditableSeeds = New List(Of Vec2)(allSeeds)

    '    Else
    '        Dim cells = VoronoiEngine.BuildCells(currentSeeds, domain)
    '        canvas.Cells = cells
    '        canvas.EditableSeeds = New List(Of Vec2)(currentSeeds)
    '    End If

    '    canvas.SeedStyleKeys = New List(Of Integer)(currentSeedStyleKeys)

    '    ApplyOptions()
    '    canvas.Invalidate()
    'End Sub

    Private Sub BuildFromCurrentSeeds()
        EnsureSeedStyleKeyCount(currentSeeds.Count, CInt(numSeed.Value))
        EnsureSeedCellScaleCount(currentSeeds.Count, CSng(numCellScale.Value))
        EnsureSeedCellRotationCount(currentSeeds.Count)

        If useSketchDomains AndAlso lockSketchViewDomain Then
            canvas.Domain = currentWorldDomain
        Else
            canvas.Domain = domain
        End If

        If useSketchDomains AndAlso currentSketchDomains IsNot Nothing AndAlso currentSketchDomains.Count > 0 Then
            Dim allCells As New List(Of VoronoiCell)
            Dim allSeeds As New List(Of Vec2)
            Dim allStyleKeys As New List(Of Integer)
            Dim allScales As New List(Of Single)
            Dim allRotations As New List(Of Single)

            For Each d In currentSketchDomains
                Dim seedsInDomain As New List(Of Vec2)
                Dim keysInDomain As New List(Of Integer)
                Dim scalesInDomain As New List(Of Single)
                Dim rotationsInDomain As New List(Of Single)

                For i As Integer = 0 To currentSeeds.Count - 1
                    If Geo2D.PointInPolygonWithHoles(currentSeeds(i), d.Outer, d.Holes) Then
                        seedsInDomain.Add(currentSeeds(i))

                        If i < currentSeedStyleKeys.Count Then
                            keysInDomain.Add(currentSeedStyleKeys(i))
                        End If

                        If i < currentSeedCellScales.Count Then
                            scalesInDomain.Add(currentSeedCellScales(i))
                        Else
                            scalesInDomain.Add(CSng(numCellScale.Value))
                        End If

                        If i < currentSeedCellRotations.Count Then
                            rotationsInDomain.Add(currentSeedCellRotations(i))
                        Else
                            rotationsInDomain.Add(0.0F)
                        End If
                    End If
                Next

                If seedsInDomain.Count = 0 Then Continue For

                Dim cells = VoronoiEngine.BuildCells(seedsInDomain, d.Outer, d.Holes)

                allSeeds.AddRange(seedsInDomain)
                allStyleKeys.AddRange(keysInDomain)
                allScales.AddRange(scalesInDomain)
                allRotations.AddRange(rotationsInDomain)
                allCells.AddRange(cells)
            Next

            currentSeeds = allSeeds
            currentSeedStyleKeys = allStyleKeys
            currentSeedCellScales = allScales
            currentSeedCellRotations = allRotations

            canvas.Cells = allCells
            canvas.EditableSeeds = New List(Of Vec2)(allSeeds)
        Else
            Dim cells = VoronoiEngine.BuildCells(currentSeeds, domain)
            canvas.Cells = cells
            canvas.EditableSeeds = New List(Of Vec2)(currentSeeds)
        End If

        EnsureSeedStyleKeyCount(currentSeeds.Count, CInt(numSeed.Value))
        EnsureSeedCellScaleCount(currentSeeds.Count, CSng(numCellScale.Value))
        EnsureSeedCellRotationCount(currentSeeds.Count)

        canvas.SeedStyleKeys = New List(Of Integer)(currentSeedStyleKeys)
        canvas.CellScales = New List(Of Single)(currentSeedCellScales)
        canvas.CellRotations = New List(Of Single)(currentSeedCellRotations)

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

    'Private Sub RefreshCanvasOptions(sender As Object, e As EventArgs)
    '    ApplyOptions()
    '    canvas.Invalidate()
    'End Sub

    Private Sub RefreshCanvasOptions(sender As Object, e As EventArgs)
        ' Lo slider globale Cell Scale agisce come fattore moltiplicativo LIVE sui
        ' valori per-cella: aggiorna subito, mantiene le differenze relative editate
        ' e non resetta ai default.
        If sender Is numCellScale Then
            ApplyGlobalScaleDelta(CSng(numCellScale.Value))
        End If

        ApplyOptions()
        canvas.Invalidate()
    End Sub

    Private Sub ApplyGlobalScaleDelta(newGlobal As Single)
        Dim oldGlobal As Single = lastCellScale
        lastCellScale = newGlobal

        If currentSeedCellScales Is Nothing OrElse currentSeedCellScales.Count = 0 Then Return
        If oldGlobal <= 0.0001F Then Return

        Dim ratio As Single = newGlobal / oldGlobal
        If Math.Abs(ratio - 1.0F) < 0.000001F Then Return

        For i As Integer = 0 To currentSeedCellScales.Count - 1
            Dim v As Single = currentSeedCellScales(i) * ratio
            currentSeedCellScales(i) = CSng(Math.Max(0.05F, Math.Min(1.5F, v)))
        Next

        canvas.CellScales = New List(Of Single)(currentSeedCellScales)
    End Sub

    Private Sub ApplyOptions()
        canvas.FillCells = chkFill.Checked
        canvas.FillSymbols = chkFillSymbols.Checked
        canvas.ShowOuterEdges = chkOuter.Checked
        canvas.ShowSeeds = chkSeeds.Checked
        canvas.ShowInnerCurve = chkInner.Checked
        canvas.RandomRotation = chkRandomRotation.Checked

        canvas.RenderStyle = CType([Enum].Parse(GetType(CellRenderStyle), cmbStyle.SelectedItem.ToString()), CellRenderStyle)
        canvas.VertexMode = VertexModeFromUi()

        canvas.CellScale = CSng(numCellScale.Value)
        canvas.InnerOffset = CSng(numInnerOffset.Value)
        canvas.VertexTrim = CSng(numVertexTrim.Value)
        canvas.InnerCurveWidth = CSng(numCurveWidth.Value)

        Dim isCurved As Boolean = (canvas.RenderStyle = CellRenderStyle.Curved)
        Dim isSymbol As Boolean = canvas.RenderStyle <> CellRenderStyle.Curved AndAlso canvas.RenderStyle <> CellRenderStyle.Straight
        Dim usesVertices As Boolean = isCurved OrElse isSymbol

        ' Lo spigolo vivo non usa la dimensione; arco e spline si'.
        Dim usesVertexSize As Boolean = usesVertices AndAlso canvas.VertexMode <> SymbolCornerStyle.Sharp

        cmbVertexMode.Enabled = usesVertices
        numVertexTrim.Enabled = usesVertexSize
        numInnerOffset.Enabled = isCurved
        chkInner.Enabled = isCurved

        numCellScale.Enabled = isSymbol
        chkRandomRotation.Enabled = isSymbol

        ' "Fill symbols" ha senso solo dove c'e' un path interno (curva o simbolo).
        chkFillSymbols.Enabled = usesVertices OrElse isSymbol
    End Sub

    Private Function VertexModeFromUi() As SymbolCornerStyle
        Select Case cmbVertexMode.SelectedIndex
            Case 1
                Return SymbolCornerStyle.FilletArc
            Case 2
                Return SymbolCornerStyle.Bezier
            Case Else
                Return SymbolCornerStyle.Sharp
        End Select
    End Function

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

            currentSeedCellScales = New List(Of Single)()
            canvas.CellScales = New List(Of Single)()
            currentSeedCellRotations = New List(Of Single)()
            canvas.CellRotations = New List(Of Single)()

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





    Private Sub ReadBlockDefaultView_Click(sender As Object, e As EventArgs)
        Try
            Dim defs As List(Of BlockDefinition) = Nothing
            Dim err As String = Nothing

            If Not SolidEdgeExporter.TryReadAllBlocksAsPrimitives(defs, err) Then
                MessageBox.Show(err,
                            "Reading Solid Edge blocks",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning)
                Return
            End If

            If defs Is Nothing OrElse defs.Count = 0 Then
                MessageBox.Show("No blocks found.",
                            "Reading Solid Edge blocks",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning)
                Return
            End If

            For Each d In defs
                ExportGeometry.NormalizeBlockInPlace(d)
            Next

            currentBlockSymbols = defs
            canvas.BlockSymbols = defs

            cmbStyle.SelectedItem = CellRenderStyle.BlockSymbol.ToString()
            ApplyOptions()
            canvas.Invalidate()

            MessageBox.Show(defs.Count & " block(s) loaded as cell symbols.",
                        "Solid Edge blocks",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information)

        Catch ex As Exception
            MessageBox.Show("Error while reading blocks: " & ex.Message,
                        "Solid Edge blocks",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error)
        End Try
    End Sub
    Private Sub SaveBlocks_Click(sender As Object, e As EventArgs)
        If currentBlockSymbols Is Nothing OrElse currentBlockSymbols.Count = 0 Then
            MessageBox.Show("Nessun blocco in memoria da salvare.", "Save Blocks", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        Using dlg As New SaveFileDialog()
            dlg.Title = "Salva libreria blocchi"
            dlg.Filter = "SE-Voronoi blocks (*.sevb)|*.sevb"
            dlg.DefaultExt = "sevb"
            dlg.AddExtension = True
            dlg.FileName = "blocks.sevb"

            If dlg.ShowDialog(Me) = DialogResult.OK Then
                Try
                    ExportGeometry.SaveBlocksToFile(dlg.FileName, currentBlockSymbols)
                    MessageBox.Show(currentBlockSymbols.Count & " blocco/i salvati.",
                                    "Save Blocks", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Catch ex As Exception
                    MessageBox.Show("Errore salvataggio blocchi:" & Environment.NewLine & ex.Message,
                                    "Save Blocks", MessageBoxButtons.OK, MessageBoxIcon.Error)
                End Try
            End If
        End Using
    End Sub

    Private Sub LoadBlocks_Click(sender As Object, e As EventArgs)
        Using dlg As New OpenFileDialog()
            dlg.Title = "Carica libreria blocchi"
            dlg.Filter = "SE-Voronoi blocks (*.sevb)|*.sevb|Tutti i file (*.*)|*.*"
            dlg.Multiselect = False

            If dlg.ShowDialog(Me) = DialogResult.OK Then
                Try
                    Dim loaded = ExportGeometry.LoadBlocksFromFile(dlg.FileName)
                    If loaded Is Nothing OrElse loaded.Count = 0 Then
                        MessageBox.Show("Nessun blocco valido nel file.",
                                        "Load Blocks", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                        Return
                    End If

                    If currentBlockSymbols Is Nothing Then currentBlockSymbols = New List(Of BlockDefinition)()
                    currentBlockSymbols.AddRange(loaded)

                    canvas.BlockSymbols = currentBlockSymbols
                    cmbStyle.SelectedItem = CellRenderStyle.BlockSymbol.ToString()
                    ApplyOptions()
                    canvas.Invalidate()

                    MessageBox.Show(loaded.Count & " blocco/i caricati (" & currentBlockSymbols.Count & " totali in memoria).",
                                    "Load Blocks", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Catch ex As Exception
                    MessageBox.Show("Errore caricamento blocchi:" & Environment.NewLine & ex.Message,
                                    "Load Blocks", MessageBoxButtons.OK, MessageBoxIcon.Error)
                End Try
            End If
        End Using
    End Sub

    Private Sub ClearBlocks_Click(sender As Object, e As EventArgs)
        If currentBlockSymbols Is Nothing OrElse currentBlockSymbols.Count = 0 Then
            MessageBox.Show("Nessun blocco in memoria.", "Clear Blocks", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        If MessageBox.Show("Rimuovere tutti i blocchi caricati dalla memoria?", "Clear Blocks",
                           MessageBoxButtons.YesNo, MessageBoxIcon.Question) <> DialogResult.Yes Then Return

        currentBlockSymbols = New List(Of BlockDefinition)()
        canvas.BlockSymbols = currentBlockSymbols

        ' Se lo stile corrente e' a blocchi, non avrebbe piu' nulla da disegnare:
        ' si torna a un contorno curvo.
        If cmbStyle.SelectedItem IsNot Nothing AndAlso
           cmbStyle.SelectedItem.ToString() = CellRenderStyle.BlockSymbol.ToString() Then
            cmbStyle.SelectedItem = CellRenderStyle.Curved.ToString()
        End If

        ApplyOptions()
        canvas.Invalidate()
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
                    SvgExporter.SaveSvgFull(dlg.FileName, canvas)
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
            If chkExportAsBlocks.Checked Then
                Dim geoms = ExportGeometry.BuildCellGeometry(canvas)
                If geoms Is Nothing OrElse geoms.Count = 0 Then
                    MessageBox.Show("No geometry to export.", "Solid Edge", MessageBoxButtons.OK, MessageBoxIcon.Information)
                    Return
                End If
                SolidEdgeExporter.ExportToActivePartSketch(geoms)
            Else
                Dim paths = ExportGeometry.BuildExportPaths(canvas)
                If paths Is Nothing OrElse paths.Count = 0 Then
                    MessageBox.Show("No geometry to export.", "Solid Edge", MessageBoxButtons.OK, MessageBoxIcon.Information)
                    Return
                End If
                SolidEdgeExporter.ExportToActivePartSketch(paths)
            End If

            MessageBox.Show("Geometry sent to Solid Edge in the sketch.", "Solid Edge", MessageBoxButtons.OK, MessageBoxIcon.Information)

        Catch ex As Exception
            MessageBox.Show("Error exporting to Solid Edge:" & Environment.NewLine & ex.Message,
                            "Solid Edge",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub RebuildSeedStyleKeys(count As Integer, baseSeed As Integer)
        currentSeedStyleKeys = New List(Of Integer)(count)

        Dim rng As New Random(baseSeed Xor &H51F15E)
        For i As Integer = 0 To count - 1
            currentSeedStyleKeys.Add(rng.Next())
        Next
    End Sub

    Private Sub EnsureSeedStyleKeyCount(count As Integer, baseSeed As Integer)
        If currentSeedStyleKeys Is Nothing Then
            currentSeedStyleKeys = New List(Of Integer)()
        End If

        While currentSeedStyleKeys.Count < count
            Dim extraSeed As Integer = baseSeed Xor (currentSeedStyleKeys.Count * 104729)
            Dim rng As New Random(extraSeed)
            currentSeedStyleKeys.Add(rng.Next())
        End While

        While currentSeedStyleKeys.Count > count
            currentSeedStyleKeys.RemoveAt(currentSeedStyleKeys.Count - 1)
        End While
    End Sub

    Private Sub GenerationParameterChanged(sender As Object, e As EventArgs)
        GenerateRandomDiagram(sender, e)
    End Sub

    Private Sub RebuildSeedCellScales(count As Integer, defaultScale As Single)
        currentSeedCellScales = New List(Of Single)(count)

        For i As Integer = 0 To count - 1
            currentSeedCellScales.Add(defaultScale)
        Next
    End Sub

    Private Sub EnsureSeedCellScaleCount(count As Integer, defaultScale As Single)
        If currentSeedCellScales Is Nothing Then
            currentSeedCellScales = New List(Of Single)()
        End If

        While currentSeedCellScales.Count < count
            currentSeedCellScales.Add(defaultScale)
        End While

        While currentSeedCellScales.Count > count
            currentSeedCellScales.RemoveAt(currentSeedCellScales.Count - 1)
        End While

        For i As Integer = 0 To currentSeedCellScales.Count - 1
            currentSeedCellScales(i) = CSng(Math.Max(0.05F, Math.Min(1.5F, currentSeedCellScales(i))))
        Next
    End Sub

    Private Sub RebuildSeedCellRotations(count As Integer)
        currentSeedCellRotations = New List(Of Single)(count)

        For i As Integer = 0 To count - 1
            currentSeedCellRotations.Add(0.0F)
        Next
    End Sub

    Private Sub EnsureSeedCellRotationCount(count As Integer)
        If currentSeedCellRotations Is Nothing Then
            currentSeedCellRotations = New List(Of Single)()
        End If

        While currentSeedCellRotations.Count < count
            currentSeedCellRotations.Add(0.0F)
        End While

        While currentSeedCellRotations.Count > count
            currentSeedCellRotations.RemoveAt(currentSeedCellRotations.Count - 1)
        End While
    End Sub

End Class
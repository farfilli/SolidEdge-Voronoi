Imports System
Imports System.Collections.Generic
Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.IO
Imports System.Linq
Imports System.Text
Imports System.Windows.Forms

Public Class MainForm
    Inherits Form
    Implements IMessageFilter

    Private ReadOnly canvas As New VoronoiCanvas()

    Private ReadOnly sidebar As New Panel()
    Private ReadOnly sideViewport As New Panel()
    Private ReadOnly btnProjNew As New ThemedButton()
    Private ReadOnly btnProjOpen As New ThemedButton()
    Private ReadOnly btnProjSave As New ThemedButton()

    ' File di progetto (.sevproj): impostazioni + profilo sketch + semi + blocchi.
    Private projectPath As String = Nothing
    Private projectDirty As Boolean = False
    Private loadingProject As Boolean = False

    Private ReadOnly topBar As New Panel()
    Private ReadOnly topBarGroupLabels As New List(Of KeyValuePair(Of Integer, String))
    Private ReadOnly topBarSeparators As New List(Of Integer)
    Private ReadOnly sideScroll As New ThemedVScrollBar()
    Private ReadOnly sideLayout As New FlowLayoutPanel()

    Private ReadOnly numCells As New ThemedNumericUpDown()
    Private ReadOnly numSeed As New ThemedNumericUpDown()
    Private ReadOnly numRelax As New ThemedNumericUpDown()

    Private ReadOnly cmbStyle As New ThemedComboBox()
    Private ReadOnly cmbSeedMode As New ThemedComboBox()
    Private ReadOnly cmbVertexMode As New ThemedComboBox()

    Private ReadOnly numCellScale As New ThemedSlider()

    Private ReadOnly numInnerOffset As New ThemedSlider()
    Private ReadOnly numVertexTrim As New ThemedSlider()
    Private ReadOnly numCurveWidth As New ThemedSlider()

    ' Sezione della sidebar in costruzione (target degli helper AddRow*)
    Private curSection As CollapsibleSection = Nothing

    ' Status bar
    Private ReadOnly statusBar As New Panel()
    Private ReadOnly lblStatusInfo As New Label()
    Private ReadOnly lblStatusCoords As New Label()
    Private ReadOnly lblStatusReady As New Label()
    Private ReadOnly lblStatusDot As New Label()


    Private ReadOnly chkFill As New ThemedCheckBox()
    Private ReadOnly chkFillSymbols As New ThemedCheckBox()
    Private ReadOnly chkDomainFill As New ThemedCheckBox()
    Private ReadOnly cmbDomainColor As New ThemedComboBox()

    ' Tavolozza degli sfondi del dominio: parente di quella delle celle ma
    ' scurita e deviata di tono, cosi' velature e simboli restano leggibili.
    Private Shared ReadOnly DomainColorNames As String() = {
        "Theme", "Deep Navy", "Midnight", "Ink Blue", "Deep Petrol",
        "Dark Teal", "Deep Lagoon", "Deep Indigo", "Dark Violet", "Aubergine",
        "Burgundy", "Forest", "Bronze Shadow", "Charcoal", "Slate",
        "Paper", "Light Gray"
    }

    Private Shared ReadOnly DomainColorValues As Color() = {
        Color.Empty,
        Color.FromArgb(8, 6, 53),
        Color.FromArgb(4, 10, 34),
        Color.FromArgb(10, 20, 66),
        Color.FromArgb(4, 44, 56),
        Color.FromArgb(6, 56, 52),
        Color.FromArgb(0, 38, 64),
        Color.FromArgb(22, 16, 72),
        Color.FromArgb(34, 10, 58),
        Color.FromArgb(44, 12, 44),
        Color.FromArgb(48, 12, 22),
        Color.FromArgb(10, 40, 26),
        Color.FromArgb(46, 34, 10),
        Color.FromArgb(24, 26, 30),
        Color.FromArgb(38, 44, 54),
        Color.FromArgb(238, 236, 228),
        Color.FromArgb(216, 220, 226)
    }

    Private Function GetSelectedDomainColor() As Color
        Dim idx As Integer = cmbDomainColor.SelectedIndex
        If idx <= 0 OrElse idx >= DomainColorValues.Length Then
            ' "Theme": BgCanvas e' navy per design in entrambi i temi, quindi
            ' qui si sceglie esplicitamente in base al tema attivo (in chiaro
            ' un tono "carta", distinto dal grigio sidebar del fuori-profilo).
            If UiTheme.IsDark Then
                Return UiTheme.BgCanvas
            Else
                Return Color.FromArgb(252, 252, 249)
            End If
        End If
        Return DomainColorValues(idx)
    End Function
    Private ReadOnly chkOuter As New ThemedCheckBox()
    Private ReadOnly chkSeeds As New ThemedCheckBox()
    Private ReadOnly chkInner As New ThemedCheckBox()
    Private ReadOnly chkRandomRotation As New ThemedCheckBox()
    Private ReadOnly chkExportAsBlocks As New ThemedCheckBox()
    Private ReadOnly chkExportProfile As New ThemedCheckBox()
    Private ReadOnly chkPeriodicX As New ThemedCheckBox()
    Private ReadOnly chkPeriodicY As New ThemedCheckBox()
    Private ReadOnly chkFullSeamCells As New ThemedCheckBox()

    Private ReadOnly btnGenerate As New ThemedButton()
    Private ReadOnly btnShuffle As New ThemedButton()

    Private ReadOnly btnExportSvg As New ThemedButton()
    Private ReadOnly btnExportDxf As New ThemedButton()
    Private ReadOnly btnExportPng As New ThemedButton()
    Private ReadOnly btnHelp As New ThemedButton()
    Private ReadOnly chkDarkTheme As New ThemedCheckBox()

    ' Pannello proprieta' della cella selezionata
    Private ReadOnly lblSelInfo As New Label()
    Private ReadOnly selScale As New ThemedSlider()
    Private ReadOnly selRotation As New ThemedSlider()
    Private ReadOnly selSymbolOffset As New ThemedNumericUpDown()
    Private ReadOnly chkSelPinned As New ThemedCheckBox()
    Private ReadOnly selColor As New ThemedComboBox()
    Private updatingSelectionUi As Boolean = False

    ' Stati apri/chiudi delle sezioni letti dai settings (applicati in NewSection).
    Private ReadOnly sectionStatesFromSettings As New Dictionary(Of String, Boolean)(StringComparer.OrdinalIgnoreCase)
    Private ReadOnly appTitleLbl As New Label()
    Private helpForm As HelpForm = Nothing
    Private ReadOnly btnToSolidEdge As New ThemedButton()

    Private ReadOnly btnReadSketchProfile As New ThemedButton()
    Private ReadOnly btnReadBlockDefaultView As New ThemedButton()
    Private ReadOnly btnSaveBlocks As New ThemedButton()
    Private ReadOnly btnLoadBlocks As New ThemedButton()
    Private ReadOnly btnClearBlocks As New ThemedButton()
    Private ReadOnly btnBlockLibrary As New ThemedButton()
    Private blockLibForm As BlockLibraryForm = Nothing
    Private currentBlockSymbols As New List(Of BlockDefinition)()

    Private ReadOnly domain As RectangleF = New RectangleF(0, 0, 1000, 700)
    Private currentWorldDomain As RectangleF
    Private lockSketchViewDomain As Boolean = False

    Private currentSeeds As New List(Of Vec2)
    Private currentSeedStyleKeys As New List(Of Integer)
    Private currentSeedCellScales As New List(Of Single)
    Private currentSeedCellRotations As New List(Of Single)
    Private currentSeedCellSymbolOffsets As New List(Of Integer)
    Private currentSeedPinned As New List(Of Boolean)
    Private currentSeedCellColors As New List(Of Integer)   ' indice tavolozza, -1 = auto
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
        ' Il titolo riporta il progetto corrente (UpdateFormTitle).
        StartPosition = FormStartPosition.CenterScreen
        Width = 1550
        Height = 920
        MinimumSize = New Size(1480, 760)

        Icon = My.Resources.SE_Voronoi
        Font = New Font("Segoe UI", 9.0F)
        BackColor = UiTheme.BgCanvas

        loadingProject = True

        ConfigureControls()
        LoadUserSettings()
        BuildSidebar()
        BuildTopBar()
        BuildStatusBar()

        canvas.Dock = DockStyle.Fill
        currentWorldDomain = domain
        canvas.Domain = currentWorldDomain
        canvas.BackColor = UiTheme.BgCanvas
        canvas.OutsideProfileColor = UiTheme.BgSidebar
        canvas.DomainFillColor = GetSelectedDomainColor()

        Controls.Add(canvas)
        Controls.Add(sidebar)
        Controls.Add(topBar)
        Controls.Add(statusBar)

        ApplyDarkTheme()

        If Not chkDarkTheme.Checked Then
            ApplyFullTheme(False)
        End If

        AddHandler canvas.WorldCursorMoved, AddressOf Canvas_WorldCursorMoved

        Application.AddMessageFilter(Me)

        AddHandler btnGenerate.Click, AddressOf GenerateRandomDiagram
        AddHandler btnShuffle.Click, AddressOf ShuffleSeed
        AddHandler btnReadSketchProfile.Click, AddressOf ReadSketchProfile_Click
        AddHandler btnReadBlockDefaultView.Click, AddressOf ReadBlockDefaultView_Click
        AddHandler btnSaveBlocks.Click, AddressOf SaveBlocks_Click
        AddHandler btnLoadBlocks.Click, AddressOf LoadBlocks_Click
        AddHandler btnClearBlocks.Click, AddressOf ClearBlocks_Click
        AddHandler btnBlockLibrary.Click, AddressOf OpenBlockLibrary_Click

        AddHandler btnExportSvg.Click, AddressOf ExportSvg_Click
        AddHandler btnExportDxf.Click, AddressOf ExportDxf_Click
        AddHandler btnExportPng.Click, AddressOf ExportPng_Click
        AddHandler btnHelp.Click, AddressOf ShowHelp_Click
        AddHandler btnProjNew.Click, AddressOf ProjectNew_Click
        AddHandler btnProjOpen.Click, AddressOf ProjectOpen_Click
        AddHandler btnProjSave.Click, AddressOf ProjectSave_Click
        AddHandler chkDarkTheme.CheckedChanged, AddressOf ThemeToggle_Changed
        AddHandler canvas.SelectedSeedChanged, AddressOf Canvas_SelectedSeedChanged
        AddHandler selScale.ValueChanged, AddressOf SelScale_Changed
        AddHandler selRotation.ValueChanged, AddressOf SelRotation_Changed
        AddHandler selSymbolOffset.ValueChanged, AddressOf SelSymbolOffset_Changed
        AddHandler chkSelPinned.CheckedChanged, AddressOf SelPinned_Changed
        AddHandler selColor.SelectedIndexChanged, AddressOf SelColor_Changed
        AddHandler btnToSolidEdge.Click, AddressOf ExportToSolidEdge_Click

        AddHandler chkFill.CheckedChanged, AddressOf RefreshCanvasOptions
        AddHandler chkDomainFill.CheckedChanged, AddressOf RefreshCanvasOptions
        AddHandler cmbDomainColor.SelectedIndexChanged, AddressOf RefreshCanvasOptions
        AddHandler chkFillSymbols.CheckedChanged, AddressOf RefreshCanvasOptions
        AddHandler chkFillSymbols.CheckedChanged, AddressOf RefreshLibraryHandler
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
        AddHandler numCurveWidth.ValueChanged, AddressOf RefreshLibraryHandler

        AddHandler canvas.SeedsEdited, AddressOf Canvas_SeedsEdited
        AddHandler canvas.SeedScalesEdited, AddressOf Canvas_SeedScalesEdited
        AddHandler canvas.SeedRotationsEdited, AddressOf Canvas_SeedRotationsEdited
        AddHandler canvas.SeedSymbolOffsetsEdited, AddressOf Canvas_SeedSymbolOffsetsEdited
        AddHandler canvas.SeedColorsEdited, AddressOf Canvas_SeedColorsEdited
        AddHandler canvas.SketchBoundariesEdited, AddressOf Canvas_SketchBoundariesEdited
        AddHandler canvas.DomainChangedByFit, AddressOf Canvas_DomainChangedByFit

        AddHandler numCells.ValueChanged, AddressOf GenerationParameterChanged
        AddHandler numSeed.ValueChanged, AddressOf GenerationParameterChanged
        AddHandler numRelax.ValueChanged, AddressOf GenerationParameterChanged
        AddHandler cmbSeedMode.SelectedIndexChanged, AddressOf GenerationParameterChanged
        AddHandler chkExportAsBlocks.CheckedChanged, AddressOf ExportAsBlocks_Changed
        AddHandler chkExportProfile.CheckedChanged, AddressOf ExportAsBlocks_Changed
        AddHandler chkPeriodicX.CheckedChanged, AddressOf GenerationParameterChanged
        AddHandler chkPeriodicY.CheckedChanged, AddressOf GenerationParameterChanged
        AddHandler chkFullSeamCells.CheckedChanged, AddressOf GenerationParameterChanged

        InstallDefaultRectangularProfile()
        GenerateRandomDiagram(Nothing, EventArgs.Empty)

        loadingProject = False
        UpdateFormTitle()
    End Sub

    Private Sub BuildSidebar()
        sidebar.Dock = DockStyle.Left
        sidebar.Width = 280
        sidebar.BackColor = UiTheme.BgSidebar
        sidebar.Padding = New Padding(10, 6, 6, 6)

        ' Scrolling custom: viewport senza AutoScroll + ThemedVScrollBar a tema.
        ' Cosi' la barra e' coerente col tema e compare solo quando serve.
        sideViewport.Dock = DockStyle.Fill
        sideViewport.BackColor = UiTheme.BgSidebar

        sideScroll.Dock = DockStyle.Right
        sideScroll.Width = 8
        sideScroll.Visible = False

        sideLayout.FlowDirection = FlowDirection.TopDown
        sideLayout.WrapContents = False
        sideLayout.AutoSize = True
        sideLayout.AutoSizeMode = AutoSizeMode.GrowAndShrink
        sideLayout.BackColor = UiTheme.BgSidebar
        sideLayout.Padding = New Padding(0)
        sideLayout.Margin = New Padding(0)
        sideLayout.Location = New Point(0, 0)

        sideViewport.Controls.Add(sideLayout)
        sidebar.Controls.Add(sideViewport)
        sidebar.Controls.Add(sideScroll)

        AddHandler sideLayout.SizeChanged, Sub(s, ev) UpdateSidebarScroll()
        AddHandler sideViewport.Resize, Sub(s, ev) UpdateSidebarScroll()
        AddHandler sideScroll.ScrollChanged, Sub(s, ev) sideLayout.Top = -sideScroll.Value

        ' Titolo app
        appTitleLbl.Text = "SE-VORONOI"
        appTitleLbl.ForeColor = UiTheme.Accent
        appTitleLbl.Font = New Font("Segoe UI", 10.0F, FontStyle.Bold)
        appTitleLbl.AutoSize = False
        appTitleLbl.Width = 238
        appTitleLbl.Height = 26
        appTitleLbl.Margin = New Padding(3, 4, 3, 2)
        appTitleLbl.TextAlign = ContentAlignment.MiddleLeft
        sideLayout.Controls.Add(appTitleLbl)

        ' ===== GENERATION =====
        curSection = NewSection("GENERATION", True)
        AddRowTitle("Seed Placement")
        AddRowControl(cmbSeedMode)
        AddDoubleRow("Cell Count", numCells,
             "Random Seed", numSeed)
        AddDoubleRow("Relax", numRelax,
             "Cell Scale", numCellScale)
        AddDoubleRow("", chkPeriodicX,
             "", chkPeriodicY, 22)
        AddRowControl(chkFullSeamCells, 22)
        ' ===== STYLE =====
        curSection = NewSection("STYLE", True)
        AddRowTitle("Cell Style")
        AddRowControl(cmbStyle)
        AddDoubleRow("Vertex Mode", cmbVertexMode,
             "Vertex Size", numVertexTrim)
        AddDoubleRow("Inner Offset", numInnerOffset,
             "Curve Width", numCurveWidth)
        AddRowTitle("Domain Color")
        AddRowControl(cmbDomainColor)
        AddRowControl(chkRandomRotation, 22)

        ' ===== DISPLAY =====
        curSection = NewSection("DISPLAY", False)
        AddRowControl(chkFill, 22)
        AddRowControl(chkFillSymbols, 22)
        AddRowControl(chkDomainFill, 22)
        AddRowControl(chkOuter, 22)
        AddRowControl(chkSeeds, 22)
        AddRowControl(chkInner, 22)


        ' ===== SELECTED CELL =====
        curSection = NewSection("SELECTED CELL", True)
        AddRowControl(lblSelInfo, 18)
        AddDoubleRow("Scale", selScale,
             "Rotation", selRotation)
        AddDoubleRow("Symbol Offset", selSymbolOffset,
             "", chkSelPinned)
        AddDoubleRow("Color", selColor,
             "", Nothing)


        chkDarkTheme.Text = "Dark theme"
    End Sub

    ' Toolbar orizzontale: tutte le azioni (pulsanti), raggruppate.
    ' La sidebar resta ai soli parametri (combo, slider, checkbox).
    Private Sub BuildTopBar()
        topBar.Dock = DockStyle.Top
        topBar.Height = 64
        topBar.BackColor = UiTheme.BgSidebar
        AddHandler topBar.Paint, AddressOf TopBar_Paint
        AddHandler topBar.Resize, AddressOf TopBar_Resize

        ' Etichette compatte per stare in toolbar (l'Help spiega i dettagli).
        btnReadSketchProfile.Text = "Read Sketch"
        btnReadBlockDefaultView.Text = "Read SE Blocks"
        btnLoadBlocks.Text = "Load"
        btnSaveBlocks.Text = "Save"
        btnClearBlocks.Text = "Clear"
        btnBlockLibrary.Text = "Library"
        btnExportSvg.Text = "SVG"
        btnExportDxf.Text = "DXF"
        btnExportPng.Text = "PNG"

        topBarGroupLabels.Clear()
        topBarSeparators.Clear()

        btnProjNew.Text = "New"
        btnProjOpen.Text = "Open"
        btnProjSave.Text = "Save"

        Dim x As Integer = 12
        x = AddTopBarGroup(x, "PROJECT",
                           {btnProjNew, btnProjOpen, btnProjSave},
                           {52, 56, 56})
        topBarSeparators.Add(x)
        x += 15

        x = AddTopBarGroup(x, "GENERATE",
                           {btnGenerate, btnShuffle},
                           {86, 82})
        topBarSeparators.Add(x)
        x += 15

        x = AddTopBarGroup(x, "SKETCH && BLOCKS",
                           {btnReadSketchProfile, btnReadBlockDefaultView, btnLoadBlocks, btnSaveBlocks, btnClearBlocks, btnBlockLibrary},
                           {100, 110, 52, 52, 52, 66})
        topBarSeparators.Add(x)
        x += 15

        x = AddTopBarGroup(x, "EXPORT",
                           {btnExportSvg, btnExportDxf, btnExportPng, btnToSolidEdge},
                           {50, 50, 50, 106})

        ' Modalita' "a blocchi": parametro contestuale a To Solid Edge,
        ' quindi vive accanto al pulsante invece che nella sidebar.
        chkExportAsBlocks.Text = "As blocks"
        chkExportAsBlocks.Width = 84
        chkExportAsBlocks.Height = 22
        chkExportAsBlocks.Location = New Point(x, 10)
        topBar.Controls.Add(chkExportAsBlocks)

        chkExportProfile.Text = "Profile"
        chkExportProfile.Width = 84
        chkExportProfile.Height = 22
        chkExportProfile.Location = New Point(x, 34)
        topBar.Controls.Add(chkExportProfile)

        x += chkExportAsBlocks.Width + 8

        ' Lato destro (riposizionato dal Resize): toggle tema + Help.
        chkDarkTheme.Width = 100
        chkDarkTheme.Height = 22
        topBar.Controls.Add(chkDarkTheme)

        btnHelp.Width = 60
        btnHelp.Height = 30
        topBar.Controls.Add(btnHelp)

        TopBar_Resize(topBar, EventArgs.Empty)
    End Sub

    Private Function AddTopBarGroup(startX As Integer,
                                    label As String,
                                    buttons As ThemedButton(),
                                    widths As Integer()) As Integer
        topBarGroupLabels.Add(New KeyValuePair(Of Integer, String)(startX, label.Replace("&&", "&")))

        Dim x As Integer = startX
        For i As Integer = 0 To buttons.Length - 1
            buttons(i).Width = widths(i)
            buttons(i).Height = 30
            buttons(i).Location = New Point(x, 26)
            topBar.Controls.Add(buttons(i))
            x += widths(i) + 8
        Next

        Return x + 6
    End Function

    Private Sub TopBar_Resize(sender As Object, e As EventArgs)
        btnHelp.Location = New Point(topBar.Width - btnHelp.Width - 12, 26)
        chkDarkTheme.Location = New Point(btnHelp.Left - chkDarkTheme.Width - 12, 30)
    End Sub

    Private Sub TopBar_Paint(sender As Object, e As PaintEventArgs)
        ' Linea di chiusura in basso.
        Using pn As New Pen(UiTheme.Border, 1.0F)
            e.Graphics.DrawLine(pn, 0, topBar.Height - 1, topBar.Width, topBar.Height - 1)
        End Using

        ' Etichette dei gruppi in maiuscoletto attenuato.
        Using f As New Font("Segoe UI", 7.0F, FontStyle.Bold)
            Using br As New SolidBrush(UiTheme.TxtDim)
                For Each kv In topBarGroupLabels
                    TextRenderer.DrawText(e.Graphics, kv.Value, f,
                                          New Point(kv.Key, 6), UiTheme.TxtDim)
                Next
            End Using
        End Using

        ' Separatori verticali tra i gruppi + prima del lato destro.
        Using pn As New Pen(UiTheme.Border, 1.0F)
            For Each sx In topBarSeparators
                e.Graphics.DrawLine(pn, sx, 12, sx, topBar.Height - 10)
            Next
            Dim rightSep As Integer = chkDarkTheme.Left - 14
            e.Graphics.DrawLine(pn, rightSep, 12, rightSep, topBar.Height - 10)
        End Using
    End Sub

    Private Function NewSection(title As String, startOpen As Boolean) As CollapsibleSection
        ' Lo stato salvato nell'ultima sessione prevale sul default.
        Dim savedOpen As Boolean
        If sectionStatesFromSettings.TryGetValue(title, savedOpen) Then
            startOpen = savedOpen
        End If

        Dim sec As New CollapsibleSection(title, startOpen)
        sideLayout.Controls.Add(sec)
        Return sec
    End Function

    Private Sub UpdateSidebarScroll()
        Dim vp As Integer = sideViewport.ClientSize.Height
        Dim ch As Integer = sideLayout.Height

        If ch > vp AndAlso vp > 0 Then
            sideScroll.ContentSize = ch
            sideScroll.ViewportSize = vp
            sideScroll.Visible = True
            sideLayout.Top = -sideScroll.Value
        Else
            sideScroll.Visible = False
            sideScroll.Value = 0
            sideLayout.Top = 0
        End If
    End Sub

    ' Rotella del mouse sopra la sidebar: se il cursore e' su un controllo che
    ' la gestisce (combo, slider, campo numerico) la inoltra a lui, altrimenti
    ' scorre la sidebar. In ogni caso la consuma (non deve arrivare al canvas).
    Public Function PreFilterMessage(ByRef m As Message) As Boolean Implements IMessageFilter.PreFilterMessage
        Const WM_MOUSEWHEEL As Integer = &H20A
        If m.Msg <> WM_MOUSEWHEEL Then Return False

        Dim pos As Point = Control.MousePosition
        If Not sidebar.RectangleToScreen(sidebar.ClientRectangle).Contains(pos) Then Return False

        ' High word di WParam = delta rotella come Int16: la si reinterpreta in
        ' complemento a due a mano (CShort su 0xFF88 andrebbe in overflow).
        Dim raw As Integer = CInt((m.WParam.ToInt64() >> 16) And &HFFFF&)
        If raw >= &H8000 Then raw -= &H10000
        Dim delta As Integer = raw

        ' Trova il controllo piu' profondo sotto il cursore.
        Dim ctl As Control = sidebar
        Do
            Dim child = ctl.GetChildAtPoint(ctl.PointToClient(pos), GetChildAtPointSkip.Invisible)
            If child Is Nothing Then Exit Do
            ctl = child
        Loop

        ' Se e' (o sta dentro) un controllo che gestisce la rotella, inoltra.
        Dim cur As Control = ctl
        While cur IsNot Nothing AndAlso cur IsNot sidebar
            If TypeOf cur Is ThemedComboBox Then
                DirectCast(cur, ThemedComboBox).PerformWheel(delta)
                Return True
            ElseIf TypeOf cur Is ThemedSlider Then
                DirectCast(cur, ThemedSlider).PerformWheel(delta)
                Return True
            ElseIf TypeOf cur Is ThemedNumericUpDown Then
                DirectCast(cur, ThemedNumericUpDown).PerformWheel(delta)
                Return True
            End If
            cur = cur.Parent
        End While

        ' Altrimenti scorre la sidebar (se la barra e' visibile).
        If sideScroll.Visible Then
            sideScroll.Value -= Math.Sign(delta) * 60
        End If
        Return True
    End Function

    ' ===== Tema scuro =====

    Private Sub ApplyDarkTheme()
        StyleButton(btnGenerate, True)
        StyleButton(btnToSolidEdge, True)
        StyleButton(btnShuffle, False)
        StyleButton(btnReadSketchProfile, False)
        StyleButton(btnReadBlockDefaultView, False)
        StyleButton(btnLoadBlocks, False)
        StyleButton(btnSaveBlocks, False)
        StyleButton(btnClearBlocks, False)
        StyleButton(btnBlockLibrary, False)
        StyleButton(btnExportSvg, False)
        StyleButton(btnExportDxf, False)
        StyleButton(btnExportPng, False)
        StyleButton(btnHelp, False)
        StyleButton(btnProjNew, False)
        StyleButton(btnProjOpen, False)
        StyleButton(btnProjSave, False)

        lblSelInfo.ForeColor = UiTheme.TxtDim

        For Each chk In New CheckBox() {chkFill, chkFillSymbols, chkDomainFill, chkOuter, chkSeeds, chkInner, chkRandomRotation, chkExportAsBlocks, chkExportProfile, chkDarkTheme, chkSelPinned, chkPeriodicX, chkPeriodicY, chkFullSeamCells}
            chk.ForeColor = UiTheme.Txt
            chk.BackColor = UiTheme.BgSidebar
        Next
    End Sub

    Private Sub ThemeToggle_Changed(sender As Object, e As EventArgs)
        ApplyFullTheme(chkDarkTheme.Checked)
    End Sub

    ' Commuta la palette e riapplica i colori a tutta la UI costruita.
    ' Il canvas resta navy in entrambi i temi (superficie di disegno).
    Private Sub ApplyFullTheme(dark As Boolean)
        UiTheme.SetTheme(dark)

        BackColor = UiTheme.BgCanvas
        canvas.BackColor = UiTheme.BgCanvas
        canvas.OutsideProfileColor = UiTheme.BgSidebar
        canvas.DomainFillColor = GetSelectedDomainColor()

        sidebar.BackColor = UiTheme.BgSidebar
        sideViewport.BackColor = UiTheme.BgSidebar
        sideLayout.BackColor = UiTheme.BgSidebar

        RethemeControlTree(sidebar)

        topBar.BackColor = UiTheme.BgSidebar
        RethemeControlTree(topBar)
        topBar.Invalidate()

        appTitleLbl.ForeColor = UiTheme.Accent

        statusBar.BackColor = UiTheme.StatusBg
        lblStatusInfo.ForeColor = UiTheme.Txt
        lblStatusCoords.ForeColor = UiTheme.TxtDim
        lblStatusReady.ForeColor = UiTheme.Txt

        ' Pulsanti, checkbox e stili derivati.
        ApplyDarkTheme()

        If IsHandleCreated Then
            UiTheme.ApplyTitleBarTheme(Handle)
        End If

        ' Finestre secondarie aperte: riallineate al tema.
        If helpForm IsNot Nothing AndAlso Not helpForm.IsDisposed Then
            helpForm.RefreshTheme()
        End If
        If blockLibForm IsNot Nothing AndAlso Not blockLibForm.IsDisposed Then
            blockLibForm.RefreshTheme()
        End If

        canvas.Invalidate()
        Refresh()
    End Sub

    ' Riapplica ricorsivamente i colori base del tema all'albero della sidebar.
    ' I figli vengono trattati prima del padre, cosi' i tipi compositi
    ' (sezioni) possono correggere per ultimi i propri elementi (header).
    Private Sub RethemeControlTree(root As Control)
        For Each c As Control In root.Controls
            RethemeControlTree(c)

            If TypeOf c Is CollapsibleSection Then
                DirectCast(c, CollapsibleSection).RefreshTheme()
            ElseIf TypeOf c Is ThemedNumericUpDown Then
                DirectCast(c, ThemedNumericUpDown).RefreshTheme()
            ElseIf TypeOf c Is ThemedSlider OrElse TypeOf c Is ThemedVScrollBar Then
                c.BackColor = UiTheme.BgSidebar
            ElseIf TypeOf c Is Label Then
                c.ForeColor = UiTheme.TxtDim
                c.BackColor = UiTheme.BgSidebar
            ElseIf TypeOf c Is TableLayoutPanel OrElse TypeOf c Is FlowLayoutPanel OrElse TypeOf c Is Panel Then
                c.BackColor = UiTheme.BgSidebar
            End If
        Next
    End Sub

    Private Sub StyleButton(btn As Button, primary As Boolean)
        btn.FlatStyle = FlatStyle.Flat
        btn.UseVisualStyleBackColor = False
        btn.Cursor = Cursors.Hand
        If primary Then
            btn.BackColor = UiTheme.Accent
            btn.ForeColor = UiTheme.BgCanvas
            btn.Font = New Font("Segoe UI", 9.0F, FontStyle.Bold)
            btn.FlatAppearance.BorderSize = 0
            btn.FlatAppearance.MouseOverBackColor = UiTheme.AccentHi
            btn.FlatAppearance.MouseDownBackColor = Color.FromArgb(0, 150, 170)
        Else
            btn.BackColor = UiTheme.BgField
            btn.ForeColor = UiTheme.Txt
            btn.FlatAppearance.BorderColor = UiTheme.Border
            btn.FlatAppearance.BorderSize = 1
            btn.FlatAppearance.MouseOverBackColor = UiTheme.BgFieldHi
            btn.FlatAppearance.MouseDownBackColor = UiTheme.Border
        End If
    End Sub

    ' ===== Status bar =====

    Private Sub BuildStatusBar()
        statusBar.Dock = DockStyle.Bottom
        statusBar.Height = 28
        statusBar.BackColor = UiTheme.StatusBg

        lblStatusInfo.AutoSize = True
        lblStatusInfo.ForeColor = UiTheme.Txt
        lblStatusInfo.Font = New Font("Segoe UI", 8.5F)
        lblStatusInfo.Location = New Point(12, 6)
        lblStatusInfo.Text = ""

        lblStatusCoords.AutoSize = True
        lblStatusCoords.ForeColor = UiTheme.TxtDim
        lblStatusCoords.Font = New Font("Segoe UI", 8.5F)
        lblStatusCoords.Text = ""

        lblStatusReady.AutoSize = True
        lblStatusReady.ForeColor = UiTheme.Txt
        lblStatusReady.Font = New Font("Segoe UI", 8.5F)
        lblStatusReady.Text = "Ready"

        lblStatusDot.AutoSize = True
        lblStatusDot.ForeColor = Color.FromArgb(0, 200, 83)
        lblStatusDot.Font = New Font("Segoe UI", 8.5F)
        lblStatusDot.Text = ChrW(&H25CF)

        statusBar.Controls.Add(lblStatusInfo)
        statusBar.Controls.Add(lblStatusCoords)
        statusBar.Controls.Add(lblStatusDot)
        statusBar.Controls.Add(lblStatusReady)

        AddHandler statusBar.Resize, Sub() PositionStatusLabels()
    End Sub

    Private Sub UpdateStatusBar()
        Dim cellCount As Integer = If(canvas.Cells Is Nothing, 0, canvas.Cells.Count)
        Dim seedCount As Integer = If(currentSeeds Is Nothing, 0, currentSeeds.Count)
        Dim blockCount As Integer = If(currentBlockSymbols Is Nothing, 0, currentBlockSymbols.Count)

        Dim domainTxt As String
        If useSketchDomains AndAlso currentSketchDomains IsNot Nothing AndAlso currentSketchDomains.Count > 0 Then
            domainTxt = "Sketch (" & currentSketchDomains.Count & " region/s)"
        Else
            domainTxt = "Rect " & CInt(domain.Width) & " × " & CInt(domain.Height)
        End If

        Dim sep As String = "   " & ChrW(&H2502) & "   "
        lblStatusInfo.Text = "Cells: " & cellCount & sep &
                             "Seeds: " & seedCount & sep &
                             "Domain: " & domainTxt & sep &
                             "Blocks in memory: " & blockCount

        PositionStatusLabels()
    End Sub

    ' Le coordinate seguono in coda ai testi informativi di sinistra.
    Private Sub PositionStatusLabels()
        lblStatusCoords.Location = New Point(lblStatusInfo.Right + 30, 6)
        lblStatusReady.Location = New Point(statusBar.Width - lblStatusReady.Width - 14, 6)
        lblStatusDot.Location = New Point(lblStatusReady.Left - lblStatusDot.Width - 2, 6)
    End Sub

    Private Sub Canvas_WorldCursorMoved(sender As Object, e As EventArgs)
        lblStatusCoords.Text = "X: " & canvas.LastWorldCursorX.ToString("0.0") &
                               "   Y: " & canvas.LastWorldCursorY.ToString("0.0")
        lblStatusCoords.Location = New Point(lblStatusInfo.Right + 30, 6)
    End Sub


    ' ===== Chrome scuro (titolo finestra + scrollbar) =====

    Protected Overrides Sub OnLoad(e As EventArgs)
        MyBase.OnLoad(e)
        UiTheme.ApplyTitleBarTheme(Handle)
    End Sub

    Protected Overrides Sub OnFormClosing(e As FormClosingEventArgs)
        MyBase.OnFormClosing(e)
        If e.Cancel Then Return

        If projectDirty Then
            Dim r = MessageBox.Show("Save changes to the project before closing?",
                                    "SE-Voronoi", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question)
            If r = DialogResult.Cancel Then
                e.Cancel = True
            ElseIf r = DialogResult.Yes Then
                If Not SaveProjectInteractive() Then e.Cancel = True
            End If
        End If
    End Sub

    Protected Overrides Sub OnFormClosed(e As FormClosedEventArgs)
        SaveUserSettings()
        Application.RemoveMessageFilter(Me)
        MyBase.OnFormClosed(e)
    End Sub

    ' ===== Persistenza impostazioni utente (%AppData%\SE-Voronoi\settings.txt) =====

    ' ===== File di progetto (.sevproj) =====

    Private Sub UpdateFormTitle()
        Dim name As String = If(String.IsNullOrEmpty(projectPath), "Untitled",
                                IO.Path.GetFileNameWithoutExtension(projectPath))
        Text = "SE-Voronoi - " & name & If(projectDirty, " *", "")
    End Sub

    Private Sub MarkProjectDirty()
        If loadingProject Then Return
        If Not projectDirty Then
            projectDirty = True
            UpdateFormTitle()
        End If
    End Sub

    Private Sub ExportAsBlocks_Changed(sender As Object, e As EventArgs)
        MarkProjectDirty()
    End Sub

    ' True = si puo' procedere (salvato, scartato o niente da salvare).
    Private Function ConfirmLoseChanges() As Boolean
        If Not projectDirty Then Return True

        Dim r = MessageBox.Show("Save changes to the current project?",
                                "SE-Voronoi", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question)
        If r = DialogResult.Cancel Then Return False
        If r = DialogResult.Yes Then Return SaveProjectInteractive()
        Return True
    End Function

    Private Function SaveProjectInteractive() As Boolean
        Dim path As String = projectPath

        If String.IsNullOrEmpty(path) Then
            Using dlg As New SaveFileDialog()
                dlg.Title = "Save project"
                dlg.Filter = "SE-Voronoi project (*.sevproj)|*.sevproj"
                dlg.DefaultExt = "sevproj"
                dlg.AddExtension = True
                dlg.FileName = "voronoi.sevproj"
                If dlg.ShowDialog(Me) <> DialogResult.OK Then Return False
                path = dlg.FileName
            End Using
        End If

        Try
            SaveProject(path)
            projectPath = path
            projectDirty = False
            UpdateFormTitle()
            Return True
        Catch ex As Exception
            MessageBox.Show("Error saving project:" & Environment.NewLine & ex.Message,
                            "SE-Voronoi", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Return False
        End Try
    End Function

    Private Sub ProjectSave_Click(sender As Object, e As EventArgs)
        SaveProjectInteractive()
    End Sub

    Private Sub ProjectNew_Click(sender As Object, e As EventArgs)
        If Not ConfirmLoseChanges() Then Return

        loadingProject = True
        Try
            InstallDefaultRectangularProfile()

            currentBlockSymbols = New List(Of BlockDefinition)()
            canvas.BlockSymbols = currentBlockSymbols
            If blockLibForm IsNot Nothing AndAlso Not blockLibForm.IsDisposed Then
                blockLibForm.SetBlocks(currentBlockSymbols)
            End If

            canvas.SelectedSeedIndex = -1
            canvas.ResetView()

            GenerateRandomDiagram(Nothing, EventArgs.Empty)
        Finally
            loadingProject = False
        End Try

        projectPath = Nothing
        projectDirty = False
        UpdateFormTitle()
    End Sub

    Private Sub ProjectOpen_Click(sender As Object, e As EventArgs)
        If Not ConfirmLoseChanges() Then Return

        Using dlg As New OpenFileDialog()
            dlg.Title = "Open project"
            dlg.Filter = "SE-Voronoi project (*.sevproj)|*.sevproj|All files (*.*)|*.*"
            If dlg.ShowDialog(Me) <> DialogResult.OK Then Return

            Try
                LoadProject(dlg.FileName)
            Catch ex As Exception
                MessageBox.Show("Error opening project:" & Environment.NewLine & ex.Message,
                                "SE-Voronoi", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
        End Using
    End Sub

    Private Function DInv(v As Double) As String
        Return v.ToString("R", Globalization.CultureInfo.InvariantCulture)
    End Function

    Private Function PtsToString(pts As List(Of Vec2)) As String
        Return String.Join(";", pts.Select(Function(p) DInv(p.X) & " " & DInv(p.Y)))
    End Function

    Private Function ParsePts(sVal As String) As List(Of Vec2)
        Dim result As New List(Of Vec2)
        For Each part In sVal.Split(";"c)
            Dim xy = part.Trim().Split(" "c)
            Dim x, y As Double
            If xy.Length = 2 AndAlso
               Double.TryParse(xy(0), Globalization.NumberStyles.Float, Globalization.CultureInfo.InvariantCulture, x) AndAlso
               Double.TryParse(xy(1), Globalization.NumberStyles.Float, Globalization.CultureInfo.InvariantCulture, y) Then
                result.Add(New Vec2(x, y))
            End If
        Next
        Return result
    End Function

    Private Sub SaveProject(path As String)
        EnsureSeedStyleKeyCount(currentSeeds.Count, CInt(numSeed.Value))
        EnsureSeedCellScaleCount(currentSeeds.Count, CSng(numCellScale.Value))
        EnsureSeedCellRotationCount(currentSeeds.Count)
        EnsureSeedCellSymbolOffsetCount(currentSeeds.Count)
        EnsureSeedPinnedCount(currentSeeds.Count)
        EnsureSeedCellColorCount(currentSeeds.Count)

        Dim lines As New List(Of String)
        lines.Add("#SEVPROJ 1")

        lines.Add("[SETTINGS]")
        lines.AddRange(BuildSettingsLines())

        lines.Add("[SEEDS]")
        For i As Integer = 0 To currentSeeds.Count - 1
            lines.Add(DInv(currentSeeds(i).X) & ";" & DInv(currentSeeds(i).Y) & ";" &
                      currentSeedStyleKeys(i).ToString(Globalization.CultureInfo.InvariantCulture) & ";" &
                      currentSeedCellScales(i).ToString("R", Globalization.CultureInfo.InvariantCulture) & ";" &
                      currentSeedCellRotations(i).ToString("R", Globalization.CultureInfo.InvariantCulture) & ";" &
                      currentSeedCellSymbolOffsets(i).ToString(Globalization.CultureInfo.InvariantCulture) & ";" &
                      currentSeedPinned(i).ToString() & ";" &
                      If(i < currentSeedCellColors.Count, currentSeedCellColors(i), -1).ToString(Globalization.CultureInfo.InvariantCulture))
        Next

        lines.Add("[SKETCH]")
        lines.Add("UseSketch=" & useSketchDomains.ToString())
        lines.Add("Domain=" & currentWorldDomain.Left.ToString("R", Globalization.CultureInfo.InvariantCulture) & ";" &
                              currentWorldDomain.Top.ToString("R", Globalization.CultureInfo.InvariantCulture) & ";" &
                              currentWorldDomain.Width.ToString("R", Globalization.CultureInfo.InvariantCulture) & ";" &
                              currentWorldDomain.Height.ToString("R", Globalization.CultureInfo.InvariantCulture))

        For i As Integer = 0 To canvas.SketchBoundaries.Count - 1
            Dim isHole As Boolean = canvas.SketchBoundaryIsHole IsNot Nothing AndAlso
                                    i < canvas.SketchBoundaryIsHole.Count AndAlso
                                    canvas.SketchBoundaryIsHole(i)
            lines.Add("LOOP;" & If(isHole, "H", "O") & ";" & PtsToString(canvas.SketchBoundaries(i)))
        Next

        For Each d In currentSketchDomains
            lines.Add("REGION_O;" & PtsToString(d.Outer))
            For Each h In d.Holes
                lines.Add("REGION_H;" & PtsToString(h))
            Next
        Next

        If currentBlockSymbols IsNot Nothing AndAlso currentBlockSymbols.Count > 0 Then
            lines.Add("[BLOCKS]")
            Dim tmp As String = IO.Path.GetTempFileName()
            Try
                ExportGeometry.SaveBlocksToFile(tmp, currentBlockSymbols)
                lines.AddRange(File.ReadAllLines(tmp))
            Finally
                Try
                    File.Delete(tmp)
                Catch
                End Try
            End Try
        End If

        File.WriteAllLines(path, lines)
    End Sub

    Private Sub LoadProject(path As String)
        Dim all() As String = File.ReadAllLines(path)

        ' Il payload blocchi (formato .sevb) e' tutto cio' che segue [BLOCKS].
        Dim blockStart As Integer = -1
        For i As Integer = 0 To all.Length - 1
            If all(i).Trim() = "[BLOCKS]" Then
                blockStart = i
                Exit For
            End If
        Next

        Dim mainCount As Integer = If(blockStart >= 0, blockStart, all.Length)

        Dim settingsMap As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)
        Dim seedLines As New List(Of String)
        Dim useSketch As Boolean = False
        Dim domRect As RectangleF = domain
        Dim loopPts As New List(Of List(Of Vec2))
        Dim loopHole As New List(Of Boolean)
        Dim regions As New List(Of SketchDomainRegion)

        Dim mode As String = ""
        For i As Integer = 0 To mainCount - 1
            Dim ln As String = all(i).Trim()
            If ln.Length = 0 OrElse ln.StartsWith("#") Then Continue For

            If ln = "[SETTINGS]" OrElse ln = "[SEEDS]" OrElse ln = "[SKETCH]" Then
                mode = ln
                Continue For
            End If

            Select Case mode
                Case "[SETTINGS]"
                    Dim k As Integer = ln.IndexOf("="c)
                    If k > 0 Then settingsMap(ln.Substring(0, k).Trim()) = ln.Substring(k + 1).Trim()

                Case "[SEEDS]"
                    seedLines.Add(ln)

                Case "[SKETCH]"
                    If ln.StartsWith("UseSketch=", StringComparison.OrdinalIgnoreCase) Then
                        Boolean.TryParse(ln.Substring(10), useSketch)
                    ElseIf ln.StartsWith("Domain=", StringComparison.OrdinalIgnoreCase) Then
                        Dim parts = ln.Substring(7).Split(";"c)
                        Dim l, t, w, h As Single
                        If parts.Length = 4 AndAlso
                           Single.TryParse(parts(0), Globalization.NumberStyles.Float, Globalization.CultureInfo.InvariantCulture, l) AndAlso
                           Single.TryParse(parts(1), Globalization.NumberStyles.Float, Globalization.CultureInfo.InvariantCulture, t) AndAlso
                           Single.TryParse(parts(2), Globalization.NumberStyles.Float, Globalization.CultureInfo.InvariantCulture, w) AndAlso
                           Single.TryParse(parts(3), Globalization.NumberStyles.Float, Globalization.CultureInfo.InvariantCulture, h) Then
                            domRect = New RectangleF(l, t, w, h)
                        End If
                    ElseIf ln.StartsWith("LOOP;") Then
                        Dim parts = ln.Split(New Char() {";"c}, 3)
                        If parts.Length = 3 Then
                            loopHole.Add(parts(1) = "H")
                            loopPts.Add(ParsePts(parts(2)))
                        End If
                    ElseIf ln.StartsWith("REGION_O;") Then
                        Dim r As New SketchDomainRegion()
                        r.Outer = ParsePts(ln.Substring(9))
                        r.Bounds = Geo2D.GetBounds(r.Outer)
                        regions.Add(r)
                    ElseIf ln.StartsWith("REGION_H;") Then
                        If regions.Count > 0 Then
                            regions(regions.Count - 1).Holes.Add(ParsePts(ln.Substring(9)))
                        End If
                    End If
            End Select
        Next

        loadingProject = True
        Try
            ApplySettingsMap(settingsMap)

            ' Blocchi: payload .sevb via file temporaneo.
            If blockStart >= 0 AndAlso blockStart < all.Length - 1 Then
                Dim tmp As String = IO.Path.GetTempFileName()
                Try
                    File.WriteAllLines(tmp, all.Skip(blockStart + 1))
                    Dim loaded = ExportGeometry.LoadBlocksFromFile(tmp)
                    currentBlockSymbols = If(loaded, New List(Of BlockDefinition)())
                Finally
                    Try
                        File.Delete(tmp)
                    Catch
                    End Try
                End Try
            Else
                currentBlockSymbols = New List(Of BlockDefinition)()
            End If

            canvas.BlockSymbols = currentBlockSymbols
            If blockLibForm IsNot Nothing AndAlso Not blockLibForm.IsDisposed Then
                blockLibForm.SetBlocks(currentBlockSymbols)
            End If

            ' Profilo sketch.
            currentSketchBoundaries = loopPts
            currentSketchDomains = regions
            canvas.SketchBoundaries = New List(Of List(Of Vec2))(loopPts)
            canvas.SketchBoundaryIsHole = New List(Of Boolean)(loopHole)
            canvas.SketchDomains = ConvertToCanvasDomains(currentSketchDomains)
            useSketchDomains = useSketch AndAlso currentSketchDomains.Count > 0
            canvas.ConstrainSeedsToSketchDomains = useSketchDomains
            canvas.ShowSketchBoundary = currentSketchBoundaries.Count > 0
            lockSketchViewDomain = useSketchDomains
            currentWorldDomain = domRect
            canvas.Domain = currentWorldDomain

            ' Progetti senza profilo (o pre-esistenti): rettangolo editabile.
            If Not useSketchDomains Then
                InstallDefaultRectangularProfile()
            End If

            ' Semi con le loro proprieta' per-cella: layout esatto, nessuna rigenerazione.
            currentSeeds = New List(Of Vec2)()
            currentSeedStyleKeys = New List(Of Integer)()
            currentSeedCellScales = New List(Of Single)()
            currentSeedCellRotations = New List(Of Single)()
            currentSeedCellSymbolOffsets = New List(Of Integer)()
            currentSeedPinned = New List(Of Boolean)()
            currentSeedCellColors = New List(Of Integer)()

            For Each ln In seedLines
                Dim p = ln.Split(";"c)
                If p.Length < 7 Then Continue For

                Dim x, y As Double
                Dim key, off As Integer
                Dim sc, rot As Single
                Dim pin As Boolean

                If Double.TryParse(p(0), Globalization.NumberStyles.Float, Globalization.CultureInfo.InvariantCulture, x) AndAlso
                   Double.TryParse(p(1), Globalization.NumberStyles.Float, Globalization.CultureInfo.InvariantCulture, y) AndAlso
                   Integer.TryParse(p(2), Globalization.NumberStyles.Integer, Globalization.CultureInfo.InvariantCulture, key) AndAlso
                   Single.TryParse(p(3), Globalization.NumberStyles.Float, Globalization.CultureInfo.InvariantCulture, sc) AndAlso
                   Single.TryParse(p(4), Globalization.NumberStyles.Float, Globalization.CultureInfo.InvariantCulture, rot) AndAlso
                   Integer.TryParse(p(5), Globalization.NumberStyles.Integer, Globalization.CultureInfo.InvariantCulture, off) AndAlso
                   Boolean.TryParse(p(6), pin) Then

                    Dim colIdx As Integer = -1
                    If p.Length >= 8 Then
                        Integer.TryParse(p(7), Globalization.NumberStyles.Integer, Globalization.CultureInfo.InvariantCulture, colIdx)
                    End If

                    currentSeeds.Add(New Vec2(x, y))
                    currentSeedStyleKeys.Add(key)
                    currentSeedCellScales.Add(sc)
                    currentSeedCellRotations.Add(rot)
                    currentSeedCellSymbolOffsets.Add(off)
                    currentSeedPinned.Add(pin)
                    currentSeedCellColors.Add(colIdx)
                End If
            Next

            canvas.SelectedSeedIndex = -1
            BuildFromCurrentSeeds()
            canvas.ResetView()

        Finally
            loadingProject = False
        End Try

        projectPath = path
        projectDirty = False
        UpdateFormTitle()
    End Sub

    Private Function SettingsPath() As String
        Dim dir As String = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SE-Voronoi")
        Directory.CreateDirectory(dir)
        Return Path.Combine(dir, "settings.txt")
    End Function

    Private Function FInv(v As Decimal) As String
        Return v.ToString(Globalization.CultureInfo.InvariantCulture)
    End Function

    Private Sub ApplySettingsMap(map As Dictionary(Of String, String))
        Dim sVal As String = Nothing

        If map.TryGetValue("CellStyle", sVal) Then cmbStyle.SelectedItem = sVal
        If map.TryGetValue("SeedMode", sVal) Then cmbSeedMode.SelectedItem = sVal

        Dim iVal As Integer
        If map.TryGetValue("VertexModeIndex", sVal) AndAlso Integer.TryParse(sVal, iVal) Then
            cmbVertexMode.SelectedIndex = iVal
        End If

        Dim dVal As Decimal
        If map.TryGetValue("CellCount", sVal) AndAlso Decimal.TryParse(sVal, Globalization.NumberStyles.Number, Globalization.CultureInfo.InvariantCulture, dVal) Then numCells.Value = dVal
        If map.TryGetValue("RandomSeed", sVal) AndAlso Decimal.TryParse(sVal, Globalization.NumberStyles.Number, Globalization.CultureInfo.InvariantCulture, dVal) Then numSeed.Value = dVal
        If map.TryGetValue("Relax", sVal) AndAlso Decimal.TryParse(sVal, Globalization.NumberStyles.Number, Globalization.CultureInfo.InvariantCulture, dVal) Then numRelax.Value = dVal
        If map.TryGetValue("CellScale", sVal) AndAlso Decimal.TryParse(sVal, Globalization.NumberStyles.Number, Globalization.CultureInfo.InvariantCulture, dVal) Then numCellScale.Value = dVal
        If map.TryGetValue("InnerOffset", sVal) AndAlso Decimal.TryParse(sVal, Globalization.NumberStyles.Number, Globalization.CultureInfo.InvariantCulture, dVal) Then numInnerOffset.Value = dVal
        If map.TryGetValue("VertexTrim", sVal) AndAlso Decimal.TryParse(sVal, Globalization.NumberStyles.Number, Globalization.CultureInfo.InvariantCulture, dVal) Then numVertexTrim.Value = dVal
        If map.TryGetValue("CurveWidth", sVal) AndAlso Decimal.TryParse(sVal, Globalization.NumberStyles.Number, Globalization.CultureInfo.InvariantCulture, dVal) Then numCurveWidth.Value = dVal

        Dim bVal As Boolean
        If map.TryGetValue("FillCells", sVal) AndAlso Boolean.TryParse(sVal, bVal) Then chkFill.Checked = bVal
        If map.TryGetValue("FillSymbols", sVal) AndAlso Boolean.TryParse(sVal, bVal) Then chkFillSymbols.Checked = bVal
        If map.TryGetValue("DomainFill", sVal) AndAlso Boolean.TryParse(sVal, bVal) Then chkDomainFill.Checked = bVal
        If map.TryGetValue("DomainColor", sVal) AndAlso cmbDomainColor.Items.Contains(sVal) Then cmbDomainColor.SelectedItem = sVal
        If map.TryGetValue("RandomRotation", sVal) AndAlso Boolean.TryParse(sVal, bVal) Then chkRandomRotation.Checked = bVal
        If map.TryGetValue("ShowOuter", sVal) AndAlso Boolean.TryParse(sVal, bVal) Then chkOuter.Checked = bVal
        If map.TryGetValue("ShowSeeds", sVal) AndAlso Boolean.TryParse(sVal, bVal) Then chkSeeds.Checked = bVal
        If map.TryGetValue("ShowInner", sVal) AndAlso Boolean.TryParse(sVal, bVal) Then chkInner.Checked = bVal
        If map.TryGetValue("ExportAsBlocks", sVal) AndAlso Boolean.TryParse(sVal, bVal) Then chkExportAsBlocks.Checked = bVal
        If map.TryGetValue("ExportProfile", sVal) AndAlso Boolean.TryParse(sVal, bVal) Then chkExportProfile.Checked = bVal
        If map.TryGetValue("PeriodicX", sVal) AndAlso Boolean.TryParse(sVal, bVal) Then chkPeriodicX.Checked = bVal
        If map.TryGetValue("PeriodicY", sVal) AndAlso Boolean.TryParse(sVal, bVal) Then chkPeriodicY.Checked = bVal
        If map.TryGetValue("FullSeamCells", sVal) AndAlso Boolean.TryParse(sVal, bVal) Then chkFullSeamCells.Checked = bVal
    End Sub

    ' Impostazioni condivise tra settings utente e file di progetto.
    Private Function BuildSettingsLines() As List(Of String)
        Return New List(Of String) From {
            "CellStyle=" & If(cmbStyle.SelectedItem Is Nothing, "", cmbStyle.SelectedItem.ToString()),
            "SeedMode=" & If(cmbSeedMode.SelectedItem Is Nothing, "", cmbSeedMode.SelectedItem.ToString()),
            "VertexModeIndex=" & cmbVertexMode.SelectedIndex.ToString(Globalization.CultureInfo.InvariantCulture),
            "CellCount=" & FInv(numCells.Value),
            "RandomSeed=" & FInv(numSeed.Value),
            "Relax=" & FInv(numRelax.Value),
            "CellScale=" & FInv(numCellScale.Value),
            "InnerOffset=" & FInv(numInnerOffset.Value),
            "VertexTrim=" & FInv(numVertexTrim.Value),
            "CurveWidth=" & FInv(numCurveWidth.Value),
            "FillCells=" & chkFill.Checked.ToString(),
            "FillSymbols=" & chkFillSymbols.Checked.ToString(),
            "DomainFill=" & chkDomainFill.Checked.ToString(),
            "DomainColor=" & If(cmbDomainColor.SelectedItem IsNot Nothing, cmbDomainColor.SelectedItem.ToString(), "Theme"),
            "RandomRotation=" & chkRandomRotation.Checked.ToString(),
            "ShowOuter=" & chkOuter.Checked.ToString(),
            "ShowSeeds=" & chkSeeds.Checked.ToString(),
            "ShowInner=" & chkInner.Checked.ToString(),
            "ExportAsBlocks=" & chkExportAsBlocks.Checked.ToString(),
            "ExportProfile=" & chkExportProfile.Checked.ToString(),
            "PeriodicX=" & chkPeriodicX.Checked.ToString(),
            "PeriodicY=" & chkPeriodicY.Checked.ToString(),
            "FullSeamCells=" & chkFullSeamCells.Checked.ToString()
        }
    End Function

    Private Sub SaveUserSettings()
        Try
            Dim lines As List(Of String) = BuildSettingsLines()
            lines.Add("DarkTheme=" & chkDarkTheme.Checked.ToString())

            For Each sec In sideLayout.Controls.OfType(Of CollapsibleSection)()
                lines.Add("Section:" & sec.SectionTitle & "=" & sec.Expanded.ToString())
            Next
            File.WriteAllLines(SettingsPath(), lines)
        Catch
            ' Il salvataggio delle preferenze non deve mai bloccare la chiusura.
        End Try
    End Sub

    Private Sub LoadUserSettings()
        Try
            Dim p As String = SettingsPath()
            If Not File.Exists(p) Then Return

            Dim map As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)
            For Each ln In File.ReadAllLines(p)
                Dim k As Integer = ln.IndexOf("="c)
                If k > 0 Then map(ln.Substring(0, k).Trim()) = ln.Substring(k + 1).Trim()
            Next

            ApplySettingsMap(map)

            Dim sVal As String = Nothing
            Dim bVal As Boolean
            If map.TryGetValue("DarkTheme", sVal) AndAlso Boolean.TryParse(sVal, bVal) Then
                chkDarkTheme.Checked = bVal
            ElseIf map.TryGetValue("LightTheme", sVal) AndAlso Boolean.TryParse(sVal, bVal) Then
                ' Chiave della versione precedente: semantica invertita.
                chkDarkTheme.Checked = Not bVal
            End If

            sectionStatesFromSettings.Clear()
            For Each kv In map
                If kv.Key.StartsWith("Section:", StringComparison.OrdinalIgnoreCase) Then
                    Dim openState As Boolean
                    If Boolean.TryParse(kv.Value, openState) Then
                        sectionStatesFromSettings(kv.Key.Substring(8)) = openState
                    End If
                End If
            Next

        Catch
            ' File assente o corrotto: si parte con i default.
        End Try
    End Sub

    Private Sub AddRowTitle(text As String)
        curSection.Content.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        Dim lbl As New Label With {
            .Text = text,
            .ForeColor = UiTheme.TxtDim,
            .Font = New Font("Segoe UI", 7.5F),
            .AutoSize = False,
            .Height = 16,
            .Width = 238,
            .Margin = New Padding(3, 4, 3, 0),
            .TextAlign = ContentAlignment.BottomLeft
        }
        curSection.Content.Controls.Add(lbl)
    End Sub

    Private Sub AddRowControl(ctrl As Control, Optional forcedHeight As Integer = 28)
        curSection.Content.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        ctrl.Width = 238
        ctrl.Height = forcedHeight
        ctrl.Margin = New Padding(3, 1, 3, 3)
        curSection.Content.Controls.Add(ctrl)
    End Sub


    Private Sub AddDoubleRow(leftTitle As String,
                         leftCtrl As Control,
                         rightTitle As String,
                         rightCtrl As Control,
                         Optional controlHeight As Integer = 28)

        curSection.Content.RowStyles.Add(New RowStyle(SizeType.AutoSize))

        Dim host As New TableLayoutPanel With {
        .ColumnCount = 2,
        .RowCount = 1,
        .Width = 238,
        .Height = 16 + controlHeight + 4,
        .AutoSize = False,
        .Margin = New Padding(3, 0, 3, 3),
        .Padding = New Padding(0)
    }

        host.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50.0F))
        host.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50.0F))
        host.RowStyles.Add(New RowStyle(SizeType.Percent, 100.0F))

        Dim leftPanel = BuildLabeledControlPanel(leftTitle, leftCtrl, controlHeight)
        Dim rightPanel = BuildLabeledControlPanel(rightTitle, rightCtrl, controlHeight)

        ' Riempiono la cella; gutter centrale di 6px, bordi allineati alle righe singole.
        leftPanel.Margin = New Padding(0, 0, 3, 0)
        rightPanel.Margin = New Padding(3, 0, 0, 0)

        host.Controls.Add(leftPanel, 0, 0)
        host.Controls.Add(rightPanel, 1, 0)

        curSection.Content.Controls.Add(host)
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
        .ForeColor = UiTheme.TxtDim,
        .Font = New Font("Segoe UI", 7.5F),
        .AutoSize = False,
        .Height = 16,
        .Dock = DockStyle.Top,
        .Margin = New Padding(0, 4, 0, 0),
        .TextAlign = ContentAlignment.BottomLeft
    }

        panel.Controls.Add(lbl, 0, 0)

        ' ctrl puo' essere Nothing: cella con sola etichetta (o vuota).
        If ctrl IsNot Nothing Then
            ctrl.Dock = DockStyle.Top
            ctrl.Height = forcedHeight
            ctrl.Margin = New Padding(0)
            panel.Controls.Add(ctrl, 0, 1)
        End If

        Return panel
    End Function

    Private Sub ConfigureControls()
        cmbStyle.DropDownStyle = ComboBoxStyle.DropDownList
        cmbStyle.Items.AddRange([Enum].GetNames(GetType(CellRenderStyle)))
        cmbStyle.SelectedItem = CellRenderStyle.Curved.ToString()

        cmbSeedMode.DropDownStyle = ComboBoxStyle.DropDownList
        cmbSeedMode.Items.AddRange([Enum].GetNames(GetType(SeedPlacementMode)))
        cmbSeedMode.SelectedItem = SeedPlacementMode.Random.ToString()

        cmbVertexMode.DropDownStyle = ComboBoxStyle.DropDownList
        cmbVertexMode.Items.AddRange(New String() {"Sharp corner", "Arc fillet", "Spline curve"})
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
        numInnerOffset.Maximum = 30
        numInnerOffset.DecimalPlaces = 1
        numInnerOffset.Increment = 1D
        numInnerOffset.Value = 0D

        numVertexTrim.Minimum = 0D
        numVertexTrim.Maximum = 1D

        lblSelInfo.Text = "No cell selected - click a cell"
        lblSelInfo.AutoSize = False
        lblSelInfo.TextAlign = ContentAlignment.MiddleLeft

        selScale.Minimum = 0.05D
        selScale.Maximum = 1.5D
        selScale.DecimalPlaces = 2
        selScale.Increment = 0.02D
        selScale.Value = 0.82D
        selScale.Enabled = False

        selRotation.Minimum = 0D
        selRotation.Maximum = 359D
        selRotation.DecimalPlaces = 0
        selRotation.Increment = 1D
        selRotation.Value = 0D
        selRotation.Enabled = False

        selSymbolOffset.Minimum = -99D
        selSymbolOffset.Maximum = 99D
        selSymbolOffset.DecimalPlaces = 0
        selSymbolOffset.Increment = 1D
        selSymbolOffset.Value = 0D
        selSymbolOffset.Enabled = False

        chkDomainFill.Text = "Domain fill"
        chkDomainFill.Checked = True

        cmbDomainColor.Items.AddRange(DomainColorNames)
        cmbDomainColor.SelectedIndex = 0

        chkPeriodicX.Text = "Periodic X (cylinder)"
        chkPeriodicX.Checked = False
        chkPeriodicY.Text = "Periodic Y"
        chkPeriodicY.Checked = False
        chkFullSeamCells.Text = "Full boundary cells"
        chkFullSeamCells.Checked = False

        chkSelPinned.Text = "Pin seed"
        chkSelPinned.Enabled = False

        selColor.Items.Add("Auto")
        For Each nm In VoronoiCanvas.CellPaletteNames
            selColor.Items.Add(nm)
        Next
        selColor.SelectedIndex = 0
        selColor.Enabled = False

        chkDarkTheme.Checked = True
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
        chkFillSymbols.Checked = True
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

        btnBlockLibrary.Text = "Block Library / Preview"
        btnBlockLibrary.UseVisualStyleBackColor = False
        btnBlockLibrary.BackColor = Color.FromArgb(0, 188, 212)
        btnBlockLibrary.ForeColor = Color.FromArgb(8, 6, 53)
        btnBlockLibrary.FlatStyle = FlatStyle.Flat

        btnExportSvg.Text = "Export SVG"
        btnExportSvg.UseVisualStyleBackColor = False
        btnExportSvg.BackColor = Color.White
        btnExportSvg.ForeColor = Color.FromArgb(30, 40, 55)
        btnExportSvg.FlatStyle = FlatStyle.Flat

        btnExportDxf.Text = "Export DXF"
        btnExportPng.Text = "Export PNG"
        btnHelp.Text = "Help"
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
        If useSketchDomains AndAlso Not (PeriodicXActive() OrElse PeriodicYActive()) AndAlso
           currentSketchDomains IsNot Nothing AndAlso currentSketchDomains.Count > 0 Then
            GenerateDiagramFromSketchDomains()
            Return
        End If

        canvas.SelectedSeedIndex = -1
        MarkProjectDirty()

        ' Semi bloccati (pin): sopravvivono alla rigenerazione con le loro
        ' proprieta' per-cella e il relax non li sposta.
        Dim pinPos As New List(Of Vec2)
        Dim pinKeys As New List(Of Integer)
        Dim pinScales As New List(Of Single)
        Dim pinRotations As New List(Of Single)
        Dim pinOffsets As New List(Of Integer)
        Dim pinColors As New List(Of Integer)

        For i As Integer = 0 To currentSeeds.Count - 1
            If i < currentSeedPinned.Count AndAlso currentSeedPinned(i) Then
                pinPos.Add(currentSeeds(i))
                pinKeys.Add(If(i < currentSeedStyleKeys.Count, currentSeedStyleKeys(i), 0))
                pinScales.Add(If(i < currentSeedCellScales.Count, currentSeedCellScales(i), CSng(numCellScale.Value)))
                pinRotations.Add(If(i < currentSeedCellRotations.Count, currentSeedCellRotations(i), 0.0F))
                pinOffsets.Add(If(i < currentSeedCellSymbolOffsets.Count, currentSeedCellSymbolOffsets(i), 0))
                pinColors.Add(If(i < currentSeedCellColors.Count, currentSeedCellColors(i), -1))
            End If
        Next

        Dim freshCount As Integer = Math.Max(0, CInt(numCells.Value) - pinPos.Count)
        Dim freshSeeds = VoronoiEngine.CreateSeedsByMode(GetSeedMode(), freshCount, domain, CInt(numSeed.Value))

        currentSeeds = New List(Of Vec2)(pinPos)
        currentSeeds.AddRange(freshSeeds)

        RebuildSeedStyleKeys(currentSeeds.Count, CInt(numSeed.Value))
        RebuildSeedCellScales(currentSeeds.Count, CSng(numCellScale.Value))
        RebuildSeedCellRotations(currentSeeds.Count)
        RebuildSeedCellSymbolOffsets(currentSeeds.Count)
        RebuildSeedPinned(currentSeeds.Count)
        RebuildSeedCellColors(currentSeeds.Count)
        lastCellScale = CSng(numCellScale.Value)

        ' La testa delle liste appartiene ai semi pinnati: ripristino proprieta'.
        For k As Integer = 0 To pinPos.Count - 1
            currentSeedStyleKeys(k) = pinKeys(k)
            currentSeedCellScales(k) = pinScales(k)
            currentSeedCellRotations(k) = pinRotations(k)
            currentSeedCellSymbolOffsets(k) = pinOffsets(k)
            currentSeedPinned(k) = True
            currentSeedCellColors(k) = pinColors(k)
        Next

        For i As Integer = 1 To CInt(numRelax.Value)
            Dim tmpCells As List(Of VoronoiCell)
            If PeriodicXActive() OrElse PeriodicYActive() Then
                ' Relax periodico: centroidi su celle intere, poi wrap nel dominio.
                tmpCells = VoronoiPeriodic.BuildCellsPeriodic(currentSeeds, domain, PeriodicXActive(), PeriodicYActive(), True)
            Else
                tmpCells = VoronoiEngine.BuildCells(currentSeeds, domain)
            End If
            currentSeeds = VoronoiEngine.RelaxSeeds(tmpCells)
            VoronoiPeriodic.WrapSeedsIntoBounds(currentSeeds, domain, PeriodicXActive(), PeriodicYActive())

            ' I semi pinnati (prefisso) tornano alla loro posizione.
            For k As Integer = 0 To pinPos.Count - 1
                If k < currentSeeds.Count Then currentSeeds(k) = pinPos(k)
            Next
        Next

        EnsureSeedStyleKeyCount(currentSeeds.Count, CInt(numSeed.Value))
        EnsureSeedCellScaleCount(currentSeeds.Count, CSng(numCellScale.Value))
        EnsureSeedCellRotationCount(currentSeeds.Count)
        EnsureSeedCellSymbolOffsetCount(currentSeeds.Count)
        EnsureSeedPinnedCount(currentSeeds.Count)

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
        Dim allOffsets As New List(Of Integer)
        Dim allPinned As New List(Of Boolean)
        Dim allColors As New List(Of Integer)

        If currentSketchDomains Is Nothing OrElse currentSketchDomains.Count = 0 Then Return

        canvas.SelectedSeedIndex = -1

        ' Partiziona i semi pinnati correnti per regione di appartenenza:
        ' sopravvivono alla rigenerazione con le loro proprieta'.
        Dim pinPosR As New List(Of List(Of Vec2))
        Dim pinKeysR As New List(Of List(Of Integer))
        Dim pinScalesR As New List(Of List(Of Single))
        Dim pinRotR As New List(Of List(Of Single))
        Dim pinOffR As New List(Of List(Of Integer))
        Dim pinColR As New List(Of List(Of Integer))

        For ri As Integer = 0 To currentSketchDomains.Count - 1
            pinPosR.Add(New List(Of Vec2))
            pinKeysR.Add(New List(Of Integer))
            pinScalesR.Add(New List(Of Single))
            pinRotR.Add(New List(Of Single))
            pinOffR.Add(New List(Of Integer))
            pinColR.Add(New List(Of Integer))
        Next

        For si As Integer = 0 To currentSeeds.Count - 1
            If si >= currentSeedPinned.Count OrElse Not currentSeedPinned(si) Then Continue For

            For ri As Integer = 0 To currentSketchDomains.Count - 1
                Dim dd = currentSketchDomains(ri)
                If Geo2D.PointInPolygonWithHoles(currentSeeds(si), dd.Outer, dd.Holes) Then
                    pinPosR(ri).Add(currentSeeds(si))
                    pinKeysR(ri).Add(If(si < currentSeedStyleKeys.Count, currentSeedStyleKeys(si), 0))
                    pinScalesR(ri).Add(If(si < currentSeedCellScales.Count, currentSeedCellScales(si), CSng(numCellScale.Value)))
                    pinRotR(ri).Add(If(si < currentSeedCellRotations.Count, currentSeedCellRotations(si), 0.0F))
                    pinOffR(ri).Add(If(si < currentSeedCellSymbolOffsets.Count, currentSeedCellSymbolOffsets(si), 0))
                    pinColR(ri).Add(If(si < currentSeedCellColors.Count, currentSeedCellColors(si), -1))
                    Exit For
                End If
            Next
        Next

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

            Dim pinCount As Integer = pinPosR(i).Count
            If quota = 0 AndAlso pinCount = 0 Then Continue For

            Dim regionSeed As Integer = seedBase + i * 997
            Dim freshQuota As Integer = Math.Max(0, quota - pinCount)
            Dim freshSeeds = VoronoiEngine.CreateSeedsByModeInPolygon(GetSeedMode(), freshQuota, d.Bounds, d.Outer, d.Holes, regionSeed)

            ' Semi pinnati in testa, nuovi a seguire (invariante di prefisso).
            Dim seeds As New List(Of Vec2)(pinPosR(i))
            seeds.AddRange(freshSeeds)

            Dim regionStyleKeys As New List(Of Integer)(pinKeysR(i))
            Dim regionScales As New List(Of Single)(pinScalesR(i))
            Dim regionRotations As New List(Of Single)(pinRotR(i))
            Dim regionOffsets As New List(Of Integer)(pinOffR(i))
            Dim regionColors As New List(Of Integer)(pinColR(i))
            Dim regionPinned As New List(Of Boolean)
            Dim rng As New Random(regionSeed Xor &H51F15E)

            For k As Integer = 1 To pinCount
                regionPinned.Add(True)
            Next

            For k As Integer = 0 To freshSeeds.Count - 1
                regionStyleKeys.Add(rng.Next())
                regionScales.Add(defaultScale)
                regionRotations.Add(0.0F)
                regionOffsets.Add(0)
                regionColors.Add(-1)
                regionPinned.Add(False)
            Next

            For r As Integer = 1 To CInt(numRelax.Value)
                Dim tmpCells = VoronoiEngine.BuildCells(seeds, d.Outer, d.Holes)
                seeds = VoronoiEngine.RelaxSeeds(tmpCells)

                ' I semi pinnati (prefisso) tornano alla loro posizione: essendo
                ' interni al dominio non verranno mai filtrati e il prefisso regge.
                For k As Integer = 0 To pinCount - 1
                    If k < seeds.Count Then seeds(k) = pinPosR(i)(k)
                Next

                Dim filteredSeeds As New List(Of Vec2)
                Dim filteredKeys As New List(Of Integer)
                Dim filteredScales As New List(Of Single)
                Dim filteredRotations As New List(Of Single)
                Dim filteredOffsets As New List(Of Integer)
                Dim filteredColors As New List(Of Integer)
                Dim filteredPinned As New List(Of Boolean)

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

                        If k < regionOffsets.Count Then
                            filteredOffsets.Add(regionOffsets(k))
                        End If

                        If k < regionColors.Count Then
                            filteredColors.Add(regionColors(k))
                        Else
                            filteredColors.Add(-1)
                        End If

                        filteredPinned.Add(k < regionPinned.Count AndAlso regionPinned(k))
                    End If
                Next

                seeds = filteredSeeds
                regionStyleKeys = filteredKeys
                regionScales = filteredScales
                regionRotations = filteredRotations
                regionOffsets = filteredOffsets
                regionColors = filteredColors
                regionPinned = filteredPinned
            Next

            Dim cells As List(Of VoronoiCell)
            If chkFullSeamCells.Checked Then
                ' Celle intere: nessun taglio su profilo e fori (il bordo
                ' lontano viene chiuso su un margine oltre la regione).
                cells = VoronoiPeriodic.BuildCellsPeriodic(seeds, d.Bounds, False, False, True)
            Else
                cells = VoronoiEngine.BuildCells(seeds, d.Outer, d.Holes)
            End If

            allSeeds.AddRange(seeds)
            allCells.AddRange(cells)
            allStyleKeys.AddRange(regionStyleKeys)
            allScales.AddRange(regionScales)
            allRotations.AddRange(regionRotations)
            allOffsets.AddRange(regionOffsets)
            allPinned.AddRange(regionPinned)
            allColors.AddRange(regionColors)
        Next

        currentSeeds = allSeeds
        currentSeedStyleKeys = allStyleKeys
        currentSeedCellScales = allScales
        currentSeedCellRotations = allRotations
        currentSeedCellSymbolOffsets = allOffsets
        currentSeedPinned = allPinned
        currentSeedCellColors = allColors

        canvas.Cells = allCells
        canvas.EditableSeeds = New List(Of Vec2)(allSeeds)
        canvas.SeedStyleKeys = New List(Of Integer)(currentSeedStyleKeys)
        canvas.CellScales = New List(Of Single)(currentSeedCellScales)
        canvas.CellRotations = New List(Of Single)(currentSeedCellRotations)
        canvas.CellSymbolOffsets = New List(Of Integer)(currentSeedCellSymbolOffsets)
        canvas.SeedPinned = New List(Of Boolean)(currentSeedPinned)
        canvas.CellColorIndices = New List(Of Integer)(currentSeedCellColors)

        ApplyOptions()
        canvas.Invalidate()
    End Sub

    'Private Sub Canvas_SeedsEdited(sender As Object, e As EventArgs)
    '    currentSeeds = New List(Of Vec2)(canvas.EditableSeeds)
    '    EnsureSeedStyleKeyCount(currentSeeds.Count, CInt(numSeed.Value))
    '    BuildFromCurrentSeeds()
    'End Sub

    Private Sub Canvas_SeedsEdited(sender As Object, e As EventArgs)
        MarkProjectDirty()
        currentSeeds = New List(Of Vec2)(canvas.EditableSeeds)
        currentSeedCellScales = New List(Of Single)(canvas.CellScales)
        currentSeedCellRotations = New List(Of Single)(canvas.CellRotations)
        currentSeedCellSymbolOffsets = New List(Of Integer)(canvas.CellSymbolOffsets)
        currentSeedPinned = New List(Of Boolean)(canvas.SeedPinned)
        currentSeedCellColors = New List(Of Integer)(canvas.CellColorIndices)

        EnsureSeedStyleKeyCount(currentSeeds.Count, CInt(numSeed.Value))
        EnsureSeedCellScaleCount(currentSeeds.Count, CSng(numCellScale.Value))
        EnsureSeedCellRotationCount(currentSeeds.Count)
        EnsureSeedCellSymbolOffsetCount(currentSeeds.Count)
        EnsureSeedPinnedCount(currentSeeds.Count)
        EnsureSeedCellColorCount(currentSeeds.Count)

        BuildFromCurrentSeeds()
        LoadSelectionPanel()
    End Sub

    Private Sub Canvas_SeedScalesEdited(sender As Object, e As EventArgs)
        MarkProjectDirty()
        currentSeedCellScales = New List(Of Single)(canvas.CellScales)
        EnsureSeedCellScaleCount(currentSeeds.Count, CSng(numCellScale.Value))
        canvas.CellScales = New List(Of Single)(currentSeedCellScales)
        canvas.Invalidate()
        LoadSelectionPanel()
    End Sub

    Private Sub Canvas_SeedRotationsEdited(sender As Object, e As EventArgs)
        MarkProjectDirty()
        currentSeedCellRotations = New List(Of Single)(canvas.CellRotations)
        EnsureSeedCellRotationCount(currentSeeds.Count)
        canvas.CellRotations = New List(Of Single)(currentSeedCellRotations)
        canvas.Invalidate()
        LoadSelectionPanel()
    End Sub

    ' Il profilo e' stato editato sul canvas: ricostruisce domini e diagramma.
    ' Installa il profilo di partenza: un rettangolo editabile pari al dominio.
    ' E' un profilo a tutti gli effetti (drag / doppio click / ALT+click) e
    ' viene interamente rimpiazzato caricando un profilo da file o Solid Edge.
    Private Sub InstallDefaultRectangularProfile()
        Dim rectLoop As New List(Of Vec2) From {
            New Vec2(domain.Left, domain.Top),
            New Vec2(domain.Right, domain.Top),
            New Vec2(domain.Right, domain.Bottom),
            New Vec2(domain.Left, domain.Bottom)
        }

        currentSketchBoundaries = New List(Of List(Of Vec2))()
        currentSketchBoundaries.Add(rectLoop)

        Dim r As New SketchDomainRegion()
        r.Outer = New List(Of Vec2)(rectLoop)
        r.Bounds = Geo2D.GetBounds(r.Outer)
        currentSketchDomains = New List(Of SketchDomainRegion) From {r}

        canvas.SketchBoundaries = currentSketchBoundaries
        canvas.SketchBoundaryIsHole = New List(Of Boolean) From {False}
        canvas.SketchDomains = ConvertToCanvasDomains(currentSketchDomains)
        canvas.ConstrainSeedsToSketchDomains = True
        canvas.ShowSketchBoundary = True

        useSketchDomains = True
        lockSketchViewDomain = True

        Dim pad As Single = 40.0F
        currentWorldDomain = New RectangleF(domain.Left - pad, domain.Top - pad,
                                            domain.Width + pad * 2.0F, domain.Height + pad * 2.0F)
        canvas.Domain = currentWorldDomain
    End Sub

    ' Il FIT (ALT+RMB) ha ricalcolato il dominio dal profilo: sincronizzo.
    Private Sub Canvas_DomainChangedByFit(sender As Object, e As EventArgs)
        currentWorldDomain = canvas.Domain
    End Sub

    Private Sub Canvas_SketchBoundariesEdited(sender As Object, e As EventArgs)
        MarkProjectDirty()

        currentSketchBoundaries = New List(Of List(Of Vec2))()
        For Each lp In canvas.SketchBoundaries
            currentSketchBoundaries.Add(New List(Of Vec2)(lp))
        Next

        RebuildSketchDomainsFromBoundaries()
        canvas.SketchDomains = ConvertToCanvasDomains(currentSketchDomains)

        BuildFromCurrentSeeds()
    End Sub

    ' Ricostruisce le regioni (contorno + fori) dalle boundary correnti,
    ' assegnando ogni foro al piu' piccolo contorno che lo contiene.
    Private Sub RebuildSketchDomainsFromBoundaries()
        currentSketchDomains = New List(Of SketchDomainRegion)()
        Dim flags = canvas.SketchBoundaryIsHole

        For i As Integer = 0 To currentSketchBoundaries.Count - 1
            Dim isHole As Boolean = flags IsNot Nothing AndAlso i < flags.Count AndAlso flags(i)
            If isHole Then Continue For

            Dim r As New SketchDomainRegion()
            r.Outer = New List(Of Vec2)(currentSketchBoundaries(i))
            r.Bounds = Geo2D.GetBounds(r.Outer)
            currentSketchDomains.Add(r)
        Next

        For i As Integer = 0 To currentSketchBoundaries.Count - 1
            Dim isHole As Boolean = flags IsNot Nothing AndAlso i < flags.Count AndAlso flags(i)
            If Not isHole Then Continue For

            Dim h = currentSketchBoundaries(i)
            Dim best As Integer = -1
            Dim bestArea As Double = Double.MaxValue

            For k As Integer = 0 To currentSketchDomains.Count - 1
                If Geo2D.PolygonContainsPolygon(currentSketchDomains(k).Outer, h) Then
                    Dim a As Double = Math.Abs(Geo2D.SignedArea(currentSketchDomains(k).Outer))
                    If a < bestArea Then
                        bestArea = a
                        best = k
                    End If
                End If
            Next

            If best >= 0 Then
                currentSketchDomains(best).Holes.Add(New List(Of Vec2)(h))
            End If
        Next
    End Sub

    Private Sub Canvas_SeedColorsEdited(sender As Object, e As EventArgs)
        MarkProjectDirty()
        currentSeedCellColors = New List(Of Integer)(canvas.CellColorIndices)
        EnsureSeedCellColorCount(currentSeeds.Count)
        canvas.CellColorIndices = New List(Of Integer)(currentSeedCellColors)
        canvas.Invalidate()
        LoadSelectionPanel()
    End Sub

    Private Sub Canvas_SeedSymbolOffsetsEdited(sender As Object, e As EventArgs)
        MarkProjectDirty()
        currentSeedCellSymbolOffsets = New List(Of Integer)(canvas.CellSymbolOffsets)
        EnsureSeedCellSymbolOffsetCount(currentSeeds.Count)
        canvas.CellSymbolOffsets = New List(Of Integer)(currentSeedCellSymbolOffsets)
        canvas.Invalidate()
        LoadSelectionPanel()
    End Sub

    ' ===== Pannello proprieta' della cella selezionata =====

    Private Function NormalizeDeg(v As Single) As Single
        Dim r As Single = v Mod 360.0F
        If r < 0.0F Then r += 360.0F
        Return r
    End Function

    Private Sub Canvas_SelectedSeedChanged(sender As Object, e As EventArgs)
        LoadSelectionPanel()
    End Sub

    ' Carica nel pannello i valori del seme selezionato (o disabilita tutto).
    Private Sub LoadSelectionPanel()
        updatingSelectionUi = True
        Try
            Dim idx As Integer = canvas.SelectedSeedIndex
            Dim has As Boolean = idx >= 0 AndAlso idx < currentSeeds.Count

            selScale.Enabled = has
            selRotation.Enabled = has
            selSymbolOffset.Enabled = has
            chkSelPinned.Enabled = has
            selColor.Enabled = has

            If has Then
                EnsureSeedCellScaleCount(currentSeeds.Count, CSng(numCellScale.Value))
                EnsureSeedCellRotationCount(currentSeeds.Count)
                EnsureSeedCellSymbolOffsetCount(currentSeeds.Count)
                EnsureSeedPinnedCount(currentSeeds.Count)
                EnsureSeedCellColorCount(currentSeeds.Count)

                lblSelInfo.Text = "Seed #" & (idx + 1).ToString() &
                                  "   X: " & currentSeeds(idx).X.ToString("0.0") &
                                  "   Y: " & currentSeeds(idx).Y.ToString("0.0")

                selScale.Value = CDec(currentSeedCellScales(idx))
                selRotation.Value = CDec(NormalizeDeg(currentSeedCellRotations(idx)))
                selSymbolOffset.Value = CDec(currentSeedCellSymbolOffsets(idx))
                chkSelPinned.Checked = currentSeedPinned(idx)
                selColor.SelectedIndex = currentSeedCellColors(idx) + 1   ' -1 (auto) -> 0
            Else
                lblSelInfo.Text = "No cell selected - click a cell"
            End If
        Finally
            updatingSelectionUi = False
        End Try
    End Sub

    Private Function SelectedIndexForEdit() As Integer
        If updatingSelectionUi Then Return -1
        Dim idx As Integer = canvas.SelectedSeedIndex
        If idx < 0 OrElse idx >= currentSeeds.Count Then Return -1
        Return idx
    End Function

    Private Sub SelColor_Changed(sender As Object, e As EventArgs)
        Dim idx As Integer = SelectedIndexForEdit()
        If idx < 0 Then Return
        MarkProjectDirty()

        EnsureSeedCellColorCount(currentSeeds.Count)
        currentSeedCellColors(idx) = selColor.SelectedIndex - 1   ' 0 (Auto) -> -1
        canvas.CellColorIndices = New List(Of Integer)(currentSeedCellColors)
        canvas.Invalidate()
    End Sub

    Private Sub SelScale_Changed(sender As Object, e As EventArgs)
        Dim idx As Integer = SelectedIndexForEdit()
        If idx < 0 Then Return
        MarkProjectDirty()

        EnsureSeedCellScaleCount(currentSeeds.Count, CSng(numCellScale.Value))
        currentSeedCellScales(idx) = CSng(selScale.Value)
        If idx < canvas.CellScales.Count Then canvas.CellScales(idx) = CSng(selScale.Value)
        canvas.Invalidate()
    End Sub

    Private Sub SelRotation_Changed(sender As Object, e As EventArgs)
        Dim idx As Integer = SelectedIndexForEdit()
        If idx < 0 Then Return
        MarkProjectDirty()

        EnsureSeedCellRotationCount(currentSeeds.Count)
        currentSeedCellRotations(idx) = CSng(selRotation.Value)
        If idx < canvas.CellRotations.Count Then canvas.CellRotations(idx) = CSng(selRotation.Value)
        canvas.Invalidate()
    End Sub

    Private Sub SelSymbolOffset_Changed(sender As Object, e As EventArgs)
        Dim idx As Integer = SelectedIndexForEdit()
        If idx < 0 Then Return
        MarkProjectDirty()

        EnsureSeedCellSymbolOffsetCount(currentSeeds.Count)
        currentSeedCellSymbolOffsets(idx) = CInt(selSymbolOffset.Value)
        If idx < canvas.CellSymbolOffsets.Count Then canvas.CellSymbolOffsets(idx) = CInt(selSymbolOffset.Value)
        canvas.Invalidate()
    End Sub

    Private Sub SelPinned_Changed(sender As Object, e As EventArgs)
        Dim idx As Integer = SelectedIndexForEdit()
        If idx < 0 Then Return
        MarkProjectDirty()

        EnsureSeedPinnedCount(currentSeeds.Count)
        currentSeedPinned(idx) = chkSelPinned.Checked
        If idx < canvas.SeedPinned.Count Then canvas.SeedPinned(idx) = chkSelPinned.Checked
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
        EnsureSeedCellSymbolOffsetCount(currentSeeds.Count)
        EnsureSeedCellColorCount(currentSeeds.Count)

        If useSketchDomains AndAlso lockSketchViewDomain Then
            canvas.Domain = currentWorldDomain
        Else
            canvas.Domain = domain
        End If

        If useSketchDomains AndAlso Not (PeriodicXActive() OrElse PeriodicYActive()) AndAlso
           currentSketchDomains IsNot Nothing AndAlso currentSketchDomains.Count > 0 Then
            Dim allCells As New List(Of VoronoiCell)
            Dim allSeeds As New List(Of Vec2)
            Dim allStyleKeys As New List(Of Integer)
            Dim allScales As New List(Of Single)
            Dim allRotations As New List(Of Single)
            Dim allOffsets As New List(Of Integer)
            Dim allPinned As New List(Of Boolean)
            Dim allColors As New List(Of Integer)

            ' Il riordino per dominio rimescola gli indici: memorizzo la
            ' posizione del seme selezionato per rimapparla dopo.
            Dim selPos As Vec2 = Nothing
            Dim hadSelection As Boolean = canvas.SelectedSeedIndex >= 0 AndAlso canvas.SelectedSeedIndex < currentSeeds.Count
            If hadSelection Then selPos = currentSeeds(canvas.SelectedSeedIndex)

            For Each d In currentSketchDomains
                Dim seedsInDomain As New List(Of Vec2)
                Dim keysInDomain As New List(Of Integer)
                Dim scalesInDomain As New List(Of Single)
                Dim rotationsInDomain As New List(Of Single)
                Dim offsetsInDomain As New List(Of Integer)
                Dim pinnedInDomain As New List(Of Boolean)
                Dim colorsInDomain As New List(Of Integer)

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

                        If i < currentSeedCellSymbolOffsets.Count Then
                            offsetsInDomain.Add(currentSeedCellSymbolOffsets(i))
                        Else
                            offsetsInDomain.Add(0)
                        End If

                        pinnedInDomain.Add(i < currentSeedPinned.Count AndAlso currentSeedPinned(i))

                        If i < currentSeedCellColors.Count Then
                            colorsInDomain.Add(currentSeedCellColors(i))
                        Else
                            colorsInDomain.Add(-1)
                        End If
                    End If
                Next

                If seedsInDomain.Count = 0 Then Continue For

                Dim cells As List(Of VoronoiCell)
                If chkFullSeamCells.Checked Then
                    cells = VoronoiPeriodic.BuildCellsPeriodic(seedsInDomain, d.Bounds, False, False, True)
                Else
                    cells = VoronoiEngine.BuildCells(seedsInDomain, d.Outer, d.Holes)
                End If

                allSeeds.AddRange(seedsInDomain)
                allStyleKeys.AddRange(keysInDomain)
                allScales.AddRange(scalesInDomain)
                allRotations.AddRange(rotationsInDomain)
                allOffsets.AddRange(offsetsInDomain)
                allPinned.AddRange(pinnedInDomain)
                allColors.AddRange(colorsInDomain)
                allCells.AddRange(cells)
            Next

            currentSeeds = allSeeds
            currentSeedStyleKeys = allStyleKeys
            currentSeedCellScales = allScales
            currentSeedCellRotations = allRotations
            currentSeedCellSymbolOffsets = allOffsets
            currentSeedPinned = allPinned
            currentSeedCellColors = allColors

            canvas.Cells = allCells
            canvas.EditableSeeds = New List(Of Vec2)(allSeeds)

            ' Rimappa la selezione sul nuovo ordinamento (per posizione).
            If hadSelection Then
                Dim newSel As Integer = -1
                For i As Integer = 0 To currentSeeds.Count - 1
                    If Geo2D.Distance(currentSeeds(i), selPos) <= 0.001 Then
                        newSel = i
                        Exit For
                    End If
                Next
                canvas.SelectedSeedIndex = newSel
            End If
        Else
            Dim cells As List(Of VoronoiCell)
            If PeriodicXActive() OrElse PeriodicYActive() OrElse chkFullSeamCells.Checked Then
                cells = VoronoiPeriodic.BuildCellsPeriodic(currentSeeds, domain,
                                                           PeriodicXActive(), PeriodicYActive(),
                                                           chkFullSeamCells.Checked)
            Else
                cells = VoronoiEngine.BuildCells(currentSeeds, domain)
            End If
            canvas.Cells = cells
            canvas.EditableSeeds = New List(Of Vec2)(currentSeeds)
        End If

        EnsureSeedStyleKeyCount(currentSeeds.Count, CInt(numSeed.Value))
        EnsureSeedCellScaleCount(currentSeeds.Count, CSng(numCellScale.Value))
        EnsureSeedCellRotationCount(currentSeeds.Count)
        EnsureSeedCellSymbolOffsetCount(currentSeeds.Count)
        EnsureSeedPinnedCount(currentSeeds.Count)
        EnsureSeedCellColorCount(currentSeeds.Count)

        canvas.SeedStyleKeys = New List(Of Integer)(currentSeedStyleKeys)
        canvas.CellScales = New List(Of Single)(currentSeedCellScales)
        canvas.CellRotations = New List(Of Single)(currentSeedCellRotations)
        canvas.CellSymbolOffsets = New List(Of Integer)(currentSeedCellSymbolOffsets)
        canvas.SeedPinned = New List(Of Boolean)(currentSeedPinned)
        canvas.CellColorIndices = New List(Of Integer)(currentSeedCellColors)

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
        MarkProjectDirty()
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
        canvas.ShowDomainFill = chkDomainFill.Checked
        canvas.DomainFillColor = GetSelectedDomainColor()
        canvas.ShowDomainRect = chkPeriodicX.Checked OrElse chkPeriodicY.Checked
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

        UpdateStatusBar()
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
            canvas.ResetView()



            currentSketchBoundaries = New List(Of List(Of Vec2))()
            currentSketchDomains = New List(Of SketchDomainRegion)()

            currentSeedCellScales = New List(Of Single)()
            canvas.CellScales = New List(Of Single)()
            currentSeedCellRotations = New List(Of Single)()
            canvas.CellRotations = New List(Of Single)()
            currentSeedCellSymbolOffsets = New List(Of Integer)()
            canvas.CellSymbolOffsets = New List(Of Integer)()
            currentSeedPinned = New List(Of Boolean)()
            canvas.SeedPinned = New List(Of Boolean)()
            currentSeedCellColors = New List(Of Integer)()
            canvas.CellColorIndices = New List(Of Integer)()
            canvas.SelectedSeedIndex = -1

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

            MarkProjectDirty()

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

            Dim addedCount As Integer = AddBlocksUnique(defs)
            canvas.BlockSymbols = currentBlockSymbols

            cmbStyle.SelectedItem = CellRenderStyle.BlockSymbol.ToString()
            ApplyOptions()
            canvas.Invalidate()

            RefreshBlockLibraryIfOpen()

            MessageBox.Show(addedCount & " block(s) added (" & currentBlockSymbols.Count & " in memory, " &
                            (defs.Count - addedCount) & " duplicate(s) skipped).",
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
            MessageBox.Show("No blocks in memory to save.", "Save Blocks", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        Using dlg As New SaveFileDialog()
            dlg.Title = "Save block library"
            dlg.Filter = "SE-Voronoi blocks (*.sevb)|*.sevb"
            dlg.DefaultExt = "sevb"
            dlg.AddExtension = True
            dlg.FileName = "blocks.sevb"

            If dlg.ShowDialog(Me) = DialogResult.OK Then
                Try
                    ExportGeometry.SaveBlocksToFile(dlg.FileName, currentBlockSymbols)
                    MessageBox.Show(currentBlockSymbols.Count & " block(s) saved.",
                                    "Save Blocks", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Catch ex As Exception
                    MessageBox.Show("Error saving blocks:" & Environment.NewLine & ex.Message,
                                    "Save Blocks", MessageBoxButtons.OK, MessageBoxIcon.Error)
                End Try
            End If
        End Using
    End Sub

    Private Sub LoadBlocks_Click(sender As Object, e As EventArgs)
        Using dlg As New OpenFileDialog()
            dlg.Title = "Load block library"
            dlg.Filter = "SE-Voronoi blocks (*.sevb)|*.sevb|All files (*.*)|*.*"
            dlg.Multiselect = True

            If dlg.ShowDialog(Me) <> DialogResult.OK Then Return

            Dim totalRead As Integer = 0
            Dim totalAdded As Integer = 0
            Dim errors As New List(Of String)

            For Each fn In dlg.FileNames
                Try
                    Dim loaded = ExportGeometry.LoadBlocksFromFile(fn)
                    If loaded IsNot Nothing Then
                        totalRead += loaded.Count
                        totalAdded += AddBlocksUnique(loaded)
                    End If
                Catch ex As Exception
                    errors.Add(IO.Path.GetFileName(fn) & ": " & ex.Message)
                End Try
            Next

            If totalRead = 0 AndAlso errors.Count = 0 Then
                MessageBox.Show("No valid blocks in the selected file(s).",
                                "Load Blocks", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            canvas.BlockSymbols = currentBlockSymbols
            cmbStyle.SelectedItem = CellRenderStyle.BlockSymbol.ToString()
            ApplyOptions()
            canvas.Invalidate()
            RefreshBlockLibraryIfOpen()

            Dim skipped As Integer = totalRead - totalAdded
            Dim msg As String = totalAdded & " block(s) added (" & currentBlockSymbols.Count & " in memory)."
            If skipped > 0 Then msg &= Environment.NewLine & skipped & " duplicate(s) skipped."
            If errors.Count > 0 Then msg &= Environment.NewLine & "Errors:" & Environment.NewLine & String.Join(Environment.NewLine, errors)

            MessageBox.Show(msg, "Load Blocks", MessageBoxButtons.OK, MessageBoxIcon.Information)
        End Using
    End Sub

    ' Aggiunge a currentBlockSymbols solo i blocchi il cui Name non e' gia' presente
    ' (confronto case-insensitive). I blocchi senza nome vengono sempre aggiunti.
    ' Ritorna quanti ne ha effettivamente aggiunti.
    Private Function AddBlocksUnique(incoming As List(Of BlockDefinition)) As Integer
        Dim added As Integer = 0
        If incoming Is Nothing Then Return 0
        If currentBlockSymbols Is Nothing Then currentBlockSymbols = New List(Of BlockDefinition)()

        Dim existing As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
        For Each b In currentBlockSymbols
            If b IsNot Nothing AndAlso Not String.IsNullOrEmpty(b.Name) Then existing.Add(b.Name)
        Next

        For Each b In incoming
            If b Is Nothing Then Continue For
            If Not String.IsNullOrEmpty(b.Name) AndAlso existing.Contains(b.Name) Then Continue For
            currentBlockSymbols.Add(b)
            If Not String.IsNullOrEmpty(b.Name) Then existing.Add(b.Name)
            added += 1
        Next
        If added > 0 Then MarkProjectDirty()
        Return added
    End Function

    Private Sub ClearBlocks_Click(sender As Object, e As EventArgs)
        MarkProjectDirty()
        If currentBlockSymbols Is Nothing OrElse currentBlockSymbols.Count = 0 Then
            MessageBox.Show("No blocks in memory.", "Clear Blocks", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        If MessageBox.Show("Remove all loaded blocks from memory?", "Clear Blocks",
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

        RefreshBlockLibraryIfOpen()
    End Sub

    Private Sub OpenBlockLibrary_Click(sender As Object, e As EventArgs)
        If blockLibForm Is Nothing OrElse blockLibForm.IsDisposed Then
            blockLibForm = New BlockLibraryForm()
            AddHandler blockLibForm.BlocksChanged, AddressOf BlockLibrary_BlocksChanged
            blockLibForm.FillSymbols = chkFillSymbols.Checked
            blockLibForm.StrokeWidth = CSng(numCurveWidth.Value)
            blockLibForm.SetBlocks(currentBlockSymbols)
            blockLibForm.Show(Me)
        Else
            blockLibForm.FillSymbols = chkFillSymbols.Checked
            blockLibForm.StrokeWidth = CSng(numCurveWidth.Value)
            blockLibForm.SetBlocks(currentBlockSymbols)
            blockLibForm.BringToFront()
            blockLibForm.Focus()
        End If
    End Sub

    ' La galleria ha rimosso un blocco dalla lista condivisa: aggiorna il canvas.
    Private Sub BlockLibrary_BlocksChanged(sender As Object, e As EventArgs)
        MarkProjectDirty()
        canvas.BlockSymbols = currentBlockSymbols
        If currentBlockSymbols.Count = 0 AndAlso cmbStyle.SelectedItem IsNot Nothing AndAlso
           cmbStyle.SelectedItem.ToString() = CellRenderStyle.BlockSymbol.ToString() Then
            cmbStyle.SelectedItem = CellRenderStyle.Curved.ToString()
            ApplyOptions()
        End If
        canvas.Invalidate()
        UpdateStatusBar()
    End Sub

    Private Sub RefreshLibraryHandler(sender As Object, e As EventArgs)
        RefreshBlockLibraryIfOpen()
    End Sub

    Private Sub RefreshBlockLibraryIfOpen()
        If blockLibForm IsNot Nothing AndAlso Not blockLibForm.IsDisposed Then
            blockLibForm.FillSymbols = chkFillSymbols.Checked
            blockLibForm.StrokeWidth = CSng(numCurveWidth.Value)
            blockLibForm.SetBlocks(currentBlockSymbols)
        End If
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

    ' Loop del profilo sketch (contorni e fori) come path esportabili.
    Private Function BuildProfileExportPaths() As List(Of ExportPath2D)
        Dim result As New List(Of ExportPath2D)
        If canvas.SketchBoundaries Is Nothing Then Return result

        For Each lp In canvas.SketchBoundaries
            If lp Is Nothing OrElse lp.Count < 2 Then Continue For

            Dim p As New ExportPath2D() With {
                .Closed = True,
                .StrokeColor = Color.Black,
                .StrokeWidth = CSng(numCurveWidth.Value)
            }

            For i As Integer = 0 To lp.Count - 1
                p.Segments.Add(New ExportLine2D(lp(i), lp((i + 1) Mod lp.Count)))
            Next

            result.Add(p)
        Next

        Return result
    End Function

    Private Sub AppendProfilePaths(ByRef paths As List(Of ExportPath2D))
        If Not chkExportProfile.Checked Then Return
        If paths Is Nothing Then paths = New List(Of ExportPath2D)()
        paths.AddRange(BuildProfileExportPaths())
    End Sub

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

    Private Sub ExportPng_Click(sender As Object, e As EventArgs)
        Try
            Using dlg As New SaveFileDialog()
                dlg.Title = "Save PNG"
                dlg.Filter = "PNG image (*.png)|*.png"
                dlg.DefaultExt = "png"
                dlg.AddExtension = True
                dlg.FileName = "voronoi.png"

                If dlg.ShowDialog(Me) = DialogResult.OK Then
                    ' Risoluzione: 2000px sul lato lungo, l'altro segue le
                    ' proporzioni del dominio (fit-to-domain, zoom ignorato).
                    Dim longSide As Integer = 2000
                    Dim pxW, pxH As Integer

                    If canvas.Domain.Width >= canvas.Domain.Height Then
                        pxW = longSide
                        pxH = CInt(Math.Round(longSide * canvas.Domain.Height / canvas.Domain.Width))
                    Else
                        pxH = longSide
                        pxW = CInt(Math.Round(longSide * canvas.Domain.Width / canvas.Domain.Height))
                    End If

                    Using bmp = canvas.RenderToBitmap(pxW, pxH)
                        bmp.Save(dlg.FileName, Imaging.ImageFormat.Png)
                    End Using

                    MessageBox.Show("PNG saved successfully (" & pxW & " × " & pxH & " px).",
                                    "Export PNG", MessageBoxButtons.OK, MessageBoxIcon.Information)
                End If
            End Using

        Catch ex As Exception
            MessageBox.Show("Error exporting PNG:" & Environment.NewLine & ex.Message,
                            "Export PNG",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub ShowHelp_Click(sender As Object, e As EventArgs)
        If helpForm Is Nothing OrElse helpForm.IsDisposed Then
            helpForm = New HelpForm()
            helpForm.Show(Me)
        Else
            helpForm.BringToFront()
            helpForm.Focus()
        End If
    End Sub

    Private Sub ExportToSolidEdge_Click(sender As Object, e As EventArgs)
        Try
            If chkExportAsBlocks.Checked Then
                Dim geoms = ExportGeometry.BuildCellGeometry(canvas)
                If geoms Is Nothing OrElse geoms.Count = 0 Then
                    MessageBox.Show("No geometry to export.", "Solid Edge", MessageBoxButtons.OK, MessageBoxIcon.Information)
                    Return
                End If
                SolidEdgeExporter.ExportToActivePartSketch(geoms, currentBlockSymbols)

                If chkExportProfile.Checked Then
                    Dim profilePaths = BuildProfileExportPaths()
                    If profilePaths.Count > 0 Then
                        SolidEdgeExporter.ExportToActivePartSketch(profilePaths)
                    End If
                End If
            Else
                Dim paths = ExportGeometry.BuildExportPaths(canvas)
                AppendProfilePaths(paths)
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

    ' La periodicita' e' intrinsecamente rettangolare: quando attiva, la
    ' generazione ignora il profilo e usa il rettangolo del dominio.
    Private Function PeriodicXActive() As Boolean
        Return chkPeriodicX.Checked
    End Function

    Private Function PeriodicYActive() As Boolean
        Return chkPeriodicY.Checked
    End Function

    Private Sub GenerationParameterChanged(sender As Object, e As EventArgs)
        MarkProjectDirty()
        GenerateRandomDiagram(sender, e)
    End Sub

    Private Function GetSeedMode() As SeedPlacementMode
        If cmbSeedMode.SelectedItem Is Nothing Then Return SeedPlacementMode.Random
        Dim v As SeedPlacementMode
        If [Enum].TryParse(Of SeedPlacementMode)(cmbSeedMode.SelectedItem.ToString(), v) Then Return v
        Return SeedPlacementMode.Random
    End Function

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

    Private Sub RebuildSeedCellColors(count As Integer)
        currentSeedCellColors = New List(Of Integer)(count)
        For i As Integer = 1 To count
            currentSeedCellColors.Add(-1)
        Next
    End Sub

    Private Sub EnsureSeedCellColorCount(count As Integer)
        If currentSeedCellColors Is Nothing Then
            currentSeedCellColors = New List(Of Integer)()
        End If

        While currentSeedCellColors.Count < count
            currentSeedCellColors.Add(-1)
        End While

        While currentSeedCellColors.Count > count
            currentSeedCellColors.RemoveAt(currentSeedCellColors.Count - 1)
        End While
    End Sub

    Private Sub RebuildSeedPinned(count As Integer)
        currentSeedPinned = New List(Of Boolean)(count)
        For i As Integer = 1 To count
            currentSeedPinned.Add(False)
        Next
    End Sub

    Private Sub EnsureSeedPinnedCount(count As Integer)
        If currentSeedPinned Is Nothing Then
            currentSeedPinned = New List(Of Boolean)()
        End If

        While currentSeedPinned.Count < count
            currentSeedPinned.Add(False)
        End While

        While currentSeedPinned.Count > count
            currentSeedPinned.RemoveAt(currentSeedPinned.Count - 1)
        End While
    End Sub

    Private Sub RebuildSeedCellSymbolOffsets(count As Integer)
        currentSeedCellSymbolOffsets = New List(Of Integer)(count)

        For i As Integer = 0 To count - 1
            currentSeedCellSymbolOffsets.Add(0)
        Next
    End Sub

    Private Sub EnsureSeedCellSymbolOffsetCount(count As Integer)
        If currentSeedCellSymbolOffsets Is Nothing Then
            currentSeedCellSymbolOffsets = New List(Of Integer)()
        End If

        While currentSeedCellSymbolOffsets.Count < count
            currentSeedCellSymbolOffsets.Add(0)
        End While

        While currentSeedCellSymbolOffsets.Count > count
            currentSeedCellSymbolOffsets.RemoveAt(currentSeedCellSymbolOffsets.Count - 1)
        End While
    End Sub

End Class

' ============================================================
'  Galleria/anteprima dei blocchi in memoria (finestra modeless).
'  Disegna una miniatura a tratto per ogni blocco (geometria gia'
'  normalizzata) e consente di rimuovere il singolo blocco.
' ============================================================
Public Class BlockLibraryForm
    Inherits Form

    Public Event BlocksChanged As EventHandler

    Private ReadOnly header As New Label()
    Private ReadOnly flow As New BufferedFlowLayoutPanel()
    Private ReadOnly viewport As New Panel()
    Private ReadOnly libScroll As New ThemedVScrollBar()
    Private wheelFilter As WheelToScrollFilter = Nothing
    Private blocks As List(Of BlockDefinition) = Nothing
    Public Property FillSymbols As Boolean = False
    Public Property StrokeWidth As Single = 1.8F

    Private Const WM_SETREDRAW As Integer = &HB
    Private Declare Function SendMessage Lib "user32" Alias "SendMessageW" (hWnd As IntPtr, msg As Integer, wParam As IntPtr, lParam As IntPtr) As IntPtr

    ' FlowLayoutPanel double-buffered: evita lo sfarfallio quando le tile vengono
    ' ricostruite (es. cambio spessore linea o Fill symbols).
    Private Class BufferedFlowLayoutPanel
        Inherits FlowLayoutPanel
        Public Sub New()
            DoubleBuffered = True
            SetStyle(ControlStyles.OptimizedDoubleBuffer Or ControlStyles.AllPaintingInWmPaint, True)
        End Sub
    End Class

    Private Const ThumbPx As Integer = 120

    Public Sub New()
        Text = "Block Library"
        Icon = My.Resources.SE_Voronoi_Blocks
        StartPosition = FormStartPosition.CenterParent
        Width = 720
        Height = 560
        MinimumSize = New Size(360, 320)

        header.Dock = DockStyle.Top
        header.Height = 28
        header.TextAlign = ContentAlignment.MiddleLeft
        header.Padding = New Padding(8, 0, 0, 0)
        header.Text = "0 blocks in memory"

        ' Scrolling con la scrollbar custom (stessa della sidebar):
        ' il flow cresce in altezza dentro un viewport e viene traslato.
        flow.AutoScroll = False
        flow.AutoSize = True
        flow.AutoSizeMode = AutoSizeMode.GrowAndShrink
        flow.WrapContents = True
        flow.Padding = New Padding(8)
        flow.Location = New Point(0, 0)

        viewport.Dock = DockStyle.Fill
        viewport.Controls.Add(flow)

        libScroll.Dock = DockStyle.Right
        AddHandler libScroll.ScrollChanged, Sub(sc, ev) flow.Top = -libScroll.Value
        AddHandler viewport.Resize, AddressOf Viewport_Resize
        AddHandler flow.SizeChanged, Sub(sc, ev) UpdateLibScroll()

        ApplyChromeColors()

        Controls.Add(viewport)
        Controls.Add(libScroll)
        Controls.Add(header)
    End Sub

    Private Sub Viewport_Resize(sender As Object, e As EventArgs)
        Dim w As Integer = Math.Max(60, viewport.ClientSize.Width)
        flow.MinimumSize = New Size(w, 0)
        flow.MaximumSize = New Size(w, 0)
        UpdateLibScroll()
    End Sub

    Private Sub UpdateLibScroll()
        libScroll.ContentSize = flow.Height
        libScroll.ViewportSize = viewport.ClientSize.Height
        Dim needed As Boolean = flow.Height > viewport.ClientSize.Height
        libScroll.Visible = needed
        If Not needed Then
            libScroll.Value = 0
            flow.Top = 0
        End If
    End Sub

    ' Chrome della finestra a tema; le anteprime restano su fondo navy
    ' (come il canvas: i colori dei blocchi sono tarati su di esso).
    Private Sub ApplyChromeColors()
        BackColor = UiTheme.BgSidebar
        ForeColor = UiTheme.Txt
        header.ForeColor = UiTheme.Txt
        header.BackColor = UiTheme.BgSidebar
        flow.BackColor = UiTheme.BgSidebar
        viewport.BackColor = UiTheme.BgSidebar
        libScroll.BackColor = UiTheme.BgSidebar
    End Sub

    ' Riapplica il tema corrente (chiamata dal toggle se la finestra e' aperta).
    Public Sub RefreshTheme()
        ApplyChromeColors()
        If IsHandleCreated Then
            UiTheme.ApplyTitleBarTheme(Handle)
        End If
        RebuildTiles()
        Refresh()
    End Sub

    Protected Overrides Sub OnLoad(e As EventArgs)
        MyBase.OnLoad(e)
        UiTheme.ApplyTitleBarTheme(Handle)
        Viewport_Resize(viewport, EventArgs.Empty)
        wheelFilter = New WheelToScrollFilter(viewport, libScroll)
        Application.AddMessageFilter(wheelFilter)
    End Sub

    Public Sub SetBlocks(list As List(Of BlockDefinition))
        blocks = list
        RebuildTiles()
    End Sub

    Private Sub RebuildTiles()
        Dim frozen As Boolean = False
        Try
            If flow.IsHandleCreated Then
                SendMessage(flow.Handle, WM_SETREDRAW, IntPtr.Zero, IntPtr.Zero)
                frozen = True
            End If
            flow.SuspendLayout()

            ' Rilascia le immagini precedenti.
            For Each c As Control In flow.Controls
                DisposeTile(c)
            Next
            flow.Controls.Clear()

            Dim n As Integer = If(blocks Is Nothing, 0, blocks.Count)
            header.Text = n & " block(s) in memory"

            If blocks IsNot Nothing Then
                For i As Integer = 0 To blocks.Count - 1
                    flow.Controls.Add(BuildTile(blocks(i)))
                Next
            End If

        Finally
            ' True: riesegue il layout del FlowLayoutPanel (riposiziona TUTTE le tile).
            flow.ResumeLayout(True)
            If frozen Then
                ' Riabilita il ridisegno e ridipinge UNA sola volta il pannello completo.
                SendMessage(flow.Handle, WM_SETREDRAW, New IntPtr(1), IntPtr.Zero)
                flow.Invalidate(True)
            End If
        End Try
    End Sub

    Private Sub DisposeTile(c As Control)
        For Each child As Control In c.Controls
            Dim pb = TryCast(child, PictureBox)
            If pb IsNot Nothing AndAlso pb.Image IsNot Nothing Then
                pb.Image.Dispose()
                pb.Image = Nothing
            End If
        Next
    End Sub

    Private Function BuildTile(def As BlockDefinition) As Panel
        Dim tile As New Panel()
        tile.Width = ThumbPx + 16
        tile.Height = ThumbPx + 52
        tile.Margin = New Padding(6)
        tile.BackColor = UiTheme.BgField

        Dim pic As New PictureBox()
        pic.Width = ThumbPx
        pic.Height = ThumbPx
        pic.Left = 8
        pic.Top = 6
        pic.BackColor = If(UiTheme.IsDark, UiTheme.BgCanvas, Color.White)
        pic.SizeMode = PictureBoxSizeMode.Normal
        pic.Image = RenderThumb(def, ThumbPx)
        tile.Controls.Add(pic)

        Dim lbl As New Label()
        lbl.Text = If(String.IsNullOrEmpty(def.Name), "(no name)", def.Name)
        lbl.AutoSize = False
        lbl.Width = ThumbPx
        lbl.Height = 16
        lbl.Left = 8
        lbl.Top = ThumbPx + 8
        lbl.ForeColor = UiTheme.Txt
        lbl.BackColor = UiTheme.BgField
        lbl.TextAlign = ContentAlignment.MiddleCenter
        lbl.AutoEllipsis = True
        tile.Controls.Add(lbl)

        Dim btnDel As New ThemedButton()
        btnDel.Text = "Remove"
        btnDel.Width = ThumbPx - 60
        btnDel.Height = 22
        btnDel.Left = 8
        btnDel.Top = ThumbPx + 26
        btnDel.BackColor = UiTheme.BgField
        btnDel.ForeColor = UiTheme.Txt
        btnDel.FlatAppearance.BorderColor = UiTheme.Border
        btnDel.FlatAppearance.MouseOverBackColor = UiTheme.BgFieldHi
        btnDel.FlatAppearance.MouseDownBackColor = UiTheme.Border
        Dim target As BlockDefinition = def
        AddHandler btnDel.Click, Sub(s, e) RemoveBlock(target)
        tile.Controls.Add(btnDel)

        ' Scala primaria del blocco: moltiplica la dimensione calcolata dalla
        ' cella, ovunque il blocco venga usato (canvas, export, Solid Edge).
        Dim numScale As New ThemedNumericUpDown()
        numScale.Minimum = 0.05D
        numScale.Maximum = 3D
        numScale.DecimalPlaces = 2
        numScale.Increment = 0.05D
        numScale.Value = CDec(Math.Max(0.05, Math.Min(3.0, target.UserScale)))
        numScale.Width = 54
        numScale.Height = 22
        numScale.Left = btnDel.Left + btnDel.Width + 4
        numScale.Top = btnDel.Top
        AddHandler numScale.ValueChanged, Sub(s, e)
                                              target.UserScale = CDbl(numScale.Value)
                                              RaiseEvent BlocksChanged(Me, EventArgs.Empty)
                                          End Sub
        tile.Controls.Add(numScale)

        Return tile
    End Function

    Private Sub RemoveBlock(def As BlockDefinition)
        If blocks Is Nothing Then Return
        blocks.Remove(def)
        RebuildTiles()
        RaiseEvent BlocksChanged(Me, EventArgs.Empty)
    End Sub

    Private Function RenderThumb(def As BlockDefinition, sz As Integer) As Bitmap
        Dim bmp As New Bitmap(sz, sz)
        Using g As Graphics = Graphics.FromImage(bmp)
            g.SmoothingMode = SmoothingMode.AntiAlias
            g.Clear(If(UiTheme.IsDark, UiTheme.BgCanvas, Color.White))

            ' Inchiostro dal tema: ciano pieno su navy, teal scuro su bianco.
            Dim ink As Color = UiTheme.Accent

            Dim polys = ExportGeometry.FlattenBlockPaths(def)
            If polys Is Nothing OrElse polys.Count = 0 Then Return bmp

            Dim minX As Double = Double.MaxValue, minY As Double = Double.MaxValue
            Dim maxX As Double = Double.MinValue, maxY As Double = Double.MinValue
            For Each poly In polys
                For Each p In poly
                    If p.X < minX Then minX = p.X
                    If p.Y < minY Then minY = p.Y
                    If p.X > maxX Then maxX = p.X
                    If p.Y > maxY Then maxY = p.Y
                Next
            Next

            Dim w As Double = maxX - minX
            Dim h As Double = maxY - minY
            If w <= 0.0000001 AndAlso h <= 0.0000001 Then Return bmp

            Dim pad As Single = 12.0F
            Dim scale As Double = (sz - 2 * pad) / Math.Max(Math.Max(w, h), 0.000001)
            Dim cx As Double = (minX + maxX) * 0.5
            Dim cy As Double = (minY + maxY) * 0.5

            ' Riempimento (anelli chiusi, even-odd) coerente con "Fill symbols".
            If FillSymbols Then
                Dim loops = ExportGeometry.BuildFillLoops(def.Entities)
                If loops IsNot Nothing AndAlso loops.Count > 0 Then
                    Using gp As New GraphicsPath()
                        gp.FillMode = FillMode.Alternate
                        For Each lp In loops
                            If lp.Count < 3 Then Continue For
                            Dim fp(lp.Count - 1) As PointF
                            For k As Integer = 0 To lp.Count - 1
                                fp(k) = New PointF(
                                    CSng(sz / 2.0 + (lp(k).X - cx) * scale),
                                    CSng(sz / 2.0 + (lp(k).Y - cy) * scale))
                            Next
                            gp.AddPolygon(fp)
                        Next
                        Using br As New SolidBrush(Color.FromArgb(235, ink.R, ink.G, ink.B))
                            g.FillPath(br, gp)
                        End Using
                    End Using
                End If
            End If

            Using pen As New Pen(ink, Math.Max(0.5F, StrokeWidth))
                pen.LineJoin = LineJoin.Round
                pen.StartCap = LineCap.Round
                pen.EndCap = LineCap.Round
                For Each poly In polys
                    If poly.Count < 2 Then Continue For
                    Dim pts(poly.Count - 1) As PointF
                    For k As Integer = 0 To poly.Count - 1
                        pts(k) = New PointF(
                            CSng(sz / 2.0 + (poly(k).X - cx) * scale),
                            CSng(sz / 2.0 + (poly(k).Y - cy) * scale))
                    Next
                    g.DrawLines(pen, pts)
                Next
            End Using
        End Using
        Return bmp
    End Function

    Protected Overrides Sub OnFormClosed(e As FormClosedEventArgs)
        If wheelFilter IsNot Nothing Then
            Application.RemoveMessageFilter(wheelFilter)
        End If

        For Each c As Control In flow.Controls
            DisposeTile(c)
        Next
        MyBase.OnFormClosed(e)
    End Sub
End Class


' ============================================================
'  Palette del tema scuro dell'applicazione.
' ============================================================

' ============================================================
'  Inoltra la rotella del mouse a una ThemedVScrollBar quando il cursore
'  e' sopra l'area indicata e la sua finestra e' attiva.
' ============================================================
Public Class WheelToScrollFilter
    Implements IMessageFilter

    Private ReadOnly area As Control
    Private ReadOnly bar As ThemedVScrollBar

    Public Sub New(scrollArea As Control, scrollBar As ThemedVScrollBar)
        area = scrollArea
        bar = scrollBar
    End Sub

    Public Function PreFilterMessage(ByRef m As Message) As Boolean Implements IMessageFilter.PreFilterMessage
        Const WM_MOUSEWHEEL As Integer = &H20A
        If m.Msg <> WM_MOUSEWHEEL Then Return False
        If area Is Nothing OrElse Not area.IsHandleCreated OrElse Not area.Visible Then Return False

        Dim owner As Form = area.FindForm()
        If owner Is Nothing OrElse Form.ActiveForm IsNot owner Then Return False

        Dim pos As Point = Control.MousePosition
        If Not area.RectangleToScreen(area.ClientRectangle).Contains(pos) Then Return False

        If bar IsNot Nothing AndAlso bar.Visible Then
            Dim raw As Integer = CInt((m.WParam.ToInt64() >> 16) And &HFFFF&)
            If raw >= &H8000 Then raw -= &H10000
            bar.Value -= Math.Sign(raw) * 60
        End If
        Return True
    End Function
End Class

Public NotInheritable Class UiTheme
    Private Sub New()
    End Sub

    Public Shared IsDark As Boolean = True

    ' Il canvas resta navy in entrambi i temi: e' la superficie di disegno
    ' e la palette delle celle e' tarata su di esso.
    Public Shared BgCanvas As Color = Color.FromArgb(8, 6, 53)
    Public Shared BgSidebar As Color = Color.FromArgb(16, 14, 60)
    Public Shared BgField As Color = Color.FromArgb(22, 20, 74)
    Public Shared BgFieldHi As Color = Color.FromArgb(30, 28, 94)
    Public Shared Border As Color = Color.FromArgb(42, 40, 112)
    Public Shared Txt As Color = Color.FromArgb(232, 236, 248)
    Public Shared TxtDim As Color = Color.FromArgb(154, 160, 200)
    Public Shared Accent As Color = Color.FromArgb(0, 188, 212)
    Public Shared AccentHi As Color = Color.FromArgb(110, 231, 243)
    Public Shared StatusBg As Color = Color.FromArgb(13, 11, 69)

    Public Shared Sub SetTheme(dark As Boolean)
        IsDark = dark

        If dark Then
            BgSidebar = Color.FromArgb(16, 14, 60)
            BgField = Color.FromArgb(22, 20, 74)
            BgFieldHi = Color.FromArgb(30, 28, 94)
            Border = Color.FromArgb(42, 40, 112)
            Txt = Color.FromArgb(232, 236, 248)
            TxtDim = Color.FromArgb(154, 160, 200)
            Accent = Color.FromArgb(0, 188, 212)
            AccentHi = Color.FromArgb(110, 231, 243)
            StatusBg = Color.FromArgb(13, 11, 69)
        Else
            BgSidebar = Color.FromArgb(238, 241, 246)
            BgField = Color.FromArgb(255, 255, 255)
            BgFieldHi = Color.FromArgb(222, 230, 240)
            Border = Color.FromArgb(178, 186, 206)
            Txt = Color.FromArgb(30, 40, 55)
            TxtDim = Color.FromArgb(106, 116, 140)
            Accent = Color.FromArgb(0, 151, 167)
            AccentHi = Color.FromArgb(0, 188, 212)
            StatusBg = Color.FromArgb(226, 230, 238)
        End If
    End Sub

    <Runtime.InteropServices.DllImport("dwmapi.dll", EntryPoint:="DwmSetWindowAttribute")>
    Private Shared Function DwmSetAttr(hwnd As IntPtr, attr As Integer, ByRef attrValue As Integer, attrSize As Integer) As Integer
    End Function

    <Runtime.InteropServices.DllImport("user32.dll")>
    Private Shared Function SetWindowPos(hWnd As IntPtr, hWndAfter As IntPtr, x As Integer, y As Integer, cx As Integer, cy As Integer, flags As UInteger) As Boolean
    End Function

    <Runtime.InteropServices.DllImport("uxtheme.dll", CharSet:=Runtime.InteropServices.CharSet.Unicode)>
    Private Shared Function SetWindowTheme(hWnd As IntPtr, pszSubAppName As String, pszSubIdList As String) As Integer
    End Function

    ' Scrollbar native (RichTextBox, AutoScroll) scure o chiare secondo il tema.
    Public Shared Sub ApplyScrollBarTheme(handle As IntPtr)
        Try
            SetWindowTheme(handle, If(IsDark, "DarkMode_Explorer", "Explorer"), Nothing)
        Catch
        End Try
    End Sub

    ' Applica alla barra del titolo i colori del tema corrente (Win10 1809+/11).
    Public Shared Sub ApplyTitleBarTheme(handle As IntPtr)
        Try
            Dim dark As Integer = If(IsDark, 1, 0)
            If DwmSetAttr(handle, 20, dark, 4) <> 0 Then
                DwmSetAttr(handle, 19, dark, 4)
            End If
            Dim captionCol As Integer = BgSidebar.R Or (CInt(BgSidebar.G) << 8) Or (CInt(BgSidebar.B) << 16)
            Dim textCol As Integer = Txt.R Or (CInt(Txt.G) << 8) Or (CInt(Txt.B) << 16)
            DwmSetAttr(handle, 35, captionCol, 4)
            DwmSetAttr(handle, 34, captionCol, 4)
            DwmSetAttr(handle, 36, textCol, 4)

            ' Forza il ridisegno dell'area non-client: senza, Windows a volte
            ' aggiorna la caption solo quando la finestra perde/riprende il fuoco.
            ' SWP_NOSIZE Or SWP_NOMOVE Or SWP_NOZORDER Or SWP_NOACTIVATE Or SWP_FRAMECHANGED
            SetWindowPos(handle, IntPtr.Zero, 0, 0, 0, 0, &H1 Or &H2 Or &H4 Or &H10 Or &H20)
        Catch
        End Try
    End Sub

    Public Shared Function RoundedRect(r As RectangleF, radius As Single) As Drawing2D.GraphicsPath
        Dim gp As New Drawing2D.GraphicsPath()
        Dim d As Single = radius * 2.0F
        If d <= 0.0F OrElse r.Width <= d OrElse r.Height <= d Then
            gp.AddRectangle(r)
            Return gp
        End If
        gp.AddArc(r.X, r.Y, d, d, 180, 90)
        gp.AddArc(r.Right - d, r.Y, d, d, 270, 90)
        gp.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90)
        gp.AddArc(r.X, r.Bottom - d, d, d, 90, 90)
        gp.CloseFigure()
        Return gp
    End Function
End Class

' ============================================================
'  Slider a tema con etichetta del valore. Espone la stessa interfaccia
'  di NumericUpDown (Minimum/Maximum/Value/Increment/DecimalPlaces,
'  evento ValueChanged) cosi' puo' sostituirlo senza toccare i chiamanti.
' ============================================================
Public Class ThemedSlider
    Inherits Control

    Private _min As Decimal = 0D
    Private _max As Decimal = 100D
    Private _val As Decimal = 0D
    Private _inc As Decimal = 1D
    Private _decimals As Integer = 0
    Private dragging As Boolean = False
    Private hovering As Boolean = False

    Private Const ValueTextW As Integer = 34
    Private Const PadX As Integer = 8

    Public Event ValueChanged As EventHandler

    Public Sub New()
        SetStyle(ControlStyles.UserPaint Or
                 ControlStyles.AllPaintingInWmPaint Or
                 ControlStyles.OptimizedDoubleBuffer Or
                 ControlStyles.ResizeRedraw Or
                 ControlStyles.Selectable, True)
        Height = 26
        Cursor = Cursors.Hand
        BackColor = UiTheme.BgSidebar
        ForeColor = UiTheme.Txt
    End Sub

    Public Property Minimum As Decimal
        Get
            Return _min
        End Get
        Set(value As Decimal)
            _min = value
            If _val < _min Then Me.Value = _min
            Invalidate()
        End Set
    End Property

    Public Property Maximum As Decimal
        Get
            Return _max
        End Get
        Set(value As Decimal)
            _max = value
            If _val > _max Then Me.Value = _max
            Invalidate()
        End Set
    End Property

    Public Property Increment As Decimal
        Get
            Return _inc
        End Get
        Set(value As Decimal)
            If value > 0D Then _inc = value
        End Set
    End Property

    Public Property DecimalPlaces As Integer
        Get
            Return _decimals
        End Get
        Set(value As Integer)
            _decimals = Math.Max(0, value)
            Invalidate()
        End Set
    End Property

    Public Property Value As Decimal
        Get
            Return _val
        End Get
        Set(value As Decimal)
            Dim v As Decimal = value
            If v < _min Then v = _min
            If v > _max Then v = _max
            If v <> _val Then
                _val = v
                Invalidate()
                RaiseEvent ValueChanged(Me, EventArgs.Empty)
            End If
        End Set
    End Property

    Private Sub SetValueFromX(x As Integer)
        Dim trackL As Integer = PadX
        Dim trackR As Integer = Width - ValueTextW - 2
        If trackR <= trackL Then Return

        Dim t As Double = (x - trackL) / CDbl(trackR - trackL)
        If t < 0.0 Then t = 0.0
        If t > 1.0 Then t = 1.0

        Dim raw As Decimal = _min + CDec(t) * (_max - _min)
        Dim snapped As Decimal = Math.Round(raw / _inc) * _inc
        snapped = Math.Round(snapped, Math.Max(_decimals, 4))
        Me.Value = snapped
    End Sub

    Protected Overrides Sub OnMouseDown(e As MouseEventArgs)
        MyBase.OnMouseDown(e)
        If Not Enabled Then Return
        Focus()
        dragging = True
        SetValueFromX(e.X)
    End Sub

    Protected Overrides Sub OnMouseMove(e As MouseEventArgs)
        MyBase.OnMouseMove(e)
        If dragging Then SetValueFromX(e.X)
    End Sub

    Protected Overrides Sub OnMouseUp(e As MouseEventArgs)
        MyBase.OnMouseUp(e)
        dragging = False
    End Sub

    Protected Overrides Sub OnMouseWheel(e As MouseEventArgs)
        MyBase.OnMouseWheel(e)
        PerformWheel(e.Delta)
    End Sub

    ' Rotella inoltrata dal message filter quando il cursore e' sopra il controllo.
    Public Sub PerformWheel(delta As Integer)
        If Not Enabled Then Return
        Me.Value = _val + If(delta > 0, _inc, -_inc)
    End Sub

    Protected Overrides Sub OnKeyDown(e As KeyEventArgs)
        MyBase.OnKeyDown(e)
        If Not Enabled Then Return
        If e.KeyCode = Keys.Left OrElse e.KeyCode = Keys.Down Then
            Me.Value = _val - _inc
        ElseIf e.KeyCode = Keys.Right OrElse e.KeyCode = Keys.Up Then
            Me.Value = _val + _inc
        End If
    End Sub

    Protected Overrides Sub OnMouseEnter(e As EventArgs)
        MyBase.OnMouseEnter(e)
        hovering = True
        Invalidate()
    End Sub

    Protected Overrides Sub OnMouseLeave(e As EventArgs)
        MyBase.OnMouseLeave(e)
        hovering = False
        Invalidate()
    End Sub

    Protected Overrides Sub OnEnabledChanged(e As EventArgs)
        MyBase.OnEnabledChanged(e)
        Invalidate()
    End Sub

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        e.Graphics.SmoothingMode = Drawing2D.SmoothingMode.AntiAlias
        e.Graphics.PixelOffsetMode = Drawing2D.PixelOffsetMode.HighQuality
        e.Graphics.Clear(BackColor)

        Dim trackL As Integer = PadX
        Dim trackR As Integer = Width - ValueTextW - 2
        Dim cy As Single = Height / 2.0F

        Dim range As Decimal = _max - _min
        Dim t As Single = 0.0F
        If range > 0D Then t = CSng((_val - _min) / range)

        Dim thumbX As Single = trackL + (trackR - trackL) * t

        Dim trackCol As Color = If(Enabled, UiTheme.Border, Color.FromArgb(34, 32, 90))
        Dim fillCol As Color = If(Enabled, UiTheme.Accent, Color.FromArgb(0, 100, 115))
        Dim thumbCol As Color = If(Not Enabled, Color.FromArgb(0, 110, 125),
                                   If(hovering OrElse dragging, UiTheme.AccentHi, UiTheme.Accent))

        Using p As New Pen(trackCol, 3.0F)
            p.StartCap = Drawing2D.LineCap.Round
            p.EndCap = Drawing2D.LineCap.Round
            e.Graphics.DrawLine(p, trackL, cy, trackR, cy)
        End Using

        If thumbX > trackL Then
            Using p As New Pen(fillCol, 3.0F)
                p.StartCap = Drawing2D.LineCap.Round
                p.EndCap = Drawing2D.LineCap.Round
                e.Graphics.DrawLine(p, trackL, cy, thumbX, cy)
            End Using
        End If

        Dim r As Single = If(hovering OrElse dragging, 6.5F, 5.5F)
        Using b As New SolidBrush(thumbCol)
            e.Graphics.FillEllipse(b, thumbX - r, cy - r, r * 2.0F, r * 2.0F)
        End Using
        Using p As New Pen(BackColor, 1.5F)
            e.Graphics.DrawEllipse(p, thumbX - r, cy - r, r * 2.0F, r * 2.0F)
        End Using

        Dim txt As String = _val.ToString("F" & _decimals, Globalization.CultureInfo.CurrentCulture)
        Using b As New SolidBrush(If(Enabled, UiTheme.Txt, UiTheme.TxtDim))
            Dim sz = e.Graphics.MeasureString(txt, Font)
            e.Graphics.DrawString(txt, Font, b, Width - sz.Width - 2, cy - sz.Height / 2.0F)
        End Using
    End Sub
End Class

' ============================================================
'  Sezione collassabile per la sidebar: header cliccabile (chevron +
'  titolo) e contenuto TableLayoutPanel a colonna singola, compatibile
'  con gli helper AddRowTitle/AddRowControl/AddDoubleRow.
' ============================================================
Public Class CollapsibleSection
    Inherits TableLayoutPanel

    Private ReadOnly headerLbl As New Label()
    Public ReadOnly Content As New TableLayoutPanel()
    Private ReadOnly titleText As String
    Private isOpen As Boolean = True

    ' Stato esposto per la persistenza (apri/chiudi tra sessioni).
    Public ReadOnly Property SectionTitle As String
        Get
            Return titleText
        End Get
    End Property

    Public ReadOnly Property Expanded As Boolean
        Get
            Return isOpen
        End Get
    End Property

    Public Sub New(title As String, Optional startOpen As Boolean = True)
        titleText = title
        isOpen = startOpen

        ColumnCount = 1
        RowCount = 2
        RowStyles.Add(New RowStyle(SizeType.AutoSize))
        RowStyles.Add(New RowStyle(SizeType.AutoSize))
        AutoSize = True
        AutoSizeMode = AutoSizeMode.GrowAndShrink
        Margin = New Padding(0, 2, 0, 2)
        BackColor = UiTheme.BgSidebar
        Width = 244

        headerLbl.AutoSize = False
        headerLbl.Width = 244
        headerLbl.Height = 26
        headerLbl.Margin = New Padding(0, 2, 0, 0)
        headerLbl.TextAlign = ContentAlignment.BottomLeft
        headerLbl.Font = New Font("Segoe UI", 8.0F, FontStyle.Bold)
        headerLbl.Cursor = Cursors.Hand
        headerLbl.BackColor = UiTheme.BgSidebar
        AddHandler headerLbl.Click, AddressOf Header_Click
        AddHandler headerLbl.Paint, AddressOf Header_Paint

        Content.ColumnCount = 1
        Content.RowCount = 0
        Content.AutoSize = True
        Content.AutoSizeMode = AutoSizeMode.GrowAndShrink
        Content.GrowStyle = TableLayoutPanelGrowStyle.AddRows
        Content.Margin = New Padding(0)
        Content.Padding = New Padding(0)
        Content.BackColor = UiTheme.BgSidebar
        Content.Width = 244

        Controls.Add(headerLbl, 0, 0)
        Controls.Add(Content, 0, 1)

        Content.Visible = isOpen
        UpdateHeader()
    End Sub

    Private Sub Header_Click(sender As Object, e As EventArgs)
        isOpen = Not isOpen
        Content.Visible = isOpen
        UpdateHeader()
    End Sub

    Private Sub UpdateHeader()
        Dim chevron As String = If(isOpen, ChrW(&H25BC), ChrW(&H25B6))
        headerLbl.Text = chevron & "  " & titleText.ToUpperInvariant()
        headerLbl.ForeColor = If(isOpen, UiTheme.Accent, UiTheme.TxtDim)
    End Sub

    Private Sub Header_Paint(sender As Object, e As PaintEventArgs)
        ' Sottile linea separatrice sopra l'header.
        Using p As New Pen(UiTheme.Border, 1.0F)
            e.Graphics.DrawLine(p, 0, 0, headerLbl.Width, 0)
        End Using
    End Sub

    ' Riapplica i colori del tema corrente (usato dal toggle dark/light).
    Public Sub RefreshTheme()
        BackColor = UiTheme.BgSidebar
        headerLbl.BackColor = UiTheme.BgSidebar
        Content.BackColor = UiTheme.BgSidebar
        UpdateHeader()
        Invalidate(True)
    End Sub
End Class


' ============================================================
'  Pulsante a tema con angoli arrotondati e stati hover/pressed.
'  I colori arrivano da StyleButton (BackColor/ForeColor/FlatAppearance),
'  quindi l'interfaccia resta quella di Button.
' ============================================================
Public Class ThemedButton
    Inherits Button

    Private hovering As Boolean = False
    Private pressed As Boolean = False

    Public Sub New()
        SetStyle(ControlStyles.UserPaint Or
                 ControlStyles.AllPaintingInWmPaint Or
                 ControlStyles.OptimizedDoubleBuffer, True)
    End Sub

    Protected Overrides Sub OnMouseEnter(e As EventArgs)
        MyBase.OnMouseEnter(e)
        hovering = True
        Invalidate()
    End Sub

    Protected Overrides Sub OnMouseLeave(e As EventArgs)
        MyBase.OnMouseLeave(e)
        hovering = False
        pressed = False
        Invalidate()
    End Sub

    Protected Overrides Sub OnMouseDown(e As MouseEventArgs)
        MyBase.OnMouseDown(e)
        pressed = True
        Invalidate()
    End Sub

    Protected Overrides Sub OnMouseUp(e As MouseEventArgs)
        MyBase.OnMouseUp(e)
        pressed = False
        Invalidate()
    End Sub

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        e.Graphics.SmoothingMode = Drawing2D.SmoothingMode.AntiAlias
        e.Graphics.PixelOffsetMode = Drawing2D.PixelOffsetMode.HighQuality

        ' Sfondo del contenitore dietro gli angoli arrotondati.
        Dim parentBg As Color = If(Parent IsNot Nothing, Parent.BackColor, UiTheme.BgSidebar)
        e.Graphics.Clear(parentBg)

        Dim bg As Color = BackColor
        If Not Enabled Then
            bg = Color.FromArgb(Math.Max(0, bg.R \ 2), Math.Max(0, bg.G \ 2 + 10), Math.Max(0, bg.B \ 2 + 20))
        ElseIf pressed AndAlso Not FlatAppearance.MouseDownBackColor.IsEmpty Then
            bg = FlatAppearance.MouseDownBackColor
        ElseIf hovering AndAlso Not FlatAppearance.MouseOverBackColor.IsEmpty Then
            bg = FlatAppearance.MouseOverBackColor
        End If

        Dim r As New RectangleF(0.5F, 0.5F, Width - 1.0F, Height - 1.0F)
        Using gp = UiTheme.RoundedRect(r, 6.0F)
            Using b As New SolidBrush(bg)
                e.Graphics.FillPath(b, gp)
            End Using
            If FlatAppearance.BorderSize > 0 Then
                Using p As New Pen(If(hovering AndAlso Enabled, UiTheme.Accent, FlatAppearance.BorderColor), 1.0F)
                    e.Graphics.DrawPath(p, gp)
                End Using
            End If
        End Using

        Dim txtCol As Color = If(Enabled, ForeColor, UiTheme.TxtDim)
        TextRenderer.DrawText(e.Graphics, Text, Font, ClientRectangle, txtCol,
                              TextFormatFlags.HorizontalCenter Or TextFormatFlags.VerticalCenter)
    End Sub
End Class

' ============================================================
'  CheckBox a tema: casella scura arrotondata, spunta su fondo accento.
'  Eredita da CheckBox: toggle, eventi e Checked restano quelli standard.
' ============================================================
Public Class ThemedCheckBox
    Inherits CheckBox

    Private hovering As Boolean = False

    Public Sub New()
        SetStyle(ControlStyles.UserPaint Or
                 ControlStyles.AllPaintingInWmPaint Or
                 ControlStyles.OptimizedDoubleBuffer, True)
        Cursor = Cursors.Hand
    End Sub

    Protected Overrides Sub OnMouseEnter(e As EventArgs)
        MyBase.OnMouseEnter(e)
        hovering = True
        Invalidate()
    End Sub

    Protected Overrides Sub OnMouseLeave(e As EventArgs)
        MyBase.OnMouseLeave(e)
        hovering = False
        Invalidate()
    End Sub

    Protected Overrides Sub OnCheckedChanged(e As EventArgs)
        MyBase.OnCheckedChanged(e)
        Invalidate()
    End Sub

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        e.Graphics.SmoothingMode = Drawing2D.SmoothingMode.AntiAlias
        e.Graphics.PixelOffsetMode = Drawing2D.PixelOffsetMode.HighQuality
        e.Graphics.Clear(BackColor)

        Dim boxSize As Single = 15.0F
        Dim by As Single = (Height - boxSize) / 2.0F
        Dim box As New RectangleF(1.0F, by, boxSize, boxSize)

        Using gp = UiTheme.RoundedRect(box, 3.0F)
            If Checked Then
                Dim fill As Color = If(Not Enabled, Color.FromArgb(0, 110, 125),
                                       If(hovering, UiTheme.AccentHi, UiTheme.Accent))
                Using b As New SolidBrush(fill)
                    e.Graphics.FillPath(b, gp)
                End Using
            Else
                Using b As New SolidBrush(If(hovering AndAlso Enabled, UiTheme.BgFieldHi, UiTheme.BgField))
                    e.Graphics.FillPath(b, gp)
                End Using
                Using p As New Pen(If(hovering AndAlso Enabled, UiTheme.Accent, UiTheme.Border), 1.0F)
                    e.Graphics.DrawPath(p, gp)
                End Using
            End If
        End Using

        If Checked Then
            Using p As New Pen(UiTheme.BgCanvas, 2.0F)
                p.StartCap = Drawing2D.LineCap.Round
                p.EndCap = Drawing2D.LineCap.Round
                p.LineJoin = Drawing2D.LineJoin.Round
                e.Graphics.DrawLines(p, New PointF() {
                    New PointF(box.X + 3.5F, box.Y + 8.0F),
                    New PointF(box.X + 6.5F, box.Y + 11.0F),
                    New PointF(box.X + 11.5F, box.Y + 4.5F)
                })
            End Using
        End If

        Dim txtCol As Color = If(Enabled, ForeColor, UiTheme.TxtDim)
        Dim txtRect As New Rectangle(CInt(boxSize) + 7, 0, Width - CInt(boxSize) - 7, Height)
        TextRenderer.DrawText(e.Graphics, Text, Font, txtRect, txtCol,
                              TextFormatFlags.Left Or TextFormatFlags.VerticalCenter)
    End Sub
End Class

' ============================================================
'  NumericUpDown a tema: campo di testo scuro + frecce disegnate.
'  Espone la stessa interfaccia usata dal resto del codice
'  (Minimum/Maximum/Value/Increment/DecimalPlaces, ValueChanged).
' ============================================================
Public Class ThemedNumericUpDown
    Inherits Control

    Private ReadOnly box As New TextBox()
    Private _min As Decimal = 0D
    Private _max As Decimal = 100D
    Private _val As Decimal = 0D
    Private _inc As Decimal = 1D
    Private _decimals As Integer = 0
    Private updatingText As Boolean = False
    Private hoverZone As Integer = 0    ' 0 nessuna, 1 su, 2 giu

    Private Const ArrowW As Integer = 18

    Public Event ValueChanged As EventHandler

    Public Sub New()
        SetStyle(ControlStyles.UserPaint Or
                 ControlStyles.AllPaintingInWmPaint Or
                 ControlStyles.OptimizedDoubleBuffer Or
                 ControlStyles.ResizeRedraw, True)
        Height = 26
        BackColor = UiTheme.BgField

        box.BorderStyle = BorderStyle.None
        box.BackColor = UiTheme.BgField
        box.ForeColor = UiTheme.Txt
        box.TextAlign = HorizontalAlignment.Left
        AddHandler box.KeyDown, AddressOf Box_KeyDown
        AddHandler box.Leave, AddressOf Box_Leave
        Controls.Add(box)

        LayoutBox()
        SyncText()
    End Sub

    Public Property Minimum As Decimal
        Get
            Return _min
        End Get
        Set(value As Decimal)
            _min = value
            If _val < _min Then Me.Value = _min
        End Set
    End Property

    Public Property Maximum As Decimal
        Get
            Return _max
        End Get
        Set(value As Decimal)
            _max = value
            If _val > _max Then Me.Value = _max
        End Set
    End Property

    Public Property Increment As Decimal
        Get
            Return _inc
        End Get
        Set(value As Decimal)
            If value > 0D Then _inc = value
        End Set
    End Property

    Public Property DecimalPlaces As Integer
        Get
            Return _decimals
        End Get
        Set(value As Integer)
            _decimals = Math.Max(0, value)
            SyncText()
        End Set
    End Property

    Public Property Value As Decimal
        Get
            Return _val
        End Get
        Set(value As Decimal)
            Dim v As Decimal = value
            If v < _min Then v = _min
            If v > _max Then v = _max
            If v <> _val Then
                _val = v
                SyncText()
                RaiseEvent ValueChanged(Me, EventArgs.Empty)
            Else
                SyncText()
            End If
        End Set
    End Property

    Private Sub LayoutBox()
        box.Location = New Point(8, (Height - box.Height) \ 2)
        box.Width = Math.Max(10, Width - ArrowW - 14)
    End Sub

    Private Sub SyncText()
        updatingText = True
        box.Text = _val.ToString("F" & _decimals, Globalization.CultureInfo.CurrentCulture)
        updatingText = False
    End Sub

    Private Sub CommitText()
        If updatingText Then Return
        Dim v As Decimal
        If Decimal.TryParse(box.Text, Globalization.NumberStyles.Number,
                            Globalization.CultureInfo.CurrentCulture, v) OrElse
           Decimal.TryParse(box.Text, Globalization.NumberStyles.Number,
                            Globalization.CultureInfo.InvariantCulture, v) Then
            Me.Value = v
        Else
            SyncText()
        End If
    End Sub

    Private Sub Box_KeyDown(sender As Object, e As KeyEventArgs)
        If e.KeyCode = Keys.Enter Then
            CommitText()
            e.Handled = True
            e.SuppressKeyPress = True
        ElseIf e.KeyCode = Keys.Up Then
            Me.Value = _val + _inc
            e.Handled = True
        ElseIf e.KeyCode = Keys.Down Then
            Me.Value = _val - _inc
            e.Handled = True
        End If
    End Sub

    Private Sub Box_Leave(sender As Object, e As EventArgs)
        CommitText()
    End Sub

    Protected Overrides Sub OnResize(e As EventArgs)
        MyBase.OnResize(e)
        LayoutBox()
    End Sub

    Protected Overrides Sub OnEnabledChanged(e As EventArgs)
        MyBase.OnEnabledChanged(e)
        box.Enabled = Enabled
        box.BackColor = UiTheme.BgField
        box.ForeColor = If(Enabled, UiTheme.Txt, UiTheme.TxtDim)
        Invalidate()
    End Sub

    ' Riapplica i colori del tema corrente alla TextBox interna.
    Public Sub RefreshTheme()
        BackColor = UiTheme.BgField
        box.BackColor = UiTheme.BgField
        box.ForeColor = If(Enabled, UiTheme.Txt, UiTheme.TxtDim)
        Invalidate()
    End Sub

    Protected Overrides Sub OnMouseMove(e As MouseEventArgs)
        MyBase.OnMouseMove(e)
        Dim z As Integer = 0
        If e.X >= Width - ArrowW Then
            z = If(e.Y < Height \ 2, 1, 2)
        End If
        If z <> hoverZone Then
            hoverZone = z
            Invalidate()
        End If
    End Sub

    Protected Overrides Sub OnMouseLeave(e As EventArgs)
        MyBase.OnMouseLeave(e)
        If hoverZone <> 0 Then
            hoverZone = 0
            Invalidate()
        End If
    End Sub

    Protected Overrides Sub OnMouseDown(e As MouseEventArgs)
        MyBase.OnMouseDown(e)
        If Not Enabled Then Return
        If e.X >= Width - ArrowW Then
            CommitText()
            If e.Y < Height \ 2 Then
                Me.Value = _val + _inc
            Else
                Me.Value = _val - _inc
            End If
        End If
    End Sub

    Protected Overrides Sub OnMouseWheel(e As MouseEventArgs)
        MyBase.OnMouseWheel(e)
        PerformWheel(e.Delta)
    End Sub

    ' Rotella inoltrata dal message filter quando il cursore e' sopra il controllo.
    Public Sub PerformWheel(delta As Integer)
        If Not Enabled Then Return
        CommitText()
        Me.Value = _val + If(delta > 0, _inc, -_inc)
    End Sub

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        e.Graphics.SmoothingMode = Drawing2D.SmoothingMode.AntiAlias
        e.Graphics.PixelOffsetMode = Drawing2D.PixelOffsetMode.HighQuality

        Dim parentBg As Color = If(Parent IsNot Nothing, Parent.BackColor, UiTheme.BgSidebar)
        e.Graphics.Clear(parentBg)

        Dim r As New RectangleF(0.5F, 0.5F, Width - 1.0F, Height - 1.0F)
        Using gp = UiTheme.RoundedRect(r, 5.0F)
            Using b As New SolidBrush(UiTheme.BgField)
                e.Graphics.FillPath(b, gp)
            End Using
            Using p As New Pen(UiTheme.Border, 1.0F)
                e.Graphics.DrawPath(p, gp)
            End Using
        End Using

        ' Zone frecce (evidenziate al passaggio).
        Dim ax As Integer = Width - ArrowW
        If hoverZone = 1 Then
            Using b As New SolidBrush(UiTheme.BgFieldHi)
                e.Graphics.FillRectangle(b, ax, 2, ArrowW - 3, Height \ 2 - 2)
            End Using
        ElseIf hoverZone = 2 Then
            Using b As New SolidBrush(UiTheme.BgFieldHi)
                e.Graphics.FillRectangle(b, ax, Height \ 2, ArrowW - 3, Height \ 2 - 3)
            End Using
        End If

        Dim arrowCol As Color = If(Enabled, UiTheme.TxtDim, UiTheme.Border)
        Using p As New Pen(arrowCol, 1.4F)
            p.StartCap = Drawing2D.LineCap.Round
            p.EndCap = Drawing2D.LineCap.Round
            Dim cxp As Single = ax + (ArrowW - 3) / 2.0F
            ' freccia su
            e.Graphics.DrawLines(p, New PointF() {
                New PointF(cxp - 3.2F, Height * 0.25F + 1.6F),
                New PointF(cxp, Height * 0.25F - 1.6F),
                New PointF(cxp + 3.2F, Height * 0.25F + 1.6F)})
            ' freccia giu
            e.Graphics.DrawLines(p, New PointF() {
                New PointF(cxp - 3.2F, Height * 0.75F - 1.6F),
                New PointF(cxp, Height * 0.75F + 1.6F),
                New PointF(cxp + 3.2F, Height * 0.75F - 1.6F)})
        End Using
    End Sub
End Class

' ============================================================
'  ComboBox a tema con tendina scura. Espone l'interfaccia usata dal
'  codice: Items (AddRange), SelectedItem, SelectedIndex, DropDownStyle
'  (ignorata), evento SelectedIndexChanged.
' ============================================================
Public Class ThemedComboBox
    Inherits Control

    Public ReadOnly Property Items As New List(Of Object)
    Private _selIndex As Integer = -1
    Private hovering As Boolean = False
    Private dropDown As ToolStripDropDown = Nothing

    Public Event SelectedIndexChanged As EventHandler

    ' Compatibilita' con ComboBox: la tendina e' sempre in stile DropDownList.
    Public Property DropDownStyle As ComboBoxStyle = ComboBoxStyle.DropDownList

    Public Sub New()
        SetStyle(ControlStyles.UserPaint Or
                 ControlStyles.AllPaintingInWmPaint Or
                 ControlStyles.OptimizedDoubleBuffer Or
                 ControlStyles.ResizeRedraw Or
                 ControlStyles.Selectable, True)
        Height = 26
        Cursor = Cursors.Hand
        BackColor = UiTheme.BgField
        ForeColor = UiTheme.Txt
    End Sub

    Public Property SelectedIndex As Integer
        Get
            Return _selIndex
        End Get
        Set(value As Integer)
            Dim v As Integer = value
            If v < -1 Then v = -1
            If v >= Items.Count Then v = Items.Count - 1
            If v <> _selIndex Then
                _selIndex = v
                Invalidate()
                RaiseEvent SelectedIndexChanged(Me, EventArgs.Empty)
            End If
        End Set
    End Property

    Public Property SelectedItem As Object
        Get
            If _selIndex >= 0 AndAlso _selIndex < Items.Count Then Return Items(_selIndex)
            Return Nothing
        End Get
        Set(value As Object)
            If value Is Nothing Then
                SelectedIndex = -1
                Return
            End If
            Dim wanted As String = value.ToString()
            For i As Integer = 0 To Items.Count - 1
                If String.Equals(Items(i).ToString(), wanted, StringComparison.Ordinal) Then
                    SelectedIndex = i
                    Return
                End If
            Next
        End Set
    End Property

    Protected Overrides Sub OnMouseEnter(e As EventArgs)
        MyBase.OnMouseEnter(e)
        hovering = True
        Invalidate()
    End Sub

    Protected Overrides Sub OnMouseLeave(e As EventArgs)
        MyBase.OnMouseLeave(e)
        hovering = False
        Invalidate()
    End Sub

    Protected Overrides Sub OnMouseDown(e As MouseEventArgs)
        MyBase.OnMouseDown(e)
        If Not Enabled Then Return
        Focus()
        OpenDropDown()
    End Sub

    Protected Overrides Sub OnMouseWheel(e As MouseEventArgs)
        MyBase.OnMouseWheel(e)
        PerformWheel(e.Delta)
    End Sub

    ' Rotella (diretta o inoltrata dal message filter): scorre le voci.
    Public Sub PerformWheel(delta As Integer)
        If Not Enabled OrElse Items.Count = 0 Then Return
        If delta > 0 Then
            If _selIndex > 0 Then
                SelectedIndex = _selIndex - 1
            ElseIf _selIndex < 0 Then
                SelectedIndex = 0
            End If
        Else
            If _selIndex < Items.Count - 1 Then SelectedIndex = _selIndex + 1
        End If
    End Sub

    Protected Overrides Sub OnKeyDown(e As KeyEventArgs)
        MyBase.OnKeyDown(e)
        If Not Enabled Then Return
        If e.KeyCode = Keys.Down AndAlso e.Alt Then
            OpenDropDown()
            e.Handled = True
        ElseIf e.KeyCode = Keys.Down Then
            If _selIndex < Items.Count - 1 Then SelectedIndex = _selIndex + 1
            e.Handled = True
        ElseIf e.KeyCode = Keys.Up Then
            If _selIndex > 0 Then SelectedIndex = _selIndex - 1
            e.Handled = True
        ElseIf e.KeyCode = Keys.Enter OrElse e.KeyCode = Keys.Space Then
            OpenDropDown()
            e.Handled = True
        End If
    End Sub

    Private Sub OpenDropDown()
        If Items.Count = 0 Then Return
        If dropDown IsNot Nothing AndAlso dropDown.Visible Then
            dropDown.Close()
            Return
        End If

        Dim lst As New ListBox()
        lst.BorderStyle = BorderStyle.None
        lst.BackColor = UiTheme.BgField
        lst.ForeColor = UiTheme.Txt
        lst.Font = Font
        lst.DrawMode = DrawMode.OwnerDrawFixed
        lst.ItemHeight = 20
        lst.IntegralHeight = False
        For Each it In Items
            lst.Items.Add(it.ToString())
        Next
        lst.SelectedIndex = _selIndex
        lst.Width = Math.Max(Width - 2, 60)
        lst.Height = Math.Min(lst.ItemHeight * Items.Count + 4, 320)

        AddHandler lst.DrawItem, AddressOf List_DrawItem
        AddHandler lst.MouseMove, Sub(s, ev)
                                      Dim idx = lst.IndexFromPoint(ev.Location)
                                      If idx >= 0 AndAlso idx <> lst.SelectedIndex Then lst.SelectedIndex = idx
                                  End Sub
        AddHandler lst.MouseUp, Sub(s, ev)
                                    Dim idx = lst.IndexFromPoint(ev.Location)
                                    If idx >= 0 Then
                                        SelectedIndex = idx
                                        dropDown.Close()
                                    End If
                                End Sub
        AddHandler lst.KeyDown, Sub(s, ev)
                                    If ev.KeyCode = Keys.Enter Then
                                        If lst.SelectedIndex >= 0 Then SelectedIndex = lst.SelectedIndex
                                        dropDown.Close()
                                    ElseIf ev.KeyCode = Keys.Escape Then
                                        dropDown.Close()
                                    End If
                                End Sub

        Dim border As New Panel()
        border.BackColor = UiTheme.Border
        border.Padding = New Padding(1)
        border.Width = lst.Width + 2
        border.Height = lst.Height + 2
        lst.Dock = DockStyle.Fill
        border.Controls.Add(lst)

        Dim host As New ToolStripControlHost(border)
        host.Margin = Padding.Empty
        host.Padding = Padding.Empty
        host.AutoSize = False
        host.Size = border.Size

        dropDown = New ToolStripDropDown()
        dropDown.Padding = Padding.Empty
        dropDown.Margin = Padding.Empty
        dropDown.AutoSize = False
        dropDown.Size = border.Size
        dropDown.DropShadowEnabled = True
        dropDown.Items.Add(host)

        dropDown.Show(Me, New Point(0, Height))
        lst.Focus()
    End Sub

    Private Sub List_DrawItem(sender As Object, e As DrawItemEventArgs)
        If e.Index < 0 Then Return
        Dim lst = DirectCast(sender, ListBox)
        Dim selected As Boolean = (e.State And DrawItemState.Selected) = DrawItemState.Selected

        Using b As New SolidBrush(If(selected, UiTheme.BgFieldHi, UiTheme.BgField))
            e.Graphics.FillRectangle(b, e.Bounds)
        End Using
        Using tb As New SolidBrush(If(selected, UiTheme.AccentHi, UiTheme.Txt))
            e.Graphics.DrawString(lst.Items(e.Index).ToString(), lst.Font, tb,
                                  e.Bounds.X + 6, e.Bounds.Y + 2)
        End Using
    End Sub

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        e.Graphics.SmoothingMode = Drawing2D.SmoothingMode.AntiAlias
        e.Graphics.PixelOffsetMode = Drawing2D.PixelOffsetMode.HighQuality

        Dim parentBg As Color = If(Parent IsNot Nothing, Parent.BackColor, UiTheme.BgSidebar)
        e.Graphics.Clear(parentBg)

        Dim r As New RectangleF(0.5F, 0.5F, Width - 1.0F, Height - 1.0F)
        Using gp = UiTheme.RoundedRect(r, 5.0F)
            Using b As New SolidBrush(If(hovering AndAlso Enabled, UiTheme.BgFieldHi, UiTheme.BgField))
                e.Graphics.FillPath(b, gp)
            End Using
            Using p As New Pen(If(hovering AndAlso Enabled, UiTheme.Accent, UiTheme.Border), 1.0F)
                e.Graphics.DrawPath(p, gp)
            End Using
        End Using

        Dim txt As String = If(SelectedItem Is Nothing, "", SelectedItem.ToString())
        Dim txtCol As Color = If(Enabled, UiTheme.Txt, UiTheme.TxtDim)
        Dim txtRect As New Rectangle(8, 0, Width - 28, Height)
        TextRenderer.DrawText(e.Graphics, txt, Font, txtRect, txtCol,
                              TextFormatFlags.Left Or TextFormatFlags.VerticalCenter Or TextFormatFlags.EndEllipsis)

        ' Chevron
        Dim chevCol As Color = If(Enabled, UiTheme.TxtDim, UiTheme.Border)
        Using p As New Pen(chevCol, 1.6F)
            p.StartCap = Drawing2D.LineCap.Round
            p.EndCap = Drawing2D.LineCap.Round
            Dim cxp As Single = Width - 15
            Dim cyp As Single = Height / 2.0F - 2.0F
            e.Graphics.DrawLines(p, New PointF() {
                New PointF(cxp - 4.0F, cyp),
                New PointF(cxp, cyp + 4.5F),
                New PointF(cxp + 4.0F, cyp)})
        End Using
    End Sub
End Class


' ============================================================
'  Scrollbar verticale a tema (thumb navy, hover chiaro, drag accento).
'  Usata dalla sidebar al posto della scrollbar di sistema.
' ============================================================
Public Class ThemedVScrollBar
    Inherits Control

    Private _content As Integer = 0
    Private _viewport As Integer = 0
    Private _val As Integer = 0
    Private dragging As Boolean = False
    Private dragOffset As Integer = 0
    Private hovering As Boolean = False

    Public Event ScrollChanged As EventHandler

    Public Sub New()
        SetStyle(ControlStyles.UserPaint Or
                 ControlStyles.AllPaintingInWmPaint Or
                 ControlStyles.OptimizedDoubleBuffer Or
                 ControlStyles.ResizeRedraw, True)
        Width = 8
        BackColor = UiTheme.BgSidebar
        Cursor = Cursors.Hand
    End Sub

    Public Property ContentSize As Integer
        Get
            Return _content
        End Get
        Set(value As Integer)
            _content = Math.Max(0, value)
            ClampValue()
            Invalidate()
        End Set
    End Property

    Public Property ViewportSize As Integer
        Get
            Return _viewport
        End Get
        Set(value As Integer)
            _viewport = Math.Max(0, value)
            ClampValue()
            Invalidate()
        End Set
    End Property

    Public ReadOnly Property MaxScroll As Integer
        Get
            Return Math.Max(0, _content - _viewport)
        End Get
    End Property

    Public Property Value As Integer
        Get
            Return _val
        End Get
        Set(value As Integer)
            Dim v As Integer = value
            If v < 0 Then v = 0
            If v > MaxScroll Then v = MaxScroll
            If v <> _val Then
                _val = v
                Invalidate()
                RaiseEvent ScrollChanged(Me, EventArgs.Empty)
            End If
        End Set
    End Property

    Private Sub ClampValue()
        If _val > MaxScroll Then Value = MaxScroll
    End Sub

    Private Function ThumbRect() As Rectangle
        Dim trackH As Integer = Math.Max(0, Height - 4)
        If _content <= 0 OrElse trackH <= 0 Then Return Rectangle.Empty

        Dim th As Integer = Math.Max(24, CInt(CLng(trackH) * _viewport \ Math.Max(_content, 1)))
        th = Math.Min(th, trackH)
        Dim travel As Integer = trackH - th
        Dim y As Integer = 2
        If MaxScroll > 0 AndAlso travel > 0 Then
            y = 2 + CInt(CLng(travel) * _val \ MaxScroll)
        End If
        Return New Rectangle(0, y, Width, th)
    End Function

    Protected Overrides Sub OnMouseDown(e As MouseEventArgs)
        MyBase.OnMouseDown(e)
        Dim tr = ThumbRect()
        If tr.Contains(e.Location) Then
            dragging = True
            dragOffset = e.Y - tr.Y
        ElseIf e.Y < tr.Y Then
            Value = _val - _viewport
        Else
            Value = _val + _viewport
        End If
        Invalidate()
    End Sub

    Protected Overrides Sub OnMouseMove(e As MouseEventArgs)
        MyBase.OnMouseMove(e)
        If dragging Then
            Dim tr = ThumbRect()
            Dim trackH As Integer = Math.Max(0, Height - 4)
            Dim travel As Integer = trackH - tr.Height
            If travel > 0 Then
                Dim y As Integer = e.Y - dragOffset - 2
                Value = CInt(CLng(Math.Max(0, Math.Min(travel, y))) * MaxScroll \ travel)
            End If
        End If
    End Sub

    Protected Overrides Sub OnMouseUp(e As MouseEventArgs)
        MyBase.OnMouseUp(e)
        dragging = False
        Invalidate()
    End Sub

    Protected Overrides Sub OnMouseEnter(e As EventArgs)
        MyBase.OnMouseEnter(e)
        hovering = True
        Invalidate()
    End Sub

    Protected Overrides Sub OnMouseLeave(e As EventArgs)
        MyBase.OnMouseLeave(e)
        hovering = False
        Invalidate()
    End Sub

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        e.Graphics.SmoothingMode = Drawing2D.SmoothingMode.AntiAlias
        e.Graphics.PixelOffsetMode = Drawing2D.PixelOffsetMode.HighQuality
        e.Graphics.Clear(BackColor)

        Dim tr = ThumbRect()
        If tr.IsEmpty Then Return

        Dim col As Color
        If dragging Then
            col = UiTheme.Accent
        ElseIf hovering Then
            col = Color.FromArgb(74, 70, 158)
        Else
            col = UiTheme.Border
        End If

        Using gp = UiTheme.RoundedRect(New RectangleF(tr.X + 0.5F, tr.Y + 0.5F, tr.Width - 1.0F, tr.Height - 1.0F), 3.5F)
            Using b As New SolidBrush(col)
                e.Graphics.FillPath(b, gp)
            End Using
        End Using
    End Sub
End Class


' ============================================================
'  Finestra di aiuto: comandi del mouse e descrizione dei controlli.
' ============================================================
Public Class HelpForm
    Inherits Form

    Private ReadOnly rtb As New RichTextBox()
    Private ReadOnly host As New Panel()
    Private ReadOnly helpScroll As New ThemedVScrollBar()
    Private wheelFilter As WheelToScrollFilter = Nothing

    Public Sub New()
        Text = "SE-Voronoi - Help"
        StartPosition = FormStartPosition.CenterParent
        Width = 640
        Height = 720
        MinimumSize = New Size(480, 400)
        BackColor = UiTheme.BgSidebar
        Font = New Font("Segoe UI", 9.0F)

        Try
            Icon = My.Resources.SE_Voronoi
        Catch
        End Try

        ' Il RichTextBox non scrolla da solo: viene reso alto quanto il suo
        ' contenuto (ContentsResized) e traslato dalla scrollbar custom.
        host.Dock = DockStyle.Fill

        rtb.ReadOnly = True
        rtb.BorderStyle = BorderStyle.None
        rtb.DetectUrls = False
        rtb.TabStop = False
        rtb.ScrollBars = RichTextBoxScrollBars.None
        rtb.WordWrap = True
        rtb.Location = New Point(16, 12)

        helpScroll.Dock = DockStyle.Right
        AddHandler helpScroll.ScrollChanged, Sub(sc, ev) rtb.Top = 12 - helpScroll.Value
        AddHandler host.Resize, AddressOf Host_Resize
        AddHandler rtb.ContentsResized, AddressOf Rtb_ContentsResized

        host.Controls.Add(rtb)
        Controls.Add(host)
        Controls.Add(helpScroll)

        ApplyChromeColors()
        BuildContent(rtb)
        rtb.SelectionStart = 0
        rtb.SelectionLength = 0
    End Sub

    Private Sub Host_Resize(sender As Object, e As EventArgs)
        rtb.Width = Math.Max(120, host.ClientSize.Width - 32)
        UpdateHelpScroll()
    End Sub

    Private Sub Rtb_ContentsResized(sender As Object, e As ContentsResizedEventArgs)
        rtb.Height = e.NewRectangle.Height + 16
        UpdateHelpScroll()
    End Sub

    Private Sub UpdateHelpScroll()
        Dim contentH As Integer = rtb.Height + 24   ' margini sopra/sotto
        helpScroll.ContentSize = contentH
        helpScroll.ViewportSize = host.ClientSize.Height
        Dim needed As Boolean = contentH > host.ClientSize.Height
        helpScroll.Visible = needed
        If Not needed Then
            helpScroll.Value = 0
            rtb.Top = 12
        End If
    End Sub

    Private Sub ApplyChromeColors()
        BackColor = UiTheme.BgSidebar
        host.BackColor = UiTheme.BgSidebar
        rtb.BackColor = UiTheme.BgSidebar
        rtb.ForeColor = UiTheme.Txt
        helpScroll.BackColor = UiTheme.BgSidebar
    End Sub

    ' Riapplica il tema corrente (chiamata dal toggle se la finestra e' aperta):
    ' i colori del testo sono nel contenuto, quindi va ricostruito.
    Public Sub RefreshTheme()
        ApplyChromeColors()
        rtb.Clear()
        BuildContent(rtb)
        rtb.SelectionStart = 0
        rtb.SelectionLength = 0
        If IsHandleCreated Then
            UiTheme.ApplyTitleBarTheme(Handle)
        End If
        Refresh()
    End Sub

    Protected Overrides Sub OnLoad(e As EventArgs)
        MyBase.OnLoad(e)
        UiTheme.ApplyTitleBarTheme(Handle)
        Host_Resize(host, EventArgs.Empty)
        wheelFilter = New WheelToScrollFilter(host, helpScroll)
        Application.AddMessageFilter(wheelFilter)
    End Sub

    Protected Overrides Sub OnFormClosed(e As FormClosedEventArgs)
        If wheelFilter IsNot Nothing Then
            Application.RemoveMessageFilter(wheelFilter)
        End If
        MyBase.OnFormClosed(e)
    End Sub

    Private Sub Heading(rtb As RichTextBox, text As String)
        rtb.SelectionFont = New Font("Segoe UI", 10.0F, FontStyle.Bold)
        rtb.SelectionColor = UiTheme.Accent
        rtb.AppendText(text & Environment.NewLine)
    End Sub

    Private Sub Item(rtb As RichTextBox, keys As String, desc As String)
        rtb.SelectionFont = New Font("Segoe UI", 9.0F, FontStyle.Bold)
        rtb.SelectionColor = UiTheme.Txt
        rtb.AppendText("  " & keys)
        rtb.SelectionFont = New Font("Segoe UI", 9.0F)
        rtb.SelectionColor = UiTheme.TxtDim
        rtb.AppendText("  -  " & desc & Environment.NewLine)
    End Sub

    Private Sub Gap(rtb As RichTextBox)
        rtb.SelectionFont = New Font("Segoe UI", 4.0F)
        rtb.AppendText(Environment.NewLine)
    End Sub

    Private Sub BuildContent(rtb As RichTextBox)
        Heading(rtb, "MOUSE - CANVAS")
        Item(rtb, "Left drag on a seed", "move the seed (diagram rebuilds live)")
        Item(rtb, "Ctrl+Left click / Double click", "add a seed at the cursor")
        Item(rtb, "Right click on a seed", "remove the seed")
        Item(rtb, "Wheel over a cell", "scale that cell's symbol/block")
        Item(rtb, "Shift+Wheel", "rotate that cell's symbol/block")
        Item(rtb, "Ctrl+Wheel", "cycle to the next/previous symbol or block (per cell, persistent)")
        Item(rtb, "Alt+Wheel", "cycle the cell color: Auto + the named palette (any style, persistent)")
        Item(rtb, "Drag profile point", "reshape the profile: cells and seeds follow live; the drag stops at the window border (zoom or pan yourself to go further), and ALT+RMB refits the view on the current profile")
        Item(rtb, "Starting profile", "a new project starts with an editable rectangular profile; reading a profile from a file or Solid Edge replaces it entirely")
        Item(rtb, "Double-click on profile edge", "insert a new point on that edge")
        Item(rtb, "Alt+Click on profile point", "delete the point (each loop keeps at least 3)")
        Item(rtb, "Ctrl+Right drag", "zoom, Solid Edge style: forward = zoom out, backward = zoom in; anchored at the cursor")
        Item(rtb, "Ctrl+Shift+Right drag", "pan; press/release Shift during the drag to switch between zoom and pan")
        Item(rtb, "Alt+Right click", "reset the view (fit to domain)")
        Item(rtb, "Left click inside a cell", "select it: the SELECTED CELL panel shows its exact values")
        Gap(rtb)

        Heading(rtb, "MOUSE - SIDEBAR")
        Item(rtb, "Wheel over a combo box", "change the selected value")
        Item(rtb, "Wheel over a slider / numeric field", "adjust the value")
        Item(rtb, "Wheel elsewhere", "scroll the sidebar (when the scrollbar is visible); all action buttons live in the top toolbar")
        Item(rtb, "Click a section header", "collapse / expand the section")
        Gap(rtb)

        Heading(rtb, "TOOLBAR")
        Item(rtb, "New / Open / Save", "project files (.sevproj): settings, sketch profile, seeds with per-cell properties and blocks, all in one file; the title bar shows the open project (* = unsaved changes) and closing prompts to save")
        Item(rtb, "Generate / New Seed", "rebuild the diagram; New Seed increments Random Seed first")
        Item(rtb, "Read Sketch", "reads the active Solid Edge sketch as the generation domain (holes supported)")
        Item(rtb, "Read SE Blocks", "imports block definitions from the document (additive, deduplicated by name)")
        Item(rtb, "Block scale (library)", "each block tile has a scale factor next to Remove: it multiplies the size computed from the cell, everywhere the block is used (canvas, export, Solid Edge occurrences)")
        Item(rtb, "Load / Save / Clear", "block library files (.sevb); Clear empties the memory")
        Item(rtb, "Library", "preview gallery of loaded blocks, with per-block removal")
        Item(rtb, "SVG / DXF", "vector output of the current geometry")
        Item(rtb, "PNG", "2000 px image of the whole domain (ignores zoom/pan)")
        Item(rtb, "To Solid Edge", "draws into the active sketch (missing block definitions are created)")
        Item(rtb, "As blocks", "checkbox next to To Solid Edge: emits block occurrences instead of raw geometry")
        Item(rtb, "Profile", "To Solid Edge only (like As blocks): also draws the sketch boundary loops (outer contours and holes) into the target sketch; PNG and SVG follow the DISPLAY section instead")
        Item(rtb, "Dark theme / Help", "right side of the toolbar: palette toggle and this window")
        Gap(rtb)

        Heading(rtb, "GENERATION")
        Item(rtb, "Seed Placement", "seed distribution: Random; RandomNearBorders / RandomFarBorders (weighted); CircularGrid (concentric rings); RectangularGrid and Staggered (odd rows shifted half a step)")
        Item(rtb, "Cell Count", "requested number of seeds (grid patterns approximate it)")
        Item(rtb, "Random Seed", "generator seed: the same number reproduces the same pattern")
        Item(rtb, "Periodic X / Y", "wrap the pattern on the domain edges (cylinder / torus): cells are computed with ghost seeds so opposite edges continue seamlessly; intrinsically rectangular, so while active the profile is ignored and the domain rectangle (shown dashed) is used")
        Item(rtb, "Full boundary cells", "draw boundary cells whole, without cutting them on the domain border: works on the rectangle and on sketch profiles (holes included); with Periodic X the overflow lands exactly on the opposite edge when the sketch is wrapped on a cylinder")
        Item(rtb, "Relax", "Lloyd relaxation iterations, evens out cell sizes (0 keeps patterns exact)")
        Item(rtb, "Cell Scale", "global symbol size relative to each cell")
        Gap(rtb)

        Heading(rtb, "STYLE")
        Item(rtb, "Cell Style", "Straight or Curved cells, fixed symbols, Random (stable per cell) or BlockSymbol (uses the blocks in memory)")
        Item(rtb, "Vertex Mode", "inner-curve corners: Sharp corner, Arc fillet or Spline curve")
        Item(rtb, "Vertex Size", "amount of corner trimming")
        Item(rtb, "Inner Offset", "inset of the inner curve from the cell borders")
        Item(rtb, "Domain Color", "background of the profile/rectangle: Theme follows the active theme, or pick a named dark (or paper) tone from the extended palette")
        Item(rtb, "Curve Width", "stroke width (canvas, exports and block previews)")
        Item(rtb, "Domain fill", "fill the sketch profile or the starting rectangle with the Domain Color (STYLE section); the background around it always follows the theme")
        Item(rtb, "Fill cells", "translucent background of each cell")
        Item(rtb, "Fill symbols", "even-odd fill of symbols/blocks: nested profiles become holes")
        Item(rtb, "Random symbol rotation", "stable random rotation per cell")
        Gap(rtb)

        Heading(rtb, "SELECTED CELL")
        Item(rtb, "Info line", "index and world coordinates of the selected seed")
        Item(rtb, "Scale / Rotation / Symbol Offset", "exact per-cell values, same data edited by the mouse wheel")
        Item(rtb, "Color", "palette color for the selected cell (Auto = automatic by cell index); applies to fill and strokes")
        Item(rtb, "Pin seed", "locks the seed position: it cannot be dragged and Generate/Relax keep it in place (white ring marker)")
        Gap(rtb)

        Heading(rtb, "DISPLAY")
        Item(rtb, "Show outer edges / seeds / inner curve", "visibility toggles; they also affect PNG export")
        Gap(rtb)

        Heading(rtb, "STATUS BAR")
        Item(rtb, "Left side", "cells, seeds, active domain, blocks in memory and cursor world coordinates")
        Gap(rtb)

        Heading(rtb, "GENERAL")
        Item(rtb, "Dark theme", "toggles between dark and light palettes (the canvas stays dark: the cell colors are designed for it); in the top toolbar and remembered across sessions")
        Item(rtb, "Help", "opens this window")
    End Sub
End Class
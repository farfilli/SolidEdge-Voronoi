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

    Private ReadOnly tips As New ToolTip()

    Private ReadOnly chkFill As New ThemedCheckBox()
    Private ReadOnly chkFillSymbols As New ThemedCheckBox()
    Private ReadOnly chkOuter As New ThemedCheckBox()
    Private ReadOnly chkSeeds As New ThemedCheckBox()
    Private ReadOnly chkInner As New ThemedCheckBox()
    Private ReadOnly chkRandomRotation As New ThemedCheckBox()
    Private ReadOnly chkExportAsBlocks As New ThemedCheckBox()

    Private ReadOnly btnGenerate As New ThemedButton()
    Private ReadOnly btnShuffle As New ThemedButton()

    Private ReadOnly btnExportSvg As New ThemedButton()
    Private ReadOnly btnExportDxf As New ThemedButton()
    Private ReadOnly btnExportPng As New ThemedButton()
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
        Font = New Font("Segoe UI", 9.0F)
        BackColor = UiTheme.BgCanvas

        ConfigureControls()
        LoadUserSettings()
        BuildSidebar()
        BuildStatusBar()

        canvas.Dock = DockStyle.Fill
        currentWorldDomain = domain
        canvas.Domain = currentWorldDomain
        canvas.BackColor = UiTheme.BgCanvas

        Controls.Add(canvas)
        Controls.Add(sidebar)
        Controls.Add(statusBar)

        ApplyDarkTheme()
        SetupTooltips()

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
        AddHandler btnToSolidEdge.Click, AddressOf ExportToSolidEdge_Click

        AddHandler chkFill.CheckedChanged, AddressOf RefreshCanvasOptions
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

        AddHandler numCells.ValueChanged, AddressOf GenerationParameterChanged
        AddHandler numSeed.ValueChanged, AddressOf GenerationParameterChanged
        AddHandler numRelax.ValueChanged, AddressOf GenerationParameterChanged
        AddHandler cmbSeedMode.SelectedIndexChanged, AddressOf GenerationParameterChanged

        GenerateRandomDiagram(Nothing, EventArgs.Empty)
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
        Dim appTitle As New Label With {
            .Text = "SE-VORONOI",
            .ForeColor = UiTheme.Accent,
            .Font = New Font("Segoe UI", 10.0F, FontStyle.Bold),
            .AutoSize = False,
            .Width = 238,
            .Height = 26,
            .Margin = New Padding(3, 4, 3, 2),
            .TextAlign = ContentAlignment.MiddleLeft
        }
        sideLayout.Controls.Add(appTitle)

        ' ===== GENERATION =====
        curSection = NewSection("GENERATION", True)
        AddRowTitle("Seed Placement")
        AddRowControl(cmbSeedMode)
        AddDoubleRow("Cell Count", numCells,
             "Random Seed", numSeed)
        AddDoubleRow("Relax", numRelax,
             "Cell Scale", numCellScale)
        AddDoubleRow("", btnGenerate,
             "", btnShuffle, 32)

        ' ===== STYLE =====
        curSection = NewSection("STYLE", True)
        AddRowTitle("Cell Style")
        AddRowControl(cmbStyle)
        AddDoubleRow("Vertex Mode", cmbVertexMode,
             "Vertex Size", numVertexTrim)
        AddDoubleRow("Inner Offset", numInnerOffset,
             "Curve Width", numCurveWidth)
        AddRowControl(chkFill, 22)
        AddRowControl(chkFillSymbols, 22)
        AddRowControl(chkRandomRotation, 22)

        ' ===== DISPLAY =====
        curSection = NewSection("DISPLAY", False)
        AddRowControl(chkOuter, 22)
        AddRowControl(chkSeeds, 22)
        AddRowControl(chkInner, 22)

        ' ===== SKETCH & BLOCKS =====
        curSection = NewSection("SKETCH & BLOCKS", False)
        AddRowControl(btnReadSketchProfile, 30)
        AddRowControl(btnReadBlockDefaultView, 30)
        AddRowControl(btnLoadBlocks, 30)
        AddRowControl(btnSaveBlocks, 30)
        AddRowControl(btnClearBlocks, 30)
        AddRowControl(btnBlockLibrary, 30)

        ' ===== EXPORT =====
        curSection = NewSection("EXPORT", True)
        AddRowControl(btnExportSvg, 30)
        AddRowControl(btnExportDxf, 30)
        AddRowControl(btnExportPng, 30)
        AddRowControl(chkExportAsBlocks, 22)
        AddRowControl(btnToSolidEdge, 32)
    End Sub

    Private Function NewSection(title As String, startOpen As Boolean) As CollapsibleSection
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

    ' Rotella del mouse sopra la sidebar -> scorre la scrollbar custom.
    Public Function PreFilterMessage(ByRef m As Message) As Boolean Implements IMessageFilter.PreFilterMessage
        Const WM_MOUSEWHEEL As Integer = &H20A
        If m.Msg <> WM_MOUSEWHEEL Then Return False
        If Not sideScroll.Visible Then Return False

        Dim pos As Point = Control.MousePosition
        If Not sidebar.RectangleToScreen(sidebar.ClientRectangle).Contains(pos) Then Return False

        ' High word di WParam = delta rotella come Int16: la si reinterpreta in
        ' complemento a due a mano (CShort su 0xFF88 andrebbe in overflow).
        Dim raw As Integer = CInt((m.WParam.ToInt64() >> 16) And &HFFFF&)
        If raw >= &H8000 Then raw -= &H10000
        Dim delta As Integer = raw
        sideScroll.Value -= Math.Sign(delta) * 60
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

        For Each chk In New CheckBox() {chkFill, chkFillSymbols, chkOuter, chkSeeds, chkInner, chkRandomRotation, chkExportAsBlocks}
            chk.ForeColor = UiTheme.Txt
            chk.BackColor = UiTheme.BgSidebar
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

    ' ===== Tooltips =====

    Private Sub SetupTooltips()
        tips.AutoPopDelay = 8000
        tips.InitialDelay = 500
        tips.SetToolTip(numRelax, "Lloyd relaxation iterations: evens out cell sizes")
        tips.SetToolTip(cmbSeedMode, "Seed distribution inside the domain")
        tips.SetToolTip(numInnerOffset, "Distance of the inner curve from the cell border")
        tips.SetToolTip(numCellScale, "Global symbol scale (mouse wheel on the canvas for a single cell)")
        tips.SetToolTip(canvas, "Wheel: scale symbol" & ChrW(10) &
                                "Shift+Wheel: rotate symbol" & ChrW(10) &
                                "CTRL+Wheel: cycle symbol/block" & ChrW(10) &
                                "Double click: add seed  -  Right click: remove seed" & ChrW(10) &
                                "CTRL+Right drag: zoom  -  CTRL+Shift+Right drag: pan" & ChrW(10) &
                                "ALT+Right click: reset view")
    End Sub

    ' ===== Chrome scuro (titolo finestra + scrollbar) =====

    <Runtime.InteropServices.DllImport("dwmapi.dll")>
    Private Shared Function DwmSetWindowAttribute(hwnd As IntPtr, attr As Integer, ByRef attrValue As Integer, attrSize As Integer) As Integer
    End Function

    <Runtime.InteropServices.DllImport("uxtheme.dll", CharSet:=Runtime.InteropServices.CharSet.Unicode)>
    Private Shared Function SetWindowTheme(hWnd As IntPtr, pszSubAppName As String, pszSubIdList As String) As Integer
    End Function

    Protected Overrides Sub OnLoad(e As EventArgs)
        MyBase.OnLoad(e)
        Try
            ' Barra del titolo scura (Windows 10 1809+ / 11); attr 20, fallback 19.
            Dim dark As Integer = 1
            If DwmSetWindowAttribute(Handle, 20, dark, 4) <> 0 Then
                DwmSetWindowAttribute(Handle, 19, dark, 4)
            End If

            ' Windows 11: colori personalizzati di caption, bordo e testo (COLORREF 0x00BBGGRR).
            ' Su Windows 10 le chiamate falliscono e resta il dark generico: va bene.
            Dim captionCol As Integer = ColRef(UiTheme.BgSidebar)
            Dim borderCol As Integer = ColRef(UiTheme.BgSidebar)
            Dim textCol As Integer = ColRef(UiTheme.Txt)
            DwmSetWindowAttribute(Handle, 35, captionCol, 4)   ' DWMWA_CAPTION_COLOR
            DwmSetWindowAttribute(Handle, 34, borderCol, 4)    ' DWMWA_BORDER_COLOR
            DwmSetWindowAttribute(Handle, 36, textCol, 4)      ' DWMWA_TEXT_COLOR
        Catch
            ' Sistemi senza supporto: si ignora, resta il chrome standard.
        End Try
    End Sub

    Private Shared Function ColRef(c As Color) As Integer
        Return c.R Or (CInt(c.G) << 8) Or (CInt(c.B) << 16)
    End Function

    Protected Overrides Sub OnFormClosed(e As FormClosedEventArgs)
        SaveUserSettings()
        Application.RemoveMessageFilter(Me)
        MyBase.OnFormClosed(e)
    End Sub

    ' ===== Persistenza impostazioni utente (%AppData%\SE-Voronoi\settings.txt) =====

    Private Function SettingsPath() As String
        Dim dir As String = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SE-Voronoi")
        Directory.CreateDirectory(dir)
        Return Path.Combine(dir, "settings.txt")
    End Function

    Private Function FInv(v As Decimal) As String
        Return v.ToString(Globalization.CultureInfo.InvariantCulture)
    End Function

    Private Sub SaveUserSettings()
        Try
            Dim lines As New List(Of String) From {
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
                "RandomRotation=" & chkRandomRotation.Checked.ToString(),
                "ShowOuter=" & chkOuter.Checked.ToString(),
                "ShowSeeds=" & chkSeeds.Checked.ToString(),
                "ShowInner=" & chkInner.Checked.ToString(),
                "ExportAsBlocks=" & chkExportAsBlocks.Checked.ToString()
            }
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
            If map.TryGetValue("RandomRotation", sVal) AndAlso Boolean.TryParse(sVal, bVal) Then chkRandomRotation.Checked = bVal
            If map.TryGetValue("ShowOuter", sVal) AndAlso Boolean.TryParse(sVal, bVal) Then chkOuter.Checked = bVal
            If map.TryGetValue("ShowSeeds", sVal) AndAlso Boolean.TryParse(sVal, bVal) Then chkSeeds.Checked = bVal
            If map.TryGetValue("ShowInner", sVal) AndAlso Boolean.TryParse(sVal, bVal) Then chkInner.Checked = bVal
            If map.TryGetValue("ExportAsBlocks", sVal) AndAlso Boolean.TryParse(sVal, bVal) Then chkExportAsBlocks.Checked = bVal

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

        ctrl.Dock = DockStyle.Top
        ctrl.Height = forcedHeight
        ctrl.Margin = New Padding(0)

        panel.Controls.Add(lbl, 0, 0)
        panel.Controls.Add(ctrl, 0, 1)

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
        numVertexTrim.Maximum = 2D
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

        currentSeeds = VoronoiEngine.CreateSeedsByMode(GetSeedMode(), CInt(numCells.Value), domain, CInt(numSeed.Value))
        RebuildSeedStyleKeys(currentSeeds.Count, CInt(numSeed.Value))
        RebuildSeedCellScales(currentSeeds.Count, CSng(numCellScale.Value))
        RebuildSeedCellRotations(currentSeeds.Count)
        RebuildSeedCellSymbolOffsets(currentSeeds.Count)
        lastCellScale = CSng(numCellScale.Value)

        For i As Integer = 1 To CInt(numRelax.Value)
            Dim tmpCells = VoronoiEngine.BuildCells(currentSeeds, domain)
            currentSeeds = VoronoiEngine.RelaxSeeds(tmpCells)
        Next

        EnsureSeedStyleKeyCount(currentSeeds.Count, CInt(numSeed.Value))
        EnsureSeedCellScaleCount(currentSeeds.Count, CSng(numCellScale.Value))
        EnsureSeedCellRotationCount(currentSeeds.Count)
        EnsureSeedCellSymbolOffsetCount(currentSeeds.Count)

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
            Dim seeds = VoronoiEngine.CreateSeedsByModeInPolygon(GetSeedMode(), quota, d.Bounds, d.Outer, d.Holes, regionSeed)

            Dim regionStyleKeys As New List(Of Integer)
            Dim regionScales As New List(Of Single)
            Dim regionRotations As New List(Of Single)
            Dim regionOffsets As New List(Of Integer)
            Dim rng As New Random(regionSeed Xor &H51F15E)

            For k As Integer = 0 To seeds.Count - 1
                regionStyleKeys.Add(rng.Next())
                regionScales.Add(defaultScale)
                regionRotations.Add(0.0F)
                regionOffsets.Add(0)
            Next

            For r As Integer = 1 To CInt(numRelax.Value)
                Dim tmpCells = VoronoiEngine.BuildCells(seeds, d.Outer, d.Holes)
                seeds = VoronoiEngine.RelaxSeeds(tmpCells)

                Dim filteredSeeds As New List(Of Vec2)
                Dim filteredKeys As New List(Of Integer)
                Dim filteredScales As New List(Of Single)
                Dim filteredRotations As New List(Of Single)
                Dim filteredOffsets As New List(Of Integer)

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
                    End If
                Next

                seeds = filteredSeeds
                regionStyleKeys = filteredKeys
                regionScales = filteredScales
                regionRotations = filteredRotations
                regionOffsets = filteredOffsets
            Next

            Dim cells = VoronoiEngine.BuildCells(seeds, d.Outer, d.Holes)

            allSeeds.AddRange(seeds)
            allCells.AddRange(cells)
            allStyleKeys.AddRange(regionStyleKeys)
            allScales.AddRange(regionScales)
            allRotations.AddRange(regionRotations)
            allOffsets.AddRange(regionOffsets)
        Next

        currentSeeds = allSeeds
        currentSeedStyleKeys = allStyleKeys
        currentSeedCellScales = allScales
        currentSeedCellRotations = allRotations
        currentSeedCellSymbolOffsets = allOffsets

        canvas.Cells = allCells
        canvas.EditableSeeds = New List(Of Vec2)(allSeeds)
        canvas.SeedStyleKeys = New List(Of Integer)(currentSeedStyleKeys)
        canvas.CellScales = New List(Of Single)(currentSeedCellScales)
        canvas.CellRotations = New List(Of Single)(currentSeedCellRotations)
        canvas.CellSymbolOffsets = New List(Of Integer)(currentSeedCellSymbolOffsets)

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
        currentSeedCellSymbolOffsets = New List(Of Integer)(canvas.CellSymbolOffsets)

        EnsureSeedStyleKeyCount(currentSeeds.Count, CInt(numSeed.Value))
        EnsureSeedCellScaleCount(currentSeeds.Count, CSng(numCellScale.Value))
        EnsureSeedCellRotationCount(currentSeeds.Count)
        EnsureSeedCellSymbolOffsetCount(currentSeeds.Count)

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

    Private Sub Canvas_SeedSymbolOffsetsEdited(sender As Object, e As EventArgs)
        currentSeedCellSymbolOffsets = New List(Of Integer)(canvas.CellSymbolOffsets)
        EnsureSeedCellSymbolOffsetCount(currentSeeds.Count)
        canvas.CellSymbolOffsets = New List(Of Integer)(currentSeedCellSymbolOffsets)
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
            Dim allOffsets As New List(Of Integer)

            For Each d In currentSketchDomains
                Dim seedsInDomain As New List(Of Vec2)
                Dim keysInDomain As New List(Of Integer)
                Dim scalesInDomain As New List(Of Single)
                Dim rotationsInDomain As New List(Of Single)
                Dim offsetsInDomain As New List(Of Integer)

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
                    End If
                Next

                If seedsInDomain.Count = 0 Then Continue For

                Dim cells = VoronoiEngine.BuildCells(seedsInDomain, d.Outer, d.Holes)

                allSeeds.AddRange(seedsInDomain)
                allStyleKeys.AddRange(keysInDomain)
                allScales.AddRange(scalesInDomain)
                allRotations.AddRange(rotationsInDomain)
                allOffsets.AddRange(offsetsInDomain)
                allCells.AddRange(cells)
            Next

            currentSeeds = allSeeds
            currentSeedStyleKeys = allStyleKeys
            currentSeedCellScales = allScales
            currentSeedCellRotations = allRotations
            currentSeedCellSymbolOffsets = allOffsets

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
        EnsureSeedCellSymbolOffsetCount(currentSeeds.Count)

        canvas.SeedStyleKeys = New List(Of Integer)(currentSeedStyleKeys)
        canvas.CellScales = New List(Of Single)(currentSeedCellScales)
        canvas.CellRotations = New List(Of Single)(currentSeedCellRotations)
        canvas.CellSymbolOffsets = New List(Of Integer)(currentSeedCellSymbolOffsets)

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
        Return added
    End Function

    Private Sub ClearBlocks_Click(sender As Object, e As EventArgs)
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

    Private Sub ExportToSolidEdge_Click(sender As Object, e As EventArgs)
        Try
            If chkExportAsBlocks.Checked Then
                Dim geoms = ExportGeometry.BuildCellGeometry(canvas)
                If geoms Is Nothing OrElse geoms.Count = 0 Then
                    MessageBox.Show("No geometry to export.", "Solid Edge", MessageBoxButtons.OK, MessageBoxIcon.Information)
                    Return
                End If
                SolidEdgeExporter.ExportToActivePartSketch(geoms, currentBlockSymbols)
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
    Private ReadOnly accent As Color = Color.FromArgb(0, 188, 212)
    Private ReadOnly bg As Color = Color.FromArgb(8, 6, 53)
    Private ReadOnly tileBg As Color = Color.FromArgb(18, 16, 70)

    Public Sub New()
        Text = "Block Library"
        Icon = My.Resources.SE_Voronoi_Blocks
        StartPosition = FormStartPosition.CenterParent
        Width = 720
        Height = 560
        MinimumSize = New Size(360, 320)
        BackColor = bg
        ForeColor = Color.White

        header.Dock = DockStyle.Top
        header.Height = 28
        header.TextAlign = ContentAlignment.MiddleLeft
        header.Padding = New Padding(8, 0, 0, 0)
        header.ForeColor = Color.White
        header.Text = "0 blocks in memory"

        flow.Dock = DockStyle.Fill
        flow.AutoScroll = True
        flow.BackColor = bg
        flow.Padding = New Padding(8)

        Controls.Add(flow)
        Controls.Add(header)
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
        tile.BackColor = tileBg

        Dim pic As New PictureBox()
        pic.Width = ThumbPx
        pic.Height = ThumbPx
        pic.Left = 8
        pic.Top = 6
        pic.BackColor = bg
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
        lbl.ForeColor = Color.White
        lbl.TextAlign = ContentAlignment.MiddleCenter
        lbl.AutoEllipsis = True
        tile.Controls.Add(lbl)

        Dim btnDel As New Button()
        btnDel.Text = "Remove"
        btnDel.Width = ThumbPx
        btnDel.Height = 22
        btnDel.Left = 8
        btnDel.Top = ThumbPx + 26
        btnDel.FlatStyle = FlatStyle.Flat
        btnDel.BackColor = UiTheme.BgField
        btnDel.ForeColor = UiTheme.Txt
        btnDel.FlatAppearance.BorderColor = UiTheme.Border
        btnDel.FlatAppearance.BorderSize = 1
        btnDel.FlatAppearance.MouseOverBackColor = UiTheme.BgFieldHi
        btnDel.Cursor = Cursors.Hand
        Dim target As BlockDefinition = def
        AddHandler btnDel.Click, Sub(s, e) RemoveBlock(target)
        tile.Controls.Add(btnDel)

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
            g.Clear(bg)

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
                        Using br As New SolidBrush(Color.FromArgb(235, accent.R, accent.G, accent.B))
                            g.FillPath(br, gp)
                        End Using
                    End Using
                End If
            End If

            Using pen As New Pen(accent, Math.Max(0.5F, StrokeWidth))
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
        For Each c As Control In flow.Controls
            DisposeTile(c)
        Next
        MyBase.OnFormClosed(e)
    End Sub
End Class


' ============================================================
'  Palette del tema scuro dell'applicazione.
' ============================================================
Public NotInheritable Class UiTheme
    Private Sub New()
    End Sub

    Public Shared ReadOnly BgCanvas As Color = Color.FromArgb(8, 6, 53)
    Public Shared ReadOnly BgSidebar As Color = Color.FromArgb(16, 14, 60)
    Public Shared ReadOnly BgField As Color = Color.FromArgb(22, 20, 74)
    Public Shared ReadOnly BgFieldHi As Color = Color.FromArgb(30, 28, 94)
    Public Shared ReadOnly Border As Color = Color.FromArgb(42, 40, 112)
    Public Shared ReadOnly Txt As Color = Color.FromArgb(232, 236, 248)
    Public Shared ReadOnly TxtDim As Color = Color.FromArgb(154, 160, 200)
    Public Shared ReadOnly Accent As Color = Color.FromArgb(0, 188, 212)
    Public Shared ReadOnly AccentHi As Color = Color.FromArgb(110, 231, 243)
    Public Shared ReadOnly StatusBg As Color = Color.FromArgb(13, 11, 69)

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
        If Not Enabled Then Return
        Me.Value = _val + If(e.Delta > 0, _inc, -_inc)
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
        Using p As New Pen(Color.FromArgb(35, 32, 100), 1.0F)
            e.Graphics.DrawLine(p, 0, 0, headerLbl.Width, 0)
        End Using
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
        If Not Enabled Then Return
        Me.Value = _val + If(e.Delta > 0, _inc, -_inc)
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
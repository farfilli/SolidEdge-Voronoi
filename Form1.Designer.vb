<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class Form1
    Inherits System.Windows.Forms.Form

    'Form esegue l'override del metodo Dispose per pulire l'elenco dei componenti.
    <System.Diagnostics.DebuggerNonUserCode()> _
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Richiesto da Progettazione Windows Form
    Private components As System.ComponentModel.IContainer

    'NOTA: la procedura che segue è richiesta da Progettazione Windows Form
    'Può essere modificata in Progettazione Windows Form.  
    'Non modificarla mediante l'editor del codice.
    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(Form1))
        Me.Label1 = New System.Windows.Forms.Label()
        Me.TB_Points = New System.Windows.Forms.TextBox()
        Me.Button1 = New System.Windows.Forms.Button()
        Me.CB_Points = New System.Windows.Forms.CheckBox()
        Me.CB_Triangles = New System.Windows.Forms.CheckBox()
        Me.CB_Voronoi = New System.Windows.Forms.CheckBox()
        Me.BT_DrawInEdge = New System.Windows.Forms.Button()
        Me.SuspendLayout()
        '
        'Label1
        '
        Me.Label1.AutoSize = True
        Me.Label1.Location = New System.Drawing.Point(12, 9)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(39, 13)
        Me.Label1.TabIndex = 1
        Me.Label1.Text = "Points"
        '
        'TB_Points
        '
        Me.TB_Points.Location = New System.Drawing.Point(54, 6)
        Me.TB_Points.Name = "TB_Points"
        Me.TB_Points.Size = New System.Drawing.Size(113, 22)
        Me.TB_Points.TabIndex = 2
        Me.TB_Points.Text = "200"
        '
        'Button1
        '
        Me.Button1.Location = New System.Drawing.Point(173, 6)
        Me.Button1.Name = "Button1"
        Me.Button1.Size = New System.Drawing.Size(68, 22)
        Me.Button1.TabIndex = 3
        Me.Button1.Text = "Draw"
        Me.Button1.UseVisualStyleBackColor = True
        '
        'CB_Points
        '
        Me.CB_Points.AutoSize = True
        Me.CB_Points.Location = New System.Drawing.Point(15, 31)
        Me.CB_Points.Name = "CB_Points"
        Me.CB_Points.Size = New System.Drawing.Size(58, 17)
        Me.CB_Points.TabIndex = 4
        Me.CB_Points.Text = "Points"
        Me.CB_Points.UseVisualStyleBackColor = True
        '
        'CB_Triangles
        '
        Me.CB_Triangles.AutoSize = True
        Me.CB_Triangles.Location = New System.Drawing.Point(15, 54)
        Me.CB_Triangles.Name = "CB_Triangles"
        Me.CB_Triangles.Size = New System.Drawing.Size(95, 17)
        Me.CB_Triangles.TabIndex = 4
        Me.CB_Triangles.Text = "Triangulation"
        Me.CB_Triangles.UseVisualStyleBackColor = True
        '
        'CB_Voronoi
        '
        Me.CB_Voronoi.AutoSize = True
        Me.CB_Voronoi.Checked = True
        Me.CB_Voronoi.CheckState = System.Windows.Forms.CheckState.Checked
        Me.CB_Voronoi.Location = New System.Drawing.Point(15, 77)
        Me.CB_Voronoi.Name = "CB_Voronoi"
        Me.CB_Voronoi.Size = New System.Drawing.Size(67, 17)
        Me.CB_Voronoi.TabIndex = 4
        Me.CB_Voronoi.Text = "Voronoi"
        Me.CB_Voronoi.UseVisualStyleBackColor = True
        '
        'BT_DrawInEdge
        '
        Me.BT_DrawInEdge.Location = New System.Drawing.Point(247, 6)
        Me.BT_DrawInEdge.Name = "BT_DrawInEdge"
        Me.BT_DrawInEdge.Size = New System.Drawing.Size(81, 22)
        Me.BT_DrawInEdge.TabIndex = 3
        Me.BT_DrawInEdge.Text = "Draw in SE"
        Me.BT_DrawInEdge.UseVisualStyleBackColor = True
        '
        'Form1
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(800, 450)
        Me.Controls.Add(Me.CB_Voronoi)
        Me.Controls.Add(Me.CB_Triangles)
        Me.Controls.Add(Me.CB_Points)
        Me.Controls.Add(Me.BT_DrawInEdge)
        Me.Controls.Add(Me.Button1)
        Me.Controls.Add(Me.TB_Points)
        Me.Controls.Add(Me.Label1)
        Me.Font = New System.Drawing.Font("Segoe UI", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Icon = CType(resources.GetObject("$this.Icon"), System.Drawing.Icon)
        Me.Name = "Form1"
        Me.Text = "Voronoi Diagram Generator"
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub
    Friend WithEvents Label1 As Label
    Friend WithEvents TB_Points As TextBox
    Friend WithEvents Button1 As Button
    Friend WithEvents CB_Points As CheckBox
    Friend WithEvents CB_Triangles As CheckBox
    Friend WithEvents CB_Voronoi As CheckBox
    Friend WithEvents BT_DrawInEdge As Button
End Class

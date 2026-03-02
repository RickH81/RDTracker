Imports System.ComponentModel
Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices
Imports System.Configuration
Imports System.IO.MemoryMappedFiles

Public Class frmMain

    Private _memManager As New MemoryManager


    <DllImport("user32.dll", CharSet:=CharSet.Auto)>
    Private Shared Sub GetClassName(ByVal hWnd As System.IntPtr, ByVal lpClassName As System.Text.StringBuilder, ByVal nMaxCount As Integer)
    End Sub

    Public Function GetWindowClass(ByVal hwnd As Long) As String
        Dim sClassName As New System.Text.StringBuilder("", 256)
        Call GetClassName(hwnd, sClassName, 256)
        Return sClassName.ToString
    End Function

    Public Maze = New Maze(My.Settings.Cata)


    Private Sub frmMain_Load(sender As Object, e As EventArgs) Handles MyBase.Load

        If My.Settings.SingleInstance AndAlso IPC.AlreadyOpen Then
            IPC.RequestActivation = True
            End
        End If
        IPC.AlreadyOpen = True

        Me.Location = My.Settings.Location
        Me.TopMost = My.Settings.Topmost

        ' Initialize default Mem (key) setting if not set
        If My.Settings.Mem = 0 Then
            My.Settings.Mem = 607204
            My.Settings.Save()
        End If

        ' Initialize Maze display with proper size accounting for control bar - make square
        Dim squareSize As Integer = Me.ClientSize.Width
        Dim displayHeight As Integer = squareSize - 25
        Maze.ResizeDisplay(squareSize, If(displayHeight > 0, displayHeight, 241))
        
        ' Ensure window is square
        Me.ClientSize = New System.Drawing.Size(squareSize, squareSize)
        
        Me.BackgroundImage = Maze.display
        Me.lblEnter.BackColor = My.Settings.background
        Me.BackColor = My.Settings.background

        btnSettings.Region = New Region(New Rectangle(3, 3, btnSettings.Width - 6, btnSettings.Height - 6))

        Dim strAltName As String = ""
        Dim intFound As Integer = 0
        For Each p As Process In listProcesses()
            strAltName = Strings.Left(p.MainWindowTitle, p.MainWindowTitle.IndexOf(" - "))
            If strAltName <> "Someone" Then
                cboAlt.Items.Add(strAltName)
                intFound += 1
            End If
        Next

        Dim args() As String = Environment.GetCommandLineArgs().Skip(1).ToArray

        If args.Count > 0 Then
            For Each arg In args
                If cboAlt.Items.Contains(arg) Then
                    cboAlt.SelectedItem = arg
                    Exit For
                End If
            Next
        Else
            cboAlt.SelectedIndex = If(intFound > 0, 1, 0)
        End If

        Me.Text = If(My.Settings.Cata, "CATA", "RD") & " Tracker"

    End Sub
    Private Sub frmMain_Shown(sender As Object, e As EventArgs) Handles MyBase.Shown
        If Not Screen.AllScreens.Any(Function(s) s.WorkingArea.Contains(Me.Location)) Then
            Me.Location = Screen.PrimaryScreen.WorkingArea.Location
        End If
    End Sub
    Private Sub cboAlt_DropDown(sender As Object, e As EventArgs) Handles cboAlt.DropDown
        Dim lstAst As New List(Of String)
        Dim Name As String = ""
        lstAst.Add("Someone")
        For Each p As Process In listProcesses()
            If p.MainWindowTitle IsNot Nothing AndAlso
             Not p.MainWindowTitle.StartsWith("Someone") Then
                Name = Strings.Left(p.MainWindowTitle, p.MainWindowTitle.IndexOf(" - "))
                lstAst.Add(Name)
                If Not cboAlt.Items.Contains(Name) Then
                    cboAlt.Items.Add(Name)
                End If
            End If
        Next

        '    clean ComboBox of idled clients
        Dim i As Integer = 1
        Do While i < cboAlt.Items.Count
            If Not lstAst.Contains(cboAlt.Items(i)) Then
                If cboAlt.SelectedIndex = i Then
                    cboAlt.SelectedIndex = 0
                End If
                cboAlt.Items.RemoveAt(i)
                i -= 1
            End If
            i += 1
        Loop

    End Sub
    Private strSock As String
    Private _mmf As MemoryMappedFile
    Private _mmva As MemoryMappedViewAccessor
    Private isSDL As Boolean = False
    Private Sub cboAlt_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cboAlt.SelectedIndexChanged

        tmrTick.Enabled = False
        _memManager.DetachFromProcess()
        If cboAlt.SelectedIndex <> 0 Then
            If _memManager.TryAttachToProcess(cboAlt.SelectedItem & " - ", My.Settings.exe.Split({"|"}, StringSplitOptions.RemoveEmptyEntries)) Then
                strSock = Strings.Right(_memManager.targetProcess.MainWindowTitle, 8)
                wasArea = False
                _mmf = MemoryMappedFile.CreateOrOpen($"MOAC{_memManager.targetProcess.Id}", Marshal.SizeOf(GetType(MoacSharedMem)))
                _mmva = _mmf.CreateViewAccessor()
                _mmva.Read(0, shm)
                If shm.pID = _memManager.targetProcess.Id Then
                    isSDL = True
                Else
                    isSDL = False
                End If
                tmrTick.Enabled = True
            End If

        End If

    End Sub

    Private shm As MoacSharedMem
    Dim ptZero As New Point(0, 0)
    Dim prevP As New Point(0, 0)
    Dim gameX, gameY As Integer
    Dim wasArea As Boolean = False
    Private reader As New MemoryManager
    Private Function readMemPoint(pp As Process) As Point
        If pp.HasExited Then Return New Point(0, 0)
        Dim mmf As MemoryMappedFile = MemoryMappedFile.CreateOrOpen($"MOAC{pp.Id}", Marshal.SizeOf(GetType(MoacSharedMem)))
        Dim mmva As MemoryMappedViewAccessor = mmf.CreateViewAccessor()
        Dim gameX, gameY As Integer
        Try
            reader.TryAttachToProcess(pp)
            Dim mshm As MoacSharedMem

            mmva.Read(0, mshm)
            Dim SDL As Boolean = False
            If mshm.pID = pp.Id Then
                SDL = True
            End If
            If SDL Then
                If mshm.swapped = 0 Then
                    gameX = reader.ReadIntWoW64(mshm.base + mshm.key)
                    gameY = reader.ReadIntWoW64(mshm.base + mshm.key + 4)
                Else
                    gameY = reader.ReadIntWoW64(mshm.base + mshm.key) 'note: Ugaris has X and Y swapped in memory
                    gameX = reader.ReadIntWoW64(mshm.base + mshm.key + 4)
                End If
            Else
                If Not My.Settings.SwapXY Then
                    gameX = reader.ReadInt32(My.Settings.PlayerX, True)
                    gameY = reader.ReadInt32(My.Settings.PlayerX + 4, True)
                Else
                    gameY = reader.ReadInt32(My.Settings.PlayerX, True) 'note: Ugaris has X and Y swapped in memory
                    gameX = reader.ReadInt32(My.Settings.PlayerX + 4, True)
                End If

            End If
        Catch
            Return New Point(0, 0)
        Finally
            mmva.Dispose()
            mmf.Dispose()
            reader.DetachFromProcess()
        End Try

        Return New Point(gameX, gameY)

    End Function

    Dim mainRdNum As Integer = 0
    Dim loopCount As Integer = 0
    Dim lstAproc As List(Of Process) = listProcesses()

    Private Sub tmrTick_Tick(sender As Object, e As EventArgs) Handles tmrTick.Tick
        If cboAlt.SelectedIndex = 0 Then
            tmrTick.Enabled = False
            Exit Sub
        End If
        If isSDL Then
            If shm.swapped = 0 Then
                gameX = _memManager.ReadIntWoW64(shm.base + shm.key + 4)
                gameY = _memManager.ReadIntWoW64(shm.base + shm.key)
            Else
                gameY = _memManager.ReadIntWoW64(shm.base + shm.key)
                gameX = _memManager.ReadIntWoW64(shm.base + shm.key + 4)
            End If
        Else
            If Not My.Settings.SwapXY Then
                gameX = _memManager.ReadInt32(My.Settings.PlayerX, True)
                gameY = _memManager.ReadInt32(My.Settings.PlayerX + 4, True)
            Else
                gameY = _memManager.ReadInt32(My.Settings.PlayerX, True)
                gameX = _memManager.ReadInt32(My.Settings.PlayerX + 4, True)
            End If
        End If

        If wasArea AndAlso Not Maze.isLobby(gameX, gameY) Then
            Exit Sub
        End If
        If gameX <= 0 OrElse gameY <= 0 OrElse gameX >= 255 OrElse gameY >= 255 Then
            'error in reading gamecoords
            lblEnter.Text = "Error " & mainRdNum
            prevP = ptZero
            Exit Sub
        End If
        _memManager.targetProcess.Refresh()
        If Not _memManager.targetProcess.MainWindowTitle.Contains(strSock) Then
            lblEnter.Text = "Area " & mainRdNum
            wasArea = True
            Exit Sub
        End If
        If Maze.isLobby(gameX, gameY) Then
            lblEnter.Text = "Lobby " & mainRdNum
            prevP = ptZero
            wasArea = False
            Exit Sub
        End If
        If Maze.isYendor(gameX, gameY) Then
            lblEnter.Text = "Yendor " & mainRdNum
            prevP = ptZero
            Exit Sub
        End If

        mainRdNum = Maze.getNum(gameX, gameY)

        lblEnter.Text = "Enter " & mainRdNum
        Dim newP As New Point(gameX, gameY)
        If prevP <> ptZero AndAlso prevP <> newP Then
            Maze.plotMaze(prevP.X, prevP.Y, My.Settings.path)
        End If
        prevP = newP

        Dim radius As Integer = 11
        'If My.Settings.V2 Then
        Dim gtInRange As Boolean = False
        
        ' Based on memory scan, sprites are at offset -60 from base iSprite address
        Dim spriteOffset As Integer = -60
        Dim spriteAddr As Integer = My.Settings.iSprite + spriteOffset
        
        ' Log first occurrence of actual sprite data
        Static logged As Boolean = False
        If Not logged Then
            Dim testSprite As Integer = _memManager.ReadInt32(spriteAddr, True)
            If testSprite <> 0 Then
                System.IO.File.AppendAllText(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "rdtracker_debug.txt"), 
                    $"[SUCCESS] Found sprite data at offset -60! Value={testSprite}" & vbCrLf)
                logged = True
            End If
        End If
        
        For dX = -radius To radius
            Dim cX As Integer = prevP.X + dX
            If Not isValidMapUnit(cX) Then
                Continue For
            End If
            For dY = -radius To radius
                Dim cY As Integer = prevP.Y + dY
                If Not isValidMapUnit(cY) Then
                    Continue For
                End If
                
                Dim offset As Integer
                If isSDL Then
                    offset = (shm.offX * dX) + (shm.offX * shm.offY * dY)
                Else
                    offset = (My.Settings.OffsetXY.X * dX) + (My.Settings.OffsetXY.X * My.Settings.OffsetXY.Y * dY)
                End If
                
                Dim isprite As Integer
                Dim flags As Integer
                Dim fsprite2 As Integer
                Dim gsprite As Integer
                If isSDL Then
                    isprite = _memManager.ReadIntWoW64(shm.base + shm.isprite + offset)
                    flags = _memManager.ReadIntWoW64(shm.base + shm.isprite + offset + shm.flags)
                    fsprite2 = _memManager.ReadIntWoW64(shm.base + shm.isprite + offset + shm.fsprite)
                    gsprite = _memManager.ReadIntWoW64(shm.base + shm.isprite + offset - 8)
                Else
                    isprite = _memManager.ReadInt32(spriteAddr + offset, True)
                    flags = _memManager.ReadInt32(spriteAddr + offset + My.Settings.flagsOffset, True)
                    fsprite2 = _memManager.ReadInt32(spriteAddr + offset + My.Settings.fSprite2Offset, True)
                    gsprite = _memManager.ReadInt32(spriteAddr + offset - 8, True)
                End If

                ' Debug: log any non-zero sprite at nearby grid positions
                ' Log all squares in a 2x2 area (4x4 block) around the player, regardless of sprite values
                If (dX >= -1 AndAlso dX <= 2 AndAlso dY >= -1 AndAlso dY <= 2) Then
                    Dim debugMsg = $"Sprite rel({dX},{dY}) abs({cX},{cY}): isprite={isprite}, fsprite2={fsprite2}, gsprite={gsprite}, flags={flags:X}"
                    System.IO.File.AppendAllText(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "rdtracker_debug.txt"), debugMsg & vbCrLf)
                End If

                ' Only skip when flags exist and visibility bit is not set
                If flags <> 0 AndAlso (flags And MazeConstants.VISIBILITY_FLAG) = 0 Then
                    Continue For 'not visible
                End If

                ' Gas traps detection (fsprite2)
                If fsprite2 <> 0 Then
                    If (fsprite2 = 15270 Or fsprite2 = 15271) Or (fsprite2 >= 15291 And fsprite2 <= 15295) Then
                        If Not Maze.hasGT(cX, cY) Then
                            If My.Settings.sysBeepOnGT Then Beep()
                            Maze.drawGT(cX, cY, My.Settings.gastrap)
                        End If
                        gtInRange = True
                    End If
                End If
                ' Trap doors detection (isprite)
                If isprite <> 0 Then
                    If (isprite >= 15272 And isprite <= 15277) Then
                        Maze.drawTD(cX, cY, False, False, My.Settings.trapdoor, My.Settings.background, My.Settings.path)
                    End If
                    ' Shrines detection (isprite)
                    If (isprite = 15268 Or isprite = 15269) Then
                        Maze.drawShrine(cX, cY, Color.Purple, My.Settings.shrineoutline)
                    End If
                    ' Junk detection (isprite)
                    If (isprite >= 15279 And isprite <= 15326) Then
                        Maze.drawTrash(cX, cY, My.Settings.junk)
                    End If
                End If
            Next
        Next
        If gtInRange Then
            lblEnter.ForeColor = My.Settings.textwarning
        Else
            lblEnter.ForeColor = My.Settings.text
        End If




        Maze.Update()

        If My.Settings.Multi Then
            If loopCount > (1500 \ tmrTick.Interval) Then
                ' only update list evey 1.5 seconds
                lstAproc = listProcesses()
                loopCount = 0
            Else
                loopCount += 1
            End If

            Dim altP As Point
            Dim altNum As Integer
            For Each pp As Process In lstAproc
                If Not pp.isRunning Then
                    Continue For
                End If
                If pp.MainWindowTitle IsNot Nothing AndAlso
                   pp.Id <> _memManager.targetProcess.Id AndAlso
                   Not pp.MainWindowTitle.StartsWith("Someone") AndAlso
                   pp.MainWindowTitle.Contains(strSock) Then
                    altP = readMemPoint(pp)
                    altNum = Maze.getNum(altP.X, altP.Y)
                    If altP <> ptZero AndAlso
                       altNum = mainRdNum Then
                        Maze.plotMaze(altP.X, altP.Y, My.Settings.path)
                        Maze.plotPlayer(altP.X, altP.Y, My.Settings.alts)
                    End If
                End If
            Next
        End If

        Maze.plotPlayer(gameX, gameY, My.Settings.main)
        Me.BackgroundImage = Maze.display
        Me.Refresh()

    End Sub

    Private Function sameSpot(prevP As Point)
        If isSDL Then
            If shm.swapped Then
                If prevP.Y = _memManager.ReadIntWoW64(shm.base + shm.key) AndAlso prevP.X = _memManager.ReadIntWoW64(shm.base + shm.key + 4) Then
                    Return True
                End If
            Else
                If prevP.X = _memManager.ReadIntWoW64(shm.base + shm.key) AndAlso prevP.Y = _memManager.ReadInt32(shm.base + shm.key + 4) Then
                    Return True
                End If
            End If
            Return False
        End If
        If My.Settings.SwapXY Then
            If prevP.Y = _memManager.ReadInt32(My.Settings.PlayerX, True) AndAlso prevP.X = _memManager.ReadInt32(My.Settings.PlayerX + 4, True) Then
                Return True
            End If
        Else
            If prevP.X = _memManager.ReadInt32(My.Settings.PlayerX, True) AndAlso prevP.Y = _memManager.ReadInt32(My.Settings.PlayerX + 4, True) Then
                Return True
            End If
        End If
        Return False
    End Function

    Private Function isValidMapUnit(unit As Integer) As Boolean
        If unit <= 0 OrElse unit >= 255 Then
            Return False
        End If
        Return True
    End Function
    Private Sub btnSettings_Click(sender As Object, e As EventArgs) Handles btnSettings.Click
        frmNewSettings.Show()
        frmNewSettings.Focus()
    End Sub

    Private Sub btnReset_Click(sender As Object, e As EventArgs) Handles btnReset.Click
        tmrTick.Enabled = False
        _memManager.DetachFromProcess()
        If cboAlt.SelectedIndex <> 0 Then
            Dim exeNames() As String = My.Settings.exe.Split({"|"}, StringSplitOptions.RemoveEmptyEntries)
            If _memManager.TryAttachToProcess(cboAlt.SelectedItem & " -", exeNames) Then
                strSock = Strings.Right(_memManager.targetProcess.MainWindowTitle, 8)
            End If
        End If
        lblEnter.Text = "Enter 0"
        mainRdNum = 0
        prevP = ptZero
        wasArea = False
        Maze.Clear(My.Settings.background)
        Me.BackColor = My.Settings.background
        lblEnter.ForeColor = My.Settings.text
        lblEnter.BackColor = My.Settings.background
        tmrTick.Enabled = True
        lblEnter.Focus()

        If cboAlt.SelectedIndex <> 0 Then
            Try
                AppActivate(CType(_memManager.targetProcess?.Id, Integer))
            Catch
            End Try
        End If

        Me.Refresh()
    End Sub

    Private Sub frmMain_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
        If Me.WindowState = FormWindowState.Normal Then
            My.Settings.Location = Me.Location
        End If
    End Sub

    Public Sub runAsAdmin()
        tmrTick.Enabled = False
        Dim procStartInfo As New ProcessStartInfo With {
            .UseShellExecute = True,
            .FileName = Environment.GetCommandLineArgs()(0),
            .Arguments = """" & Me.cboAlt.SelectedItem & """",
            .WindowStyle = ProcessWindowStyle.Normal,
            .Verb = "runas" 'add this to prompt for elevation
        }

        If Me.WindowState = FormWindowState.Normal Then
            My.Settings.Location = Me.Location
        End If
        My.Settings.Save()

        Try
            Process.Start(procStartInfo).WaitForInputIdle()
        Catch e As System.ComponentModel.Win32Exception
            'operation cancelled
        Catch e As InvalidOperationException
            'wait for inputidle is needed
        Catch e As Exception
            Throw e
        End Try

    End Sub

    Public Function listProcesses() As List(Of Process)
        Dim lst As List(Of Process) = New List(Of Process)()
        For Each pp As Process In ListProcessesByNameArray(My.Settings.exe.Split({"|"}, StringSplitOptions.RemoveEmptyEntries))
            If pp.MainWindowTitle IsNot Nothing AndAlso
             Not pp.MainWindowTitle.StartsWith("Someone") AndAlso
             isAstoniaClass(pp) Then
                lst.Add(pp)
            End If
        Next
        Return lst
    End Function
    Public Function ListProcessesByNameArray(exes() As String) As List(Of Process)
        Dim list As List(Of Process) = New List(Of Process)
        For Each exe As String In exes
            list.AddRange(Process.GetProcessesByName(Trim(exe)))
        Next
        Return list
    End Function

    Private Async Sub tmrActive_Tick(sender As Object, e As EventArgs) Handles tmrActive.Tick
        If IPC.RequestActivation Then
            IPC.RequestActivation = 0

            If Me.WindowState = FormWindowState.Minimized Then
                Const WM_SYSCOMMAND = &H112
                Const SC_RESTORE = &HF120
                WndProc(Message.Create(Me.Handle, WM_SYSCOMMAND, SC_RESTORE, IntPtr.Zero))
            End If

            Me.TopMost = True
            Me.BringToFront()
            Await Task.Delay(100)
            Me.TopMost = My.Settings.Topmost

            'Me.Activate()

        End If
    End Sub

    'Private Sub SaveAsToolStripMenuItem_Click(sender As Object, e As EventArgs)
    '    If frmSettings.Visible = True Then frmSettings.btnOK.PerformClick()
    '    Me.TopMost = False

    '    Dim name As String = InputBox("Choose a name to save current settings as", "Save Settings", My.Settings.SelectedConfig)

    '    If name = "" Then
    '        Me.TopMost = My.Settings.Topmost
    '        Exit Sub
    '    End If
    '    For Each kar As Char In name.ToUpper

    '        If Not "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789 _".Contains(kar) Then
    '            MessageBox.Show("Illegal charater """ & kar & """")
    '            Me.TopMost = My.Settings.Topmost
    '            Exit Sub
    '        End If
    '    Next



    '    If System.IO.File.Exists(configDir & "\" & name & ".RDT.config") AndAlso
    '       MessageBox.Show("""" & name & """ config already exists. Overwrite?", "Notice", MessageBoxButtons.OKCancel) = DialogResult.Cancel Then
    '        Me.TopMost = My.Settings.Topmost
    '        Exit Sub
    '    End If

    '    My.Settings.SelectedConfig = name
    '    My.Settings.Save()

    '    FileIO.FileSystem.CopyFile(configDir & "\user.config", configDir & "\" & name & ".RDT.config", True)

    '    Me.TopMost = My.Settings.Topmost

    'End Sub


    Private Function isAstoniaClass(pp As Process) As Boolean
        Dim wndClass As String() = My.Settings.className.Split({"|"}, StringSplitOptions.RemoveEmptyEntries)
        For i As Integer = 0 To UBound(wndClass)
            wndClass(i) = Trim(wndClass(i))
        Next
        Return wndClass.Contains(GetWindowClass(pp.MainWindowHandle))
    End Function

    ''' <summary>
    ''' Handle form resize to scale the maze display
    ''' </summary>
    Private Sub frmMain_Resize(sender As Object, e As EventArgs) Handles MyBase.Resize
        If Maze IsNot Nothing Then
            ' Calculate the available display area (exclude control bar height)
            Dim displayHeight As Integer = Me.ClientSize.Height - 25 ' 25 pixels for controls at bottom
            Dim displayWidth As Integer = Me.ClientSize.Width
            
            ' Ensure minimum size
            If displayHeight > 0 AndAlso displayWidth > 0 Then
                Maze.ResizeDisplay(displayWidth, displayHeight)
                Me.BackgroundImage = Maze.display
                Me.Invalidate()
            End If
        End If
    End Sub

    '        My.Settings.Reload()
    '    Finally
    '        FileIO.FileSystem.DeleteFile(configDir & "\backup.config")
    '    End Try


    '    My.Settings.Save()
    'End Sub

End Class

Module extensions
    <Extension()>
    Public Function isRunning(AltPP As Process) As Boolean
        Try
            Return AltPP IsNot Nothing AndAlso Not AltPP.HasExited
        Catch e As Exception
            frmMain.runAsAdmin()
            End
        End Try
    End Function
End Module
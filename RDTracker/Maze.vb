Public Class Maze
    Implements IDisposable

    Public display As New Bitmap(255, 255)
    Private buffer As New Bitmap(255, 255)
    Private v2Over As New Bitmap(255, 255)
    Private v2Unde As New Bitmap(255, 255)

    Private grDisp As Graphics = Graphics.FromImage(display)
    Private grBuff As Graphics = Graphics.FromImage(buffer)

    Public grOver As Graphics = Graphics.FromImage(v2Over)
    Public grUnde As Graphics = Graphics.FromImage(v2unde)

    Public cata As Boolean
    Private size, div, bound As Integer
    Private scaleX As Double = 1.0
    Private scaleY As Double = 1.0
    Private Boxes(,,) As Rectangle
    Private gasTraps(,,) As Rectangle
    Private Walls(,,) As Rectangle
    Private TrapDoors(,) As Point
    Private disposed As Boolean = False

    Public Sub New(Optional cata As Boolean = False)
        Clear(My.Settings.background)
        Me.cata = cata
        size = If(cata, MazeConstants.CATA_SIZE, MazeConstants.RD_SIZE)
        div = If(cata, MazeConstants.CATA_DIV, MazeConstants.RD_DIV)
        bound = If(cata, MazeConstants.CATA_BOUND, MazeConstants.RD_BOUND)
        Boxes = New Rectangle(bound, bound, 2) {}
        For Y = 0 To bound
            For X = 0 To bound
                Boxes(X, Y, 0) = New Rectangle(size * (4 * X + 1), size * (4 * Y + 1), size * 3, size * 3)
                Boxes(X, Y, 1) = New Rectangle(size * (4 * X + 1), size * 4 * Y, size * 3, size)
                Boxes(X, Y, 2) = New Rectangle(size * 4 * X, size * (4 * Y + 1), size, size * 3)
            Next
        Next
        Walls = New Rectangle(bound + 1, bound + 1, 2) {}
        For Y = 0 To bound + 1
            For X = 0 To bound + 1
                Walls(X, Y, 0) = New Rectangle(size * (4 * X), size * (4 * Y), size, size)
                Walls(X, Y, 1) = New Rectangle(size * (4 * X + 1), size * 4 * Y, size * 3, size)
                Walls(X, Y, 2) = New Rectangle(size * 4 * X, size * (4 * Y + 1), size, size * 3)
            Next
        Next




        If Not cata Then
            TrapDoors = New Point(bound, bound) {}
            For Y = 0 To bound
                For X = 0 To bound
                    TrapDoors(X, Y) = New Point(size * (4 * X + 2), size * (4 * Y + 2))
                Next
            Next
            gasTraps = New Rectangle(bound, bound, 1) {}
            For Y = 0 To bound
                For X = 0 To bound
                    gasTraps(X, Y, 0) = New Rectangle(size * (4 * X + 1), size * (4 * Y + 2), size * 2, size)
                    gasTraps(X, Y, 1) = New Rectangle(size * (4 * X + 2), size * (4 * Y + 1), size, size * 2)
                Next
            Next
        End If
    End Sub

    Private Function toMaz(GameX As Integer, GameY As Integer) As Point
        Return New Point(CInt(((GameX - MazeConstants.COORD_OFFSET) Mod div) * size * scaleX), _
                        CInt(((GameY - MazeConstants.COORD_OFFSET) Mod div) * size * scaleY))
    End Function

    Public Function getNum(GameX As Integer, GameY As Integer) As Integer
        Return ((GameX - MazeConstants.COORD_OFFSET) \ div + 1) + (((GameY - MazeConstants.COORD_OFFSET) \ div) * size)
    End Function

    Public Sub plotMaze(GameX As Integer, GameY As Integer, col As Drawing.Color)
        Dim player As Point = toMaz(GameX, GameY)
        For Each box As Rectangle In Boxes
            If box.Contains(player) Then
                grBuff.FillRectangle(New SolidBrush(col), box)
                Exit Sub
            End If
        Next
    End Sub

    Public Sub plotPlayer(GameX As Integer, GameY As Integer, col As Color)
        Dim Player As Point = toMaz(GameX, GameY)
        grDisp.FillRectangle(New SolidBrush(col), Player.X, Player.Y, CInt(size * scaleX), CInt(size * scaleY))
    End Sub
    Private ptZero As New Point(0, 0)

    Public Sub Update()
        grDisp.DrawImage(v2Unde, ptZero)
        grDisp.DrawImage(buffer, ptZero)
        grDisp.DrawImage(v2Over, ptZero)
    End Sub
    Public Sub Clear(col As Color)
        grDisp.Clear(col)
        grBuff.Clear(Color.Transparent)
        grOver.Clear(Color.Transparent)
        grUnde.Clear(Color.Transparent)
    End Sub


    Public Sub drawTrash(gameX As Integer, gameY As Integer, col As Drawing.Color)
        Dim target As Point = toMaz(gameX, gameY)
        grBuff.FillRectangle(New SolidBrush(col), target.X, target.Y, CInt(size * scaleX), CInt(size * scaleY))
    End Sub

    'Public Sub drawfloor(gameX As Integer, gameY As Integer)
    '    Dim target As Point = toMaz(gameX, gameY)

    '    grUnde.FillRectangle(New SolidBrush(Color.LightGray), target.X, target.Y, size, size)

    'End Sub

    Public Sub drawShrine(gameX As Integer, gameY As Integer, col As Color, outline As Color)
        Dim target As Point = toMaz(gameX, gameY)

        grOver.FillRectangle(New SolidBrush(outline), New Rectangle(target.X - 2, target.Y - 2, CInt(8 * scaleX), CInt(8 * scaleY)))
        grOver.FillRectangle(New SolidBrush(col), New Rectangle(target.X, target.Y, CInt(4 * scaleX), CInt(4 * scaleY)))


    End Sub

    Public Sub drawWall(gameX As Integer, gameY As Integer, col As Color)
        Dim target As Point = toMaz(gameX, gameY)
        For Each wall As Rectangle In Walls
            If wall.Contains(target) Then
                grUnde.FillRectangle(New SolidBrush(col), wall)
                Exit Sub
            End If
        Next
    End Sub
    Public Function isWall(gameX As Integer, gameY As Integer)
        Dim target As Point = toMaz(gameX, gameY)
        For Each wall As Rectangle In Walls
            If wall.Contains(target) Then
                Return True
            End If
        Next
        Return False
    End Function

    Public Sub drawTD(gameX As Integer, gameY As Integer, NEtoSW As Boolean, open As Boolean, col As Drawing.Color, opencol As Drawing.Color, closedcol As Drawing.Color)
        Dim xOff As Integer = 0
        Dim yOff As Integer = 0
        Dim door As Drawing.Size
        Dim target As Point = toMaz(gameX, gameY)
        If NEtoSW Then
            yOff = -CInt(size * scaleY)
            door = New Size(CInt(size * scaleX), CInt(size * 3 * scaleY))
        Else
            xOff = -CInt(size * scaleX)
            door = New Size(CInt(size * 3 * scaleX), CInt(size * scaleY))
        End If
        For Each trap As Point In TrapDoors
            If trap = target Then
                grOver.FillRectangle(New SolidBrush(col), New Rectangle(New Point(trap.X + xOff, trap.Y + yOff), door))
                grOver.FillRectangle(New SolidBrush(If(open, opencol, closedcol)), trap.X, trap.Y, CInt(size * scaleX), CInt(size * scaleY))
            End If
        Next
    End Sub

    Public Function hasGT(gameX As Integer, gameY As Integer) As Boolean
        Return isGT(gameX, gameY)
    End Function
    Public Sub drawGT(gameX As Integer, gameY As Integer, col As Drawing.Color)
        Dim target As Point = toMaz(gameX, gameY)
        For Each gt As Rectangle In gasTraps
            If gt.Contains(target) Then
                grOver.FillRectangle(New SolidBrush(col), gt)
                Exit Sub
            End If
        Next
    End Sub
    Public Function isGT(gameX As Integer, gameY As Integer)
        Dim target As Point = toMaz(gameX, gameY)
        For Each gt As Rectangle In gasTraps
            If gt.Contains(target) Then
                Return True
            End If
        Next
        Return False
    End Function

    Public Function isLobby(GameX As Integer, GameY As Integer) As Boolean
        If cata Then
            If (GameX >= 245 OrElse GameY >= 245) Then
                Return True
            End If
        Else
            If (GameX >= 226 And GameX <= 253 And GameY <= 253 And GameY >= 247) Or
               (GameX >= 247 And GameX <= 253 And GameY <= 246 And GameY >= 226) Then
                Return True
            End If
        End If
        Return False
    End Function
    Public Function isYendor(GameX As Integer, GameY As Integer) As Boolean
        If Not cata AndAlso (GameX >= MazeConstants.YENDOR_MIN_X And GameX <= MazeConstants.YENDOR_MAX_X And _
                            GameY >= MazeConstants.YENDOR_MIN_Y And GameY <= MazeConstants.YENDOR_MAX_Y) Then
            Return True
        End If
        Return False
    End Function

    ''' <summary>
    ''' Resize the display bitmaps and recalculate all drawing coordinates
    ''' </summary>
    Public Sub ResizeDisplay(newWidth As Integer, newHeight As Integer)
        ' Calculate scale factor to maintain proportions
        scaleX = newWidth / 255.0
        scaleY = newHeight / 255.0
        
        ' Dispose old graphics and bitmaps
        Try
            If grDisp IsNot Nothing Then grDisp.Dispose()
            If grBuff IsNot Nothing Then grBuff.Dispose()
            If grOver IsNot Nothing Then grOver.Dispose()
            If grUnde IsNot Nothing Then grUnde.Dispose()
            If display IsNot Nothing Then display.Dispose()
            If buffer IsNot Nothing Then buffer.Dispose()
            If v2Over IsNot Nothing Then v2Over.Dispose()
            If v2Unde IsNot Nothing Then v2Unde.Dispose()
        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"Maze.ResizeDisplay dispose error: {ex.Message}")
        End Try
        
        ' Create new bitmaps with window dimensions
        display = New Bitmap(newWidth, newHeight)
        buffer = New Bitmap(newWidth, newHeight)
        v2Over = New Bitmap(newWidth, newHeight)
        v2Unde = New Bitmap(newWidth, newHeight)
        
        ' Create new graphics objects
        grDisp = Graphics.FromImage(display)
        grBuff = Graphics.FromImage(buffer)
        grOver = Graphics.FromImage(v2Over)
        grUnde = Graphics.FromImage(v2Unde)
        
        ' Recalculate all rectangles with scaling
        Boxes = New Rectangle(bound, bound, 2) {}
        For Y = 0 To bound
            For X = 0 To bound
                Boxes(X, Y, 0) = New Rectangle(CInt(size * (4 * X + 1) * scaleX), CInt(size * (4 * Y + 1) * scaleY), CInt(size * 3 * scaleX), CInt(size * 3 * scaleY))
                Boxes(X, Y, 1) = New Rectangle(CInt(size * (4 * X + 1) * scaleX), CInt(size * 4 * Y * scaleY), CInt(size * 3 * scaleX), CInt(size * scaleY))
                Boxes(X, Y, 2) = New Rectangle(CInt(size * 4 * X * scaleX), CInt(size * (4 * Y + 1) * scaleY), CInt(size * scaleX), CInt(size * 3 * scaleY))
            Next
        Next
        
        Walls = New Rectangle(bound + 1, bound + 1, 2) {}
        For Y = 0 To bound + 1
            For X = 0 To bound + 1
                Walls(X, Y, 0) = New Rectangle(CInt(size * 4 * X * scaleX), CInt(size * 4 * Y * scaleY), CInt(size * scaleX), CInt(size * scaleY))
                Walls(X, Y, 1) = New Rectangle(CInt(size * (4 * X + 1) * scaleX), CInt(size * 4 * Y * scaleY), CInt(size * 3 * scaleX), CInt(size * scaleY))
                Walls(X, Y, 2) = New Rectangle(CInt(size * 4 * X * scaleX), CInt(size * (4 * Y + 1) * scaleY), CInt(size * scaleX), CInt(size * 3 * scaleY))
            Next
        Next
        
        If Not cata Then
            TrapDoors = New Point(bound, bound) {}
            For Y = 0 To bound
                For X = 0 To bound
                    TrapDoors(X, Y) = New Point(CInt(MazeConstants.TRAP_DOOR_GRID_MULT_X * X * scaleX + MazeConstants.TRAP_DOOR_OFFSET_X * scaleX), _
                                               CInt(MazeConstants.TRAP_DOOR_GRID_MULT_Y * Y * scaleY + MazeConstants.TRAP_DOOR_OFFSET_Y * scaleY))
                Next
            Next
            
            gasTraps = New Rectangle(bound, bound, 1) {}
            For Y = 0 To bound
                For X = 0 To bound
                    gasTraps(X, Y, 0) = New Rectangle(CInt(4 * (4 * X + 1) * scaleX), CInt((4 * 4 * Y * scaleY) + MazeConstants.TRAP_DOOR_OFFSET_Y * scaleY), _
                                                     CInt(MazeConstants.GAS_TRAP_WIDTH * scaleX), CInt(MazeConstants.GAS_TRAP_HEIGHT * scaleY))
                    gasTraps(X, Y, 1) = New Rectangle(CInt((4 * 4 * Y * scaleY) + MazeConstants.TRAP_DOOR_OFFSET_Y * scaleY), CInt(4 * (4 * X + 1) * scaleX), _
                                                     CInt(MazeConstants.GAS_TRAP_HEIGHT * scaleY), CInt(MazeConstants.GAS_TRAP_WIDTH * scaleX))
                Next
            Next
        End If
        
        ' Clear with background color
        Clear(My.Settings.background)
    End Sub

    ''' <summary>
    ''' Properly dispose of graphics resources
    ''' </summary>
    Public Sub Dispose() Implements IDisposable.Dispose
        Dispose(True)
        GC.SuppressFinalize(Me)
    End Sub

    ''' <summary>
    ''' Protected dispose method for proper resource cleanup
    ''' </summary>
    Protected Sub Dispose(disposing As Boolean)
        If Not disposed Then
            If disposing Then
                Try
                    If grDisp IsNot Nothing Then grDisp.Dispose()
                    If grBuff IsNot Nothing Then grBuff.Dispose()
                    If grOver IsNot Nothing Then grOver.Dispose()
                    If grUnde IsNot Nothing Then grUnde.Dispose()
                    If display IsNot Nothing Then display.Dispose()
                    If buffer IsNot Nothing Then buffer.Dispose()
                    If v2Over IsNot Nothing Then v2Over.Dispose()
                    If v2Unde IsNot Nothing Then v2Unde.Dispose()
                Catch ex As Exception
                    System.Diagnostics.Debug.WriteLine($"Maze.Dispose error: {ex.Message}")
                End Try
            End If
            disposed = True
        End If
    End Sub

    ''' <summary>
    ''' Finalizer to ensure resources are cleaned up if not explicitly disposed
    ''' </summary>
    Protected Overrides Sub Finalize()
        Dispose(False)
        MyBase.Finalize()
    End Sub

End Class

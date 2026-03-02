''' <summary>
''' Constants module for maze configuration and memory offsets
''' </summary>
Module MazeConstants

    ' Maze dimensions and scaling factors
    Public Const CATA_SIZE As Integer = 3
    Public Const RD_SIZE As Integer = 4

    ' Division factors for coordinate calculations (141 - 20 * size)
    Public Const CATA_DIV As Integer = 81      ' 141 - 20 * 3
    Public Const RD_DIV As Integer = 61        ' 141 - 20 * 4

    ' Boundary values (34 - 5 * size)
    Public Const CATA_BOUND As Integer = 19    ' 34 - 5 * 3
    Public Const RD_BOUND As Integer = 14      ' 34 - 5 * 4

    ' Coordinate offset for calculations
    Public Const COORD_OFFSET As Integer = 2

    ' Visibility flag mask for sprite visibility checking
    Public Const VISIBILITY_FLAG As Integer = &H10

    ' Memory offset defaults
    Public Const FLAGS_OFFSET_DEFAULT As Integer = 12
    Public Const FSPRITE_OFFSET_DEFAULT As Integer = -4

    ' Trap door and gas trap positions
    Public Const TRAP_DOOR_OFFSET_X As Integer = 8
    Public Const TRAP_DOOR_OFFSET_Y As Integer = 8
    Public Const TRAP_DOOR_GRID_MULT_X As Integer = 16
    Public Const TRAP_DOOR_GRID_MULT_Y As Integer = 16

    ' Gas trap dimensions
    Public Const GAS_TRAP_WIDTH As Integer = 8
    Public Const GAS_TRAP_HEIGHT As Integer = 4

    ' Map boundaries for special locations
    Public Const YENDOR_MIN_X As Integer = 2
    Public Const YENDOR_MAX_X As Integer = 43
    Public Const YENDOR_MIN_Y As Integer = 248
    Public Const YENDOR_MAX_Y As Integer = 252

    ' Map grid
    Public Const MAP_SIZE As Integer = 255

    ' Sprite IDs for traps
    Public Const TRAP_SPRITE_ID_1 As Integer = 15291
    Public Const TRAP_SPRITE_ID_2 As Integer = 15300

    ' Wall sprite ID range (filters out floor sprites)
    Public Const WALL_SPRITE_MIN As Integer = 16384
    Public Const WALL_SPRITE_MAX As Integer = 32767

    ' Radius for scanning surrounding map units
    Public Const SCAN_RADIUS As Integer = 11

End Module

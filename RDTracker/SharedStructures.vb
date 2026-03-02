Imports System.Runtime.InteropServices

''' <summary>
''' Shared memory structure for MOAC game client integration
''' </summary>
<StructLayout(LayoutKind.Sequential, CharSet:=CharSet.Auto, Pack:=0)>
Public Structure MoacSharedMem
    ''' <summary>Process ID of the game client</summary>
    Public pID As UInt32
    ''' <summary>Player health points</summary>
    Public hp As Byte
    ''' <summary>Player shield value</summary>
    Public shield As Byte
    ''' <summary>Player end/stamina</summary>
    Public [end] As Byte
    ''' <summary>Player mana</summary>
    Public mana As Byte
    ''' <summary>Base address of game memory</summary>
    Public base As UInt64
    ''' <summary>Key offset for coordinate reading</summary>
    Public key As Integer
    ''' <summary>Sprite index offset</summary>
    Public isprite As Integer
    ''' <summary>X axis offset</summary>
    Public offX As Integer
    ''' <summary>Y axis offset</summary>
    Public offY As Integer
    ''' <summary>Flags offset</summary>
    Public flags As Integer
    ''' <summary>Foreground sprite offset</summary>
    Public fsprite As Integer
    ''' <summary>Whether X and Y coordinates are swapped</summary>
    Public swapped As Byte
End Structure

''' <summary>
''' Constants module for shared memory and configuration
''' </summary>
Module SharedConstants

End Module

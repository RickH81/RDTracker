Imports System.IO.MemoryMappedFiles
Imports System.Runtime.InteropServices

''' <summary>
''' Utility class for common memory reading patterns and helpers
''' </summary>
Public Class MemoryUtilities

    ''' <summary>
    ''' Safe read with validation and error handling
    ''' </summary>
    ''' <param name="manager">MemoryManager instance</param>
    ''' <param name="address">Address to read from</param>
    ''' <param name="defaultValue">Default value if read fails</param>
    ''' <returns>Read value or default value</returns>
    Public Shared Function SafeReadInt32(manager As MemoryManager, address As IntPtr, Optional defaultValue As Integer = 0) As Integer
        If manager Is Nothing OrElse manager.targetProcess Is Nothing Then
            Return defaultValue
        End If
        If Not ValidationHelpers.IsValidAddress(address) Then
            Return defaultValue
        End If
        Return manager.ReadInt32(address, False)
    End Function

    ''' <summary>
    ''' Safe read of shared memory structure
    ''' </summary>
    ''' <param name="processId">ID of game process</param>
    ''' <param name="shm">Output shared memory structure</param>
    ''' <returns>True if read successful</returns>
    Public Shared Function SafeReadSharedMem(processId As Integer, ByRef shm As MoacSharedMem) As Boolean
        Try
            If processId <= 0 Then
                Return False
            End If
            Dim mmf = MemoryMappedFile.CreateOrOpen($"MOAC{processId}", Marshal.SizeOf(GetType(MoacSharedMem)))
            Dim mmva = mmf.CreateViewAccessor()
            mmva.Read(0, shm)
            mmva.Dispose()
            mmf.Dispose()
            Return True
        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"SafeReadSharedMem error: {ex.Message}")
            Return False
        End Try
    End Function

    ''' <summary>
    ''' Calculate memory offset based on grid position
    ''' </summary>
    ''' <param name="dX">X offset in grid units</param>
    ''' <param name="dY">Y offset in grid units</param>
    ''' <param name="offsetX">X stride in bytes</param>
    ''' <param name="offsetY">Y stride in bytes</param>
    ''' <returns>Calculated memory offset</returns>
    Public Shared Function CalculateOffset(dX As Integer, dY As Integer, offsetX As Integer, offsetY As Integer) As Integer
        Return (offsetX * dX) + (offsetX * offsetY * dY)
    End Function

End Class

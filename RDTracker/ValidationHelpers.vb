Imports System.Runtime.InteropServices

''' <summary>
''' Helper methods for validating memory addresses and inputs
''' </summary>
Module ValidationHelpers

    ''' <summary>
    ''' Validate that an address is a valid pointer
    ''' </summary>
    ''' <param name="address">Address to validate</param>
    ''' <returns>True if address is valid (non-zero)</returns>
    Public Function IsValidAddress(address As IntPtr) As Boolean
        Return address <> IntPtr.Zero
    End Function

    ''' <summary>
    ''' Validate that an address is a valid pointer
    ''' </summary>
    ''' <param name="address">ULong address to validate</param>
    ''' <returns>True if address is valid (non-zero)</returns>
    Public Function IsValidAddress(address As ULong) As Boolean
        Return address <> 0
    End Function

    ''' <summary>
    ''' Validate that a memory size is within acceptable bounds
    ''' </summary>
    ''' <param name="size">Size in bytes</param>
    ''' <param name="maxSize">Maximum allowed size</param>
    ''' <returns>True if size is valid</returns>
    Public Function IsValidSize(size As Integer, Optional maxSize As Integer = 65536) As Boolean
        Return size > 0 AndAlso size <= maxSize
    End Function

    ''' <summary>
    ''' Validate game coordinates are within map bounds
    ''' </summary>
    ''' <param name="x">X coordinate</param>
    ''' <param name="y">Y coordinate</param>
    ''' <returns>True if coordinates are within bounds</returns>
    Public Function IsValidMapCoordinate(x As Integer, y As Integer) As Boolean
        Return x > 0 AndAlso y > 0 AndAlso x < 255 AndAlso y < 255
    End Function

    ''' <summary>
    ''' Validate process is running and has valid window handle
    ''' </summary>
    ''' <param name="process">Process to validate</param>
    ''' <returns>True if process is valid and active</returns>
    Public Function IsValidProcess(process As Process) As Boolean
        If process Is Nothing Then Return False
        Try
            Return Not process.HasExited AndAlso process.MainWindowHandle <> IntPtr.Zero
        Catch
            Return False
        End Try
    End Function

End Module

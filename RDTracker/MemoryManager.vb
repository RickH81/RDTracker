Imports System.IO.MemoryMappedFiles
Imports System.Runtime.InteropServices

''' <summary>
''' Manages memory reading/writing operations for target game process
''' </summary>
Public Class MemoryManager
    Implements IDisposable

    Private Declare Function OpenProcess Lib "kernel32.dll" (ByVal dwDesiredAcess As UInt32, ByVal bInheritHandle As Boolean, ByVal dwProcessId As Int32) As IntPtr
    Private Declare Function CloseHandle Lib "kernel32.dll" (ByVal hObject As IntPtr) As Boolean
    ''' <summary>Target process being monitored</summary>
    Public targetProcess As Process = Nothing
    ''' <summary>Whether the target is SDL client or legacy client</summary>
    Public isSDL As Boolean = False
    ''' <summary>Handle to target process for memory operations</summary>
    Private targetProcessHandle As IntPtr = IntPtr.Zero
    Private Const PROCESS_ALL_ACCESS As UInt32 = &H1F0FFF
    Private Const PROCESS_VM_READ As UInt32 = &H10
    Private _mmf As MemoryMappedFile
    Private _mmva As MemoryMappedViewAccessor
    Public shm As MoacSharedMem
    Private base As UInt32 = 0
    Private disposed As Boolean = False
#Region "attach by class"
    <DllImport("user32.dll", CharSet:=CharSet.Auto)>
    Private Shared Sub GetClassName(ByVal hWnd As System.IntPtr, ByVal lpClassName As System.Text.StringBuilder, ByVal nMaxCount As Integer)
    End Sub
    Private Function GetWindowClass(ByVal hwnd As Long) As String
        Dim sClassName As New System.Text.StringBuilder("", 256)
        Call GetClassName(hwnd, sClassName, 256)
        Return sClassName.ToString
    End Function
    Public Function TryAttachToProcess(ByVal windowCaption As String, ByVal windowClass As String, ByVal exeNames() As String) As Boolean
        For Each pp As Process In ListProcessesByNameArray(exeNames)
            If pp.MainWindowTitle.Contains(windowCaption) AndAlso GetWindowClass(pp.MainWindowHandle) = windowClass Then
                Return TryAttachToProcess(pp)
            End If
        Next
        Return False
    End Function
#End Region
    ''' <summary>
    ''' List processes by their executable names
    ''' </summary>
    ''' <param name="strings">Array of executable names to search for</param>
    ''' <returns>List of matching processes</returns>
    Public Shared Function ListProcessesByNameArray(strings() As String) As List(Of Process)
        Dim list As List(Of Process) = New List(Of Process)
        For Each exe As String In strings
            list.AddRange(Process.GetProcessesByName(Trim(exe)))
        Next
        Return list
    End Function

    ''' <summary>
    ''' Attach to a process by window caption and executable names
    ''' </summary>
    ''' <param name="windowCaption">Caption text the window title must contain</param>
    ''' <param name="exeNames">Array of possible executable names</param>
    ''' <returns>True if attachment successful</returns>
    Public Function TryAttachToProcess(ByVal windowCaption As String, ByVal exeNames() As String) As Boolean
        For Each pp As Process In ListProcessesByNameArray(exeNames)
            If pp.MainWindowTitle.Contains(windowCaption) Then
                Return TryAttachToProcess(pp)
            End If
        Next

        Return False
    End Function

    ''' <summary>
    ''' Attach to a specific process for memory reading/writing
    ''' </summary>
    ''' <param name="proc">Target process</param>
    ''' <returns>True if attachment successful</returns>
    Public Function TryAttachToProcess(ByVal proc As Process) As Boolean
        If proc Is Nothing Then Return False
        If targetProcessHandle = IntPtr.Zero Then 'not already attached
            targetProcess = proc
            'targetProcessHandle = OpenProcess(PROCESS_ALL_ACCESS, False, targetProcess.Id)
            targetProcessHandle = OpenProcess(PROCESS_VM_READ, False, targetProcess.Id)
            If targetProcessHandle = 0 Then
                TryAttachToProcess = False
                frmMain.runAsAdmin()
                End
                'MessageBox.Show("OpenProcess() FAIL!")
            Else
                'if we get here, all connected and ready to use ReadProcessMemory()
                _mmf = MemoryMappedFile.CreateOrOpen($"MOAC{Me.targetProcess.Id}", Marshal.SizeOf(GetType(MoacSharedMem)))
                _mmva = _mmf.CreateViewAccessor()
                _mmva.Read(0, shm)
                If shm.pID = Me.targetProcess.Id Then
                    isSDL = True
                Else
                    isSDL = False
                    base = targetProcess.MainModule.BaseAddress
                End If
                TryAttachToProcess = True
                'MessageBox.Show("OpenProcess() OK")
            End If
        Else
            'MessageBox.Show("Already attached! (Please Detach first?)")
            TryAttachToProcess = False
        End If
    End Function

    ''' <summary>
    ''' Detach from current process and clean up handles
    ''' </summary>
    Public Sub DetachFromProcess()
        If Not (targetProcessHandle = IntPtr.Zero) Then
            targetProcess = Nothing
            Try
                CloseHandle(targetProcessHandle)
                targetProcessHandle = IntPtr.Zero
                'MessageBox.Show("MemReader::Detach() OK")
            Catch ex As Exception
                MessageBox.Show("MemoryManager::DetachFromProcess::CloseHandle error " & Environment.NewLine & ex.Message)
            End Try
        End If
    End Sub

#Region "readmemory"
    Private Declare Function ReadProcessMemory Lib "kernel32" (ByVal hProcess As IntPtr, ByVal lpBaseAddress As IntPtr, ByVal lpBuffer() As Byte, ByVal iSize As Integer, ByRef lpNumberOfBytesRead As Integer) As Boolean
    <DllImport("NTDll.dll", SetLastError:=True, CharSet:=CharSet.Unicode)>
    Public Shared Function NtWow64ReadVirtualMemory64(
                handle As IntPtr,
                BaseAddress As UInt64,
                Buffer As Byte(),
                Size As UInt64,
                ByRef NumberOfBytesRead As UInt64) As Integer
    End Function
    ''' <summary>
    ''' Read a 32-bit integer from target process memory
    ''' </summary>
    ''' <param name="addr">Address to read from</param>
    ''' <param name="isOffset">If true, address is treated as offset from process base</param>
    ''' <returns>Value read, or 0 if read fails</returns>
    Public Function ReadInt32(ByVal addr As IntPtr, Optional ByVal isOffset As Boolean = False) As Int32
        Try
            If isOffset Then
                addr = addr + base
            End If
            If Not ValidationHelpers.IsValidAddress(addr) Then
                Return 0
            End If
            If targetProcessHandle = IntPtr.Zero Then
                Return 0
            End If
            Dim _dataBytes(3) As Byte
            Dim bytesRead As Integer = 0
            If Not ReadProcessMemory(targetProcessHandle, addr, _dataBytes, 4, bytesRead) Then
                Return 0
            End If
            Return BitConverter.ToInt32(_dataBytes, 0)
        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"MemoryManager.ReadInt32 error: {ex.Message}")
            Return 0
        End Try
    End Function

    Public Function ReadInt16(ByVal addr As IntPtr) As Int16
        Try
            If targetProcessHandle = IntPtr.Zero Then
                Return 0
            End If
            Dim _rtnBytes(1) As Byte
            Dim bytesRead As Integer = 0
            If Not ReadProcessMemory(targetProcessHandle, addr, _rtnBytes, 2, bytesRead) Then
                Return 0
            End If
            Return BitConverter.ToInt16(_rtnBytes, 0)
        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"MemoryManager.ReadInt16 error: {ex.Message}")
            Return 0
        End Try
    End Function
    'Public Function ReadInt32(ByVal addr As IntPtr) As Int32
    '    Dim _rtnBytes(3) As Byte
    '    ReadProcessMemory(targetProcessHandle, addr, _rtnBytes, 4, vbNull)

    '    Return BitConverter.ToInt32(_rtnBytes, 0)
    'End Function
    Public Function ReadIntWoW64(ByVal addr As ULong) As Integer
        Try
            If Not ValidationHelpers.IsValidAddress(addr) Then
                Return 0
            End If
            If targetProcessHandle = IntPtr.Zero Then
                Return 0
            End If
            Dim _rtnBytes(3) As Byte
            Dim bytesRead As UInt64 = 0
            Dim result = NtWow64ReadVirtualMemory64(targetProcessHandle, addr, _rtnBytes, 4, bytesRead)
            If result <> 0 Then
                Return 0
            End If
            Return BitConverter.ToInt32(_rtnBytes, 0)
        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"MemoryManager.ReadIntWoW64 error: {ex.Message}")
            Return 0
        End Try
    End Function
    Public Function ReadInt64(ByVal addr As IntPtr) As Int64
        Try
            If targetProcessHandle = IntPtr.Zero Then
                Return 0
            End If
            Dim _rtnBytes(7) As Byte
            Dim bytesRead As Integer = 0
            If Not ReadProcessMemory(targetProcessHandle, addr, _rtnBytes, 8, bytesRead) Then
                Return 0
            End If
            Return BitConverter.ToInt64(_rtnBytes, 0)
        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"MemoryManager.ReadInt64 error: {ex.Message}")
            Return 0
        End Try
    End Function
    Public Function ReadUInt16(ByVal addr As IntPtr) As UInt16
        Try
            If targetProcessHandle = IntPtr.Zero Then
                Return 0
            End If
            Dim _rtnBytes(1) As Byte
            Dim bytesRead As Integer = 0
            If Not ReadProcessMemory(targetProcessHandle, addr, _rtnBytes, 2, bytesRead) Then
                Return 0
            End If
            Return BitConverter.ToUInt16(_rtnBytes, 0)
        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"MemoryManager.ReadUInt16 error: {ex.Message}")
            Return 0
        End Try
    End Function
    Public Function ReadUInt32(ByVal addr As IntPtr) As UInt32
        Try
            If targetProcessHandle = IntPtr.Zero Then
                Return 0
            End If
            Dim _rtnBytes(3) As Byte
            Dim bytesRead As Integer = 0
            If Not ReadProcessMemory(targetProcessHandle, addr, _rtnBytes, 4, bytesRead) Then
                Return 0
            End If
            Return BitConverter.ToUInt32(_rtnBytes, 0)
        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"MemoryManager.ReadUInt32 error: {ex.Message}")
            Return 0
        End Try
    End Function
    Public Function ReadUInt64(ByVal addr As IntPtr) As UInt64
        Try
            If targetProcessHandle = IntPtr.Zero Then
                Return 0
            End If
            Dim _rtnBytes(7) As Byte
            Dim bytesRead As Integer = 0
            If Not ReadProcessMemory(targetProcessHandle, addr, _rtnBytes, 8, bytesRead) Then
                Return 0
            End If
            Return BitConverter.ToUInt64(_rtnBytes, 0)
        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"MemoryManager.ReadUInt64 error: {ex.Message}")
            Return 0
        End Try
    End Function
    Public Function ReadFloat(ByVal addr As IntPtr) As Single
        Try
            If targetProcessHandle = IntPtr.Zero Then
                Return 0
            End If
            Dim _rtnBytes(3) As Byte
            Dim bytesRead As Integer = 0
            If Not ReadProcessMemory(targetProcessHandle, addr, _rtnBytes, 4, bytesRead) Then
                Return 0
            End If
            Return BitConverter.ToSingle(_rtnBytes, 0)
        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"MemoryManager.ReadFloat error: {ex.Message}")
            Return 0
        End Try
    End Function
    Public Function ReadDouble(ByVal addr As IntPtr) As Double
        Try
            If targetProcessHandle = IntPtr.Zero Then
                Return 0
            End If
            Dim _rtnBytes(7) As Byte
            Dim bytesRead As Integer = 0
            If Not ReadProcessMemory(targetProcessHandle, addr, _rtnBytes, 8, bytesRead) Then
                Return 0
            End If
            Return BitConverter.ToDouble(_rtnBytes, 0)
        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"MemoryManager.ReadDouble error: {ex.Message}")
            Return 0
        End Try
    End Function
    Public Function ReadIntPtr(ByVal addr As IntPtr) As IntPtr
        Try
            If targetProcessHandle = IntPtr.Zero Then
                Return IntPtr.Zero
            End If
            Dim _rtnBytes(IntPtr.Size - 1) As Byte
            Dim bytesRead As Integer = 0
            If Not ReadProcessMemory(targetProcessHandle, addr, _rtnBytes, IntPtr.Size, bytesRead) Then
                Return IntPtr.Zero
            End If
            If IntPtr.Size = 4 Then
                Return New IntPtr(BitConverter.ToUInt32(_rtnBytes, 0))
            Else
                Return New IntPtr(BitConverter.ToInt64(_rtnBytes, 0))
            End If
        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"MemoryManager.ReadIntPtr error: {ex.Message}")
            Return IntPtr.Zero
        End Try
    End Function
    Public Function ReadBytes(ByVal addr As IntPtr, ByVal size As Int32) As Byte()
        Try
            If targetProcessHandle = IntPtr.Zero Then
                Return New Byte() {}
            End If
            Dim _rtnBytes(size - 1) As Byte
            Dim bytesRead As Integer = 0
            If Not ReadProcessMemory(targetProcessHandle, addr, _rtnBytes, size, bytesRead) Then
                Return New Byte() {}
            End If
            Return _rtnBytes
        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"MemoryManager.ReadBytes error: {ex.Message}")
            Return New Byte() {}
        End Try
    End Function
#End Region
#Region "writememory"
    Private Declare Function WriteProcessMemory Lib "kernel32" (ByVal hProcess As IntPtr, ByVal lpBaseAddress As IntPtr, ByVal lpBuffer() As Byte, ByVal iSize As Integer, ByRef lpNumberOfBytesRead As Integer) As Boolean

    Public Function WriteByte(ByVal addr As IntPtr, ByVal aByte As Byte) As Boolean
        Try
            If targetProcessHandle = IntPtr.Zero Then
                Return False
            End If
            Dim bytesWritten As Integer = 0
            Return WriteProcessMemory(targetProcessHandle, addr, New Byte() {aByte}, 1, bytesWritten)
        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"MemoryManager.WriteByte error: {ex.Message}")
            Return False
        End Try
    End Function
    Public Function WriteInt16(ByVal addr As IntPtr, ByVal data As Int16) As Boolean
        Try
            If targetProcessHandle = IntPtr.Zero Then
                Return False
            End If
            Dim bytesWritten As Integer = 0
            Return WriteProcessMemory(targetProcessHandle, addr, BitConverter.GetBytes(data), 2, bytesWritten)
        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"MemoryManager.WriteInt16 error: {ex.Message}")
            Return False
        End Try
    End Function
    Public Function WriteInt32(ByVal addr As IntPtr, ByVal data As Int32) As Boolean
        Try
            If targetProcessHandle = IntPtr.Zero Then
                Return False
            End If
            Dim bytesWritten As Integer = 0
            Return WriteProcessMemory(targetProcessHandle, addr, BitConverter.GetBytes(data), 4, bytesWritten)
        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"MemoryManager.WriteInt32 error: {ex.Message}")
            Return False
        End Try
    End Function
    Public Function WriteInt64(ByVal addr As IntPtr, ByVal data As Int64) As Boolean
        Try
            If targetProcessHandle = IntPtr.Zero Then
                Return False
            End If
            Dim bytesWritten As Integer = 0
            Return WriteProcessMemory(targetProcessHandle, addr, BitConverter.GetBytes(data), 8, bytesWritten)
        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"MemoryManager.WriteInt64 error: {ex.Message}")
            Return False
        End Try
    End Function
    Public Function WriteUInt16(ByVal addr As IntPtr, ByVal data As UInt16) As Boolean
        Try
            If targetProcessHandle = IntPtr.Zero Then
                Return False
            End If
            Dim bytesWritten As Integer = 0
            Return WriteProcessMemory(targetProcessHandle, addr, BitConverter.GetBytes(data), 2, bytesWritten)
        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"MemoryManager.WriteUInt16 error: {ex.Message}")
            Return False
        End Try
    End Function
    Public Function WriteUInt32(ByVal addr As IntPtr, ByVal data As UInt32) As Boolean
        Try
            If targetProcessHandle = IntPtr.Zero Then
                Return False
            End If
            Dim bytesWritten As Integer = 0
            Return WriteProcessMemory(targetProcessHandle, addr, BitConverter.GetBytes(data), 4, bytesWritten)
        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"MemoryManager.WriteUInt32 error: {ex.Message}")
            Return False
        End Try
    End Function
    Public Function WriteUInt64(ByVal addr As IntPtr, ByVal data As UInt64) As Boolean
        Try
            If targetProcessHandle = IntPtr.Zero Then
                Return False
            End If
            Dim bytesWritten As Integer = 0
            Return WriteProcessMemory(targetProcessHandle, addr, BitConverter.GetBytes(data), 8, bytesWritten)
        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"MemoryManager.WriteUInt64 error: {ex.Message}")
            Return False
        End Try
    End Function
    Public Function WriteFloat(ByVal addr As IntPtr, ByVal data As Single) As Boolean
        Try
            If targetProcessHandle = IntPtr.Zero Then
                Return False
            End If
            Dim bytesWritten As Integer = 0
            Return WriteProcessMemory(targetProcessHandle, addr, BitConverter.GetBytes(data), 4, bytesWritten)
        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"MemoryManager.WriteFloat error: {ex.Message}")
            Return False
        End Try
    End Function
    Public Function WriteDouble(ByVal addr As IntPtr, ByVal data As Double) As Boolean
        Try
            If targetProcessHandle = IntPtr.Zero Then
                Return False
            End If
            Dim bytesWritten As Integer = 0
            Return WriteProcessMemory(targetProcessHandle, addr, BitConverter.GetBytes(data), 8, bytesWritten)
        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"MemoryManager.WriteDouble error: {ex.Message}")
            Return False
        End Try
    End Function
    Public Function WriteIntPtr(ByVal addr As IntPtr, ByVal ptr As IntPtr) As Boolean
        Try
            If targetProcessHandle = IntPtr.Zero Then
                Return False
            End If
            Dim _bytes(IntPtr.Size - 1) As Byte
            Dim bytesWritten As Integer = 0
            If IntPtr.Size = 4 Then
                _bytes = BitConverter.GetBytes(Convert.ToUInt32(ptr))
                Return WriteProcessMemory(targetProcessHandle, addr, _bytes, 4, bytesWritten)
            Else
                _bytes = BitConverter.GetBytes(Convert.ToUInt64(ptr))
                Return WriteProcessMemory(targetProcessHandle, addr, _bytes, 8, bytesWritten)
            End If
        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"MemoryManager.WriteIntPtr error: {ex.Message}")
            Return False
        End Try
    End Function
    Public Function WriteUnicodeString(ByVal addr As IntPtr, ByVal str As String) As Boolean
        Try
            If targetProcessHandle = IntPtr.Zero Then
                Return False
            End If
            Dim _bytes() As Byte = System.Text.Encoding.Unicode.GetBytes(str)
            Dim bytesWritten As Integer = 0
            Return WriteProcessMemory(targetProcessHandle, addr, _bytes, _bytes.Length, bytesWritten)
        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"MemoryManager.WriteUnicodeString error: {ex.Message}")
            Return False
        End Try
    End Function
    Public Function WriteAsciiString(ByVal addr As IntPtr, ByVal str As String) As Boolean
        Try
            If targetProcessHandle = IntPtr.Zero Then
                Return False
            End If
            Dim _bytes() As Byte = System.Text.Encoding.ASCII.GetBytes(str)
            Dim bytesWritten As Integer = 0
            Return WriteProcessMemory(targetProcessHandle, addr, _bytes, _bytes.Length, bytesWritten)
        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"MemoryManager.WriteAsciiString error: {ex.Message}")
            Return False
        End Try
    End Function
    Public Function WriteBytes(ByVal addr As IntPtr, ByVal bytes() As Byte) As Boolean
        Try
            If targetProcessHandle = IntPtr.Zero Then
                Return False
            End If
            Dim _writeLength As Int32 = 0
            If WriteProcessMemory(targetProcessHandle, addr, bytes, bytes.Length, _writeLength) Then
                If _writeLength = bytes.Length Then
                    Return True
                Else
                    System.Diagnostics.Debug.WriteLine("MemoryManager::WriteBytes() writeLength < buff.size")
                    Return False
                End If
            Else
                Return False 'wpm failed!
            End If
        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"MemoryManager.WriteBytes error: {ex.Message}")
            Return False
        End Try
    End Function
#End Region

    ''' <summary>
    ''' Properly dispose of memory mapped resources
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
                    DetachFromProcess()
                    If _mmva IsNot Nothing Then _mmva.Dispose()
                    If _mmf IsNot Nothing Then _mmf.Dispose()
                Catch ex As Exception
                    System.Diagnostics.Debug.WriteLine($"MemoryManager.Dispose error: {ex.Message}")
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
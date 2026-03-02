Imports System.IO.MemoryMappedFiles
Imports System.Threading

''' <summary>
''' Inter-process communication module for managing single instance and activation
''' </summary>
Module IPC

    ' Synchronization lock for thread-safe access
    Private ReadOnly _syncLock As New Object()
    Private ReadOnly _MeStr As String = Application.ExecutablePath.Replace(":", "").Replace("\", "").Replace(".", "")
    Private ReadOnly _mmfSI As MemoryMappedFile = MemoryMappedFile.CreateOrOpen($"RDT_IPCSI_{_MeStr}", 1 + 1)
    Private ReadOnly _mmvaSI As MemoryMappedViewAccessor = _mmfSI.CreateViewAccessor()
    
    ''' <summary>
    ''' Whether this instance is already open (single instance check)
    ''' </summary>
    Public Property AlreadyOpen As Boolean
        Get
            SyncLock _syncLock
                Return _mmvaSI.ReadBoolean(0)
            End SyncLock
        End Get
        Set(value As Boolean)
            SyncLock _syncLock
                _mmvaSI.Write(0, value)
            End SyncLock
        End Set
    End Property
    
    ''' <summary>
    ''' Whether another instance requested activation/focus
    ''' </summary>
    Public Property RequestActivation As Boolean
        Get
            SyncLock _syncLock
                Return _mmvaSI.ReadBoolean(1)
            End SyncLock
        End Get
        Set(ByVal value As Boolean)
            SyncLock _syncLock
                _mmvaSI.Write(1, value)
            End SyncLock
        End Set
    End Property

End Module
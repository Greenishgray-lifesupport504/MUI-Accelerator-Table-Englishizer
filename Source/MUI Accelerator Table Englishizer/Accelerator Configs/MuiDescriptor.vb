
#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Diagnostics

#End Region

#Region " MuiDescriptor "

''' <summary>
''' Provides specific information for a MUI file.
''' </summary>
Public NotInheritable Class MuiDescriptor

#Region " Properties "

    ''' <summary>
    ''' Gets or sets the name of the MUI file.
    ''' </summary>
    Public Property FileName As String

    ''' <summary>
    ''' Gets or sets the CRC-32 checksum.
    ''' </summary>
    Public Property Checksum As String

    ''' <summary>
    ''' Gets or sets the accelerator table content or identifier associated with the MUI file.
    ''' </summary>
    Public Property AcceleratorTable As String

#End Region

#Region " Constructors "

    ''' <summary>
    ''' Prevents a default instance of the <see cref="MuiDescriptor"/> module from being created.
    ''' </summary>
    Private Sub New()
    End Sub

    ''' <summary>
    ''' Initializes a new instance of the <see cref="MuiDescriptor"/> class.
    ''' </summary>
    ''' 
    ''' <param name="muiFileName">
    ''' The name of the MUI file.
    ''' </param>
    ''' 
    ''' <param name="checksum">
    ''' The CRC-32 of the MUI file.
    ''' </param>
    ''' 
    ''' <param name="accTable">
    ''' The accelerator table associated with the MUI file.
    ''' </param>
    <DebuggerStepThrough>
    Public Sub New(muiFileName As String, checksum As String, accTable As String)

        Me.FileName = muiFileName
        Me.Checksum = checksum
        Me.AcceleratorTable = accTable
    End Sub

#End Region

End Class

#End Region
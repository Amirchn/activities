Imports Ayehu.Sdk.ActivityCreation.Interfaces
Imports Ayehu.Sdk.ActivityCreation.Extension
Imports System.Text
Imports System.DirectoryServices
Imports System.IO
Imports System.Net
Imports System
Imports System.Data
Imports Microsoft.VisualBasic

Namespace Ayehu.Sdk.ActivityCreation
    Public Class ActivityClass
        Implements IActivity


        Private Const DefaultAdPort As String = "389"
        Public HostName As String
        Public UserName As String
        Public Password As String
        Public ADUserName As String
        Public ChangeNextLogon As Integer
        Public NeverExpires As Integer
        Public SecurePort As String

        Public Function Execute() As ICustomActivityResult Implements IActivity.Execute


            Dim dt As DataTable = New DataTable("resultSet")
            dt.Columns.Add("Result", GetType(String))

            If String.IsNullOrEmpty(SecurePort) = True Then
                SecurePort = DefaultAdPort
            End If

            If IsNumeric(SecurePort) = False Then
                Dim msg As String = "Port parameter must be number"
                Throw New ApplicationException(msg)
            End If

            Dim de As DirectoryEntry = GetAdEntry(HostName, SecurePort, UserName, Password)

            Dim ds As DirectorySearcher = New DirectorySearcher(de)
            ds.Filter = "(&(objectClass=user) (sAMAccountName=" + ADUserName + "))"
            ds.SearchScope = SearchScope.Subtree
            Dim results As SearchResult = ds.FindOne()
            If results IsNot Nothing Then
                Dim user As DirectoryEntry = results.GetDirectoryEntry()
                If ChangeNextLogon = 1 Then
                    user.Properties("pwdLastSet").Value = 0
                Else
                    user.Properties("pwdLastSet").Value = -1
                End If
                user.CommitChanges()

                If NeverExpires = 1 Then 'Password expired
                    Dim value3 As Integer = Val(de.Properties("userAccountControl").Value)
                    user.Properties("userAccountControl").Value = value3 Or &H10000
                    user.CommitChanges()
                Else
                    Dim value3 As Integer = Val(de.Properties("userAccountControl").Value)
                    user.Properties("userAccountControl").Value = value3 And Not &H10000
                    user.CommitChanges()
                End If
                user.Close()
                dt.Rows.Add("Success")
            Else
                Throw New Exception("User does not exist")
            End If
            de.Close()

            Return Me.GenerateActivityResult(dt)
        End Function
        Public Function GetAdEntry(ByVal domainServer As String, ByVal domainPort As String, ByVal username As String, ByVal password As String) As DirectoryEntry
            Dim defaultAdSecurePort As String = "636"
            If domainPort.Equals(defaultAdSecurePort) AndAlso IsIpAddress(domainServer) Then Throw New Exception("When using a secure port, a server domain name must be defined for the device.")
            Dim domainUrl As String = "LDAP://" & domainServer

            If Not domainPort.Equals(DefaultAdPort) Then
                domainUrl = domainUrl & ":" & domainPort
            End If

            Dim adEntry = New DirectoryEntry(domainUrl, username, password, AuthenticationTypes.Secure)
            Return adEntry
        End Function

        Private Function IsIpAddress(ByVal domainServer As String) As Boolean
            Dim address As IPAddress
            Return IPAddress.TryParse(domainServer, address)
        End Function



    End Class
End Namespace


Imports MimeKit.Cryptography
Imports Org.BouncyCastle.Bcpg.OpenPgp

Public NotInheritable Class LcarsOpenPgpContext
    Inherits GnuPGContext

    Public Sub New()
        MyBase.New(MailPgpService.GnupgHomeDirectory())
    End Sub

    ''' <summary>
    ''' GnuPG passphrase callback. Empty string lets gpg-agent / pinentry handle interactive unlock when available.
    ''' </summary>
    Protected Overrides Function GetPasswordForKey(key As PgpSecretKey) As String
        Return String.Empty
    End Function
End Class

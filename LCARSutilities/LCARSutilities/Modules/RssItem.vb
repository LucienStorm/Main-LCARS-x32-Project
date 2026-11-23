Public Class RssItem
        Public Title As String

        Public Link As String

        Public Description As String

        Public PublishedUtc As Date

        Public Sub New()
            Title = String.Empty
            Link = String.Empty
            Description = String.Empty
            PublishedUtc = Date.MinValue
        End Sub
    End Class

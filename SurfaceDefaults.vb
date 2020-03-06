Option Explicit On
Option Strict On

Namespace Models.EntityHierarchy

    Public Class SurfaceDefaults
        Public Property SurfaceDefaultsGuid As String
        Public Property SystemAttributeName As String
        Public Property SystemAttributeGuid As String = String.Empty
        Public Property ColorRampName As String = String.Empty
        Public Property ColorRampGuid As String
        Public Property ClassificationMethodName As String
        Public Property ClassificationMethodGuid As String = String.Empty
        Public Property NumberOfClasses As String = String.Empty
        Public Property ModifiedDate As String = String.Empty
        Public Property OrgLevelList As List(Of SurfaceDefaultsOrgLevel)
    End Class

    Public Class SurfaceDefaultsImportExport
        Public Property SystemAttributeName As String
        Public Property ColorRampName As String = String.Empty
        Public Property ClassificationMethodName As String
        Public Property NumberOfClasses As String
        Public Property SurfaceDefaultsGuid As String
    End Class

    Public Class SurfaceDefaultsOrgLevel
        Public Property SurfaceDefaultsOrgLevelGuid As String
        Public Property SurfaceDefaultsGuid As String
        Public Property OrgLevelGuid As String
        Public Property OrgLevelName As String
        Public Property City As String
        Public Property StateAbbreviation As String
        Public Property ParentOrgLevelGuid As String
        Public Property EditMode As Boolean
    End Class

    Public Class ClassificationMethod
        Public Property ClassificationMethodGuid As String
        Public Property ClassificationMethodName As String
    End Class

    Public Class ListItem
        Public Property Guid As String
        Public Property Name As String
    End Class
End Namespace
Option Explicit On
Option Strict On

Namespace Models.EntityHierarchy

    Public Class SurfaceDefaultsAutoCompleteRequest
        Public Property SurfaceDefaultsFilter As SurfaceDefaultsFilters
        Public Property SearchName As String
        Public Property SearchString As String
        Public Property UserGuid As String
    End Class

    Public Class SurfaceDefaultsFilters
        Public Property CanDelete As String
        Public Property SystemAttributeName As String
        Public Property ColorRampName As String
        Public Property ClassificationMethodName As String
        Public Property NumberOfClasses As String
        Public Property ModifiedDate As String
        Public Property LocationLevel As String
        Public Property OrgLevelGuid As String 'from hierarchy filters
    End Class
    
	Public Class SurfaceDefaultsGridRow
        Public Property CanDelete As Boolean
        Public Property SystemAttributeName As String
        Public Property ColorRampName As String
        Public Property ClassificationMethodName As String
        Public Property NumberOfClasses As String
        Public Property ModifiedDate As String
        Public Property OrgLevelGuid As String
        Public Property OrgLevelId As String
        Public Property OrgLevelName As String
        Public Property SurfaceDefaultsGuid As String
    End Class

    Public Class SurfaceDefaultsRequest
        Public Const FieldCanDelete As String = "CanDelete"
        Public Const FieldSystemAttributeName As String = "SystemAttributeName"
        Public Const FieldColorRampName As String = "ColorRampName"
        Public Const FieldClassificationMethodName As String = "ClassificationMethodName"
        Public Const FieldLocationLevelName As String = "LocationLevel"
        Public Const FieldNumberOfClasses As String = "NumberOfClasses"
        Public Const FieldModifiedDate As String = "ModifiedDate"
        Public Property SurfaceDefaultsFilter As SurfaceDefaultsFilters
        Public Property SurfaceDefaultsPageOptions As PageOptions
        Public Property SurfaceDefaultsSort As List(Of Sorts)

    End Class

End Namespace
Option Explicit On
Option Strict On

Imports HaasWebApp.Models.EntityHierarchy.Support
Namespace Models.EntityHierarchy

    Public Class ProductAssignment
        Public Property ActiveYn As Boolean
        Public Property CropList As List(Of ProductAssignmentCrop)
        Public Property ProductAssignmentGuid As String
        Public Property SampleTypeName As String
        Public Property SampleTypeGuid As String = String.Empty
        Public Property NutrientGuid As String = String.Empty
        Public Property NutrientName As String
        Public Property OrgLevelList As List(Of ProductAssignmentOrgLevel)
        Public Property ProductList As List(Of ProductAssignmentProduct)
    End Class

    Public Class ProductAssignmentCrop
        Public Property ProductAssignmentCropGuid As String
        Public Property ProductAssignmentGuid As String
        Public Property CropGuid As String
        Public Property CropName As String
        Public Property CropId As String
        Public Property CropClassNameGuid As String
        Public Property CropPurposeName As String
        Public Property CropPurposeGuid As String
        Public Property GrowthStageOrderName As String
        Public Property GrowthStageOrderGuid As String
    End Class

    Public Class ProductAssignmentImportExportBase
        Public Property ActiveYn As Boolean = True

        Public Property SampleTypeName As String = String.Empty
        Public Property NutrientName As String = String.Empty
    End Class

    Public Class ProductAssignmentImportExport
        Inherits ProductAssignmentImportExportBase

        Public Property ProductAssignmentGuid As String
    End Class

    Public Class ProductAssignmentOrgLevel
        Public Property ProductAssignmentOrgLevelGuid As String
        Public Property ProductAssignmentGuid As String
        Public Property OrgLevelGuid As String
        Public Property OrgLevelName As String
        Public Property City As String
        Public Property StateAbbreviation As String
        Public Property ParentOrgLevelGuid As String
        Public Property EditMode As Boolean
    End Class

    Public Class ProductAssignmentProduct
        Public Property ProductAssignmentProductGuid As String
        Public Property ProductAssignmentGuid As String
        Public Property ProductGuid As String
        Public Property ProductName As String
    End Class
End Namespace
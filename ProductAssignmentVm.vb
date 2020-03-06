Imports HaasWebApp.Models.EntityHierarchy
Imports HaasCore.ViewModels.Data
Imports HaasCore.Models
Imports HaasCore.ViewModels.Data.DataServer
Imports HaasWebApp.ViewModels.UserRoles
Imports HaasWebApp.Models
Imports HaasCore.ViewModels
Imports System.IO
Imports Microsoft.VisualBasic.FileIO
Imports HaasWebApp.ViewModels.AgBytes.AgBytesCommonVm
Imports HaasWebApp.ViewModels.Users
Imports HaasCore.ViewModels.Data.SqlBuilder
Imports HaasWebApp.Models.AgBytes
Imports HaasWebApp.ViewModels.AgBytes

Namespace ViewModels.EntityHierarchy
    Public Class ProductAssignmentVm

#Region " Declarations "
        Public Const NoneIndicator As String = "None"
#End Region

#Region " Shared Methods "

        Public Shared Function Valid(productAssignment As ProductAssignment, loggedInUserGuid As String, apiResult As ApiResult, errorList As List(Of String)) As Boolean
            Dim count As Integer = apiResult.ErrorCodeList.Count

            If UserRoleVm.AllowedUser(SoftwareFunctionVm.FunctionName.ProductAssignment, loggedInUserGuid) = False Then
                apiResult.ErrorCodeList.Add(ApiResultVm.ErrorCode.ApiAccessDenied)
                Return False
            End If

            If String.IsNullOrEmpty(productAssignment.SampleTypeGuid) Then
                apiResult.ErrorCodeList.Add(ApiResultVm.ErrorCode.SampleTypeNameRequired)
            End If

            If String.IsNullOrEmpty(productAssignment.NutrientGuid) Then
                apiResult.ErrorCodeList.Add(ApiResultVm.ErrorCode.NutrientNameRequired)
            End If

            If productAssignment.ProductList.Count = 0 Then
                apiResult.ErrorCodeList.Add(ApiResultVm.ErrorCode.ProductRequired)
            End If

            If productAssignment.OrgLevelList.Count = 0 Then
                apiResult.ErrorCodeList.Add(ApiResultVm.ErrorCode.LocationRequired)
            End If

            If Not IsProductAssignmentUnique(productAssignment, loggedInUserGuid) Then
                apiResult.ErrorCodeList.Add(ApiResultVm.ErrorCode.ProductAssignmentNotUnique)
            End If

            If productAssignment.CropList.GroupBy(Function(x) $"{x.CropGuid}{x.CropPurposeGuid}{x.GrowthStageOrderGuid}"
                                                           ).Where(Function(x) x.Count > 1).Count > 0 Then
                apiResult.ErrorCodeList.Add(ApiResultVm.ErrorCode.CropListHasDuplicates)
            End If

            If productAssignment.ProductList.GroupBy(Function(x) $"{x.ProductGuid}"
                                                           ).Where(Function(x) x.Count > 1).Count > 0 Then
                apiResult.ErrorCodeList.Add(ApiResultVm.ErrorCode.ProductListHasDuplicates)
            End If

            ApiResultVm.RemoveDuplicates(apiResult)

            Return count = apiResult.ErrorCodeList.Count
        End Function

        Public Shared Function IsProductAssignmentUnique(productAssignment As ProductAssignment, loggedInUserGuid As String) As Boolean
            Dim sb As New SqlBuilder
            sb.Add("SELECT COUNT(*)")
            sb.Add("FROM {0}ProductAssignment pa", Schema)
            sb.Add("INNER JOIN {0}ProductAssignmentOrgLevel pao ON pao.ProductAssignmentGuid = pa.ProductAssignmentGuid", Schema)
            sb.Add("LEFT JOIN {0}ProductAssignmentCrop pac ON pac.ProductAssignmentGuid = pa.ProductAssignmentGuid", Schema)
            sb.Add("LEFT JOIN {0}ProductAssignmentProduct pap ON pap.ProductAssignmentGuid = pa.ProductAssignmentGuid", Schema)
            sb.Add("WHERE pa.ProductAssignmentGuid " & If(StringIsEmpty(productAssignment.ProductAssignmentGuid), " IS NOT NULL", " <> " & FormatKeyOrNull(productAssignment.ProductAssignmentGuid)))
            sb.Add(" AND pa.SampleTypeGuid = " & FormatKeyOrNull(productAssignment.SampleTypeGuid))
            sb.Add(" AND pa.NutrientGuid = " & FormatKeyOrNull(productAssignment.NutrientGuid))
            sb.Add(" AND pa.ActiveYN = 1")
            Dim loopPrefix = ""
            If productAssignment.CropList.Count > 0 Then
                sb.Add(" AND (")
                For Each cropGrowth In productAssignment.CropList
                    sb.Add(loopPrefix)
                    sb.Add(" ( pac.CropGuid = " & FormatKeyOrNull(cropGrowth.CropGuid))
                    sb.Add(" AND pac.CropPurposeGuid " & If(StringHasValue(cropGrowth.CropPurposeGuid), "=" & FormatKeyOrNull(cropGrowth.CropPurposeGuid), "IS NULL"))
                    sb.Add(" AND pac.GrowthStageOrderGuid " & If(StringHasValue(cropGrowth.GrowthStageOrderGuid), "=" & FormatKeyOrNull(cropGrowth.GrowthStageOrderGuid) & ")", "IS NULL)"))
                    loopPrefix = " AND "
                Next
                sb.Add(")")
            Else
                sb.Add("AND (")
                sb.Add(" pac.CropGuid IS NULL")
                sb.Add(" )")
            End If
            loopPrefix = ""
            If productAssignment.OrgLevelList.Count > 0 Then
                sb.Add(" AND (")
                For Each orgLevel In productAssignment.OrgLevelList
                    sb.Add(loopPrefix)
                    sb.Add(" ( pao.OrgLevelGuid = " & FormatKeyOrNull(orgLevel.OrgLevelGuid) & ")")
                    loopPrefix = " AND "
                Next
                sb.Add(")")
            End If

            Dim dt As DataTable = GetDataTable(sb.Sql)
            Return GetInteger(dt.Rows(0)(0).ToString()) = 0
        End Function

        Public Shared Function ValidGetProductAssignmentList(request As ProductAssignmentRequest, apiResult As ApiResult) As Boolean
            Dim count As Integer = apiResult.ErrorCodeList.Count
            If Sorts.SortExists(request.ProductAssignmentSort) Then
                Select Case request.ProductAssignmentSort(0).FieldName
                    Case ProductAssignmentRequest.FieldCanDelete,
                        ProductAssignmentRequest.FieldCropName,
                        ProductAssignmentRequest.FieldCropPurposeName,
                        ProductAssignmentRequest.FieldGrowthStageOrderName,
                        ProductAssignmentRequest.FieldIsActive,
                        ProductAssignmentRequest.FieldLocationLevelName,
                        ProductAssignmentRequest.FieldNutrientName,
                        ProductAssignmentRequest.FieldProductName,
                        ProductAssignmentRequest.FieldSampleTypeName
                        'success
                    Case Else
                        apiResult.ErrorCodeList.Add(ApiResultVm.ErrorCode.SortNameInvalid)
                End Select
            End If
            Return count = apiResult.ErrorCodeList.Count
        End Function

#End Region

#Region " Get List "
        Public Shared Function GetCropListSelect() As String
            Return String.Format("ISNULL((SELECT STUFF((SELECT '; ' + Crop.Name FROM {0}ProductAssignmentCrop ProductAssignmentCrop INNER JOIN {0}Crop Crop ON Crop.CropGuid = ProductAssignmentCrop.CropGuid WHERE ProductAssignmentCrop.ProductAssignmentGuid = ProductAssignment.ProductAssignmentGuid ORDER BY Crop.Name For XML Path(''), Type).value('.', 'nvarchar(max)'), 1, 2, '')), '')", Schema)
        End Function

        Public Shared Function GetCropPurposeListSelect() As String
            Return String.Format("ISNULL((SELECT STUFF((SELECT '; ' + CropPurpose.Name FROM {0}ProductAssignmentCrop ProductAssignmentCrop INNER JOIN {0}CropPurpose CropPurpose ON CropPurpose.CropPurposeGuid = ProductAssignmentCrop.CropPurposeGuid WHERE ProductAssignmentCrop.ProductAssignmentGuid = ProductAssignment.ProductAssignmentGuid ORDER BY CropPurpose.Name For XML Path(''), Type).value('.', 'nvarchar(max)'), 1, 2, '')), '')", Schema)
        End Function

        Public Shared Function GetFilterSql(filter As ProductAssignmentFilters, activeOnly As Boolean, loggedInUserGuid As String) As String
            Dim sb As New SqlBuilder
            If activeOnly = True Then 'checkbox value
                sb.Add("    AND productAssignment.ActiveYN = 1")
            End If
            If String.IsNullOrEmpty(filter.IsActive) = False Then 'column filter
                sb.Add("    AND  productAssignment.ActiveYN = {0}", StringAsBoolean(filter.IsActive))
            End If
            If String.IsNullOrEmpty(filter.CanDelete) = False Then
                sb.Add("    AND CanDelete = {0}", StringAsBoolean(filter.CanDelete))
            End If
            If String.IsNullOrEmpty(filter.SampleTypeName) = False Then
                sb.Add("    AND (st.Name LIKE '{0}%' OR st.Name LIKE '% {0}%')", filter.SampleTypeName)
            End If
            If String.IsNullOrEmpty(filter.NutrientName) = False Then
                sb.Add("    AND (nu.Name LIKE '{0}%' OR nu.Name LIKE '% {0}%')", filter.NutrientName)
            End If
            If String.IsNullOrEmpty(filter.LocationLevel) = False Then
                sb.Add("    AND ({0}", PadTic.No, GetOrgLevelListSelect(loggedInUserGuid))
                sb.Add("        LIKE '{0}%' ", filter.LocationLevel)
                sb.Add("    OR {0}", PadTic.No, GetOrgLevelListSelect(loggedInUserGuid))
                sb.Add("        LIKE '% {0}%' )", filter.LocationLevel)
            End If
            If String.IsNullOrEmpty(filter.CropName) = False Then
                sb.Add("    AND ({0}", PadTic.No, GetCropListSelect)
                sb.Add("        LIKE '{0}%' ", filter.CropName)
                sb.Add("    OR {0}", PadTic.No, GetCropListSelect)
                sb.Add("        LIKE '% {0}%' )", filter.CropName)
            End If
            If String.IsNullOrEmpty(filter.CropPurposeName) = False Then
                sb.Add("    AND ({0}", PadTic.No, GetCropPurposeListSelect)
                sb.Add("        LIKE '{0}%' ", filter.CropPurposeName)
                sb.Add("    OR {0}", PadTic.No, GetCropPurposeListSelect)
                sb.Add("        LIKE '% {0}%' )", filter.CropPurposeName)
            End If
            If String.IsNullOrEmpty(filter.ProductName) = False Then
                sb.Add("    AND ({0}", PadTic.No, GetProductListSelect)
                sb.Add("        LIKE '{0}%' ", filter.ProductName)
                sb.Add("    OR {0}", PadTic.No, GetProductListSelect)
                sb.Add("        LIKE '% {0}%' )", filter.ProductName)
            End If
            If String.IsNullOrEmpty(filter.GrowthStageOrderName) = False Then
                sb.Add("    AND ({0}", PadTic.No, GetGrowthStageOrderListSelect)
                sb.Add("        LIKE '{0}%' ", filter.GrowthStageOrderName)
                sb.Add("    OR {0}", PadTic.No, GetGrowthStageOrderListSelect)
                sb.Add("        LIKE '% {0}%' )", filter.GrowthStageOrderName)
            End If
            Return sb.Sql
        End Function

        Public Shared Function GetGrowthStageOrderListSelect() As String
            Return String.Format("ISNULL((SELECT STUFF((SELECT '; ' + GrowthStageOrder.Name FROM {0}ProductAssignmentCrop ProductAssignmentCrop INNER JOIN {0}GrowthStageOrder GrowthStageOrder ON GrowthStageOrder.GrowthStageOrderGuid = ProductAssignmentCrop.GrowthStageOrderGuid WHERE ProductAssignmentCrop.ProductAssignmentGuid = ProductAssignment.ProductAssignmentGuid ORDER BY GrowthStageOrder.Name For XML Path(''), Type).value('.', 'nvarchar(max)'), 1, 2, '')), '')", Schema)
        End Function

        Public Shared Sub GetProductAssignmentList(request As ProductAssignmentRequest, loggedInUserGuid As String, apiResult As ApiResult)
            If ValidGetProductAssignmentList(request, apiResult) = False Then
                Return
            End If

            Dim sortOrder As String = GetSortOrderSql(apiResult, request.ProductAssignmentSort, loggedInUserGuid)
            Dim listSql As String = GetProductAssignmentListSql(request, sortOrder, loggedInUserGuid)
            Dim dt As DataTable = GetDataTable(listSql)

            Dim list As New List(Of ProductAssignmentGridRow)
            Dim gridPageResponse As New GridPageResponse
            Dim firstRow As Boolean = True
            For Each row As DataRow In dt.Rows
                If firstRow Then
                    gridPageResponse.TotalCount = CInt(row.Item("TotalRows"))
                    firstRow = False
                End If

                Dim item As New ProductAssignmentGridRow
                item.ProductAssignmentGuid = row.Item("ProductAssignmentGuid").ToString
                item.SampleTypeName = row.Item("SampleTypeName").ToString
                item.NutrientName = row.Item("NutrientName").ToString
                item.OrgLevelName = row("OrgLevelList").ToString
                item.CropName = row("CropList").ToString
                item.CropPurposeName = row("CropPurposeList").ToString
                item.GrowthStageOrderName = row("GrowthStageOrderList").ToString
                item.ProductName = row("ProductList").ToString
                item.IsActive = CBool(row.Item("ActiveYn"))
                item.CanDelete = GetBoolean(row.Item("CanDelete").ToString())

                list.Add(item)
            Next

            gridPageResponse.GridRows = list
            apiResult.Model = gridPageResponse
            apiResult.Success = True

        End Sub

        Public Shared Function GetProductAssignmentListSql(request As ProductAssignmentRequest, sortOrder As String, loggedInUserGuid As String) As String

            Dim sb As New SqlBuilder

            sb.Add("SELECT * FROM")
            sb.Add("(")
            sb.Add("    SELECT ROW_NUMBER() OVER (ORDER BY {0}) AS RowNumber", PadTic.No, sortOrder)
            sb.Add("        , COUNT(*) OVER () AS TotalRows")
            sb.Add("        ,productAssignment.ProductAssignmentGuid")
            sb.Add("        ,st.Name AS SampleTypeName")
            sb.Add("        ,nu.Name AS NutrientName")
            sb.Add("        ,{0} AS OrgLevelList", PadTic.No, GetOrgLevelListSelect(loggedInUserGuid))
            sb.Add("        ,{0} AS ProductList", PadTic.No, GetProductListSelect)
            sb.Add("        ,{0} AS CropList", PadTic.No, GetCropListSelect)
            sb.Add("        ,{0} AS CropPurposeList", PadTic.No, GetCropPurposeListSelect)
            sb.Add("        ,{0} AS GrowthStageOrderList", PadTic.No, GetGrowthStageOrderListSelect)
            sb.Add("        ,{0} AS ModifiedDate", PadTic.No, GetModifiedDateSql(TableEnum.ProductAssignment, loggedInUserGuid))
            sb.Add("        ,productAssignment.ActiveYN AS ActiveYN ")
            sb.Add("        ,CanDelete ")
            sb.Add("    FROM  {0}ProductAssignment productAssignment", Schema)
            sb.Add("    INNER JOIN {0}SampleType st ON productAssignment.SampleTypeGuid = st.SampleTypeGuid ", Schema)
            sb.Add("    INNER JOIN {0}Nutrient nu ON productAssignment.NutrientGuid = nu.NutrientGuid ", Schema)
            sb.Add(GetCanDeleteApplySql(TableEnum.ProductAssignment))
            sb.Add("    WHERE 1=1")
            sb.Add(GetFilterSql(request.ProductAssignmentFilter, request.ActiveOnly, loggedInUserGuid))
            sb.Add(" AND EXISTS (SELECT * FROM {0}GetUserHierarchyAccess('{1}') ha INNER JOIN {0}ProductAssignmentOrgLevel pao ON pao.OrgLevelGuid = ha.OrgLevelGuid WHERE pao.ProductAssignmentGuid = productAssignment.ProductAssignmentGuid )", Schema, loggedInUserGuid)
            If StringHasValue(request.ProductAssignmentFilter.OrgLevelGuid) Then
                sb.Add(" AND EXISTS (SELECT * FROM {0}GetHierarchyChildren('{1}') ha INNER JOIN {0}ProductAssignmentOrgLevel pao ON pao.OrgLevelGuid = ha.OrgLevelGuid WHERE pao.ProductAssignmentGuid = productAssignment.ProductAssignmentGuid )", Schema, request.ProductAssignmentFilter.OrgLevelGuid)
            End If
            sb.Add(") As ProductAssignments")
            sb.Add("WHERE RowNumber > {0}", request.ProductAssignmentPageOptions.Skip)
            sb.Add("And RowNumber <= {0}", request.ProductAssignmentPageOptions.Skip + request.ProductAssignmentPageOptions.PageSize)
            sb.Add("ORDER BY RowNumber")

            Return sb.Sql
        End Function
        Public Shared Function GetProductListSelect() As String
            Return String.Format("ISNULL((Select STUFF((Select '; ' + Product.Name FROM {0}ProductAssignmentProduct ProductAssignmentProduct INNER JOIN {0}Product Product ON Product.ProductGuid = ProductAssignmentProduct.ProductGuid WHERE ProductAssignmentProduct.ProductAssignmentGuid = ProductAssignment.ProductAssignmentGuid ORDER BY Product.Name For XML Path(''), Type).value('.', 'nvarchar(max)'), 1, 2, '')), '')", Schema)
        End Function

        Public Shared Function GetOrgLevelListSelect(loggedInUserGuid As String) As String
            Return String.Format("ISNULL((SELECT STUFF((SELECT '; ' + OrgLevel.Name FROM {0}ProductAssignmentOrgLevel ProductAssignmentOrgLevel INNER JOIN {0}OrgLevel OrgLevel ON OrgLevel.OrgLevelGuid = ProductAssignmentOrgLevel.OrgLevelGuid INNER JOIN {0}GetHierarchyChildren('{1}') Children ON OrgLevel.OrgLevelGuid = Children.OrgLevelGuid  WHERE ProductAssignmentOrgLevel.ProductAssignmentGuid = ProductAssignment.ProductAssignmentGuid ORDER BY OrgLevel.Name For XML Path(''), Type).value('.', 'nvarchar(max)'), 1, 2, '')), '')", Schema, UserVm.GetCompanyGuid(loggedInUserGuid))
        End Function

        Public Shared Function GetSortOrderSql(apiResult As ApiResult, sortList As List(Of Sorts), loggedInUserGuid As String) As String
            Dim sortOrder As String = String.Empty

            If Sorts.SortExists(sortList) Then

                For i = 0 To 6 'Loop should match number of items in Select Case.
                    For Each item As Sorts In sortList
                        If item.Sort.Order = i Then
                            If i > 0 Then
                                sortOrder = sortOrder & ", "
                            End If
                            Dim columnName As String = String.Empty
                            Select Case item.FieldName
                                Case ProductAssignmentRequest.FieldIsActive
                                    columnName = "ProductAssignment.ActiveYN"
                                Case ProductAssignmentRequest.FieldCanDelete
                                    columnName = "CanDelete"
                                Case ProductAssignmentRequest.FieldCropName
                                    columnName = GetCropListSelect()
                                Case ProductAssignmentRequest.FieldCropPurposeName
                                    columnName = GetCropPurposeListSelect()
                                Case ProductAssignmentRequest.FieldGrowthStageOrderName
                                    columnName = GetGrowthStageOrderListSelect()
                                Case ProductAssignmentRequest.FieldNutrientName
                                    columnName = "nu.Name"
                                Case ProductAssignmentRequest.FieldLocationLevelName
                                    columnName = GetOrgLevelListSelect(loggedInUserGuid)
                                Case ProductAssignmentRequest.FieldProductName
                                    columnName = GetProductListSelect()
                                Case ProductAssignmentRequest.FieldSampleTypeName
                                    columnName = "st.Name"
                            End Select
                            sortOrder = sortOrder & columnName & " " & item.Sort.Direction
                        End If
                    Next item
                Next i
            End If

            If String.IsNullOrEmpty(sortOrder) Then
                'Use this as the default sort if not sort set by UI
                sortOrder = "st.Name, " + GetOrgLevelListSelect(loggedInUserGuid) + ", nu.Name, " + GetProductListSelect()
            End If

            Return sortOrder

        End Function

#End Region

#Region "Add "

        Public Shared Sub AddProductAssignment(ByVal loggedInUserGuid As String, ByVal productAssignment As ProductAssignment, apiResult As ApiResult)
            If Valid(productAssignment, loggedInUserGuid, apiResult, New List(Of String)) Then
                Dim list As New List(Of String)
                productAssignment.ProductAssignmentGuid = GetNewGuid()
                list.Add(GetInsertStatement(productAssignment, loggedInUserGuid))
                list.AddRange(GetProductAssignmentCropSql(productAssignment))
                list.AddRange(GetProductAssignmentOrgLevelSql(productAssignment))
                list.AddRange(GetProductAssignmentProductSql(productAssignment))
                Execute(list)
                apiResult.Model = productAssignment
                apiResult.Success = True
            End If

        End Sub

        Public Shared Function GetInsertStatement(productAssignment As ProductAssignment, loggedInUserGuid As String) As String
            Dim sb As New SqlBuilder

            sb.Add("INSERT INTO {0}ProductAssignment (", Schema)
            sb.Add("ProductAssignmentGuid")
            sb.Add(",CompanyGuid")
            sb.Add(",SampleTypeGuid")
            sb.Add(",NutrientGuid")
            sb.Add(",ActiveYN")
            sb.Add(",RecordOwnerGuid")
            sb.Add(",CreatedByGuid")
            sb.Add(",CreatedDate")
            sb.Add(") VALUES (")
            sb.Add("'{0}'", productAssignment.ProductAssignmentGuid)
            sb.Add(",'{0}'", UserVm.GetCompanyGuid(loggedInUserGuid))
            sb.Add(",'{0}'", productAssignment.SampleTypeGuid)
            sb.Add(",'{0}'", productAssignment.NutrientGuid)
            sb.Add(",'{0}'", FormatBoolean(productAssignment.ActiveYn))
            sb.Add(",'{0}'", loggedInUserGuid)
            sb.Add(",'{0}'", loggedInUserGuid)
            sb.Add("," & FormatCurrentDateTime())
            sb.Add(")")

            Return sb.Sql
        End Function

        Public Shared Function GetProductAssignmentCropSql(productAssignment As ProductAssignment) As List(Of String)
            Dim list As New List(Of String)
            Dim sb As New SqlBuilder
            Dim guidList As New List(Of String)

            For Each item As ProductAssignmentCrop In productAssignment.CropList
                If String.IsNullOrEmpty(item.ProductAssignmentCropGuid) = False Then
                    guidList.Add(item.ProductAssignmentCropGuid)
                End If
            Next

            sb.Add("DELETE FROM {0}ProductAssignmentCrop", Schema)
            sb.Add("WHERE ProductAssignmentGuid = '{0}'", productAssignment.ProductAssignmentGuid)
            If guidList.Count > 0 Then
                sb.Add("AND ProductAssignmentCropGuid NOT IN ({0})", PadTic.No, GetListSql(guidList))
            End If

            list.Add(sb.Sql)

            For Each item As ProductAssignmentCrop In productAssignment.CropList
                If String.IsNullOrEmpty(item.ProductAssignmentCropGuid) Then
                    sb.Clear()
                    sb.Add("INSERT INTO {0}ProductAssignmentCrop (ProductAssignmentCropGuid, ProductAssignmentGuid, CropGuid, CropPurposeGuid, GrowthStageOrderGuid) VALUES (", Schema)
                    sb.Add("'{0}'", GetNewGuid())
                    sb.Add(",'{0}'", productAssignment.ProductAssignmentGuid)
                    sb.Add(",'{0}'", If(String.IsNullOrEmpty(item.CropClassNameGuid), item.CropGuid, item.CropClassNameGuid))
                    sb.Add("," & FormatKeyOrNull(item.CropPurposeGuid))
                    sb.Add("," & FormatKeyOrNull(item.GrowthStageOrderGuid))
                    sb.Add(")")
                    list.Add(sb.Sql)
                Else
                    sb.Clear()
                    sb.Add("UPDATE {0}ProductAssignmentCrop", Schema)
                    sb.Add("SET CropGuid = '{0}'", If(String.IsNullOrEmpty(item.CropClassNameGuid), item.CropGuid, item.CropClassNameGuid))
                    sb.Add(",CropPurposeGuid = " & FormatKeyOrNull(item.CropPurposeGuid))
                    sb.Add(",GrowthStageOrderGuid = " & FormatKeyOrNull(item.GrowthStageOrderGuid))
                    sb.Add("WHERE ProductAssignmentCropGuid = '{0}'", item.ProductAssignmentCropGuid)
                    list.Add(sb.Sql)
                End If
            Next

            Return list
        End Function

        Public Shared Function GetProductAssignmentOrgLevelSql(productAssignment As ProductAssignment) As List(Of String)
            Dim list As New List(Of String)
            Dim sb As New SqlBuilder
            Dim guidList As New List(Of String)

            For Each item As ProductAssignmentOrgLevel In productAssignment.OrgLevelList
                If String.IsNullOrEmpty(item.ProductAssignmentOrgLevelGuid) = False Then
                    guidList.Add(item.ProductAssignmentOrgLevelGuid)
                End If
            Next

            sb.Add("DELETE FROM {0}ProductAssignmentOrgLevel", Schema)
            sb.Add("WHERE ProductAssignmentGuid = '{0}'", productAssignment.ProductAssignmentGuid)
            If guidList.Count > 0 Then
                sb.Add("AND ProductAssignmentOrgLevelGuid NOT IN ({0})", PadTic.No, GetListSql(guidList))
            End If

            list.Add(sb.Sql)

            For Each item As ProductAssignmentOrgLevel In productAssignment.OrgLevelList
                If String.IsNullOrEmpty(item.ProductAssignmentOrgLevelGuid) Then
                    sb.Clear()
                    sb.Add("INSERT INTO {0}ProductAssignmentOrgLevel (ProductAssignmentOrgLevelGuid, ProductAssignmentGuid, OrgLevelGuid) VALUES (", Schema)
                    sb.Add("'{0}'", GetNewGuid())
                    sb.Add(",'{0}'", productAssignment.ProductAssignmentGuid)
                    sb.Add(",'{0}'", item.OrgLevelGuid)
                    sb.Add(")")
                    list.Add(sb.Sql)
                Else
                    sb.Clear()
                    sb.Add("UPDATE {0}ProductAssignmentOrgLevel", Schema)
                    sb.Add("SET OrgLevelGuid = '{0}'", item.OrgLevelGuid)
                    sb.Add("WHERE ProductAssignmentOrgLevelGuid = '{0}'", item.ProductAssignmentOrgLevelGuid)
                    list.Add(sb.Sql)
                End If
            Next

            Return list
        End Function

        Public Shared Function GetProductAssignmentProductSql(productAssignment As ProductAssignment) As List(Of String)
            Dim list As New List(Of String)
            Dim sb As New SqlBuilder
            Dim guidList As New List(Of String)

            For Each item As ProductAssignmentProduct In productAssignment.ProductList
                If String.IsNullOrEmpty(item.ProductAssignmentProductGuid) = False Then
                    guidList.Add(item.ProductAssignmentProductGuid)
                End If
            Next

            sb.Add("DELETE FROM {0}ProductAssignmentProduct", Schema)
            sb.Add("WHERE ProductAssignmentGuid = '{0}'", productAssignment.ProductAssignmentGuid)
            If guidList.Count > 0 Then
                sb.Add("AND ProductAssignmentProductGuid NOT IN ({0})", PadTic.No, GetListSql(guidList))
            End If

            list.Add(sb.Sql)

            For Each item As ProductAssignmentProduct In productAssignment.ProductList
                If String.IsNullOrEmpty(item.ProductAssignmentProductGuid) Then
                    sb.Clear()
                    sb.Add("INSERT INTO {0}ProductAssignmentProduct (ProductAssignmentProductGuid, ProductAssignmentGuid, ProductGuid) VALUES (", Schema)
                    sb.Add("'{0}'", GetNewGuid())
                    sb.Add(",'{0}'", productAssignment.ProductAssignmentGuid)
                    sb.Add(",'{0}'", item.ProductGuid)
                    sb.Add(")")
                    list.Add(sb.Sql)
                Else
                    sb.Clear()
                    sb.Add("UPDATE {0}ProductAssignmentProduct", Schema)
                    sb.Add("SET ProductGuid = '{0}'", item.ProductGuid)
                    sb.Add("WHERE ProductAssignmentProductGuid = '{0}'", item.ProductAssignmentProductGuid)
                    list.Add(sb.Sql)
                End If
            Next

            Return list
        End Function

#End Region

#Region " Get Product Assignment "

        Public Shared Function GetCropList(productAssignmentGuid As String) As List(Of ProductAssignmentCrop)
            Dim sb As New SqlBuilder
            Dim result As New List(Of ProductAssignmentCrop)

            sb.Add("SELECT ProductAssignmentCropGuid")
            sb.Add(",ProductAssignmentCrop.CropGuid")
            sb.Add(",Crop.Name AS CropName")
            sb.Add(",Crop.CropId")
            sb.Add(",ProductAssignmentCrop.CropPurposeGuid")
            sb.Add(",CropPurpose.Name AS CropPurposeName")
            sb.Add(",ProductAssignmentCrop.GrowthStageOrderGuid")
            sb.Add(",GrowthStageOrder.Name AS GrowthStageOrderName")
            sb.Add("FROM {0}ProductAssignmentCrop ProductAssignmentCrop", Schema)
            sb.Add("INNER JOIN {0}Crop Crop ON Crop.CropGuid = ProductAssignmentCrop.CropGuid", Schema)
            sb.Add("LEFT JOIN {0}CropPurpose CropPurpose ON CropPurpose.CropPurposeGuid = ProductAssignmentCrop.CropPurposeGuid", Schema)
            sb.Add("LEFT JOIN {0}GrowthStageOrder GrowthStageOrder ON GrowthStageOrder.GrowthStageOrderGuid = ProductAssignmentCrop.GrowthStageOrderGuid", Schema)
            sb.Add("WHERE ProductAssignmentGuid = '{0}'", productAssignmentGuid)
            sb.Add("ORDER BY CropName")

            For Each row As DataRow In GetDataTable(sb.Sql).Rows
                Dim item As New ProductAssignmentCrop
                item.ProductAssignmentGuid = productAssignmentGuid
                item.ProductAssignmentCropGuid = row("ProductAssignmentCropGuid").ToString
                item.CropName = row("CropName").ToString
                item.CropId = row("CropID").ToString
                item.CropPurposeGuid = row("CropPurposeGuid").ToString
                item.CropPurposeName = row("CropPurposeName").ToString
                item.GrowthStageOrderGuid = row("GrowthStageOrderGuid").ToString
                item.GrowthStageOrderName = row("GrowthStageOrderName").ToString

                Dim savedCropGuid As String = row("CropGuid").ToString
                Dim cropPrimaryGuid As String = AgBytes.CropVm.GetCropPrimaryGuid(savedCropGuid)

                If savedCropGuid = cropPrimaryGuid Then
                    item.CropGuid = savedCropGuid
                Else
                    item.CropGuid = cropPrimaryGuid
                    item.CropClassNameGuid = savedCropGuid
                End If

                result.Add(item)
            Next

            Return result
        End Function

        Public Shared Sub GetProductAssignment(productAssignmentGuid As String, loggedInUserGuid As String, apiResult As ApiResult)
            apiResult.Model = GetProductAssignment(productAssignmentGuid)
            apiResult.Success = True
        End Sub

        Public Shared Function GetProductAssignment(productAssignmentGuid As String) As ProductAssignment
            Dim result As New ProductAssignment
            Dim sb As New SqlBuilder

            sb.Add("SELECT productAssignment.ProductAssignmentGuid")
            sb.Add(",sampleType.SampleTypeGuid")
            sb.Add(",sampleType.Name AS SampleTypeName")
            sb.Add(",nutrient.NutrientGuid")
            sb.Add(",nutrient.Name AS NutrientName")
            sb.Add(",productAssignment.ActiveYN")
            sb.Add("FROM {0}ProductAssignment productAssignment", Schema)
            sb.Add("INNER JOIN {0}SampleType sampleType ON sampleType.SampleTypeGuid = productAssignment.SampleTypeGuid", Schema)
            sb.Add("INNER JOIN {0}Nutrient nutrient ON nutrient.NutrientGuid = productAssignment.NutrientGuid", Schema)
            sb.Add("WHERE productAssignment.ProductAssignmentGuid = '{0}'", productAssignmentGuid)

            Dim row As DataRow = GetDataTable(sb.Sql).Rows(0)

            result.ProductAssignmentGuid = row("ProductAssignmentGuid").ToString
            result.SampleTypeGuid = row("SampleTypeGuid").ToString
            result.SampleTypeName = row("SampleTypeName").ToString
            result.NutrientGuid = row("NutrientGuid").ToString
            result.NutrientName = row("NutrientName").ToString
            result.ActiveYn = Boolean.Parse(row("ActiveYN").ToString)
            result.CropList = GetCropList(result.ProductAssignmentGuid)
            result.OrgLevelList = GetOrgLevelList(result.ProductAssignmentGuid)
            result.ProductList = GetProductList(result.ProductAssignmentGuid)
            Return result
        End Function

        Public Shared Sub GetProductAssignmentSelectAllList(request As ProductAssignmentRequest, loggedInUserGuid As String, apiResult As ApiResult)
            request.ProductAssignmentPageOptions.Skip = 0
            request.ProductAssignmentPageOptions.PageSize = PageOptions.MaxPageSize

            Dim sortOrder As String = GetSortOrderSql(apiResult, request.ProductAssignmentSort, loggedInUserGuid)
            Dim sql As String = GetProductAssignmentListSql(request, sortOrder, loggedInUserGuid)
            Dim model As New List(Of String)

            Dim dt = GetDataTable(sql)
            For Each row As DataRow In dt.Rows
                model.Add(row("ProductAssignmentGuid").ToString)
            Next

            apiResult.Model = model
            apiResult.Success = True

        End Sub

        Private Shared Function GetOrgLevelList(productAssignmentGuid As String) As List(Of ProductAssignmentOrgLevel)
            Dim sb As New SqlBuilder
            Dim result As New List(Of ProductAssignmentOrgLevel)

            sb.Add("SELECT ProductAssignmentOrgLevelGuid, ProductAssignmentOrgLevel.OrgLevelGuid, OrgLevel.Name, City, Abbreviation, OrgLevel.ParentOrgLevelGuid")
            sb.Add("FROM {0}ProductAssignmentOrgLevel ProductAssignmentOrgLevel", Schema)
            sb.Add("INNER JOIN {0}OrgLevel OrgLevel ON OrgLevel.OrgLevelGuid = ProductAssignmentOrgLevel.OrgLevelGuid", Schema)
            sb.Add("INNER JOIN {0}OrgLevelAddress OrgLevelAddress ON OrgLevelAddress.OrgLevelGuid = OrgLevel.OrgLevelGuid", Schema)
            sb.Add("INNER JOIN {0}Address_EVW Address ON Address.AddressGuid = OrgLevelAddress.AddressGuid", SpatialSchema)
            sb.Add("INNER JOIN {0}State State ON State.StateGuid = Address.StateGuid", Schema)
            sb.Add("WHERE ProductAssignmentGuid = '{0}'", productAssignmentGuid)
            sb.Add("AND OrgLevelAddress.IsPrimary = 1")
            sb.Add("ORDER BY Name")

            For Each row As DataRow In GetDataTable(sb.Sql).Rows
                Dim item As New ProductAssignmentOrgLevel
                item.ProductAssignmentGuid = productAssignmentGuid
                item.ProductAssignmentOrgLevelGuid = row("ProductAssignmentOrgLevelGuid").ToString
                item.OrgLevelGuid = row("OrgLevelGuid").ToString
                item.OrgLevelName = row("Name").ToString
                item.City = row("City").ToString
                item.StateAbbreviation = row("Abbreviation").ToString
                item.ParentOrgLevelGuid = row("ParentOrgLevelGuid").ToString()
                result.Add(item)
            Next

            Return result
        End Function

        Public Shared Function GetProductList(productAssignmentGuid As String) As List(Of ProductAssignmentProduct)
            Dim sb As New SqlBuilder
            Dim result As New List(Of ProductAssignmentProduct)

            sb.Add("SELECT ProductAssignmentProductGuid")
            sb.Add(",ProductAssignmentProduct.ProductGuid")
            sb.Add(",Product.Name AS ProductName")
            sb.Add("FROM {0}ProductAssignmentProduct ProductAssignmentProduct", Schema)
            sb.Add("INNER JOIN {0}Product Product ON Product.ProductGuid = ProductAssignmentProduct.ProductGuid", Schema)
            sb.Add("WHERE ProductAssignmentGuid = '{0}'", productAssignmentGuid)
            sb.Add("ORDER BY ProductName")

            For Each row As DataRow In GetDataTable(sb.Sql).Rows
                Dim item As New ProductAssignmentProduct
                item.ProductAssignmentGuid = productAssignmentGuid
                item.ProductAssignmentProductGuid = row("ProductAssignmentProductGuid").ToString
                item.ProductName = row("ProductName").ToString
                item.ProductGuid = row("ProductGuid").ToString
                result.Add(item)
            Next

            Return result
        End Function


#End Region

#Region " Update Product Assignment "

        Public Shared Function GetUpdateStatement(productAssignment As ProductAssignment, loggedInUserGuid As String) As String
            Dim sb As New SqlBuilder

            sb.Add("UPDATE {0}ProductAssignment ", Schema)
            sb.Add("SET SampleTypeGuid = '{0}'", productAssignment.SampleTypeGuid)
            sb.Add(",NutrientGuid = '{0}'", productAssignment.NutrientGuid)
            sb.Add(",ActiveYN = '{0}'", FormatBoolean(productAssignment.ActiveYn))
            sb.Add(",ModifiedDate = {0}", FormatCurrentDateTime())
            sb.Add(",ModifiedByGuid = '{0}'", loggedInUserGuid)
            sb.Add("WHERE [ProductAssignmentGuid] = '{0}'", productAssignment.ProductAssignmentGuid)

            Return sb.Sql
        End Function

        Public Shared Sub UpdateProductAssignment(productAssignment As ProductAssignment, loggedInUserGuid As String, apiResult As ApiResult)
            If Valid(productAssignment, loggedInUserGuid, apiResult, New List(Of String)) Then
                Dim list As New List(Of String)
                list.Add(GetUpdateStatement(productAssignment, loggedInUserGuid))
                list.AddRange(GetProductAssignmentCropSql(productAssignment))
                list.AddRange(GetProductAssignmentOrgLevelSql(productAssignment))
                list.AddRange(GetProductAssignmentProductSql(productAssignment))
                Execute(list)
                apiResult.Success = True
            End If
        End Sub



#End Region

#Region " Autocomplete List "
        ''' <summary>
        ''' Gets a distinct list of values for Autocomplete search
        ''' </summary>
        ''' <returns>AutoCompleteResponse</returns>
        ''' <remarks></remarks>    
        Public Shared Sub GetAutoSearchList(request As ProductAssignmentAutoCompleteRequest, loggedInUserGuid As String, apiResult As ApiResult)

            If ValidGetAutoSearchList(request, apiResult) = False Then
                Return
            End If

            request.SearchString = RemoveExtraSpaces(request.SearchString)

            Dim selectColumn As String = String.Empty

            Select Case request.SearchName
                Case ProductAssignmentRequest.FieldCropName
                    selectColumn = GetCropListSelect()
                Case ProductAssignmentRequest.FieldCropPurposeName
                    selectColumn = GetCropPurposeListSelect()
                Case ProductAssignmentRequest.FieldGrowthStageOrderName
                    selectColumn = GetGrowthStageOrderListSelect()
                Case ProductAssignmentRequest.FieldIsActive
                    selectColumn = "ProductAssignment.ActiveYN"
                Case ProductAssignmentRequest.FieldLocationLevelName
                    selectColumn = GetOrgLevelListSelect(loggedInUserGuid)
                Case ProductAssignmentRequest.FieldNutrientName
                    selectColumn = "nu.Name"
                Case ProductAssignmentRequest.FieldProductName
                    selectColumn = GetProductListSelect()
                Case ProductAssignmentRequest.FieldSampleTypeName
                    selectColumn = "st.Name"
            End Select

            Dim autoSearchSql As String = GetAutoSearchSql(selectColumn, request, loggedInUserGuid)
            Dim dt As DataTable = GetDataTable(autoSearchSql)

            Dim list As New List(Of AutoCompleteRow)
            For Each row As DataRow In dt.Rows
                Dim item As New AutoCompleteRow
                item.Value = row.Item("returnValue").ToString()
                list.Add(item)
            Next

            apiResult.Model = list
            apiResult.Success = True

        End Sub

        ''' <summary>
        ''' Return the sql for the AutoCompleteList search and AutoCompleteName search
        ''' </summary>
        Public Shared Function GetAutoSearchSql(selectColumn As String, request As ProductAssignmentAutoCompleteRequest, loggedInUserGuid As String) As String
            Dim sb As New SqlBuilder

            sb.Add("SELECT DISTINCT ")
            sb.Add("{0} AS returnValue ", PadTic.No, selectColumn)
            sb.Add("FROM  {0}ProductAssignment ProductAssignment", Schema)
            sb.Add("INNER JOIN {0}SampleType st ON ProductAssignment.sampleTypeGuid = st.sampleTypeGuid ", Schema)
            sb.Add("INNER JOIN {0}Nutrient nu ON ProductAssignment.nutrientGuid = nu.nutrientGuid ", Schema)
            sb.Add("INNER JOIN {0}ProductAssignmentOrgLevel pao ON pao.ProductAssignmentGuid = ProductAssignment.ProductAssignmentGuid ", Schema)
            sb.Add("WHERE ({0}", PadTic.No, selectColumn)
            sb.Add(" LIKE '{0}%'", request.SearchString)
            sb.Add(" OR {0}", PadTic.No, selectColumn)
            sb.Add(" LIKE '% {0}%')", request.SearchString)
            sb.Add(" AND EXISTS (SELECT * FROM {0}GetUserHierarchyAccess('{1}') WHERE OrgLevelGuid = pao.OrgLevelGuid)", Schema, loggedInUserGuid)
            sb.Add(GetFilterSql(request.ProductAssignmentFilter, request.ActiveOnly, loggedInUserGuid))

            Return sb.Sql

        End Function

        Public Shared Function ValidGetAutoSearchList(request As ProductAssignmentAutoCompleteRequest, apiResult As ApiResult) As Boolean
            Dim count As Integer = apiResult.ErrorCodeList.Count

            Select Case request.SearchName
                Case ProductAssignmentRequest.FieldCropName,
                    ProductAssignmentRequest.FieldCropPurposeName,
                    ProductAssignmentRequest.FieldGrowthStageOrderName,
                    ProductAssignmentRequest.FieldLocationLevelName,
                    ProductAssignmentRequest.FieldNutrientName,
                    ProductAssignmentRequest.FieldProductName,
                    ProductAssignmentRequest.FieldSampleTypeName
                    'success
                Case Else
                    apiResult.ErrorCodeList.Add(ApiResultVm.ErrorCode.SearchNameInvalid)
            End Select

            Return count = apiResult.ErrorCodeList.Count
        End Function

#End Region

#Region " Delete Product Assignment "
        Public Shared Sub DeleteProductAssignment(productAssignmentGuid As String, loggedInUserGuid As String, apiResult As ApiResult)
            If ValidDelete(productAssignmentGuid, loggedInUserGuid, apiResult) Then
                Dim list As New List(Of String)
                list.Add(GetDeleteProductAssignmentCropSql(productAssignmentGuid, loggedInUserGuid))
                list.Add(GetDeleteProductAssignmentOrgLevelSql(productAssignmentGuid, loggedInUserGuid))
                list.Add(GetDeleteProductAssignmentProductSql(productAssignmentGuid, loggedInUserGuid))
                list.Add(GetDeleteProductAssignmentSql(productAssignmentGuid, loggedInUserGuid))
                Execute(list)
                apiResult.Success = True
            End If
        End Sub

        Public Shared Sub DeleteProductAssignmentList(rows As List(Of String), loggedInUserGuid As String, apiResult As ApiResult)
            Dim list As New List(Of String)
            If ValidDeleteProductAssignmentList(rows, loggedInUserGuid, apiResult) Then
                For Each item In rows
                    If RecordInUse(TableEnum.ProductAssignment, item) = False Then
                        list.Add(GetDeleteProductAssignmentCropSql(item, loggedInUserGuid))
                        list.Add(GetDeleteProductAssignmentOrgLevelSql(item, loggedInUserGuid))
                        list.Add(GetDeleteProductAssignmentProductSql(item, loggedInUserGuid))
                        list.Add(GetDeleteProductAssignmentSql(item, loggedInUserGuid))
                    End If
                Next
                If list.Count > 0 Then
                    Execute(list)
                End If
                apiResult.Success = True
            End If
        End Sub

        Private Shared Function GetDeleteProductAssignmentCropSql(productAssignmentGuid As String, loggedInUserGuid As String) As String
            Dim sb As New SqlBuilder

            sb.Add("DELETE FROM {0}ProductAssignmentCrop", Schema)
            sb.Add("WHERE ProductAssignmentGuid = '{0}'", productAssignmentGuid)

            Return sb.Sql
        End Function

        Private Shared Function GetDeleteProductAssignmentOrgLevelSql(productAssignmentGuid As String, loggedInUserGuid As String) As String
            Dim sb As New SqlBuilder

            sb.Add("DELETE FROM {0}ProductAssignmentOrgLevel", Schema)
            sb.Add("WHERE ProductAssignmentGuid = '{0}'", productAssignmentGuid)

            Return sb.Sql
        End Function

        Private Shared Function GetDeleteProductAssignmentProductSql(productAssignmentGuid As String, loggedInUserGuid As String) As String
            Dim sb As New SqlBuilder

            sb.Add("DELETE FROM {0}ProductAssignmentProduct", Schema)
            sb.Add("WHERE ProductAssignmentGuid = '{0}'", productAssignmentGuid)

            Return sb.Sql
        End Function

        Private Shared Function GetDeleteProductAssignmentSql(productAssignmentGuid As String, loggedInUserGuid As String) As String
            Dim sb As New SqlBuilder

            sb.Add("DELETE FROM {0}ProductAssignment", Schema)
            sb.Add("WHERE ProductAssignmentGuid = '{0}'", productAssignmentGuid)

            Return sb.Sql
        End Function

        Public Shared Function ValidDelete(productAssignmentGuid As String, loggedInUserGuid As String, apiResult As ApiResult) As Boolean
            Dim count As Integer = apiResult.ErrorCodeList.Count

            If UserRoleVm.AllowedUser(SoftwareFunctionVm.FunctionName.ProductAssignment, loggedInUserGuid) = False Then
                apiResult.ErrorCodeList.Add(ApiResultVm.ErrorCode.ApiAccessDenied)
                Return False
            End If

            If RecordInUse(TableEnum.ProductAssignment, productAssignmentGuid) Then
                apiResult.ErrorCodeList.Add(ApiResultVm.ErrorCode.RecordInUse)
            End If

            Return count = apiResult.ErrorCodeList.Count
        End Function

        Private Shared Function ValidDeleteProductAssignmentList(list As List(Of String), loggedInUserGuid As String, apiResult As ApiResult) As Boolean
            Dim count As Integer = apiResult.ErrorCodeList.Count

            If UserRoleVm.AllowedUser(SoftwareFunctionVm.FunctionName.ProductAssignment, loggedInUserGuid) = False Then
                apiResult.ErrorCodeList.Add(ApiResultVm.ErrorCode.ApiAccessDenied)
                Return False
            End If

            Return count = apiResult.ErrorCodeList.Count
        End Function


#End Region

#Region "Import"
        Public Shared Function ValidImportProductAssignmentList(list As List(Of ProductAssignmentGridRow), loggedInUserGuid As String, apiResult As ApiResult) As Boolean
            Dim count As Integer = apiResult.ErrorCodeList.Count

            If UserRoleVm.AllowedUser(SoftwareFunctionVm.FunctionName.ProductAssignment, loggedInUserGuid) = False Then
                apiResult.ErrorCodeList.Add(ApiResultVm.ErrorCode.ApiAccessDenied)
                Return False
            End If

            If list Is Nothing OrElse list.Count < 1 Then
                apiResult.ErrorCodeList.Add(ApiResultVm.ErrorCode.AgBytesUpdateRowRequired)
                Return False
            End If

            Dim errorList As New List(Of String)

            For Each row In list
                ValidProductAssignment(row, loggedInUserGuid, apiResult, errorList)
            Next

            Return GetImportApiResult(count, apiResult, errorList)
        End Function

        Public Shared Sub ValidProductAssignmentImport(base64Text As String, loggedInUserGuid As String, apiResult As ApiResult)
            Dim productAssignmentList As New List(Of ProductAssignmentGridRow)
            If ParseFile(base64Text, productAssignmentList, apiResult) Then
                If ValidImportProductAssignmentList(productAssignmentList, loggedInUserGuid, apiResult) Then
                    Dim result As New ImportStatistics

                    For Each item As ProductAssignmentGridRow In productAssignmentList
                        If String.IsNullOrEmpty(item.ProductAssignmentGuid) Then
                            result.AddCount += 1
                        Else
                            result.UpdateCount += 1
                        End If
                    Next

                    apiResult.Model = result
                    apiResult.Success = True

                End If
            End If
        End Sub

        Public Shared Sub ProductAssignmentImport(base64Text As String, loggedInUserGuid As String, apiResult As ApiResult)
            Dim productAssignmentList As New List(Of ProductAssignmentGridRow)
            If ParseFile(base64Text, productAssignmentList, apiResult) Then
                Dim addCount As Integer
                Dim updateCount As Integer
                Dim list As New List(Of String)

                For Each item As ProductAssignmentGridRow In productAssignmentList
                    If String.IsNullOrEmpty(item.ProductAssignmentGuid) Then
                        addCount += 1
                        item.ProductAssignmentGuid = GetNewGuid()
                        list.Add(GetProductAssignmentInsertSql(item, loggedInUserGuid))
                    Else
                        updateCount += 1
                        list.Add(GetProductAssignmentUpdateSql(item, loggedInUserGuid))
                    End If
                    list.AddRange(GetProductAssignmentCropSql(item))
                    list.AddRange(GetProductAssignmentProductSql(item))
                    list.AddRange(GetProductAssignmentOrgLevelSql(item, loggedInUserGuid))
                Next

                list.Add(AgBytesLogVm.GetImportLogSql(AgBytesLogVm.Feature.ProductAssignment, addCount, updateCount, loggedInUserGuid))
                Execute(list)

                apiResult.Success = True

            End If

        End Sub

        Public Shared Function GetProductAssignmentProductSql(item As ProductAssignmentGridRow) As List(Of String)
            Dim productList() As String = item.ProductName.Split(";"c)

            Dim productAssignment = New ProductAssignment
            productAssignment.ProductAssignmentGuid = item.ProductAssignmentGuid
            productAssignment.ProductList = New List(Of ProductAssignmentProduct)
            For i = 0 To productList.Count - 1
                productAssignment.ProductList.Add(New ProductAssignmentProduct() With {
                    .ProductGuid = ProductVm.GetProductGuidFromName(productList(i).Trim()),
                    .ProductName = productList(i).Trim()
                })
            Next
            Return GetProductAssignmentProductSql(productAssignment)
        End Function

        Public Shared Function GetProductAssignmentOrgLevelSql(item As ProductAssignmentGridRow, loggedInUserGuid As String) As List(Of String)
            Dim orgLevelList() As String = item.OrgLevelId.Split(";"c)

            Dim productAssignment = New ProductAssignment
            productAssignment.ProductAssignmentGuid = item.ProductAssignmentGuid
            productAssignment.OrgLevelList = New List(Of ProductAssignmentOrgLevel)
            For i = 0 To orgLevelList.Count - 1
                productAssignment.OrgLevelList.Add(New ProductAssignmentOrgLevel() With {
                    .OrgLevelGuid = OrgLevelVm.GetOrgLevelGuid(orgLevelList(i), UserVm.GetCompanyGuid(loggedInUserGuid))
                })
            Next
            Return GetProductAssignmentOrgLevelSql(productAssignment)
        End Function

        Public Shared Function GetProductAssignmentCropSql(item As ProductAssignmentGridRow) As List(Of String)
            Dim cropIdlist() As String = item.CropId.Split(";"c)
            Dim cropClassIdlist() As String = item.CropClassId.Split(";"c)
            Dim cropPurposelist() As String = item.CropPurposeName.Split(";"c)
            Dim growthStagelist() As String = item.GrowthStageOrderName.Split(";"c)

            Dim productAssignment = New ProductAssignment
            productAssignment.ProductAssignmentGuid = item.ProductAssignmentGuid
            productAssignment.CropList = New List(Of ProductAssignmentCrop)
            For i = 0 To cropIdlist.Count - 1
                Dim s As String = cropIdlist(i).Trim
                If StringHasValue(s) Then
                    Dim cropPurpose As String = String.Empty
                    If i < cropPurposelist.Length Then
                        cropPurpose = cropPurposelist(i).Trim
                        If String.IsNullOrEmpty(cropPurpose) OrElse cropPurpose.ToLower = NoneIndicator.ToLower Then
                            cropPurpose = String.Empty
                        Else
                            cropPurpose = NutrientRemovalRateVm.GetCropPurposeGuid(cropPurpose)
                        End If
                    End If

                    Dim cropClassId As String = String.Empty
                    If i < cropClassIdlist.Length Then
                        cropClassId = cropClassIdlist(i).Trim
                        If String.IsNullOrEmpty(cropClassId) OrElse cropClassId.ToLower = NoneIndicator.ToLower Then
                            cropClassId = String.Empty
                        End If
                    End If

                    Dim growthStageOrder As String = String.Empty
                    If i < growthStagelist.Length Then
                        growthStageOrder = growthStagelist(i).Trim
                        If String.IsNullOrEmpty(growthStageOrder) OrElse growthStageOrder.ToLower = NoneIndicator.ToLower Then
                            growthStageOrder = String.Empty
                        Else
                            growthStageOrder = GrowthStageVm.GetGrowthStageOrderGuid(s, cropClassId, GrowthStageVm.GrowthStageGroupType.Tissue, growthStageOrder)
                        End If
                    End If
                    productAssignment.CropList.Add(New ProductAssignmentCrop() With {
                        .CropGuid = CropVm.GetCropGuid(If(StringHasValue(cropClassId), cropClassId, s)),
                        .GrowthStageOrderGuid = growthStageOrder,
                        .CropPurposeGuid = cropPurpose
                    })
                End If

            Next
            Return GetProductAssignmentCropSql(productAssignment)
        End Function

        Public Shared Function GetProductAssignmentInsertSql(productAssignment As ProductAssignmentGridRow, loggedInUserGuid As String) As String
            Dim sb As New SqlBuilder
            sb.Add("INSERT INTO {0}ProductAssignment (", Schema)
            sb.Add("ProductAssignmentGuid")
            sb.Add(",CompanyGuid")
            sb.Add(",SampleTypeGuid")
            sb.Add(",NutrientGuid")
            sb.Add(",ActiveYN")
            sb.Add(",RecordOwnerGuid")
            sb.Add(",CreatedByGuid")
            sb.Add(",CreatedDate")
            sb.Add(") VALUES (")
            sb.Add("'{0}'", productAssignment.ProductAssignmentGuid)
            sb.Add(",'{0}'", UserVm.GetUserCompany(loggedInUserGuid))
            sb.Add("," & FormatKeyOrNull(If(String.IsNullOrEmpty(productAssignment.SampleTypeName), String.Empty, SampleAttributeVm.GetSampleTypeGuid(productAssignment.SampleTypeName))))
            sb.Add("," & FormatKeyOrNull(If(String.IsNullOrEmpty(productAssignment.NutrientName), String.Empty, NutrientVm.GetNutrientGuidFromName(productAssignment.NutrientName))))
            sb.Add(",'{0}'", FormatBoolean(productAssignment.ActiveYn))
            sb.Add(",'{0}'", loggedInUserGuid)
            sb.Add(",'{0}'", loggedInUserGuid)
            sb.Add(",{0}", FormatCurrentDateTime())
            sb.Add(")")

            Return sb.Sql

        End Function

        Public Shared Function GetProductAssignmentUpdateSql(productAssignment As ProductAssignmentGridRow, loggedInUserGuid As String) As String
            Dim sb As New SqlBuilder
            sb.Add("UPDATE {0}ProductAssignment", Schema)
            sb.Add("SET SampleTypeGuid = " & FormatKeyOrNull(If(String.IsNullOrEmpty(productAssignment.SampleTypeName), String.Empty, SampleAttributeVm.GetSampleTypeGuid(productAssignment.SampleTypeName))))
            sb.Add(", NutrientGuid = " & FormatKeyOrNull(If(String.IsNullOrEmpty(productAssignment.NutrientName), String.Empty, NutrientVm.GetNutrientGuidFromName(productAssignment.NutrientName))))
            sb.Add(",ActiveYn = '{0}'", FormatBoolean(productAssignment.ActiveYn))
            sb.Add(",ModifiedDate = {0}", FormatCurrentDateTime)
            sb.Add(",ModifiedByGuid = '{0}'", loggedInUserGuid)
            sb.Add("WHERE ProductAssignmentGuid = '{0}'", productAssignment.ProductAssignmentGuid)

            Return sb.Sql

        End Function


        Public Shared Function ParseFile(base64Text As String, list As List(Of ProductAssignmentGridRow), apiResult As ApiResult) As Boolean
            Try
                Return TryParseFile(base64Text, list, apiResult)
            Catch ex As Exception
                apiResult.ErrorCodeList.Add(ApiResultVm.ErrorCode.AgBytesFileFormatInvalid)
                Return False
            End Try
        End Function

        Public Shared Function TryParseFile(base64Text As String, list As List(Of ProductAssignmentGridRow), apiResult As ApiResult) As Boolean
            Dim count As Integer = apiResult.ErrorCodeList.Count

            Using parser As New TextFieldParser(New MemoryStream(Convert.FromBase64String(base64Text)))
                parser.TextFieldType = FieldType.Delimited
                parser.Delimiters = New String() {","}
                Const baseFieldCount = 11
                Const reservedFieldCount = 2
                Dim lineNumber = 1
                While parser.EndOfData = False
                    Dim row() As String = parser.ReadFields()
                    If ValidAgBytesFileRow(row, baseFieldCount, lineNumber, reservedFieldCount, apiResult) Then
                        list.Add(GetFileRow(row, baseFieldCount, reservedFieldCount, apiResult))
                    End If
                    lineNumber += 1
                End While
            End Using

            Return GetImportApiResult(count, apiResult)
        End Function

        Public Shared Function GetFileRow(row() As String, baseFieldCount As Integer, reservedColumnCount As Integer, apiResult As ApiResult) As ProductAssignmentGridRow
            Dim item As New ProductAssignmentGridRow
            Dim baseIndex = 0

            'All items come in Active to begin with
            item.ActiveYn = True

            'If we have the extra columns, then reset the Guid and Active flags
            If row.Count = baseFieldCount + reservedColumnCount Then
                item.ProductAssignmentGuid = row(0)
                item.ActiveYn = (row(1).ToLower.StartsWith("y"))
                baseIndex = reservedColumnCount
            End If

            Dim i = baseIndex
            item.SampleTypeName = row(i)
            i += 1 : item.NutrientName = row(i)
            i += 1 : item.OrgLevelId = GetImportDelimitedList(row(i))
            i += 1 : item.OrgLevelName = GetImportDelimitedList(row(i))
            i += 1 : item.CropId = GetImportDelimitedList(row(i)) ' GetImportInteger(row(i), apiResult, ApiResultVm.ErrorCode.AgBytesCropIdMustBeNumeric)
            i += 1 : item.CropName = GetImportDelimitedList(row(i))
            i += 1 : item.CropPurposeName = GetImportDelimitedList(row(i))
            i += 1 : item.CropClassId = GetImportDelimitedList(row(i))
            i += 1 : item.CropClassName = GetImportDelimitedList(row(i))
            i += 1 : item.GrowthStageOrderName = GetImportDelimitedList(row(i))
            i += 1 : item.ProductName = GetImportDelimitedList(row(i))

            Return item
        End Function


        Private Shared Function ProductAssignmentGuidExists(productAssignmentGuid As String) As Boolean
            Dim sb As New SqlBuilder
            sb.Add("SELECT ProductAssignmentGuid")
            sb.Add("FROM {0}ProductAssignment", Schema)
            sb.Add("WHERE ProductAssignmentGuid = '{0}'", productAssignmentGuid)
            Return GetDataTable(sb.Sql).Rows.Count > 0
        End Function

        Private Shared Function ValidProductAssignment(gridRow As ProductAssignmentGridRow, loggedInUserGuid As String, apiResult As ApiResult, errorList As List(Of String)) As Boolean
            Dim count As Integer = apiResult.ErrorCodeList.Count

            Dim orgLvlList() As String = gridRow.OrgLevelId.Split(";"c)
            For Each item As String In orgLvlList
                CheckForNegative(item, "Hierarchy Level ID", errorList, apiResult)
            Next
            Dim cropList() As String = gridRow.CropId.Split(";"c)
            For Each item As String In cropList
                CheckForNegative(item, "Crop ID", errorList, apiResult)
            Next
            Dim cropClassList() As String = gridRow.CropClassId.Split(";"c)
            For Each item As String In cropClassList
                item = item.Trim
                If item.ToLower <> NoneIndicator.ToLower Then
                    CheckForNegative(item, "Crop Class", errorList, apiResult)
                End If
            Next
            If String.IsNullOrEmpty(gridRow.ProductAssignmentGuid) = False Then
                If ValidGuidFormat(gridRow.ProductAssignmentGuid) = False OrElse ProductAssignmentGuidExists(gridRow.ProductAssignmentGuid) = False Then
                    apiResult.ErrorCodeList.Add(ApiResultVm.ErrorCode.AgBytesGuidInvalid)
                    AddError(errorList, "Product Assignment guid is invalid: {0}", gridRow.ProductAssignmentGuid)
                    Return False
                End If
            End If

            If String.IsNullOrEmpty(gridRow.SampleTypeName) Then
                apiResult.ErrorCodeList.Add(ApiResultVm.ErrorCode.AgBytesSampleAttributeSampleTypeRequired)
                AddError(errorList, "Sample type is required.")
            Else
                If SampleAttributeVm.SampleTypeExists(gridRow.SampleTypeName) = False Then
                    apiResult.ErrorCodeList.Add(ApiResultVm.ErrorCode.AgBytesSampleAttributeSampleTypeInvalid)
                    AddError(errorList, "Sample type is invalid: {0}", gridRow.SampleTypeName)
                End If
            End If

            If String.IsNullOrEmpty(gridRow.NutrientName) Then
                apiResult.ErrorCodeList.Add(ApiResultVm.ErrorCode.AgBytesNutrientNameRequired)
                AddError(errorList, "Nutrient name is required.")
            Else
                If NutrientVm.NutrientNameExists(gridRow.NutrientName) = False Then
                    apiResult.ErrorCodeList.Add(ApiResultVm.ErrorCode.AgBytesNutrientInvalid)
                    AddError(errorList, "Nutrient is invalid: {0}", gridRow.NutrientName)
                End If
            End If

            Dim keyList As New List(Of String)
            Dim cropIdList() As String = gridRow.CropId.Split(";"c)
            Dim cropClassIdList() As String = gridRow.CropClassId.Split(";"c)
            Dim growthStageOrderList() As String = gridRow.GrowthStageOrderName.Split(";"c)
            Dim cropPurposeList() As String = gridRow.CropPurposeName.Split(";"c)
            For i = 0 To cropIdList.Length - 1
                Dim cropId As String = cropIdList(i).Trim
                Dim classId As String = String.Empty
                Dim growthStageOrderName As String = String.Empty
                Dim cropPurposeName As String = String.Empty
                If cropClassIdList.Length > i Then
                    classId = cropClassIdList(i).Trim
                    If classId = NoneIndicator Then
                        classId = String.Empty
                    End If
                End If
                If String.IsNullOrEmpty(cropId) = False Then
                    If CropVm.CropIdExists(CInt(Val(cropId))) = False Then
                        apiResult.ErrorCodeList.Add(ApiResultVm.ErrorCode.AgBytesCropIdInvalid)
                        AddError(errorList, "Crop ID is invalid: {0}", cropId)
                    Else
                        If CropVm.CropClassIdExists(CInt(Val(cropId)), If(String.IsNullOrEmpty(classId), New Integer?, CInt(Val(classId)))) = False Then
                            apiResult.ErrorCodeList.Add(ApiResultVm.ErrorCode.AgBytesCropClassIdInvalid)
                            AddError(errorList, "Crop ID + Crop Class ID is invalid: {0} + {1}", cropId, classId)
                        End If
                    End If

                    If growthStageOrderList.Length > i Then
                        growthStageOrderName = growthStageOrderList(i).Trim
                        If growthStageOrderName = NoneIndicator Then
                            growthStageOrderName = String.Empty
                        End If
                    End If
                    If String.IsNullOrEmpty(growthStageOrderName) = False Then
                        If GrowthStageVm.GrowthStageOrderExists(cropId, classId, GrowthStageVm.GrowthStageGroupType.Tissue, growthStageOrderName) = False Then
                            apiResult.ErrorCodeList.Add(ApiResultVm.ErrorCode.AgBytesGrowthStageOrderNameInvalid)
                            AddError(errorList, "Growth stage order name is invalid: {0}", growthStageOrderName)
                        End If
                    End If
                End If

                If cropPurposeList.Length > i Then
                    cropPurposeName = cropPurposeList(i).Trim
                    If cropPurposeName = NoneIndicator Then
                        cropPurposeName = String.Empty
                    End If
                End If
                Dim key As String = $"{cropId}+{classId}+{growthStageOrderName}+{cropPurposeName}".ToLower
                If keyList.Contains(key) Then
                    apiResult.ErrorCodeList.Add(ApiResultVm.ErrorCode.AgBytesCropIdListHasDuplicates)
                    AddError(errorList, "Crop list has duplicate Crop ID + Class ID + Growth Stage + Crop Purpose: {0} + {1} + {2} + {3}", cropId, classId, growthStageOrderName, cropPurposeName)
                Else
                    keyList.Add(key)
                End If
            Next

            For Each s In gridRow.CropPurposeName.Split(";"c)
                If String.IsNullOrEmpty(s.Trim) = False AndAlso s.Trim.ToLower <> NoneIndicator.ToLower Then
                    If NutrientRemovalRateVm.CropPurposeExists(s.Trim) = False Then
                        apiResult.ErrorCodeList.Add(ApiResultVm.ErrorCode.AgBytesCropPurposeInvalid)
                        AddError(errorList, "Crop purpose is invalid: {0}", s.Trim)
                    End If
                End If
            Next

            If String.IsNullOrEmpty(gridRow.OrgLevelId) Then
                apiResult.ErrorCodeList.Add(ApiResultVm.ErrorCode.LocationRequired)
                AddError(errorList, "Location ID is required.")
            Else
                For Each s In gridRow.OrgLevelId.Split(";"c)
                    If String.IsNullOrEmpty(s.Trim) = False Then
                        If OrgLevelVm.OrgLevelIdExists(s.Trim, UserVm.GetCompanyGuid(loggedInUserGuid)) = False Then
                            apiResult.ErrorCodeList.Add(ApiResultVm.ErrorCode.AgBytesLocationIdInvalid)
                            AddError(errorList, "Location ID is invalid: {0}", s.Trim)
                        End If
                    End If
                Next
            End If

            If TextBuilder.HasDuplicates(gridRow.ProductName, TextBuilder.DelimiterType.Semicolon) Then
                apiResult.ErrorCodeList.Add(ApiResultVm.ErrorCode.AgBytesProductListHasDuplicates)
                AddError(errorList, "Product list has duplicates: {0}", gridRow.ProductName)
            End If

            If TextBuilder.HasDuplicates(gridRow.OrgLevelId, TextBuilder.DelimiterType.Semicolon) Then
                apiResult.ErrorCodeList.Add(ApiResultVm.ErrorCode.AgBytesLocationIdListHasDuplicates)
                AddError(errorList, "Location ID list has duplicates: {0}", gridRow.OrgLevelId)
            End If
            'Check the length of the crop, crop purpose and growth stage lists

            'if no errors check 
            If DuplicateProductAssignmentExists(gridRow, loggedInUserGuid) Then
                apiResult.ErrorCodeList.Add(ApiResultVm.ErrorCode.ProductAssignmentNotUnique)
                AddError(errorList, "Product Assignment is not unique: {0} + {1}", gridRow.SampleTypeName, gridRow.NutrientName)
            End If
            Return count = apiResult.ErrorCodeList.Count

        End Function

        Public Shared Function DuplicateProductAssignmentExists(productAssignmentGridRow As ProductAssignmentGridRow, loggedInUserGuid As String) As Boolean
            Dim sql = GetDuplicateProductAssignmentExistsSql(productAssignmentGridRow, loggedInUserGuid)
            Return GetDataTable(sql).Rows.Count > 0
        End Function

        Public Shared Function GetDuplicateProductAssignmentExistsSql(productAssignmentGridRow As ProductAssignmentGridRow, loggedInUserGuid As String) As String
            Dim sb As New SqlBuilder

            Dim listSql As New List(Of String)
            sb.Add("SELECT pa.ProductAssignmentGuid")
            sb.Add(" FROM {0}ProductAssignment pa", Schema)
            sb.Add("LEFT JOIN {0}ProductAssignmentCrop pac ON pac.ProductAssignmentGuid = pa.ProductAssignmentGuid", Schema)
            sb.Add(" WHERE pa.SampleTypeGuid = '{0}'", SampleAttributeVm.GetSampleTypeGuid(productAssignmentGridRow.SampleTypeName))
            sb.Add(" AND pa.NutrientGuid = '{0}'", NutrientVm.GetNutrientGuidFromName(productAssignmentGridRow.NutrientName))
            sb.Add(" AND pa.ActiveYN = 1")

            sb.Add(GetListsSql(productAssignmentGridRow, loggedInUserGuid))
            ' On update, ignore existing record
            If StringHasValue(productAssignmentGridRow.ProductAssignmentGuid) Then
                sb.Add(" AND pa.ProductAssignmentGuid <> '{0}'", productAssignmentGridRow.ProductAssignmentGuid)
            End If

            Return sb.Sql
        End Function

        Public Shared Function GetListsSql(productAssignmentGridRow As ProductAssignmentGridRow, loggedInUserGuid As String) As String
            Dim sb As New SqlBuilder
            Dim cropGuid = String.Empty
            Dim cropIdx = 0
            Dim orgIdx = 0
            Dim prodIdx = 0

            'split the lists
            Dim cropNameList() As String = productAssignmentGridRow.CropName.Split(";"c)
            Dim cropPurposeList() As String = productAssignmentGridRow.CropPurposeName.Split(";"c)
            Dim growthStageOrderList() As String = productAssignmentGridRow.GrowthStageOrderName.Split(";"c)
            Dim orgLevelIdList() As String = productAssignmentGridRow.OrgLevelId.Split(";"c)
            Dim productList() As String = productAssignmentGridRow.ProductName.Split(";"c)

            ' Loop through Required lists
            For Each orgLevelId In orgLevelIdList
                If orgIdx = 0 Then
                    sb.Add(" AND (")
                End If
                sb.Add(" Exists(")
                sb.Add(" SELECT pao.OrgLevelGuid")
                sb.Add(" FROM {0}ProductAssignmentOrgLevel pao", Schema)
                sb.Add("LEFT JOIN {0}ProductAssignmentCrop pac ON pac.ProductAssignmentGuid = pa.ProductAssignmentGuid", Schema)
                sb.Add(" WHERE pao.ProductAssignmentGuid = pa.ProductAssignmentGuid")
                sb.Add(" AND OrgLevelGuid = '{0}'", OrgLevelVm.GetOrgLevelGuid(orgLevelId, UserVm.GetCompanyGuid(loggedInUserGuid)))
                sb.Add(" ))")
                If orgIdx < orgLevelIdList.Length - 1 Then
                    sb.Add(" OR (")
                End If
                orgIdx = orgIdx + 1
            Next
            'Loop through Optional lists
            For Each crop In cropNameList
                If StringHasValue(crop) Then
                    If crop = String.Empty Then
                        sb.Add("AND (pac.CropGuid IS NUll)")
                    End If
                    If cropIdx = 0 Then
                        sb.Add(" AND (")
                    End If
                    cropGuid = CropVm.GetCropGuidFromName(crop.Trim)

                    sb.Add(" Exists(")
                    sb.Add(" SELECT pac.CropGuid")
                    sb.Add(" FROM {0}ProductAssignmentCrop pac", Schema)
                    sb.Add(" WHERE pac.ProductAssignmentGuid = pa.ProductAssignmentGuid")
                    sb.Add(" AND CropGuid = '{0}'", cropGuid.Trim)
                    If StringHasValue(cropPurposeList(cropIdx)) Then
                        sb.Add(" AND CropPurposeGuid = '{0}'", NutrientRemovalRateVm.GetCropPurposeGuid(cropPurposeList(cropIdx).Trim))
                    Else
                        sb.Add(" AND CropPurposeGuid IS NULL")
                    End If
                    If (growthStageOrderList.Length > cropIdx AndAlso StringHasValue(growthStageOrderList(cropIdx)) AndAlso Not growthStageOrderList(cropIdx) = NoneIndicator) Then
                        sb.Add(" AND GrowthStageOrderGuid = '{0}'", GrowthStageVm.GetGrowthStageOrderGuidFromName(cropGuid, growthStageOrderList(cropIdx).Trim))
                    Else
                        sb.Add(" AND GrowthStageOrderGuid IS NULL")
                    End If
                    sb.Add(" ))")
                    If cropIdx < cropNameList.Length - 1 Then
                        sb.Add(" AND (")
                    End If
                    cropIdx = cropIdx + 1
                Else
                    sb.Add("AND (pac.CropGuid IS NUll)")
                End If

            Next

            Return sb.Sql
        End Function
#End Region

#Region " Export "

        Public Shared Sub ExportProductAssignmentList(list As List(Of String), loggedInUserGuid As String, apiResult As ApiResult)

            Dim tb As New TextBuilder

            tb.Add("ProductAssignmentGuid")
            tb.Add("ActiveYN")
            tb.Add("Sample Type")
            tb.Add("Nutrient")
            tb.Add("Hierarchy Level ID 0-n")
            tb.Add("Hierarchy Level Name 0-n")
            tb.Add("Crop ID 0-n")
            tb.Add("Crop 0-n")
            tb.Add("Crop Purpose 0-n")
            tb.Add("Crop Class ID 0-n")
            tb.Add("Crop Class Name 0-n")
            tb.Add("Growth Stage 0-n")
            tb.Add("Product 0-n")

            tb.EndLine()

            For Each item As String In list
                Dim row As ProductAssignmentGridRow = GetProductAssignmentGridRow(item)
                tb.Add(row.ProductAssignmentGuid)
                tb.Add(If(row.ActiveYn, "Y", "N"))
                tb.Add(row.SampleTypeName)
                tb.Add(row.NutrientName)
                tb.Add(row.OrgLevelId)
                tb.Add(row.OrgLevelName)
                tb.Add(row.CropId)
                tb.Add(row.CropName)
                tb.Add(row.CropPurposeName)
                tb.Add(row.CropClassId)
                tb.Add(row.CropClassName)
                tb.Add(row.GrowthStageOrderName)
                tb.Add(row.ProductName)
                tb.EndLine()
            Next

            AgBytesLogVm.WriteExportLog(AgBytesLogVm.Feature.ProductAssignment, list.Count, loggedInUserGuid)
            Dim fileDataStream As New MemoryStream(Convert.FromBase64String(tb.Base64String))

            SetupDataStreamToExportFolder(loggedInUserGuid, "ProductAssignment", fileDataStream, apiResult)

        End Sub

        Public Shared Function GetProductAssignmentCropClassId(productAssignmentGuid As String) As String
            Dim tb As New TextBuilder(TextBuilder.DelimiterType.SemicolonSpace)
            Dim sb As New SqlBuilder

            sb.Add("SELECT Crop.CropClassId AS CropClassId")
            sb.Add("FROM {0}ProductAssignmentCrop ProductAssignmentCrop", Schema)
            sb.Add("INNER JOIN {0}Crop Crop ON ProductAssignmentCrop.CropGuid = Crop.CropGuid", Schema)
            sb.Add("WHERE ProductAssignmentGuid = '{0}'", productAssignmentGuid)
            sb.Add("ORDER BY Crop.Name")

            Dim dt As DataTable = GetDataTable(sb.Sql)

            Dim exists As Boolean = False
            For Each row As DataRow In dt.Rows
                If row("CropClassId").ToString <> "1" Then
                    exists = True
                End If
            Next
            If exists Then
                For Each row As DataRow In dt.Rows
                    Dim cropClassId As String = row("CropClassId").ToString
                    If cropClassId = "1" Then
                        cropClassId = NoneIndicator
                    End If
                    tb.Add(cropClassId)
                Next
            End If

            tb.EndList()

            Return tb.Text
        End Function

        Public Shared Function GetProductAssignmentCropClassName(productAssignmentGuid As String) As String
            Dim tb As New TextBuilder(TextBuilder.DelimiterType.SemicolonSpace)
            Dim sb As New SqlBuilder

            sb.Add("SELECT Crop.CropClassName AS CropClassName")
            sb.Add("FROM {0}ProductAssignmentCrop ProductAssignmentCrop", Schema)
            sb.Add("INNER JOIN {0}Crop Crop ON ProductAssignmentCrop.CropGuid = Crop.CropGuid", Schema)
            sb.Add("WHERE ProductAssignmentGuid = '{0}'", productAssignmentGuid)
            sb.Add("ORDER BY Crop.Name")

            Dim dt As DataTable = GetDataTable(sb.Sql)

            Dim exists As Boolean = False
            For Each row As DataRow In dt.Rows
                If row("CropClassName").ToString <> String.Empty Then
                    exists = True
                End If
            Next
            If exists Then
                For Each row As DataRow In dt.Rows
                    Dim cropClassName As String = row("CropClassName").ToString
                    If String.IsNullOrEmpty(cropClassName) Then
                        cropClassName = NoneIndicator
                    End If
                    tb.Add(cropClassName)
                Next
            End If

            tb.EndList()

            Return tb.Text
        End Function

        Public Shared Function GetProductAssignmentCropId(productAssignmentGuid As String) As String
            Dim tb As New TextBuilder(TextBuilder.DelimiterType.SemicolonSpace)
            Dim sb As New SqlBuilder

            sb.Add("SELECT CropID")
            sb.Add("FROM {0}Crop", Schema)
            sb.Add("INNER JOIN {0}ProductAssignmentCrop ON ProductAssignmentCrop.CropGuid = Crop.CropGuid", Schema)
            sb.Add("WHERE ProductAssignmentGuid = '{0}'", productAssignmentGuid)
            sb.Add("ORDER BY Crop.Name")

            For Each row As DataRow In GetDataTable(sb.Sql).Rows
                tb.Add(row("CropID").ToString)
            Next

            tb.EndList()

            Return tb.Text
        End Function

        Public Shared Function GetProductAssignmentCropName(productAssignmentGuid As String) As String
            Dim tb As New TextBuilder(TextBuilder.DelimiterType.SemicolonSpace)
            Dim sb As New SqlBuilder

            sb.Add("SELECT Crop.Name AS CropName")
            sb.Add("FROM {0}Crop", Schema)
            sb.Add("INNER JOIN {0}ProductAssignmentCrop ON ProductAssignmentCrop.CropGuid = Crop.CropGuid", Schema)
            sb.Add("WHERE ProductAssignmentGuid = '{0}'", productAssignmentGuid)
            sb.Add("ORDER BY Crop.Name")

            For Each row As DataRow In GetDataTable(sb.Sql).Rows
                tb.Add(row("CropName").ToString)
            Next

            tb.EndList()

            Return tb.Text
        End Function

        Public Shared Function GetProductAssignmentCropPurpose(productAssignmentGuid As String) As String
            Dim tb As New TextBuilder(TextBuilder.DelimiterType.SemicolonSpace)
            Dim sb As New SqlBuilder

            sb.Add("SELECT CropPurpose.Name AS CropPurposeName")
            sb.Add("FROM {0}ProductAssignmentCrop ProductAssignmentCrop", Schema)
            sb.Add("INNER JOIN {0}Crop Crop ON ProductAssignmentCrop.CropGuid = Crop.CropGuid", Schema)
            sb.Add("LEFT JOIN {0}CropPurpose CropPurpose ON ProductAssignmentCrop.CropPurposeGuid = CropPurpose.CropPurposeGuid", Schema)
            sb.Add("WHERE ProductAssignmentGuid = '{0}'", productAssignmentGuid)
            sb.Add("ORDER BY Crop.Name")

            Dim dt As DataTable = GetDataTable(sb.Sql)

            Dim exists As Boolean = False
            For Each row As DataRow In dt.Rows
                If row("CropPurposeName").ToString <> String.Empty Then
                    exists = True
                End If
            Next
            If exists Then
                For Each row As DataRow In dt.Rows
                    Dim cropPurpose As String = row("CropPurposeName").ToString
                    If String.IsNullOrEmpty(cropPurpose) Then
                        cropPurpose = NoneIndicator
                    End If
                    tb.Add(cropPurpose)
                Next
            End If

            tb.EndList()

            Return tb.Text
        End Function

        Public Shared Function GetProductAssignmentGridRow(productAssignmentGuid As String) As ProductAssignmentGridRow
            Dim result As New ProductAssignmentGridRow
            Dim sb As New SqlBuilder

            sb.Add("SELECT SampleType.Name AS SampleTypeName")
            sb.Add(",Nutrient.Name As NutrientName")
            sb.Add(",ProductAssignment.ActiveYN")
            sb.Add("FROM {0}ProductAssignment ProductAssignment", Schema)
            sb.Add("LEFT JOIN {0}SampleType SampleType ON SampleType.SampleTypeGuid = ProductAssignment.SampleTypeGuid", Schema)
            sb.Add("LEFT JOIN {0}Nutrient Nutrient ON Nutrient.NutrientGuid = ProductAssignment.NutrientGuid", Schema)
            sb.Add("WHERE ProductAssignment.ProductAssignmentGuid = '{0}'", productAssignmentGuid)

            Dim row As DataRow = GetDataTable(sb.Sql).Rows(0)

            result.ProductAssignmentGuid = productAssignmentGuid
            result.SampleTypeName = row("SampleTypeName").ToString
            result.NutrientName = row("NutrientName").ToString
            result.OrgLevelId = GetProductAssignmentOrgLevelId(productAssignmentGuid)
            result.OrgLevelName = GetProductAssignmentOrgLevelName(productAssignmentGuid)
            result.CropId = GetProductAssignmentCropId(productAssignmentGuid)
            result.CropName = GetProductAssignmentCropName(productAssignmentGuid)
            result.CropPurposeName = GetProductAssignmentCropPurpose(productAssignmentGuid)
            result.CropClassId = GetProductAssignmentCropClassId(productAssignmentGuid)
            result.CropClassName = GetProductAssignmentCropClassName(productAssignmentGuid)
            result.GrowthStageOrderName = GetProductAssignmentGrowthStageOrder(productAssignmentGuid)
            result.ProductName = GetProductAssignmentProductName(productAssignmentGuid)
            result.ActiveYn = Boolean.Parse(row("ActiveYN").ToString)

            Return result
        End Function

        Public Shared Function GetProductAssignmentGrowthStageOrder(productAssignmentGuid As String) As String
            Dim tb As New TextBuilder(TextBuilder.DelimiterType.SemicolonSpace)
            Dim sb As New SqlBuilder

            sb.Add("SELECT GrowthStageOrder.Name AS GrowthStageOrderName")
            sb.Add("FROM {0}ProductAssignmentCrop ProductAssignmentCrop", Schema)
            sb.Add("INNER JOIN {0}Crop Crop ON ProductAssignmentCrop.CropGuid = Crop.CropGuid", Schema)
            sb.Add("LEFT JOIN {0}GrowthStageOrder GrowthStageOrder ON ProductAssignmentCrop.GrowthStageOrderGuid = GrowthStageOrder.GrowthStageOrderGuid", Schema)
            sb.Add("WHERE ProductAssignmentGuid = '{0}'", productAssignmentGuid)
            sb.Add("ORDER BY Crop.Name")

            Dim dt As DataTable = GetDataTable(sb.Sql)

            Dim exists As Boolean = False
            For Each row As DataRow In dt.Rows
                If row("GrowthStageOrderName").ToString <> String.Empty Then
                    exists = True
                End If
            Next
            If exists Then
                For Each row As DataRow In dt.Rows
                    Dim growthStageOrder As String = row("GrowthStageOrderName").ToString
                    If String.IsNullOrEmpty(growthStageOrder) Then
                        growthStageOrder = NoneIndicator
                    End If
                    tb.Add(growthStageOrder)
                Next
            End If

            tb.EndList()

            Return tb.Text
        End Function

        Public Shared Function GetProductAssignmentOrgLevelId(productAssignmentGuid As String) As String
            Dim tb As New TextBuilder(TextBuilder.DelimiterType.SemicolonSpace)
            Dim sb As New SqlBuilder

            sb.Add("SELECT OrgLevel.ID")
            sb.Add("FROM {0}OrgLevel", Schema)
            sb.Add("INNER JOIN {0}ProductAssignmentOrgLevel ON ProductAssignmentOrgLevel.OrgLevelGuid = OrgLevel.OrgLevelGuid", Schema)
            sb.Add("WHERE ProductAssignmentGuid = '{0}'", productAssignmentGuid)
            sb.Add("ORDER BY OrgLevel.ID")

            For Each row As DataRow In GetDataTable(sb.Sql).Rows
                tb.Add(row("ID").ToString)
            Next

            tb.EndList()

            Return tb.Text
        End Function

        Public Shared Function GetProductAssignmentOrgLevelName(productAssignmentGuid As String) As String
            Dim tb As New TextBuilder(TextBuilder.DelimiterType.SemicolonSpace)
            Dim sb As New SqlBuilder

            sb.Add("SELECT OrgLevel.Name AS OrgLevelName")
            sb.Add("FROM {0}OrgLevel", Schema)
            sb.Add("INNER JOIN {0}ProductAssignmentOrgLevel ON ProductAssignmentOrgLevel.OrgLevelGuid = OrgLevel.OrgLevelGuid", Schema)
            sb.Add("WHERE ProductAssignmentGuid = '{0}'", productAssignmentGuid)
            sb.Add("ORDER BY OrgLevel.ID")

            For Each row As DataRow In GetDataTable(sb.Sql).Rows
                tb.Add(row("OrgLevelName").ToString)
            Next

            tb.EndList()

            Return tb.Text
        End Function

        Public Shared Function GetProductAssignmentProductName(productAssignmentGuid As String) As String
            Dim tb As New TextBuilder(TextBuilder.DelimiterType.SemicolonSpace)
            Dim sb As New SqlBuilder

            sb.Add("SELECT Product.Name AS ProductName")
            sb.Add("FROM {0}Product", Schema)
            sb.Add("INNER JOIN {0}ProductAssignmentProduct ON ProductAssignmentProduct.ProductGuid = Product.ProductGuid", Schema)
            sb.Add("WHERE ProductAssignmentGuid = '{0}'", productAssignmentGuid)
            sb.Add("ORDER BY Product.Name")

            For Each row As DataRow In GetDataTable(sb.Sql).Rows
                tb.Add(row("ProductName").ToString)
            Next

            tb.EndList()

            Return tb.Text
        End Function
#End Region
    End Class
End Namespace
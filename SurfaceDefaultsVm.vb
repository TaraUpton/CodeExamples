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
Imports HaasWebApp.Models.EntityHierarchy.Zapper

Namespace ViewModels.EntityHierarchy
    Public Class SurfaceDefaultsVm

#Region " Declarations "
        Public Const NoneIndicator As String = "None"
#End Region

#Region " Shared Methods "

        Public Shared Function Valid(surfaceDefaults As SurfaceDefaults, loggedInUserGuid As String, apiResult As ApiResult, errorList As List(Of String)) As Boolean
            Dim count As Integer = apiResult.ErrorCodeList.Count

            If UserRoleVm.AllowedUser(SoftwareFunctionVm.FunctionName.SurfaceDefaults, loggedInUserGuid) = False Then
                apiResult.ErrorCodeList.Add(ApiResultVm.ErrorCode.ApiAccessDenied)
                Return False
            End If

            If String.IsNullOrEmpty(surfaceDefaults.SystemAttributeGuid) Then
                apiResult.ErrorCodeList.Add(ApiResultVm.ErrorCode.SystemAttributeNameRequired)
            End If

            If String.IsNullOrEmpty(surfaceDefaults.ColorRampGuid) Then
                apiResult.ErrorCodeList.Add(ApiResultVm.ErrorCode.ColorRampRequired)
            End If
            If String.IsNullOrEmpty(surfaceDefaults.ClassificationMethodGuid) Then
                apiResult.ErrorCodeList.Add(ApiResultVm.ErrorCode.ClassificationMethodRequired)
            End If
            If String.IsNullOrEmpty(surfaceDefaults.NumberOfClasses) Then
                apiResult.ErrorCodeList.Add(ApiResultVm.ErrorCode.NumberOfClassesRequired)
            End If

            If surfaceDefaults.OrgLevelList.Count = 0 Then
                apiResult.ErrorCodeList.Add(ApiResultVm.ErrorCode.LocationRequired)
            End If

            If Not IsSurfaceDefaultsUnique(surfaceDefaults, loggedInUserGuid) Then
                apiResult.ErrorCodeList.Add(ApiResultVm.ErrorCode.SurfaceDefaultsNotUnique)
            End If

            ApiResultVm.RemoveDuplicates(apiResult)

            Return count = apiResult.ErrorCodeList.Count
        End Function

        Public Shared Function IsSurfaceDefaultsUnique(surfaceDefaults As SurfaceDefaults, loggedInUserGuid As String) As Boolean
            Dim sb As New SqlBuilder
            sb.Add("SELECT COUNT(*)")
            sb.Add("FROM {0}SurfaceDefaults sd", Schema)
            sb.Add("INNER JOIN {0}SurfaceDefaultsOrgLevel sdol ON sdol.SurfaceDefaultsGuid = sd.SurfaceDefaultsGuid", Schema)
            sb.Add("WHERE sd.SurfaceDefaultsGuid " & If(StringIsEmpty(surfaceDefaults.SurfaceDefaultsGuid), " IS NOT NULL", " <> " & FormatKeyOrNull(surfaceDefaults.SurfaceDefaultsGuid)))
            sb.Add(" AND sd.SystemAttributeGuid = " & FormatKeyOrNull(surfaceDefaults.SystemAttributeGuid))
            Dim loopPrefix = ""
            If surfaceDefaults.OrgLevelList.Count > 0 Then
                sb.Add(" AND (")
                For Each orgLevel In surfaceDefaults.OrgLevelList
                    sb.Add(loopPrefix)
                    sb.Add(" ( pao.OrgLevelGuid = " & FormatKeyOrNull(orgLevel.OrgLevelGuid) & ")")
                    loopPrefix = " AND "
                Next
                sb.Add(")")
            End If

            Dim dt As DataTable = GetDataTable(sb.Sql)
            Return GetInteger(dt.Rows(0)(0).ToString()) = 0
        End Function

        Public Shared Function ValidGetSurfaceDefaultsList(request As SurfaceDefaultsRequest, apiResult As ApiResult) As Boolean
            Dim count As Integer = apiResult.ErrorCodeList.Count
            If Sorts.SortExists(request.SurfaceDefaultsSort) Then
                Select Case request.SurfaceDefaultsSort(0).FieldName
                    Case SurfaceDefaultsRequest.FieldCanDelete,
                        SurfaceDefaultsRequest.FieldSystemAttributeName,
                        SurfaceDefaultsRequest.FieldColorRampName,
                        SurfaceDefaultsRequest.FieldClassificationMethodName,
                        SurfaceDefaultsRequest.FieldLocationLevelName,
                        SurfaceDefaultsRequest.FieldModifiedDate
                        'success
                    Case Else
                        apiResult.ErrorCodeList.Add(ApiResultVm.ErrorCode.SortNameInvalid)
                End Select
            End If
            Return count = apiResult.ErrorCodeList.Count
        End Function

#End Region

#Region " Get List "
        Public Shared Function GetFilterSql(filter As SurfaceDefaultsFilters, loggedInUserGuid As String) As String
            Dim sb As New SqlBuilder
            If String.IsNullOrEmpty(filter.CanDelete) = False Then
                sb.Add("    AND CanDelete = {0}", StringAsBoolean(filter.CanDelete))
            End If
            If String.IsNullOrEmpty(filter.SystemAttributeName) = False Then
                sb.Add("    AND (sa.Name LIKE '{0}%' OR sa.Name LIKE '% {0}%')", filter.SystemAttributeName)
            End If
            If String.IsNullOrEmpty(filter.ColorRampName) = False Then
                sb.Add("    AND (cr.Name LIKE '{0}%' OR cr.Name LIKE '% {0}%')", filter.ColorRampName)
            End If
            If String.IsNullOrEmpty(filter.ClassificationMethodName) = False Then
                sb.Add("    AND (cm.Name LIKE '{0}%' OR cm.Name LIKE '% {0}%')", filter.ClassificationMethodName)
            End If
            If String.IsNullOrEmpty(filter.NumberOfClasses) = False Then
                sb.Add("    AND (surfaceDefaults.NumberOfClasses LIKE '{0}%' OR surfaceDefaults.NumberOfClasses LIKE '% {0}%')", filter.NumberOfClasses)
            End If
            If String.IsNullOrEmpty(filter.LocationLevel) = False Then
                sb.Add("    AND ({0}", PadTic.No, GetOrgLevelListSelect(loggedInUserGuid))
                sb.Add("        LIKE '{0}%' ", filter.LocationLevel)
                sb.Add("    OR {0}", PadTic.No, GetOrgLevelListSelect(loggedInUserGuid))
                sb.Add("        LIKE '% {0}%' )", filter.LocationLevel)
            End If
            If String.IsNullOrEmpty(filter.ModifiedDate) = False Then
                sb.Add("    AND ({0}", SqlBuilder.PadTic.No, GetModifiedDateSql(TableEnum.Crop, loggedInUserGuid))
                sb.Add("    LIKE '{0}%'  )", filter.ModifiedDate)
                sb.Add("    OR ({0}", SqlBuilder.PadTic.No, GetModifiedDateSql(TableEnum.Crop, loggedInUserGuid))
                sb.Add("    LIKE '% {0}%'  )", filter.ModifiedDate)
            End If
            Return sb.Sql
        End Function

        Public Shared Sub GetSurfaceDefaultsList(request As SurfaceDefaultsRequest, loggedInUserGuid As String, apiResult As ApiResult)
            If ValidGetSurfaceDefaultsList(request, apiResult) = False Then
                Return
            End If

            Dim sortOrder As String = GetSortOrderSql(apiResult, request.SurfaceDefaultsSort, loggedInUserGuid)
            Dim listSql As String = GetSurfaceDefaultsListSql(request, sortOrder, loggedInUserGuid)
            Dim dt As DataTable = GetDataTable(listSql)

            Dim list As New List(Of SurfaceDefaultsGridRow)
            Dim gridPageResponse As New GridPageResponse
            Dim firstRow As Boolean = True
            For Each row As DataRow In dt.Rows
                If firstRow Then
                    gridPageResponse.TotalCount = CInt(row.Item("TotalRows"))
                    firstRow = False
                End If

                Dim item As New SurfaceDefaultsGridRow
                item.SurfaceDefaultsGuid = row.Item("SurfaceDefaultsGuid").ToString
                item.SystemAttributeName = row.Item("SystemAttributeName").ToString
                item.ColorRampName = row.Item("ColorRampName").ToString
                item.OrgLevelName = row("OrgLevelList").ToString
                item.ClassificationMethodName = row("ClassificationMethodName").ToString
                item.NumberOfClasses = row("NumberOfClasses").ToString
                item.ModifiedDate = row("ModifiedDate").ToString
                item.CanDelete = GetBoolean(row.Item("CanDelete").ToString())

                list.Add(item)
            Next

            gridPageResponse.GridRows = list
            apiResult.Model = gridPageResponse
            apiResult.Success = True

        End Sub

        Public Shared Function GetSurfaceDefaultsListSql(request As SurfaceDefaultsRequest, sortOrder As String, loggedInUserGuid As String) As String

            Dim sb As New SqlBuilder

            sb.Add("SELECT * FROM")
            sb.Add("(")
            sb.Add("    SELECT ROW_NUMBER() OVER (ORDER BY {0}) AS RowNumber", PadTic.No, sortOrder)
            sb.Add("        , COUNT(*) OVER () AS TotalRows")
            sb.Add("        ,surfaceDefaults.SurfaceDefaultsGuid")
            sb.Add("        ,sa.Name AS SystemAttributeName")
            sb.Add("        ,cr.Name AS ColorRampName")
            sb.Add("        ,cm.Name AS ClassificationMethodName")
            sb.Add("        ,surfaceDefaults.NumberOfClasses AS NumberOfClasses")
            sb.Add("        ,{0} AS OrgLevelList", PadTic.No, GetOrgLevelListSelect(loggedInUserGuid))
            sb.Add("        ,{0} AS ModifiedDate", PadTic.No, GetModifiedDateSql(TableEnum.SurfaceDefaults, loggedInUserGuid))
            sb.Add("        ,CanDelete ")
            sb.Add("    FROM  {0}SurfaceDefaults surfaceDefaults", Schema)
            sb.Add("    INNER JOIN {0}SystemAttribute sa ON surfaceDefaults.SystemAttributeGuid = sa.SystemAttributeGuid ", Schema)
            sb.Add("    INNER JOIN {0}ColorRamp cr ON surfaceDefaults.ColorRampGuid = cr.ColorRampGuid ", SpatialSchema)
            sb.Add("    INNER JOIN {0}ClassificationMethod cm ON surfaceDefaults.ClassificationMethodGuid = cm.ClassificationMethodGuid ", SpatialSchema)
            sb.Add(GetCanDeleteApplySql(TableEnum.SurfaceDefaults))
            sb.Add("    WHERE 1=1")
            sb.Add(GetFilterSql(request.SurfaceDefaultsFilter, loggedInUserGuid))
            sb.Add(" AND EXISTS (SELECT * FROM {0}GetUserHierarchyAccess('{1}') ha INNER JOIN {0}SurfaceDefaultsOrgLevel pao ON pao.OrgLevelGuid = ha.OrgLevelGuid WHERE pao.SurfaceDefaultsGuid = surfaceDefaults.SurfaceDefaultsGuid )", Schema, loggedInUserGuid)
            If StringHasValue(request.SurfaceDefaultsFilter.OrgLevelGuid) Then
                sb.Add(" AND EXISTS (SELECT * FROM {0}GetHierarchyChildren('{1}') ha INNER JOIN {0}SurfaceDefaultsOrgLevel pao ON pao.OrgLevelGuid = ha.OrgLevelGuid WHERE pao.SurfaceDefaultsGuid = surfaceDefaults.SurfaceDefaultsGuid )", Schema, request.SurfaceDefaultsFilter.OrgLevelGuid)
            End If
            sb.Add(") As SurfaceDefaults")
            sb.Add("WHERE RowNumber > {0}", request.SurfaceDefaultsPageOptions.Skip)
            sb.Add("And RowNumber <= {0}", request.SurfaceDefaultsPageOptions.Skip + request.SurfaceDefaultsPageOptions.PageSize)
            sb.Add("ORDER BY RowNumber")

            Return sb.Sql
        End Function

        Public Shared Function GetOrgLevelListSelect(loggedInUserGuid As String) As String
            Return String.Format("ISNULL((SELECT STUFF((SELECT '; ' + OrgLevel.Name FROM {0}SurfaceDefaultsOrgLevel SurfaceDefaultsOrgLevel INNER JOIN {0}OrgLevel OrgLevel ON OrgLevel.OrgLevelGuid = SurfaceDefaultsOrgLevel.OrgLevelGuid INNER JOIN {0}GetHierarchyChildren('{1}') Children ON OrgLevel.OrgLevelGuid = Children.OrgLevelGuid  WHERE SurfaceDefaultsOrgLevel.SurfaceDefaultsGuid = SurfaceDefaults.SurfaceDefaultsGuid ORDER BY OrgLevel.Name For XML Path(''), Type).value('.', 'nvarchar(max)'), 1, 2, '')), '')", Schema, UserVm.GetCompanyGuid(loggedInUserGuid))
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
                                Case SurfaceDefaultsRequest.FieldCanDelete
                                    columnName = "CanDelete"
                                Case SurfaceDefaultsRequest.FieldSystemAttributeName
                                    columnName = "sa.Name"
                                Case SurfaceDefaultsRequest.FieldColorRampName
                                    columnName = "cr.Name"
                                Case SurfaceDefaultsRequest.FieldClassificationMethodName
                                    columnName = "cm.Name"
                                Case SurfaceDefaultsRequest.FieldNumberOfClasses
                                    columnName = "SurfaceDefaults.NumberOfClasses"
                                Case SurfaceDefaultsRequest.FieldLocationLevelName
                                    columnName = GetOrgLevelListSelect(loggedInUserGuid)
                            End Select
                            sortOrder = sortOrder & columnName & " " & item.Sort.Direction
                        End If
                    Next item
                Next i
            End If

            If String.IsNullOrEmpty(sortOrder) Then
                'Use this as the default sort if not sort set by UI
                sortOrder = "sa.Name, " + GetOrgLevelListSelect(loggedInUserGuid)
            End If

            Return sortOrder

        End Function

#End Region

#Region "Add "

        Public Shared Sub AddSurfaceDefaults(ByVal loggedInUserGuid As String, ByVal surfaceDefaults As SurfaceDefaults, apiResult As ApiResult)
            If Valid(surfaceDefaults, loggedInUserGuid, apiResult, New List(Of String)) Then
                Dim list As New List(Of String)
                surfaceDefaults.SurfaceDefaultsGuid = GetNewGuid()
                list.Add(GetInsertStatement(surfaceDefaults, loggedInUserGuid))
                list.AddRange(GetSurfaceDefaultsOrgLevelSql(surfaceDefaults))
                Execute(list)
                apiResult.Model = surfaceDefaults
                apiResult.Success = True
            End If

        End Sub

        Public Shared Function GetInsertStatement(surfaceDefaults As SurfaceDefaults, loggedInUserGuid As String) As String
            Dim sb As New SqlBuilder

            sb.Add("INSERT INTO {0}SurfaceDefaults (", Schema)
            sb.Add("SurfaceDefaultsGuid")
            ' sb.Add(",CompanyGuid")
            sb.Add(",SystemAttributeGuid")
            sb.Add(",ColorRampGuid")
            sb.Add(",ClassificationMethodGuid")
            sb.Add(",NumberOfClasses")
            sb.Add(",ModifiedDate")
            sb.Add(",RecordOwnerGuid")
            sb.Add(",CreatedByGuid")
            sb.Add(",CreatedDate")
            sb.Add(") VALUES (")
            sb.Add("'{0}'", surfaceDefaults.SurfaceDefaultsGuid)
            sb.Add(",'{0}'", UserVm.GetCompanyGuid(loggedInUserGuid))
            sb.Add(",'{0}'", surfaceDefaults.SystemAttributeGuid)
            sb.Add(",'{0}'", surfaceDefaults.ColorRampGuid)
            sb.Add(",'{0}'", surfaceDefaults.ClassificationMethodGuid)
            sb.Add(",'{0}'", surfaceDefaults.NumberOfClasses)
            sb.Add("," & FormatCurrentDateTime())
            sb.Add(",'{0}'", loggedInUserGuid)
            sb.Add(",'{0}'", loggedInUserGuid)
            sb.Add("," & FormatCurrentDateTime())
            sb.Add(")")

            Return sb.Sql
        End Function


        Public Shared Function GetSurfaceDefaultsOrgLevelSql(surfaceDefaults As SurfaceDefaults) As List(Of String)
            Dim list As New List(Of String)
            Dim sb As New SqlBuilder
            Dim guidList As New List(Of String)

            For Each item As SurfaceDefaultsOrgLevel In surfaceDefaults.OrgLevelList
                If String.IsNullOrEmpty(item.SurfaceDefaultsOrgLevelGuid) = False Then
                    guidList.Add(item.SurfaceDefaultsOrgLevelGuid)
                End If
            Next

            sb.Add("DELETE FROM {0}SurfaceDefaultsOrgLevel", Schema)
            sb.Add("WHERE SurfaceDefaultsGuid = '{0}'", surfaceDefaults.SurfaceDefaultsGuid)
            If guidList.Count > 0 Then
                sb.Add("AND SurfaceDefaultsOrgLevelGuid NOT IN ({0})", PadTic.No, GetListSql(guidList))
            End If

            list.Add(sb.Sql)

            For Each item As SurfaceDefaultsOrgLevel In surfaceDefaults.OrgLevelList
                If String.IsNullOrEmpty(item.SurfaceDefaultsOrgLevelGuid) Then
                    sb.Clear()
                    sb.Add("INSERT INTO {0}SurfaceDefaultsOrgLevel (SurfaceDefaultsOrgLevelGuid, SurfaceDefaultsGuid, OrgLevelGuid) VALUES (", Schema)
                    sb.Add("'{0}'", GetNewGuid())
                    sb.Add(",'{0}'", surfaceDefaults.SurfaceDefaultsGuid)
                    sb.Add(",'{0}'", item.OrgLevelGuid)
                    sb.Add(")")
                    list.Add(sb.Sql)
                Else
                    sb.Clear()
                    sb.Add("UPDATE {0}SurfaceDefaultsOrgLevel", Schema)
                    sb.Add("SET OrgLevelGuid = '{0}'", item.OrgLevelGuid)
                    sb.Add("WHERE SurfaceDefaultsOrgLevelGuid = '{0}'", item.SurfaceDefaultsOrgLevelGuid)
                    list.Add(sb.Sql)
                End If
            Next

            Return list
        End Function

#End Region

#Region " Get Surface Defaults "

        Public Shared Sub GetSurfaceDefaults(surfaceDefaultsGuid As String, loggedInUserGuid As String, apiResult As ApiResult)
            apiResult.Model = GetSurfaceDefaults(surfaceDefaultsGuid)
            apiResult.Success = True
        End Sub

        Public Shared Function GetSurfaceDefaults(surfaceDefaultsGuid As String) As SurfaceDefaults
            Dim result As New SurfaceDefaults
            Dim sb As New SqlBuilder

            sb.Add("SELECT surfaceDefaults.SurfaceDefaultsGuid")
            sb.Add(",systemAttribute.SystemAttributeGuid")
            sb.Add(",systemAttribute.Name AS SystemAttributeName")
            sb.Add(",colorRamp.ColorRampGuid")
            sb.Add(",colorRamp.Name AS ColorRampName")
            sb.Add(",classificationMethod.ClassificationMethodGuid")
            sb.Add(",classificationMethod.Name AS ClassificationMethod")
            sb.Add(",surfaceDefaults.NumberOfClasses AS NumberOfClasses")
            sb.Add(",surfaceDefaults.ModifiedDate")
            sb.Add("FROM {0}SurfaceDefaults surfaceDefaults", Schema)

            sb.Add("INNER JOIN {0}SystemAttribute systemAttribute ON systemAttribute.SystemAttributeGuid = surfaceDefaults.SystemAttributeGuid", Schema)
            sb.Add("INNER JOIN {0}ColorRamp colorRamp ON colorRamp.ColorRampGuid = surfaceDefaults.ColorRampGuid", Schema)
            sb.Add("INNER JOIN {0}ClassificationMethod classificationMethod ON classificationMethod.ClassificationMethodGuid = surfaceDefaults.ClassificationMethodGuid", Schema)
            sb.Add("WHERE surfaceDefaults.SurfaceDefaultsGuid = '{0}'", surfaceDefaultsGuid)

            Dim row As DataRow = GetDataTable(sb.Sql).Rows(0)

            result.SurfaceDefaultsGuid = row("SurfaceDefaultsGuid").ToString
            result.SystemAttributeGuid = row("SystemAttributeGuid").ToString
            result.SystemAttributeName = row("SystemAttributeName").ToString
            result.ColorRampGuid = row("ColorRampGuid").ToString
            result.ColorRampName = row("ColorRampName").ToString
            result.ClassificationMethodGuid = row("ClassificationMethodGuid").ToString
            result.ClassificationMethodName = row("ClassificationMethodName").ToString
            result.NumberOfClasses = row("NumberOfClasses").ToString
            result.OrgLevelList = GetOrgLevelList(result.SurfaceDefaultsGuid)
            Return result
        End Function

        Public Shared Sub GetSurfaceDefaultsSelectAllList(request As SurfaceDefaultsRequest, loggedInUserGuid As String, apiResult As ApiResult)
            request.SurfaceDefaultsPageOptions.Skip = 0
            request.SurfaceDefaultsPageOptions.PageSize = PageOptions.MaxPageSize

            Dim sortOrder As String = GetSortOrderSql(apiResult, request.SurfaceDefaultsSort, loggedInUserGuid)
            Dim sql As String = GetSurfaceDefaultsListSql(request, sortOrder, loggedInUserGuid)
            Dim model As New List(Of String)

            Dim dt = GetDataTable(sql)
            For Each row As DataRow In dt.Rows
                model.Add(row("SurfaceDefaultsGuid").ToString)
            Next

            apiResult.Model = model
            apiResult.Success = True

        End Sub

        Private Shared Function GetOrgLevelList(surfaceDefaultsGuid As String) As List(Of SurfaceDefaultsOrgLevel)
            Dim sb As New SqlBuilder
            Dim result As New List(Of SurfaceDefaultsOrgLevel)

            sb.Add("SELECT SurfaceDefaultsOrgLevelGuid, SurfaceDefaultsOrgLevel.OrgLevelGuid, OrgLevel.Name, City, Abbreviation, OrgLevel.ParentOrgLevelGuid")
            sb.Add("FROM {0}SurfaceDefaultsOrgLevel SurfaceDefaultsOrgLevel", Schema)
            sb.Add("INNER JOIN {0}OrgLevel OrgLevel ON OrgLevel.OrgLevelGuid = SurfaceDefaultsOrgLevel.OrgLevelGuid", Schema)
            sb.Add("INNER JOIN {0}OrgLevelAddress OrgLevelAddress ON OrgLevelAddress.OrgLevelGuid = OrgLevel.OrgLevelGuid", Schema)
            sb.Add("INNER JOIN {0}Address_EVW Address ON Address.AddressGuid = OrgLevelAddress.AddressGuid", SpatialSchema)
            sb.Add("INNER JOIN {0}State State ON State.StateGuid = Address.StateGuid", Schema)
            sb.Add("WHERE SurfaceDefaultsGuid = '{0}'", surfaceDefaultsGuid)
            sb.Add("AND OrgLevelAddress.IsPrimary = 1")
            sb.Add("ORDER BY Name")

            For Each row As DataRow In GetDataTable(sb.Sql).Rows
                Dim item As New SurfaceDefaultsOrgLevel
                item.SurfaceDefaultsGuid = surfaceDefaultsGuid
                item.SurfaceDefaultsOrgLevelGuid = row("SurfaceDefaultsOrgLevelGuid").ToString
                item.OrgLevelGuid = row("OrgLevelGuid").ToString
                item.OrgLevelName = row("Name").ToString
                item.City = row("City").ToString
                item.StateAbbreviation = row("Abbreviation").ToString
                item.ParentOrgLevelGuid = row("ParentOrgLevelGuid").ToString()
                result.Add(item)
            Next

            Return result
        End Function
        Public Shared Sub GetCompanyList(loggedInUserGuid As String, apiResult As ApiResult)
            apiResult.Model = GetCompanyList(loggedInUserGuid)
            apiResult.Success = True
        End Sub
        Public Shared Function GetCompanyList(loggedInUserGuid As String) As List(Of ListItem)
            Dim result As New List(Of ListItem)
            Dim userCompanyList As New List(Of UserCompany)
            userCompanyList = HierarchyZapperVm.GetUserCompanyList(loggedInUserGuid)
            For Each company In userCompanyList
                'If the companyGuid is in the list already do not add it to the dropdown list
                If result.Exists(Function(x) x.Guid.ToLower() = company.CompanyGuid.ToLower()) Then
                    Continue For
                End If
                Dim item As New ListItem
                item.Name = company.CompanyName
                item.Guid = company.CompanyGuid
                result.Add(item)
            Next
            Return result
        End Function

        Public Shared Sub GetClassificationMethodList(apiResult As ApiResult)
            Dim result As New List(Of ListItem)

            Dim sb As New SqlBuilder
            sb.Add("SELECT ClassificationMethodGuid")
            sb.Add(", Name ")
            sb.Add("FROM {0}[CLASSIFICATIONMETHOD] ", SpatialSchema)
            sb.Add("WHERE UserSelectable = 1")
            sb.Add("ORDER BY NAME")

            For Each row As DataRow In GetDataTable(sb.Sql).Rows
                Dim item As New ListItem
                item.Guid = row("ClassificationMethodGuid").ToString
                item.Name = row("Name").ToString
                result.Add(item)
            Next

            apiResult.Success = True
            apiResult.Model = result
        End Sub

        Public Shared Sub GetSystemAttributeList(apiResult As ApiResult)
            Dim result As New List(Of ListItem)

            Dim sb As New SqlBuilder
            sb.Add("SELECT SystemAttributeGuid")
            sb.Add(", Name ")
            sb.Add("FROM {0}[SystemAttribute] ", Schema)
            sb.Add("WHERE ActiveYn = 1")
            sb.Add("AND Surface = 1")
            sb.Add("ORDER BY NAME")

            For Each row As DataRow In GetDataTable(sb.Sql).Rows
                Dim item As New ListItem
                item.Guid = row("SystemAttributeGuid").ToString
                item.Name = row("Name").ToString
                result.Add(item)
            Next

            apiResult.Success = True
            apiResult.Model = result
        End Sub

#End Region

#Region " Update Surface Defaults "

        Public Shared Function GetUpdateStatement(surfaceDefaults As SurfaceDefaults, loggedInUserGuid As String) As String
            Dim sb As New SqlBuilder

            sb.Add("UPDATE {0}SurfaceDefaults ", Schema)
            sb.Add("SET SystemAttributeGuid = '{0}'", surfaceDefaults.SystemAttributeGuid)
            sb.Add(",ColorRampGuid = '{0}'", surfaceDefaults.ColorRampGuid)
            sb.Add(",ClassificationMethodGuid = '{0}'", surfaceDefaults.ClassificationMethodGuid)
            sb.Add(",NumberOfClasses = '{0}'", surfaceDefaults.NumberOfClasses)
            sb.Add(",ModifiedDate = '{0}'", FormatCurrentDateTime())
            sb.Add(",ModifiedByGuid = '{0}'", loggedInUserGuid)
            sb.Add("WHERE [SurfaceDefaultsGuid] = '{0}'", surfaceDefaults.SurfaceDefaultsGuid)

            Return sb.Sql
        End Function

        Public Shared Sub UpdateSurfaceDefaults(surfaceDefaults As SurfaceDefaults, loggedInUserGuid As String, apiResult As ApiResult)
            If Valid(surfaceDefaults, loggedInUserGuid, apiResult, New List(Of String)) Then
                Dim list As New List(Of String)
                list.Add(GetUpdateStatement(surfaceDefaults, loggedInUserGuid))
                list.AddRange(GetSurfaceDefaultsOrgLevelSql(surfaceDefaults))
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
        Public Shared Sub GetAutoSearchList(request As SurfaceDefaultsAutoCompleteRequest, loggedInUserGuid As String, apiResult As ApiResult)

            If ValidGetAutoSearchList(request, apiResult) = False Then
                Return
            End If

            request.SearchString = RemoveExtraSpaces(request.SearchString)

            Dim selectColumn As String = String.Empty

            Select Case request.SearchName
                Case SurfaceDefaultsRequest.FieldSystemAttributeName
                    selectColumn = "sa.Name"
                Case SurfaceDefaultsRequest.FieldColorRampName
                    selectColumn = "cr.Name"
                Case SurfaceDefaultsRequest.FieldClassificationMethodName
                    selectColumn = "cm.Name"
                Case SurfaceDefaultsRequest.FieldNumberOfClasses
                    selectColumn = "SurfaceDefaults.NumberOfClasses"
                Case SurfaceDefaultsRequest.FieldLocationLevelName
                    selectColumn = GetOrgLevelListSelect(loggedInUserGuid)
                Case SurfaceDefaultsRequest.FieldModifiedDate
                    selectColumn = "sd.ModifiedDate"
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
        Public Shared Function GetAutoSearchSql(selectColumn As String, request As SurfaceDefaultsAutoCompleteRequest, loggedInUserGuid As String) As String
            Dim sb As New SqlBuilder

            sb.Add("SELECT DISTINCT ")
            sb.Add("{0} AS returnValue ", PadTic.No, selectColumn)
            sb.Add("FROM  {0}SurfaceDefaults SurfaceDefaults", Schema)
            sb.Add("INNER JOIN {0}SystemAttribute sa ON SurfaceDefaults.systemAttributeGuid = sa.systemAttributeGuid ", Schema)
            sb.Add("INNER JOIN {0}ColorRamp cr ON SurfaceDefaults.ColorRampGuid = cr.ColorRampGuid ", Schema)
            sb.Add("INNER JOIN {0}ClassificationMethod cm ON SurfaceDefaults.ClassificationMethodGuid = cm.ClassificationMethodGuid ", Schema)
            sb.Add("INNER JOIN {0}SurfaceDefaultsOrgLevel pao ON pao.SurfaceDefaultsGuid = SurfaceDefaults.SurfaceDefaultsGuid ", Schema)
            sb.Add("WHERE ({0}", PadTic.No, selectColumn)
            sb.Add(" LIKE '{0}%'", request.SearchString)
            sb.Add(" OR {0}", PadTic.No, selectColumn)
            sb.Add(" LIKE '% {0}%')", request.SearchString)
            sb.Add(" AND EXISTS (SELECT * FROM {0}GetUserHierarchyAccess('{1}') WHERE OrgLevelGuid = pao.OrgLevelGuid)", Schema, loggedInUserGuid)
            sb.Add(GetFilterSql(request.SurfaceDefaultsFilter, loggedInUserGuid))

            Return sb.Sql

        End Function

        Public Shared Function ValidGetAutoSearchList(request As SurfaceDefaultsAutoCompleteRequest, apiResult As ApiResult) As Boolean
            Dim count As Integer = apiResult.ErrorCodeList.Count

            Select Case request.SearchName
                Case SurfaceDefaultsRequest.FieldSystemAttributeName,
                    SurfaceDefaultsRequest.FieldColorRampName,
                    SurfaceDefaultsRequest.FieldClassificationMethodName,
                    SurfaceDefaultsRequest.FieldLocationLevelName,
                    SurfaceDefaultsRequest.FieldModifiedDate,
                    SurfaceDefaultsRequest.FieldNumberOfClasses
                    'success
                Case Else
                    apiResult.ErrorCodeList.Add(ApiResultVm.ErrorCode.SearchNameInvalid)
            End Select

            Return count = apiResult.ErrorCodeList.Count
        End Function

#End Region

#Region " Delete Surface Defaults "
        Public Shared Sub DeleteSurfaceDefaults(surfaceDefaultsGuid As String, loggedInUserGuid As String, apiResult As ApiResult)
            If ValidDelete(surfaceDefaultsGuid, loggedInUserGuid, apiResult) Then
                Dim list As New List(Of String)
                list.Add(GetDeleteSurfaceDefaultsOrgLevelSql(surfaceDefaultsGuid, loggedInUserGuid))
                Execute(list)
                apiResult.Success = True
            End If
        End Sub

        Public Shared Sub DeleteSurfaceDefaultsList(rows As List(Of String), loggedInUserGuid As String, apiResult As ApiResult)
            Dim list As New List(Of String)
            If ValidDeleteSurfaceDefaultsList(rows, loggedInUserGuid, apiResult) Then
                For Each item In rows
                    If RecordInUse(TableEnum.SurfaceDefaults, item) = False Then
                        list.Add(GetDeleteSurfaceDefaultsOrgLevelSql(item, loggedInUserGuid))
                    End If
                Next
                If list.Count > 0 Then
                    Execute(list)
                End If
                apiResult.Success = True
            End If
        End Sub

        Private Shared Function GetDeleteSurfaceDefaultsOrgLevelSql(surfaceDefaultsGuid As String, loggedInUserGuid As String) As String
            Dim sb As New SqlBuilder

            sb.Add("DELETE FROM {0}SurfaceDefaultsOrgLevel", Schema)
            sb.Add("WHERE SurfaceDefaultsGuid = '{0}'", surfaceDefaultsGuid)

            Return sb.Sql
        End Function

        Private Shared Function GetDeleteSurfaceDefaultsSql(surfaceDefaultsGuid As String, loggedInUserGuid As String) As String
            Dim sb As New SqlBuilder

            sb.Add("DELETE FROM {0}SurfaceDefaults", Schema)
            sb.Add("WHERE SurfaceDefaultsGuid = '{0}'", surfaceDefaultsGuid)

            Return sb.Sql
        End Function

        Public Shared Function ValidDelete(surfaceDefaultsGuid As String, loggedInUserGuid As String, apiResult As ApiResult) As Boolean
            Dim count As Integer = apiResult.ErrorCodeList.Count

            If UserRoleVm.AllowedUser(SoftwareFunctionVm.FunctionName.SurfaceDefaults, loggedInUserGuid) = False Then
                apiResult.ErrorCodeList.Add(ApiResultVm.ErrorCode.ApiAccessDenied)
                Return False
            End If

            If RecordInUse(TableEnum.SurfaceDefaults, surfaceDefaultsGuid) Then
                apiResult.ErrorCodeList.Add(ApiResultVm.ErrorCode.RecordInUse)
            End If

            Return count = apiResult.ErrorCodeList.Count
        End Function

        Private Shared Function ValidDeleteSurfaceDefaultsList(list As List(Of String), loggedInUserGuid As String, apiResult As ApiResult) As Boolean
            Dim count As Integer = apiResult.ErrorCodeList.Count

            If UserRoleVm.AllowedUser(SoftwareFunctionVm.FunctionName.SurfaceDefaults, loggedInUserGuid) = False Then
                apiResult.ErrorCodeList.Add(ApiResultVm.ErrorCode.ApiAccessDenied)
                Return False
            End If

            Return count = apiResult.ErrorCodeList.Count
        End Function

#End Region

#Region "Import"
        Public Shared Function ValidImportSurfaceDefaultsList(list As List(Of SurfaceDefaultsGridRow), loggedInUserGuid As String, apiResult As ApiResult) As Boolean
            Dim count As Integer = apiResult.ErrorCodeList.Count

            If UserRoleVm.AllowedUser(SoftwareFunctionVm.FunctionName.SurfaceDefaults, loggedInUserGuid) = False Then
                apiResult.ErrorCodeList.Add(ApiResultVm.ErrorCode.ApiAccessDenied)
                Return False
            End If

            If list Is Nothing OrElse list.Count < 1 Then
                apiResult.ErrorCodeList.Add(ApiResultVm.ErrorCode.AgBytesUpdateRowRequired)
                Return False
            End If

            Dim errorList As New List(Of String)

            For Each row In list
                ValidSurfaceDefaults(row, loggedInUserGuid, apiResult, errorList)
            Next

            Return GetImportApiResult(count, apiResult, errorList)
        End Function

        Public Shared Sub ValidSurfaceDefaultsImport(base64Text As String, loggedInUserGuid As String, apiResult As ApiResult)
            Dim surfaceDefaultsList As New List(Of SurfaceDefaultsGridRow)
            If ParseFile(base64Text, surfaceDefaultsList, apiResult) Then
                If ValidImportSurfaceDefaultsList(surfaceDefaultsList, loggedInUserGuid, apiResult) Then
                    Dim result As New ImportStatistics

                    For Each item As SurfaceDefaultsGridRow In surfaceDefaultsList
                        If String.IsNullOrEmpty(item.SurfaceDefaultsGuid) Then
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

        Public Shared Sub SurfaceDefaultsImport(base64Text As String, loggedInUserGuid As String, apiResult As ApiResult)
            Dim surfaceDefaultsList As New List(Of SurfaceDefaultsGridRow)
            If ParseFile(base64Text, surfaceDefaultsList, apiResult) Then
                Dim addCount As Integer
                Dim updateCount As Integer
                Dim list As New List(Of String)

                For Each item As SurfaceDefaultsGridRow In surfaceDefaultsList
                    If String.IsNullOrEmpty(item.SurfaceDefaultsGuid) Then
                        addCount += 1
                        item.SurfaceDefaultsGuid = GetNewGuid()
                        list.Add(GetSurfaceDefaultsInsertSql(item, loggedInUserGuid))
                    Else
                        updateCount += 1
                        list.Add(GetSurfaceDefaultsUpdateSql(item, loggedInUserGuid))
                    End If
                    list.AddRange(GetSurfaceDefaultsOrgLevelSql(item, loggedInUserGuid))
                Next

                list.Add(AgBytesLogVm.GetImportLogSql(AgBytesLogVm.Feature.SurfaceDefaults, addCount, updateCount, loggedInUserGuid))
                Execute(list)

                apiResult.Success = True

            End If

        End Sub

        Public Shared Function GetSurfaceDefaultsOrgLevelSql(item As SurfaceDefaultsGridRow, loggedInUserGuid As String) As List(Of String)
            Dim orgLevelList() As String = item.OrgLevelId.Split(";"c)

            Dim surfaceDefaults = New SurfaceDefaults
            surfaceDefaults.SurfaceDefaultsGuid = item.SurfaceDefaultsGuid
            surfaceDefaults.OrgLevelList = New List(Of SurfaceDefaultsOrgLevel)
            For i = 0 To orgLevelList.Count - 1
                surfaceDefaults.OrgLevelList.Add(New SurfaceDefaultsOrgLevel() With {
                    .OrgLevelGuid = OrgLevelVm.GetOrgLevelGuid(orgLevelList(i), UserVm.GetCompanyGuid(loggedInUserGuid))
                })
            Next
            Return GetSurfaceDefaultsOrgLevelSql(surfaceDefaults)
        End Function

        Public Shared Function GetSurfaceDefaultsInsertSql(surfaceDefaults As SurfaceDefaultsGridRow, loggedInUserGuid As String) As String
            Dim sb As New SqlBuilder
            sb.Add("INSERT INTO {0}SurfaceDefaults (", Schema)
            sb.Add("SurfaceDefaultsGuid")
            ' sb.Add(",CompanyGuid")
            sb.Add(",SystemAttributeGuid")
            sb.Add(",ColorRampGuid")
            sb.Add(",ClassificationMethodGuid")
            sb.Add(",NumberOfClasses")
            sb.Add(",ModifiedDate")
            sb.Add(",RecordOwnerGuid")
            sb.Add(",CreatedByGuid")
            sb.Add(",CreatedDate")
            sb.Add(") VALUES (")
            sb.Add("'{0}'", surfaceDefaults.SurfaceDefaultsGuid)
            sb.Add(",'{0}'", UserVm.GetUserCompany(loggedInUserGuid))
            sb.Add("," & FormatKeyOrNull(If(String.IsNullOrEmpty(surfaceDefaults.SystemAttributeName), String.Empty, SampleAttributeVm.GetSystemAttributeGuid(surfaceDefaults.SystemAttributeName))))
            sb.Add("," & FormatKeyOrNull(If(String.IsNullOrEmpty(surfaceDefaults.ColorRampName), String.Empty, GetColorRampGuid(surfaceDefaults.ColorRampName))))
            sb.Add("," & FormatKeyOrNull(If(String.IsNullOrEmpty(surfaceDefaults.ClassificationMethodName), String.Empty, GetClassificationMethodGuid(surfaceDefaults.ClassificationMethodName))))
            sb.Add("'{0}'", surfaceDefaults.NumberOfClasses)
            sb.Add(",{0}", FormatCurrentDateTime())
            sb.Add(",'{0}'", loggedInUserGuid)
            sb.Add(",'{0}'", loggedInUserGuid)
            sb.Add(",{0}", FormatCurrentDateTime())
            sb.Add(")")

            Return sb.Sql

        End Function

        Public Shared Function GetColorRampGuid(colorRampName As String) As String
            Dim sb As New SqlBuilder
            sb.Add("SELECT ColorRampGuid")
            sb.Add("FROM {0}ColorRamp", Schema)
            sb.Add("WHERE Name = '{0}'", colorRampName)
            Return GetDataTable(sb.Sql).Rows(0)(0).ToString
        End Function

        Public Shared Function GetClassificationMethodGuid(classificationMethodName As String) As String
            Dim sb As New SqlBuilder
            sb.Add("SELECT ClassificationMethodGuid")
            sb.Add("FROM {0}ClassificationMethod", Schema)
            sb.Add("WHERE Name = '{0}'", classificationMethodName)
            Return GetDataTable(sb.Sql).Rows(0)(0).ToString
        End Function

        Public Shared Function GetSurfaceDefaultsUpdateSql(surfaceDefaults As SurfaceDefaultsGridRow, loggedInUserGuid As String) As String
            Dim sb As New SqlBuilder
            sb.Add("UPDATE {0}SurfaceDefaults", Schema)
            sb.Add("SET SystemAttributeGuid = " & FormatKeyOrNull(If(String.IsNullOrEmpty(surfaceDefaults.SystemAttributeName), String.Empty, SampleAttributeVm.GetSystemAttributeGuid(surfaceDefaults.SystemAttributeName))))
            sb.Add(", ColorRampGuid = " & FormatKeyOrNull(If(String.IsNullOrEmpty(surfaceDefaults.ColorRampName), String.Empty, GetColorRampGuid(surfaceDefaults.ColorRampName))))
            sb.Add(", ClassificationMethodGuid = " & FormatKeyOrNull(If(String.IsNullOrEmpty(surfaceDefaults.ClassificationMethodName), String.Empty, GetClassificationMethodGuid(surfaceDefaults.ClassificationMethodName))))
            sb.Add(", NumberOfClasses =  '{0}'", surfaceDefaults.NumberOfClasses)
            sb.Add(",ModifiedDate = {0}", FormatCurrentDateTime)
            sb.Add(",ModifiedByGuid = '{0}'", loggedInUserGuid)
            sb.Add("WHERE SurfaceDefaultsGuid = '{0}'", surfaceDefaults.SurfaceDefaultsGuid)

            Return sb.Sql

        End Function


        Public Shared Function ParseFile(base64Text As String, list As List(Of SurfaceDefaultsGridRow), apiResult As ApiResult) As Boolean
            Try
                Return TryParseFile(base64Text, list, apiResult)
            Catch ex As Exception
                apiResult.ErrorCodeList.Add(ApiResultVm.ErrorCode.AgBytesFileFormatInvalid)
                Return False
            End Try
        End Function

        Public Shared Function TryParseFile(base64Text As String, list As List(Of SurfaceDefaultsGridRow), apiResult As ApiResult) As Boolean
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

        Public Shared Function GetFileRow(row() As String, baseFieldCount As Integer, reservedColumnCount As Integer, apiResult As ApiResult) As SurfaceDefaultsGridRow
            Dim item As New SurfaceDefaultsGridRow
            Dim baseIndex = 0

            'If we have the extra columns, then reset the Guid flag
            If row.Count = baseFieldCount + reservedColumnCount Then
                item.SurfaceDefaultsGuid = row(0)
                baseIndex = reservedColumnCount
            End If

            Dim i = baseIndex
            item.SystemAttributeName = row(i)
            i += 1 : item.ColorRampName = row(i)
            i += 1 : item.ClassificationMethodName = row(i)
            i += 1 : item.NumberOfClasses = row(i)
            i += 1 : item.OrgLevelId = GetImportDelimitedList(row(i))
            i += 1 : item.OrgLevelName = GetImportDelimitedList(row(i))

            Return item
        End Function

        Private Shared Function SurfaceDefaultsGuidExists(surfaceDefaultsGuid As String) As Boolean
            Dim sb As New SqlBuilder
            sb.Add("SELECT SurfaceDefaultsGuid")
            sb.Add("FROM {0}SurfaceDefaults", Schema)
            sb.Add("WHERE SurfaceDefaultsGuid = '{0}'", surfaceDefaultsGuid)
            Return GetDataTable(sb.Sql).Rows.Count > 0
        End Function

        Private Shared Function ValidSurfaceDefaults(gridRow As SurfaceDefaultsGridRow, loggedInUserGuid As String, apiResult As ApiResult, errorList As List(Of String)) As Boolean
            Dim count As Integer = apiResult.ErrorCodeList.Count

            Dim orgLvlList() As String = gridRow.OrgLevelId.Split(";"c)
            For Each item As String In orgLvlList
                CheckForNegative(item, "Hierarchy Level ID", errorList, apiResult)
            Next
            If String.IsNullOrEmpty(gridRow.SurfaceDefaultsGuid) = False Then
                If ValidGuidFormat(gridRow.SurfaceDefaultsGuid) = False OrElse SurfaceDefaultsGuidExists(gridRow.SurfaceDefaultsGuid) = False Then
                    apiResult.ErrorCodeList.Add(ApiResultVm.ErrorCode.AgBytesGuidInvalid)
                    AddError(errorList, "Surface Defaults guid is invalid: {0}", gridRow.SurfaceDefaultsGuid)
                    Return False
                End If
            End If

            If String.IsNullOrEmpty(gridRow.SystemAttributeName) Then
                apiResult.ErrorCodeList.Add(ApiResultVm.ErrorCode.AgBytesSystemAttributeRequired)
                AddError(errorList, "System Attribute is required.")
            Else
                If SystemAttributeExists(gridRow.SystemAttributeName, SampleAttributeVm.GetSystemAttributeGuid(gridRow.SystemAttributeName)) = False Then
                    apiResult.ErrorCodeList.Add(ApiResultVm.ErrorCode.AgBytesSystemAttributeInvalid)
                    AddError(errorList, "System Attribute is invalid: {0}", gridRow.SystemAttributeName)
                End If
            End If

            If String.IsNullOrEmpty(gridRow.ColorRampName) Then
                apiResult.ErrorCodeList.Add(ApiResultVm.ErrorCode.ColorRampNameRequired)
                AddError(errorList, "Color Ramp Name is required.")
            Else
                If ColorRampExists(gridRow.ColorRampName, GetColorRampGuid(gridRow.ColorRampName)) = False Then
                    apiResult.ErrorCodeList.Add(ApiResultVm.ErrorCode.ColorRampNameInvalid)
                    AddError(errorList, "Color Ramp Name is invalid: {0}", gridRow.ColorRampName)
                End If
            End If


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

            If TextBuilder.HasDuplicates(gridRow.OrgLevelId, TextBuilder.DelimiterType.Semicolon) Then
                apiResult.ErrorCodeList.Add(ApiResultVm.ErrorCode.AgBytesLocationIdListHasDuplicates)
                AddError(errorList, "Location ID list has duplicates: {0}", gridRow.OrgLevelId)
            End If
            'Check the length of the crop, crop purpose and growth stage lists

            'if no errors check 
            If DuplicateSurfaceDefaultsExists(gridRow, loggedInUserGuid) Then
                apiResult.ErrorCodeList.Add(ApiResultVm.ErrorCode.SurfaceDefaultsNotUnique)
                AddError(errorList, "Surface Default is not unique: {0} + {1}", gridRow.SystemAttributeName, gridRow.OrgLevelName)
            End If
            Return count = apiResult.ErrorCodeList.Count

        End Function

        Public Shared Function SystemAttributeExists(systemAttributeName As String, ignoreGuid As String) As Boolean
            Dim sb As New SqlBuilder
            sb.Add("SELECT SystemAttributeGuid")
            sb.Add("FROM {0}SystemAttribute", Schema)
            sb.Add("WHERE Name = '{0}'", systemAttributeName)
            If String.IsNullOrEmpty(ignoreGuid) = False Then
                sb.Add("AND SystemAttributeGuid <> '{0}'", ignoreGuid)
            End If
            Return GetDataTable(sb.Sql).Rows.Count > 0
        End Function

        Public Shared Function ColorRampExists(colorRampName As String, ignoreGuid As String) As Boolean
            Dim sb As New SqlBuilder
            sb.Add("SELECT ColorRampGuid")
            sb.Add("FROM {0}ColorRamp", Schema)
            sb.Add("WHERE Name = '{0}'", colorRampName)
            If String.IsNullOrEmpty(ignoreGuid) = False Then
                sb.Add("AND ColorRampGuid <> '{0}'", ignoreGuid)
            End If
            Return GetDataTable(sb.Sql).Rows.Count > 0
        End Function

        Public Shared Function DuplicateSurfaceDefaultsExists(surfaceDefaultsGridRow As SurfaceDefaultsGridRow, loggedInUserGuid As String) As Boolean
            Dim sql = GetDuplicateSurfaceDefaultsExistsSql(surfaceDefaultsGridRow, loggedInUserGuid)
            Return GetDataTable(sql).Rows.Count > 0
        End Function

        Public Shared Function GetDuplicateSurfaceDefaultsExistsSql(surfaceDefaultsGridRow As SurfaceDefaultsGridRow, loggedInUserGuid As String) As String
            Dim sb As New SqlBuilder
            ' TODO fix this sql
            Dim listSql As New List(Of String)
            sb.Add("SELECT sd.SurfaceDefaultsGuid")
            sb.Add(" FROM {0}SurfaceDefaults sd", Schema)
            sb.Add("LEFT JOIN {0}SurfaceDefaultsCrop pac ON pac.SurfaceDefaultsGuid = pa.SurfaceDefaultsGuid", Schema)
            sb.Add(" WHERE pa.SystemAttributeGuid = '{0}'", SampleAttributeVm.GetSystemAttributeGuid(surfaceDefaultsGridRow.SystemAttributeName))

            sb.Add(GetListsSql(surfaceDefaultsGridRow, loggedInUserGuid))
            ' On update, ignore existing record
            If StringHasValue(surfaceDefaultsGridRow.SurfaceDefaultsGuid) Then
                sb.Add(" AND pa.SurfaceDefaultsGuid <> '{0}'", surfaceDefaultsGridRow.SurfaceDefaultsGuid)
            End If

            Return sb.Sql
        End Function

        Public Shared Function GetListsSql(surfaceDefaultsGridRow As SurfaceDefaultsGridRow, loggedInUserGuid As String) As String
            Dim sb As New SqlBuilder
            Dim orgIdx = 0

            'split the lists
            Dim orgLevelIdList() As String = surfaceDefaultsGridRow.OrgLevelId.Split(";"c)

            ' Loop through Required lists
            For Each orgLevelId In orgLevelIdList
                If orgIdx = 0 Then
                    sb.Add(" AND (")
                End If
                sb.Add(" Exists(")
                sb.Add(" SELECT sdo.OrgLevelGuid")
                sb.Add(" FROM {0}SurfaceDefaultsOrgLevel sdo", Schema)
                ' sb.Add("LEFT JOIN {0}SurfaceDefaultsCrop pac ON pac.SurfaceDefaultsGuid = pa.SurfaceDefaultsGuid", Schema)
                sb.Add(" WHERE sdo.SurfaceDefaultsGuid = sd.SurfaceDefaultsGuid")
                sb.Add(" AND OrgLevelGuid = '{0}'", OrgLevelVm.GetOrgLevelGuid(orgLevelId, UserVm.GetCompanyGuid(loggedInUserGuid)))
                sb.Add(" ))")
                If orgIdx < orgLevelIdList.Length - 1 Then
                    sb.Add(" OR (")
                End If
                orgIdx = orgIdx + 1
            Next

            Return sb.Sql
        End Function
#End Region

#Region " Export "

        Public Shared Sub ExportSurfaceDefaultsList(list As List(Of String), loggedInUserGuid As String, apiResult As ApiResult)

            Dim tb As New TextBuilder

            tb.Add("SurfaceDefaultsGuid")
            tb.Add("System Attribute")
            tb.Add("Color Ramp")
            tb.Add("Classification Method")
            tb.Add("Number of Classes")
            tb.Add("Hierarchy Level ID 0-n")
            tb.Add("Hierarchy Level Name 0-n")

            tb.EndLine()

            For Each item As String In list
                Dim row As SurfaceDefaultsGridRow = GetSurfaceDefaultsGridRow(item)
                tb.Add(row.SurfaceDefaultsGuid)
                tb.Add(row.SystemAttributeName)
                tb.Add(row.ColorRampName)
                tb.Add(row.ClassificationMethodName)
                tb.Add(row.NumberOfClasses)
                tb.Add(row.OrgLevelId)
                tb.Add(row.OrgLevelName)

                tb.EndLine()
            Next

            AgBytesLogVm.WriteExportLog(AgBytesLogVm.Feature.SurfaceDefaults, list.Count, loggedInUserGuid)
            Dim fileDataStream As New MemoryStream(Convert.FromBase64String(tb.Base64String))

            SetupDataStreamToExportFolder(loggedInUserGuid, "SurfaceDefaults", fileDataStream, apiResult)

        End Sub

        Public Shared Function GetSurfaceDefaultsGridRow(surfaceDefaultsGuid As String) As SurfaceDefaultsGridRow
            Dim result As New SurfaceDefaultsGridRow
            Dim sb As New SqlBuilder

            sb.Add("SELECT SystemAttribute.Name AS SystemAttributeName")
            sb.Add(",ColorRamp.Name As ColorRampName")
            sb.Add(",ClassificationMethod.Name As ClassificationMethodName")
            sb.Add("FROM {0}SurfaceDefaults SurfaceDefaults", Schema)
            sb.Add("LEFT JOIN {0}SystemAttribute SystemAttribute ON SystemAttribute.SystemAttributeGuid = SurfaceDefaults.SystemAttributeGuid", Schema)
            sb.Add("LEFT JOIN {0}ColorRamp ColorRamp ON ColorRamp.ColorRampGuid = SurfaceDefaults.ColorRampGuid", Schema)
            sb.Add("LEFT JOIN {0}ClassificationMethod ClassificationMethod ON SystemAttribute.ClassificationMethodGuid = SurfaceDefaults.ClassificationMethodGuid", Schema)
            sb.Add("LEFT JOIN {0}Nutrient Nutrient ON Nutrient.NutrientGuid = SurfaceDefaults.NutrientGuid", Schema)
            sb.Add("WHERE SurfaceDefaults.SurfaceDefaultsGuid = '{0}'", surfaceDefaultsGuid)

            Dim row As DataRow = GetDataTable(sb.Sql).Rows(0)

            result.SurfaceDefaultsGuid = surfaceDefaultsGuid
            result.SystemAttributeName = row("SystemAttributeName").ToString
            result.ColorRampName = row("ColorRampName").ToString
            result.ClassificationMethodName = row("ClassificationMethodName").ToString
            result.NumberOfClasses = row("NumberOfClasses").ToString
            result.OrgLevelId = GetSurfaceDefaultsOrgLevelId(surfaceDefaultsGuid)
            result.OrgLevelName = GetSurfaceDefaultsOrgLevelName(surfaceDefaultsGuid)

            Return result
        End Function

        Public Shared Function GetSurfaceDefaultsOrgLevelId(surfaceDefaultsGuid As String) As String
            Dim tb As New TextBuilder(TextBuilder.DelimiterType.SemicolonSpace)
            Dim sb As New SqlBuilder

            sb.Add("SELECT OrgLevel.ID")
            sb.Add("FROM {0}OrgLevel", Schema)
            sb.Add("INNER JOIN {0}SurfaceDefaultsOrgLevel ON SurfaceDefaultsOrgLevel.OrgLevelGuid = OrgLevel.OrgLevelGuid", Schema)
            sb.Add("WHERE SurfaceDefaultsGuid = '{0}'", surfaceDefaultsGuid)
            sb.Add("ORDER BY OrgLevel.ID")

            For Each row As DataRow In GetDataTable(sb.Sql).Rows
                tb.Add(row("ID").ToString)
            Next

            tb.EndList()

            Return tb.Text
        End Function

        Public Shared Function GetSurfaceDefaultsOrgLevelName(surfaceDefaultsGuid As String) As String
            Dim tb As New TextBuilder(TextBuilder.DelimiterType.SemicolonSpace)
            Dim sb As New SqlBuilder

            sb.Add("SELECT OrgLevel.Name AS OrgLevelName")
            sb.Add("FROM {0}OrgLevel", Schema)
            sb.Add("INNER JOIN {0}SurfaceDefaultsOrgLevel ON SurfaceDefaultsOrgLevel.OrgLevelGuid = OrgLevel.OrgLevelGuid", Schema)
            sb.Add("WHERE SurfaceDefaultsGuid = '{0}'", surfaceDefaultsGuid)
            sb.Add("ORDER BY OrgLevel.ID")

            For Each row As DataRow In GetDataTable(sb.Sql).Rows
                tb.Add(row("OrgLevelName").ToString)
            Next

            tb.EndList()

            Return tb.Text
        End Function

#End Region
    End Class
End Namespace
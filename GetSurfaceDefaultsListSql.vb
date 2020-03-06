
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
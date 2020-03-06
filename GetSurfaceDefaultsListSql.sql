SELECT * FROM
(
    SELECT ROW_NUMBER() OVER (ORDER BY sa.Name, ISNULL((SELECT STUFF((SELECT '; ' + OrgLevel.Name FROM [nso].SurfaceDefaultsOrgLevel SurfaceDefaultsOrgLevel INNER JOIN [nso].OrgLevel OrgLevel ON OrgLevel.OrgLevelGuid = SurfaceDefaultsOrgLevel.OrgLevelGuid INNER JOIN [nso].GetHierarchyChildren('8fe61431-51a1-4701-a29a-3857deaa162e') Children ON OrgLevel.OrgLevelGuid = Children.OrgLevelGuid  WHERE SurfaceDefaultsOrgLevel.SurfaceDefaultsGuid = SurfaceDefaults.SurfaceDefaultsGuid ORDER BY OrgLevel.Name For XML Path(''), Type).value('.', 'nvarchar(max)'), 1, 2, '')), '')) AS RowNumber
        , COUNT(*) OVER () AS TotalRows
        ,surfaceDefaults.SurfaceDefaultsGuid
        ,sa.Name AS SystemAttributeName
        ,cr.Name AS ColorRampName
        ,cm.Name AS ClassificationMethodName
        ,surfaceDefaults.NumberOfClasses AS NumberOfClasses
        ,ISNULL((SELECT STUFF((SELECT '; ' + OrgLevel.Name FROM [nso].SurfaceDefaultsOrgLevel SurfaceDefaultsOrgLevel INNER JOIN [nso].OrgLevel OrgLevel ON OrgLevel.OrgLevelGuid = SurfaceDefaultsOrgLevel.OrgLevelGuid INNER JOIN [nso].GetHierarchyChildren('8fe61431-51a1-4701-a29a-3857deaa162e') Children ON OrgLevel.OrgLevelGuid = Children.OrgLevelGuid  WHERE SurfaceDefaultsOrgLevel.SurfaceDefaultsGuid = SurfaceDefaults.SurfaceDefaultsGuid ORDER BY OrgLevel.Name For XML Path(''), Type).value('.', 'nvarchar(max)'), 1, 2, '')), '') AS OrgLevelList
        ,FORMAT([nso].ConvertUtcToLocalByUser('986feb62-22ee-4309-9695-d8141cc5ce67', ISNULL(SurfaceDefaults.ModifiedDate, SurfaceDefaults.CreatedDate)), 'MM/dd/yyyy h:mm:ss tt') AS ModifiedDate
        ,CanDelete 
    FROM  [nso].SurfaceDefaults surfaceDefaults
    INNER JOIN [nso].SystemAttribute sa ON surfaceDefaults.SystemAttributeGuid = sa.SystemAttributeGuid 
    INNER JOIN [sdo].ColorRamp cr ON surfaceDefaults.ColorRampGuid = cr.ColorRampGuid 
    INNER JOIN [sdo].ClassificationMethod cm ON surfaceDefaults.ClassificationMethodGuid = cm.ClassificationMethodGuid 
        OUTER APPLY ( 
            SELECT '1' AS CanDelete 
        ) AS CanDel

    WHERE 1=1

 AND EXISTS (SELECT * FROM [nso].GetUserHierarchyAccess('986feb62-22ee-4309-9695-d8141cc5ce67') ha INNER JOIN [nso].SurfaceDefaultsOrgLevel pao ON pao.OrgLevelGuid = ha.OrgLevelGuid WHERE pao.SurfaceDefaultsGuid = surfaceDefaults.SurfaceDefaultsGuid )
) As SurfaceDefaults
WHERE RowNumber > 0
And RowNumber <= 50
ORDER BY RowNumber

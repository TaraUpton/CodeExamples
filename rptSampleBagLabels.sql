IF NOT EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'rptSampleBagLabels')
   exec('CREATE PROCEDURE [sdo].[rptSampleBagLabels] AS BEGIN SET NOCOUNT ON; END')
GO

ALTER PROCEDURE [sdo].[rptSampleBagLabels]
	@AgEventGuidList NVARCHAR(MAX)
	,@ReportUserName NVARCHAR(101)
	,@ReportGuid UNIQUEIDENTIFIER
	,@StartDate AS NVARCHAR(20) = ''
	,@EndDate AS NVARCHAR(20) = ''
	,@UserGuid AS UNIQUEIDENTIFIER
AS
BEGIN
	DECLARE @tmpAgEventGuids as TABLE (
		AgEventGuid UNIQUEIDENTIFIER
	)

	INSERT INTO @tmpAgEventGuids
	SELECT * FROM STRING_SPLIT(@AgEventGuidList, ',')
	
	SELECT DISTINCT
	SamplingEvent.EventID
	,Customer.Name AS Grower
	,Field.FarmName
	,Field.Name AS FieldName
    ,AgEvent.AgEventGuid
	,ReportImage.ImageDescription AS ImageDescription
	,SampleType.Id As SampleTypeId
	,AgEventGeneral.StartDateTime
	,SampleId AS SequenceID
	,SoilSamplePointDepth.DepthID As DepthID

	FROM sdo.AgEventGeneral_EVW AgEventGeneral
	INNER JOIN sdo.EventArea_EVW EventArea ON EventArea.AgEventGeneralGuid = AgEventGeneral.AgEventGeneralGuid
	INNER JOIN sdo.AgEvent_EVW AgEvent ON AgEvent.EventAreaGuid = EventArea.EventAreaGuid
	INNER JOIN sdo.SamplingEvent_EVW SamplingEvent ON SamplingEvent.AgEventGuid = AgEvent.AgEventGuid
	INNER JOIN sdo.Field_EVW Field ON Field.FieldGuid = AgEventGeneral.FieldGuid
	INNER JOIN sdo.Customer_EVW Customer ON Customer.CustomerGuid = Field.CustomerGuid
	INNER JOIN sdo.SoilSamplePoint_EVW SoilSamplePoint ON SoilSamplePoint.AgEventGuid = AgEvent.AgEventGuid
	INNER JOIN nso.SampleType SampleType ON SampleType.SampleTypeGuid = SamplingEvent.SampleTypeGuid
	INNER JOIN sdo.SoilSamplePointDepth_evw SoilSamplePointDepth ON SoilSamplePointDepth.SoilSamplePointGuid = SoilSamplePoint.SoilSamplePointGuid

	LEFT JOIN nso.ReportImage ReportImage on ReportImage.ReportGuid = @ReportGuid AND SamplingEvent.AgEventGuid = ReportImage.AgEventGuid
	INNER JOIN @tmpAgEventGuids tmptbl ON tmptbl.AgEventGuid = AgEvent.AgEventGuid
	OUTER APPLY (SELECT COUNT(DISTINCT SampleID) * COUNT(DISTINCT ISNULL(DepthID, '')) As SampleCount, COUNT(DISTINCT ISNULL(DepthID, '')) As DepthCount
					FROM sdo.SoilSamplePoint_EVW SoilSamplePoint
					INNER JOIN sdo.SoilSamplePointDepth_evw SoilSamplePointDepth ON SoilSamplePointDepth.SoilSamplePointGuid = SoilSamplePoint.SoilSamplePointGuid
					WHERE SoilSamplePoint.AgEventGuid = AgEvent.AgEventGuid ) AS SampleCountTable
 	WHERE AgEventGeneral.ActiveYN = 1
		AND ((ISNULL(@StartDate, '') = '' AND ISNULL(@EndDate, '') = '') OR (nso.ConvertUtcToLocalByUser(@UserGuid, AgEventGeneral.StartDateTime) BETWEEN @StartDate + ' 00:00:00' AND @EndDate + ' 23:59:59'))
	ORDER BY SamplingEvent.EventID, SampleID, Customer.Name, Field.Name, Field.FarmName
END

GO
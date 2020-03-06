Imports System.Web.Http
Imports HaasCore.Models
Imports Newtonsoft.Json.Linq
Imports HaasWebApp.Models.EntityHierarchy
Imports HaasWebApp.ViewModels.EntityHierarchy

Namespace Controllers.WebApiControllers

    <Authorize>
    Public Class SurfaceDefaultsController
        Inherits ApiController

        ''' <summary>
        ''' Add a new Surface Defaults from the Add/Edit page.
        ''' </summary>
        ''' <param name="objJson">object of Surface defaults with a LoggedInUserGuid</param>
        ''' <returns>Boolean indicating success or failure</returns>
        ''' <remarks></remarks>
        <HttpPost()>
        Public Function AddSurfaceDefaults(ByVal objJson As JObject) As ApiResult
            Dim result As New ApiResult
            Dim loggedInUserGuid As String = objJson("UserGuid").ToObject(Of String)()
            Dim surfaceDefaults As SurfaceDefaults = objJson("Model").ToObject(Of SurfaceDefaults)()

            SurfaceDefaultsVm.AddSurfaceDefaults(loggedInUserGuid, surfaceDefaults, result)

            Return result
        End Function

        ''' <summary>
        ''' Delete an existing Surface Defaults
        ''' </summary>
        ''' <param name="objJson"></param>
        ''' <returns>Boolean indicating success or failure</returns>
        ''' <remarks></remarks>
        <HttpDelete()>
        Public Function DeleteSurfaceDefaults(ByVal objJson As JObject) As ApiResult
            Dim result As New ApiResult
            Dim loggedInUserGuid As String = objJson("UserGuid").ToObject(Of String)()
            Dim surfaceDefaultsGuid As String = objJson("Model").ToString()

            SurfaceDefaultsVm.DeleteSurfaceDefaults(surfaceDefaultsGuid, loggedInUserGuid, result)

            Return result

        End Function

        ''' <summary>
        ''' Delete the SurfaceDefaults in the list that are not in use.
        ''' </summary>
        ''' <param name="objJson">Model = List(Of SurfaceDefaultsGridRow)</param>
        ''' <returns>A string denoting the success or failure of the delete.</returns>
        ''' <remarks></remarks>
        <HttpDelete()>
        Public Function DeleteSurfaceDefaultsList(ByVal objJson As JObject) As ApiResult
            Dim result As New ApiResult
            Dim loggedInUserGuid As String = objJson("UserGuid").ToObject(Of String)()
            Dim surfaceDefaultsList As List(Of String) = objJson("Model").ToObject(Of List(Of String))()

            SurfaceDefaultsVm.DeleteSurfaceDefaultsList(surfaceDefaultsList, loggedInUserGuid, result)

            Return result
        End Function

        ''' <summary>
        ''' Validate the SurfaceDefaults file to be imported, return the number of records to be added and updated
        ''' </summary>
        ''' <param name="objJson"></param>
        ''' <returns> Api result success or error code</returns>
        ''' <remarks></remarks>
        <HttpPost>
        Public Function ValidSurfaceDefaultsImport(ByVal objJson As JObject) As ApiResult
            Dim result As New ApiResult
            Dim loggedInUserGuid As String = objJson("UserGuid").ToString
            Dim base64Text As String = objJson("Model").ToString

            SurfaceDefaultsVm.ValidSurfaceDefaultsImport(base64Text, loggedInUserGuid, result)

            Return result
        End Function

        ''' <summary>
        ''' Import a SurfaceDefaults file into the database
        ''' </summary>
        ''' <param name="objJson"></param>
        ''' <returns> Api result success or error code</returns>
        ''' <remarks></remarks>
        <HttpPost>
        Public Function SurfaceDefaultsImport(ByVal objJson As JObject) As ApiResult
            Dim result As New ApiResult
            Dim loggedInUserGuid As String = objJson("UserGuid").ToString
            Dim base64Text As String = objJson("Model").ToString

            SurfaceDefaultsVm.SurfaceDefaultsImport(base64Text, loggedInUserGuid, result)

            Return result
        End Function

        ''' <summary>
        ''' Export the list of Surface Defaults to an S3 bucket
        ''' </summary>
        ''' <param name="objJson"></param>
        ''' <returns> Api result success containing the URL and S3 info or error code on failure</returns>
        ''' <remarks></remarks>
        <HttpPost>
        Public Function ExportSurfaceDefaultsList(ByVal objJson As JObject) As ApiResult
            Dim result As New ApiResult
            Dim loggedInUserGuid As String = objJson("UserGuid").ToString
            Dim model As List(Of String) = objJson("Model").ToObject(Of List(Of String))()

            SurfaceDefaultsVm.ExportSurfaceDefaultsList(model, loggedInUserGuid, result)

            Return result
        End Function

        ''' <summary>
        ''' Returns a list for the autocomplete searches in the grid
        ''' </summary>
        ''' <param name="surfaceDefaultsAutoCompleteRequest">object of SurfaceDefaultsAutoCompleteRequest</param>
        ''' <returns>ApiResult</returns>
        ''' <remarks></remarks>
        <HttpPost()>
        Public Function GetAutoSearchList(surfaceDefaultsAutoCompleteRequest As JObject) As ApiResult
            Dim result As New ApiResult
            Dim req As SurfaceDefaultsAutoCompleteRequest = surfaceDefaultsAutoCompleteRequest("Model").ToObject(Of SurfaceDefaultsAutoCompleteRequest)()

            SurfaceDefaultsVm.GetAutoSearchList(req, req.UserGuid, result)

            Return result

        End Function

        ''' <summary>
        ''' Fetch Surface Defaults object for the Add/Edit page
        ''' </summary>
        ''' <param name="objJson">
        '''  - UserGuid -- the identity of the logged in user requesting the fetch
        '''  - Model -- the surface defaults guid populated by the client
        ''' </param>
        ''' <returns>
        '''  Success - Boolean
        '''  Model - Surface Defaults Object 
        '''  Error Code List
        ''' </returns>
        ''' <remarks></remarks>
        <HttpPost>
        Public Function GetSurfaceDefaults(ByVal objJson As JObject) As ApiResult
            Dim result As New ApiResult
            Dim loggedInUserGuid As String = objJson("UserGuid").ToObject(Of String)()
            Dim surfaceDefaultsGuid As String = objJson("Model").ToObject(Of String)()

            SurfaceDefaultsVm.GetSurfaceDefaults(surfaceDefaultsGuid, loggedInUserGuid, result)

            Return result

        End Function

        ''' <summary>
        ''' To get a list of Surface Defaults for the grid.
        ''' </summary>
        ''' <param name="objJson">Model=SurfaceDefaultsRequest object</param>
        ''' <returns>
        ''' ApiResult.Model=GridPageResponse with list of SurfaceDefaultsGridRows in the GridRows Property)
        ''' </returns>
        ''' ErrorCodes: SortNameInvalid
        ''' <remarks></remarks>
        <HttpPost>
        Public Function GetSurfaceDefaultsList(ByVal objJson As JObject) As ApiResult
            Dim result As New ApiResult
            Dim loggedInUserGuid As String = objJson("UserGuid").ToObject(Of String)()
            Dim request As SurfaceDefaultsRequest = objJson("Model").ToObject(Of SurfaceDefaultsRequest)()

            SurfaceDefaultsVm.GetSurfaceDefaultsList(request, loggedInUserGuid, result)

            Return result

        End Function

        ''' <summary>
        ''' fetch a list of Selected Surface Defaults
        ''' </summary>
        ''' <param name="objJson">
        '''  - UserGuid -- the identity of the logged in user requesting the fetch
        '''  - Model -- the surface defaults request object populated by the client
        ''' </param>
        ''' <returns>
        '''  Success - Boolean
        '''  Model - Surface Defaults List 
        '''  Error Code List
        ''' </returns>
        ''' <remarks></remarks>
        <HttpPost>
        Public Function GetSurfaceDefaultsSelectAllList(ByVal objJson As JObject) As ApiResult
            Dim result As New ApiResult
            Dim loggedInUserGuid As String = objJson("UserGuid").ToString
            Dim surfaceDefaultsRequest As SurfaceDefaultsRequest = objJson("Model").ToObject(Of SurfaceDefaultsRequest)()

            SurfaceDefaultsVm.GetSurfaceDefaultsSelectAllList(surfaceDefaultsRequest, loggedInUserGuid, result)

            Return result
        End Function

        ''' <summary>
        ''' Update an entire Surface Defaults from the add/edit page
        ''' </summary>
        ''' <param name="objJson">
        '''  - UserGuid -- the identity of the logged in user performing the update
        '''  - Model -- the surface defaults object populated by the client
        ''' </param>
        ''' <returns>
        '''  Success - Boolean 
        '''  Error Code List -- 
        ''' </returns>
        ''' <remarks></remarks>
        <HttpPost>
        Function UpdateSurfaceDefaults(objJson As JObject) As ApiResult
            Dim result As New ApiResult()
            Dim loggedInUserGuid As String = objJson("UserGuid").ToString()
            Dim surfaceDefaults As SurfaceDefaults = objJson("Model").ToObject(Of SurfaceDefaults)()

            SurfaceDefaultsVm.UpdateSurfaceDefaults(surfaceDefaults, loggedInUserGuid, result)
            Return result
        End Function

        ''' <summary>
        ''' To get a list of Classification Methods for the add/edit page.
        ''' </summary>
        ''' <remarks></remarks>

        <HttpPost>
        Public Function GetClassificationMethodList() As ApiResult
            Dim result As New ApiResult
            SurfaceDefaultsVm.GetClassificationMethodList(result)

            Return result
        End Function


        ''' <summary>
        ''' To get a list of System Attributes for the add/edit page.
        ''' </summary>
        ''' <remarks></remarks>
        <HttpPost>
        Public Function GetSystemAttributeList(ByVal objJson As JObject) As ApiResult
            Dim result As New ApiResult
            SurfaceDefaultsVm.GetSystemAttributeList(result)

            Return result
        End Function
        ''' <summary>
        ''' fetch a list of Companies for the add/edit page
        ''' </summary>
        ''' <param name="objJson">
        '''  - UserGuid -- the identity of the logged in user requesting the fetch
        ''' </param>
        ''' <returns>
        '''  Success - Boolean
        '''  Model - Company List 
        '''  Error Code List
        ''' </returns>
        ''' <remarks></remarks>
        <HttpPost>
        Function GetCompanyList(objJson As JObject) As ApiResult
            Dim result As New ApiResult()
            Dim loggedInUserGuid As String = objJson("UserGuid").ToString()

            SurfaceDefaultsVm.GetCompanyList(loggedInUserGuid, result)
            Return result
        End Function
    End Class
End Namespace

Imports System.Web.Http
Imports HaasCore.Models
Imports Newtonsoft.Json.Linq
Imports HaasWebApp.Models.EntityHierarchy
Imports HaasWebApp.ViewModels.EntityHierarchy

Namespace Controllers.WebApiControllers

    <Authorize>
    Public Class SurfaceDefaultsController
        Inherits ApiController

        ''' <summary>
        ''' Add a new Surface Defaults from the Add/Edit page.
        ''' </summary>
        ''' <param name="objJson">object of Surface defaults with a LoggedInUserGuid</param>
        ''' <returns>Boolean indicating success or failure</returns>
        ''' <remarks></remarks>
        <HttpPost()>
        Public Function AddSurfaceDefaults(ByVal objJson As JObject) As ApiResult
            Dim result As New ApiResult
            Dim loggedInUserGuid As String = objJson("UserGuid").ToObject(Of String)()
            Dim surfaceDefaults As SurfaceDefaults = objJson("Model").ToObject(Of SurfaceDefaults)()

            SurfaceDefaultsVm.AddSurfaceDefaults(loggedInUserGuid, surfaceDefaults, result)

            Return result
        End Function

        ''' <summary>
        ''' Delete an existing Surface Defaults
        ''' </summary>
        ''' <param name="objJson"></param>
        ''' <returns>Boolean indicating success or failure</returns>
        ''' <remarks></remarks>
        <HttpDelete()>
        Public Function DeleteSurfaceDefaults(ByVal objJson As JObject) As ApiResult
            Dim result As New ApiResult
            Dim loggedInUserGuid As String = objJson("UserGuid").ToObject(Of String)()
            Dim surfaceDefaultsGuid As String = objJson("Model").ToString()

            SurfaceDefaultsVm.DeleteSurfaceDefaults(surfaceDefaultsGuid, loggedInUserGuid, result)

            Return result

        End Function

        ''' <summary>
        ''' Delete the SurfaceDefaults in the list that are not in use.
        ''' </summary>
        ''' <param name="objJson">Model = List(Of SurfaceDefaultsGridRow)</param>
        ''' <returns>A string denoting the success or failure of the delete.</returns>
        ''' <remarks></remarks>
        <HttpDelete()>
        Public Function DeleteSurfaceDefaultsList(ByVal objJson As JObject) As ApiResult
            Dim result As New ApiResult
            Dim loggedInUserGuid As String = objJson("UserGuid").ToObject(Of String)()
            Dim surfaceDefaultsList As List(Of String) = objJson("Model").ToObject(Of List(Of String))()

            SurfaceDefaultsVm.DeleteSurfaceDefaultsList(surfaceDefaultsList, loggedInUserGuid, result)

            Return result
        End Function

        ''' <summary>
        ''' Validate the SurfaceDefaults file to be imported, return the number of records to be added and updated
        ''' </summary>
        ''' <param name="objJson"></param>
        ''' <returns> Api result success or error code</returns>
        ''' <remarks></remarks>
        <HttpPost>
        Public Function ValidSurfaceDefaultsImport(ByVal objJson As JObject) As ApiResult
            Dim result As New ApiResult
            Dim loggedInUserGuid As String = objJson("UserGuid").ToString
            Dim base64Text As String = objJson("Model").ToString

            SurfaceDefaultsVm.ValidSurfaceDefaultsImport(base64Text, loggedInUserGuid, result)

            Return result
        End Function

        ''' <summary>
        ''' Import a SurfaceDefaults file into the database
        ''' </summary>
        ''' <param name="objJson"></param>
        ''' <returns> Api result success or error code</returns>
        ''' <remarks></remarks>
        <HttpPost>
        Public Function SurfaceDefaultsImport(ByVal objJson As JObject) As ApiResult
            Dim result As New ApiResult
            Dim loggedInUserGuid As String = objJson("UserGuid").ToString
            Dim base64Text As String = objJson("Model").ToString

            SurfaceDefaultsVm.SurfaceDefaultsImport(base64Text, loggedInUserGuid, result)

            Return result
        End Function

        ''' <summary>
        ''' Export the list of Surface Defaults to an S3 bucket
        ''' </summary>
        ''' <param name="objJson"></param>
        ''' <returns> Api result success containing the URL and S3 info or error code on failure</returns>
        ''' <remarks></remarks>
        <HttpPost>
        Public Function ExportSurfaceDefaultsList(ByVal objJson As JObject) As ApiResult
            Dim result As New ApiResult
            Dim loggedInUserGuid As String = objJson("UserGuid").ToString
            Dim model As List(Of String) = objJson("Model").ToObject(Of List(Of String))()

            SurfaceDefaultsVm.ExportSurfaceDefaultsList(model, loggedInUserGuid, result)

            Return result
        End Function

        ''' <summary>
        ''' Returns a list for the autocomplete searches in the grid
        ''' </summary>
        ''' <param name="surfaceDefaultsAutoCompleteRequest">object of SurfaceDefaultsAutoCompleteRequest</param>
        ''' <returns>ApiResult</returns>
        ''' <remarks></remarks>
        <HttpPost()>
        Public Function GetAutoSearchList(surfaceDefaultsAutoCompleteRequest As JObject) As ApiResult
            Dim result As New ApiResult
            Dim req As SurfaceDefaultsAutoCompleteRequest = surfaceDefaultsAutoCompleteRequest("Model").ToObject(Of SurfaceDefaultsAutoCompleteRequest)()

            SurfaceDefaultsVm.GetAutoSearchList(req, req.UserGuid, result)

            Return result

        End Function

        ''' <summary>
        ''' Fetch Surface Defaults object for the Add/Edit page
        ''' </summary>
        ''' <param name="objJson">
        '''  - UserGuid -- the identity of the logged in user requesting the fetch
        '''  - Model -- the surface defaults guid populated by the client
        ''' </param>
        ''' <returns>
        '''  Success - Boolean
        '''  Model - Surface Defaults Object 
        '''  Error Code List
        ''' </returns>
        ''' <remarks></remarks>
        <HttpPost>
        Public Function GetSurfaceDefaults(ByVal objJson As JObject) As ApiResult
            Dim result As New ApiResult
            Dim loggedInUserGuid As String = objJson("UserGuid").ToObject(Of String)()
            Dim surfaceDefaultsGuid As String = objJson("Model").ToObject(Of String)()

            SurfaceDefaultsVm.GetSurfaceDefaults(surfaceDefaultsGuid, loggedInUserGuid, result)

            Return result

        End Function

        ''' <summary>
        ''' To get a list of Surface Defaults for the grid.
        ''' </summary>
        ''' <param name="objJson">Model=SurfaceDefaultsRequest object</param>
        ''' <returns>
        ''' ApiResult.Model=GridPageResponse with list of SurfaceDefaultsGridRows in the GridRows Property)
        ''' </returns>
        ''' ErrorCodes: SortNameInvalid
        ''' <remarks></remarks>
        <HttpPost>
        Public Function GetSurfaceDefaultsList(ByVal objJson As JObject) As ApiResult
            Dim result As New ApiResult
            Dim loggedInUserGuid As String = objJson("UserGuid").ToObject(Of String)()
            Dim request As SurfaceDefaultsRequest = objJson("Model").ToObject(Of SurfaceDefaultsRequest)()

            SurfaceDefaultsVm.GetSurfaceDefaultsList(request, loggedInUserGuid, result)

            Return result

        End Function

        ''' <summary>
        ''' fetch a list of Selected Surface Defaults
        ''' </summary>
        ''' <param name="objJson">
        '''  - UserGuid -- the identity of the logged in user requesting the fetch
        '''  - Model -- the surface defaults request object populated by the client
        ''' </param>
        ''' <returns>
        '''  Success - Boolean
        '''  Model - Surface Defaults List 
        '''  Error Code List
        ''' </returns>
        ''' <remarks></remarks>
        <HttpPost>
        Public Function GetSurfaceDefaultsSelectAllList(ByVal objJson As JObject) As ApiResult
            Dim result As New ApiResult
            Dim loggedInUserGuid As String = objJson("UserGuid").ToString
            Dim surfaceDefaultsRequest As SurfaceDefaultsRequest = objJson("Model").ToObject(Of SurfaceDefaultsRequest)()

            SurfaceDefaultsVm.GetSurfaceDefaultsSelectAllList(surfaceDefaultsRequest, loggedInUserGuid, result)

            Return result
        End Function

        ''' <summary>
        ''' Update an entire Surface Defaults from the add/edit page
        ''' </summary>
        ''' <param name="objJson">
        '''  - UserGuid -- the identity of the logged in user performing the update
        '''  - Model -- the surface defaults object populated by the client
        ''' </param>
        ''' <returns>
        '''  Success - Boolean 
        '''  Error Code List -- 
        ''' </returns>
        ''' <remarks></remarks>
        <HttpPost>
        Function UpdateSurfaceDefaults(objJson As JObject) As ApiResult
            Dim result As New ApiResult()
            Dim loggedInUserGuid As String = objJson("UserGuid").ToString()
            Dim surfaceDefaults As SurfaceDefaults = objJson("Model").ToObject(Of SurfaceDefaults)()

            SurfaceDefaultsVm.UpdateSurfaceDefaults(surfaceDefaults, loggedInUserGuid, result)
            Return result
        End Function

        ''' <summary>
        ''' To get a list of Classification Methods for the add/edit page.
        ''' </summary>
        ''' <remarks></remarks>

        <HttpPost>
        Public Function GetClassificationMethodList() As ApiResult
            Dim result As New ApiResult
            SurfaceDefaultsVm.GetClassificationMethodList(result)

            Return result
        End Function


        ''' <summary>
        ''' To get a list of System Attributes for the add/edit page.
        ''' </summary>
        ''' <remarks></remarks>
        <HttpPost>
        Public Function GetSystemAttributeList(ByVal objJson As JObject) As ApiResult
            Dim result As New ApiResult
            SurfaceDefaultsVm.GetSystemAttributeList(result)

            Return result
        End Function
        ''' <summary>
        ''' fetch a list of Companies for the add/edit page
        ''' </summary>
        ''' <param name="objJson">
        '''  - UserGuid -- the identity of the logged in user requesting the fetch
        ''' </param>
        ''' <returns>
        '''  Success - Boolean
        '''  Model - Company List 
        '''  Error Code List
        ''' </returns>
        ''' <remarks></remarks>
        <HttpPost>
        Function GetCompanyList(objJson As JObject) As ApiResult
            Dim result As New ApiResult()
            Dim loggedInUserGuid As String = objJson("UserGuid").ToString()

            SurfaceDefaultsVm.GetCompanyList(loggedInUserGuid, result)
            Return result
        End Function
    End Class
End Namespace


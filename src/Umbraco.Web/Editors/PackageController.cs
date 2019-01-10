﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Web.Http;
using Umbraco.Core.IO;
using Umbraco.Core.Models.Packaging;
using Umbraco.Web.Models.ContentEditing;
using Umbraco.Web.Mvc;
using Umbraco.Web.WebApi;
using Umbraco.Web.WebApi.Filters;
using Umbraco.Web._Legacy.Packager.PackageInstance;

namespace Umbraco.Web.Editors
{
    //TODO: Packager stuff still lives in business logic - YUK

    /// <summary>
    /// A controller used for installing packages and managing all of the data in the packages section in the back office
    /// </summary>
    [PluginController("UmbracoApi")]
    [SerializeVersion]
    [UmbracoApplicationAuthorize(Core.Constants.Applications.Packages)]
    public class PackageController : UmbracoAuthorizedJsonController
    {
        public IEnumerable<PackageDefinition> GetCreatedPackages()
        {
            return Services.PackagingService.GetAll();
        }

        public PackageDefinition GetCreatedPackageById(int id)
        {
            var package = Services.PackagingService.GetById(id);
            if (package == null)
                throw new HttpResponseException(HttpStatusCode.NotFound);

            return package;
        }

        public PackageDefinition GetEmpty()
        {
            return new PackageDefinition();
        }

        /// <summary>
        /// Creates or updates a package
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public PackageDefinition PostSavePackage(PackageDefinition model)
        {
            if (ModelState.IsValid == false)
                throw new HttpResponseException(Request.CreateValidationErrorResponse(ModelState));

            //save it
            if (!Services.PackagingService.SavePackage(model))
                throw new HttpResponseException(Request.CreateNotificationValidationErrorResponse("The package with id {definition.Id} was not found"));

            Services.PackagingService.ExportPackage(model);

            //the packagePath will be on the model 
            return model;
        }

        /// <summary>
        /// Deletes a created package
        /// </summary>
        /// <param name="packageId"></param>
        /// <returns></returns>
        [HttpPost]
        [HttpDelete]
        public IHttpActionResult DeleteCreatedPackage(int packageId)
        {
            Services.PackagingService.Delete(packageId);

            return Ok();
        }

        [HttpGet]
        public HttpResponseMessage DownloadCreatedPackage(int id)
        {
            var package = Services.PackagingService.GetById(id);
            if (package == null)
                return Request.CreateResponse(HttpStatusCode.NotFound);

            var fullPath = IOHelper.MapPath(package.PackagePath);
            if (!File.Exists(fullPath))
                return Request.CreateNotificationValidationErrorResponse("No file found for path " + package.PackagePath);

            var fileName = Path.GetFileName(package.PackagePath);

            var response = new HttpResponseMessage
            {
                Content = new StreamContent(File.OpenRead(fullPath))
                {
                    Headers =
                    {
                        ContentDisposition = new ContentDispositionHeaderValue("attachment")
                        {
                            FileName = fileName
                        },
                        ContentType = new MediaTypeHeaderValue( "application/octet-stream")
                    }
                }
            };

            // Set custom header so umbRequestHelper.downloadFile can save the correct filename
            response.Headers.Add("x-filename", fileName);

            return response;
        }

    }
}

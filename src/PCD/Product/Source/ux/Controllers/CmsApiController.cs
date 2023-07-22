using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Osgs.Infra.Monitoring.AspNetCore;
using Microsoft.PrivacyServices.UX.Core.Cms.Services;

//  NOTE: The endpoint still exists, if someone wants to resurrect the CMS work.
//  See other files in the commit for changes that need to be reverted.

namespace Microsoft.PrivacyServices.UX.Controllers
{
    [Authorize("Api")]
    [HandleAjaxErrors(ErrorCode = "generic")]
    public class CmsApiController : Controller
    {
        //private readonly ICmsService cmsService;

        //public CmsApiController(ICmsService cmsService)
        //{
        //    this.cmsService = cmsService ?? throw new ArgumentNullException(nameof(cmsService));
        //}

        [HttpPost]
        [ValidateAntiForgeryToken]
        public Task<IActionResult> GetContentItems([FromBody]IEnumerable<CmsKey> cmsKeys)
        {
            //var requestCultureFeature = HttpContext.Features.Get<IRequestCultureFeature>();
            //var requestCulture = requestCultureFeature.RequestCulture;
            //var cultureCode = requestCulture.UICulture.Name;

            //return Json(await cmsService.GetCmsContentItemsAsync(cmsKeys, cultureCode));

            var result = new JsonResult(new { message = "Integration with CMS is disabled." })
            {
                StatusCode = (int)HttpStatusCode.InternalServerError
            };

            return Task.FromResult((IActionResult)result);
        }
    }
}

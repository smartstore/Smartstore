using Microsoft.AspNetCore.Mvc;
using Smartstore.Web.Models.Common;
using Smartstore.Web.TagHelpers.Shared;
using System.Threading.Tasks;

namespace Smartstore.Web.Components
{
    public class FileUploaderViewComponent : SmartViewComponent
    {
        public FileUploaderViewComponent()
        {
        }

        public async Task<IViewComponentResult> InvokeAsync(IFileUploaderModel fileModel)
        {
            Guard.NotNull(fileModel, nameof(fileModel));

            var model = new FileUploaderModel(fileModel);
            if (model == null)
            {
                return Empty();
            }

            if (!model.UploadText.HasValue())
            {
                model.UploadText = T("Common.Fileuploader.Upload");
            }

            return View(model);
        }
    }
}

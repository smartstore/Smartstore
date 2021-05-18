using Microsoft.AspNetCore.Mvc;
using Smartstore.Web.Models.Common;
using Smartstore.Web.TagHelpers.Shared;

namespace Smartstore.Web.Components
{
    public class FileUploaderViewComponent : SmartViewComponent
    {
        public FileUploaderViewComponent()
        {
        }

        public IViewComponentResult Invoke(IFileUploaderModel fileModel)
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

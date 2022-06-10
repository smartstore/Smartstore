using Smartstore.Utilities;
using Smartstore.Web.TagHelpers.Shared;

namespace Smartstore.Web.Components
{
    public class FileUploaderViewComponent : SmartViewComponent
    {
        public IViewComponentResult Invoke(FileUploaderModel model)
        {
            if (model == null)
            {
                return Empty();
            }

            if (model.UploadText.IsEmpty())
            {
                model.UploadText = T("Common.Fileuploader.Upload");
            }

            if (model.Name.IsEmpty())
            {
                model.Name = "upload-" + CommonHelper.GenerateRandomInteger();
            }

            return View(model);
        }
    }
}

using FluentValidation;
using Smartstore.Admin.Models.Scheduling;
using Smartstore.Core.DataExchange;
using Smartstore.Core.DataExchange.Import;
using Smartstore.Core.Localization;

namespace Smartstore.Admin.Models.Import
{
    [LocalizedDisplay("Admin.DataExchange.Import.")]
    public class ImportProfileModel : EntityModelBase
    {
        [LocalizedDisplay("*Name")]
        public string Name { get; set; }

        [LocalizedDisplay("Admin.Common.ImportFiles")]
        public IList<ImportFile> ExistingFiles { get; set; }

        [LocalizedDisplay("Admin.Common.Entity")]
        public ImportEntityType EntityType { get; set; }

        [LocalizedDisplay("Admin.Common.Entity")]
        public string EntityTypeName { get; set; }

        [LocalizedDisplay("Common.Enabled")]
        public bool Enabled { get; set; }

        [LocalizedDisplay("*ImportRelatedData")]
        public bool ImportRelatedData { get; set; }

        [LocalizedDisplay("Admin.Common.RecordsSkip")]
        public int? Skip { get; set; }

        [LocalizedDisplay("Admin.Common.RecordsTake")]
        public int? Take { get; set; }

        [LocalizedDisplay("*UpdateOnly")]
        public bool UpdateOnly { get; set; }

        [LocalizedDisplay("*KeyFieldNames")]
        public string[] KeyFieldNames { get; set; }

        [LocalizedDisplay("Common.Execution")]
        public int TaskId { get; set; }
        public string TaskName { get; set; }
        public bool IsTaskRunning { get; set; }
        public bool IsTaskEnabled { get; set; }

        public TaskModel TaskModel { get; set; }

        [LocalizedDisplay("*LastImportResult")]
        public SerializableImportResult ImportResult { get; set; }

        public bool LogFileExists { get; set; }
        public string TempFileName { get; set; }

        [LocalizedDisplay("*FolderName")]
        public string FolderName { get; set; }

        public ImportFileType FileType { get; set; }
        public CsvConfigurationModel CsvConfiguration { get; set; }
        public ExtraDataModel ExtraData { get; set; } = new();

        [LocalizedDisplay("Admin.DataExchange.Import.")]
        public class ExtraDataModel
        {
            [LocalizedDisplay("*NumberOfPictures")]
            public int? NumberOfPictures { get; set; }
        }
    }

    public class ColumnMappingItemModel
    {
        public int Index { get; set; }

        public string Column { get; set; }
        public string ColumnWithoutIndex { get; set; }
        public string ColumnIndex { get; set; }

        public string Property { get; set; }
        public string PropertyDescription { get; set; }

        public string Default { get; set; }
        public bool IsDefaultDisabled { get; set; }
    }

    public partial class ImportProfileValidator : AbstractValidator<ImportProfileModel>
    {
        public ImportProfileValidator(Localizer T)
        {
            RuleFor(x => x.Name).NotEmpty();
            RuleFor(x => x.Skip).GreaterThanOrEqualTo(0);
            RuleFor(x => x.Take).GreaterThanOrEqualTo(0);

            RuleFor(x => x.KeyFieldNames)
                .NotEmpty()
                .When(x => x.Id != 0)
                .WithMessage(T("Admin.DataExchange.Import.Validate.OneKeyFieldRequired"));
        }
    }
}

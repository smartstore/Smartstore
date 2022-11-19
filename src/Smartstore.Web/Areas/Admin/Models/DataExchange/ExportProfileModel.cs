using FluentValidation;
using Smartstore.Admin.Models.Scheduling;
using Smartstore.Core.DataExchange;
using Smartstore.Core.Localization;
using Smartstore.IO;

namespace Smartstore.Admin.Models.Export
{
    [LocalizedDisplay("Admin.DataExchange.Export.")]
    public class ExportProfileModel : EntityModelBase
    {
        [LocalizedDisplay("*Name")]
        public string Name { get; set; }

        [LocalizedDisplay("*SystemName")]
        public string SystemName { get; set; }

        [LocalizedDisplay("*IsSystemProfile")]
        public bool IsSystemProfile { get; set; }

        [LocalizedDisplay("*ProviderSystemName")]
        public string ProviderSystemName { get; set; }

        [LocalizedDisplay("*FolderName")]
        public string FolderName { get; set; }

        [LocalizedDisplay("*FileNamePattern")]
        public string FileNamePattern { get; set; }
        public string FileNamePatternExample { get; set; }

        [LocalizedDisplay("Common.Enabled")]
        public bool Enabled { get; set; }

        [LocalizedDisplay("*ExportRelatedData")]
        public bool ExportRelatedData { get; set; }

        [LocalizedDisplay("Common.Execution")]
        public int TaskId { get; set; }
        public string TaskName { get; set; }
        public bool IsTaskRunning { get; set; }
        public bool IsTaskEnabled { get; set; }

        [LocalizedDisplay("Admin.Common.RecordsSkip")]
        public int Offset { get; set; }

        [LocalizedDisplay("Admin.Common.RecordsTake")]
        public int? Limit { get; set; }

        [LocalizedDisplay("*BatchSize")]
        public int? BatchSize { get; set; }

        [LocalizedDisplay("*PerStore")]
        public bool PerStore { get; set; }

        [LocalizedDisplay("*EmailAccountId")]
        public int? EmailAccountId { get; set; }

        [LocalizedDisplay("*CompletedEmailAddresses")]
        public string[] CompletedEmailAddresses { get; set; }

        [LocalizedDisplay("*CreateZipArchive")]
        public bool CreateZipArchive { get; set; }

        [LocalizedDisplay("*Cleanup")]
        public bool Cleanup { get; set; }

        [LocalizedDisplay("*CloneProfile")]
        public int? CloneProfileId { get; set; }

        public ProviderModel Provider { get; set; }
        public ExportFilterModel Filter { get; set; }
        public ExportProjectionModel Projection { get; set; }
        public List<ExportDeploymentModel> Deployments { get; set; }

        public TaskModel TaskModel { get; set; }

        public bool LogFileExists { get; set; }
        public bool HasActiveProvider { get; set; }
        public string[] FileNamePatternDescriptions { get; set; }
        public string PrimaryStoreCurrencyCode { get; set; }
        public int FileCount { get; set; }

        [LocalizedDisplay("Admin.DataExchange.Export.")]
        public class ProviderModel
        {
            [LocalizedDisplay("Common.Image")]
            public string ThumbnailUrl { get; set; }

            [LocalizedDisplay("Common.Website")]
            public string Url { get; set; }

            [LocalizedDisplay("Common.Provider")]
            public string FriendlyName { get; set; }

            [LocalizedDisplay("Admin.Configuration.Plugins.Fields.Author")]
            public string Author { get; set; }

            [LocalizedDisplay("Admin.Configuration.Plugins.Fields.Version")]
            public string Version { get; set; }

            [LocalizedDisplay("Common.Description")]
            public string Description { get; set; }

            [LocalizedDisplay("*EntityType")]
            public ExportEntityType EntityType { get; set; }

            [LocalizedDisplay("*EntityType")]
            public string EntityTypeName { get; set; }

            [LocalizedDisplay("*FileExtension")]
            public string FileExtension { get; set; }

            public bool IsFileBasedExport => FileExtension.HasValue();

            [LocalizedDisplay("*SupportedFileTypes")]
            public string SupportedFileTypes { get; set; }

            public Widget ConfigurationWidget { get; set; }
            public Type ConfigDataType { get; set; }
            public object ConfigData { get; set; }
            public ExportFeatures Feature { get; set; }
        }

        public class ProviderSelectItem
        {
            public int Id { get; set; }
            public string SystemName { get; set; }
            public string FriendlyName { get; set; }
            public string ImageUrl { get; set; }
            public string Description { get; set; }
        }
    }

    public partial class ExportFileDetailsModel : EntityModelBase
    {
        public int FileCount
        {
            get
            {
                var result = ExportFiles.Count;

                if (result == 0)
                    result = PublicFiles.Count;

                return result;
            }
        }

        public List<FileInfo> ExportFiles { get; set; } = new();
        public List<FileInfo> PublicFiles { get; set; } = new();

        public bool IsForDeployment { get; set; }

        public class FileInfo
        {
            public int StoreId { get; set; }
            public string StoreName { get; set; }
            public string Label { get; set; }
            public int DisplayOrder { get; set; }
            public RelatedEntityType? RelatedType { get; set; }

            public IFile File { get; set; }
            public string FileUrl { get; set; }
            public string FriendlyFileUrl { get; set; }
            public string FileRootPath { get; set; }
        }
    }

    public partial class ExportProfileValidator : AbstractValidator<ExportProfileModel>
    {
        public ExportProfileValidator(Localizer T)
        {
            RuleFor(x => x.Name).NotEmpty();
            RuleFor(x => x.FileNamePattern).NotEmpty();
            RuleFor(x => x.Offset).GreaterThanOrEqualTo(0);
            RuleFor(x => x.Limit).GreaterThanOrEqualTo(0);
            RuleFor(x => x.BatchSize).GreaterThanOrEqualTo(0);

            RuleFor(x => x.ExportRelatedData)
                .Must(x => x == false)
                .When(x => x.Projection?.AttributeCombinationAsProduct ?? false)
                .WithMessage(T("Admin.DataExchange.Export.ExportRelatedData.Validate"));
        }
    }
}

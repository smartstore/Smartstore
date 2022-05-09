using Smartstore.Core.Localization;

namespace Smartstore.Core.Search.Indexing
{
    public interface IIndexStoreProvider
    {
        FieldAnalysisType? GetDefaultAnalysisType(FieldAnalysisReason reason, IIndexStore indexStore);

        IList<FieldAnalysisInfo> GetFieldAnalysisInfos(FieldAnalysisReason reason, IList<Language> languages, IIndexStore indexStore);
    }
}

using Smartstore.Core.Localization;

namespace Smartstore.Core.Search.Indexing
{
    // TODO: (mg) (core) Bad API design. We already have IIndexStore. Distinct responsibilities of each are not clear. TBD with MC.
    public interface IIndexStoreProvider
    {
        FieldAnalysisType? GetDefaultAnalysisType(FieldAnalysisReason reason, IIndexStore indexStore);

        IList<FieldAnalysisInfo> GetFieldAnalysisInfos(FieldAnalysisReason reason, IList<Language> languages, IIndexStore indexStore);
    }
}

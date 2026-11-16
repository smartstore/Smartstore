using System;
using System.Threading;
using NUnit.Framework;
using Smartstore.Core.DataExchange;
using Smartstore.Core.DataExchange.Import;

namespace Smartstore.Core.Tests.DataExchange;

[TestFixture]
public class ImportExecuteContextTests
{
    [Test]
    public void SuppliedDataSourceAndCancellationArePreserved()
    {
        var table = new LightweightDataTable([], []);
        using var cancellation = new CancellationTokenSource();
        var context = new ImportExecuteContext("Import {0}/{1}", table, cancellation.Token);

        Assert.That(context.DataTable, Is.SameAs(table));
        cancellation.Cancel();
        Assert.That(context.CancelToken.IsCancellationRequested, Is.True);
        Assert.That(context.Abort, Is.EqualTo(DataExchangeAbortion.Hard));
    }

    [Test]
    public void ExplicitDataSourceCannotBeNull()
    {
        Assert.Throws<ArgumentNullException>(() => new ImportExecuteContext(null, null, default));
    }
}

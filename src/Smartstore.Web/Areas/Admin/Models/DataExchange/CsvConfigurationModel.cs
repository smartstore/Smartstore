using FluentValidation;
using Smartstore.Core.DataExchange.Csv;
using Smartstore.Core.Localization;

namespace Smartstore.Admin.Models.Import;

[LocalizedDisplay("Admin.DataExchange.Csv.")]
public class CsvConfigurationModel : ModelBase, ICloneable<CsvConfiguration>
{
    public CsvConfigurationModel()
    {
    }

    public CsvConfigurationModel(CsvConfiguration config)
    {
        QuoteAllFields = config.QuoteAllFields;
        TrimValues = config.TrimValues;
        SupportsMultiline = config.SupportsMultiline;
        Delimiter = EscapeDisplayChar(config.Delimiter);
        Quote = EscapeDisplayChar(config.Quote);
        Escape = EscapeDisplayChar(config.Escape);
    }

    public bool Validate { get; set; }

    [LocalizedDisplay("*QuoteAllFields")]
    public bool QuoteAllFields { get; set; }

    [LocalizedDisplay("*TrimValues")]
    public bool TrimValues { get; set; }

    [LocalizedDisplay("*SupportsMultiline")]
    public bool SupportsMultiline { get; set; }

    [LocalizedDisplay("*Delimiter")]
    public string Delimiter { get; set; }

    [LocalizedDisplay("*Quote")]
    public string Quote { get; set; }

    [LocalizedDisplay("*Escape")]
    public string Escape { get; set; }

    object ICloneable.Clone()
        => Clone();

    public CsvConfiguration Clone()
    {
        return new CsvConfiguration
        {
            QuoteAllFields = QuoteAllFields,
            TrimValues = TrimValues,
            SupportsMultiline = SupportsMultiline,
            Delimiter = Delimiter.ToChar(true),
            Quote = Quote.ToChar(true),
            Escape = Escape.ToChar(true)
        };
    }

    private static string EscapeDisplayChar(char value)
        => value switch
        {
            '\\' => @"\\",
            '\t' => @"\t",
            '\r' => @"\r",
            '\n' => @"\n",
            '\0' => @"\0",
            _ => value.ToString()
        };
}

public partial class CsvConfigurationValidator : AbstractValidator<CsvConfigurationModel>
{
    public CsvConfigurationValidator(Localizer T)
    {
        When(x => x.Validate, () =>
        {
            RuleFor(x => x.Delimiter)
                .Must(x => !CsvConfiguration.PresetCharacters.Contains(x.ToChar(true)))
                .WithMessage(T("Admin.DataExchange.Csv.Delimiter.Validation"));

            RuleFor(x => x.Quote)
                .Must(x => !CsvConfiguration.PresetCharacters.Contains(x.ToChar(true)))
                .WithMessage(T("Admin.DataExchange.Csv.Quote.Validation"));

            RuleFor(x => x.Escape)
                .Must(x => !CsvConfiguration.PresetCharacters.Contains(x.ToChar(true)))
                .WithMessage(T("Admin.DataExchange.Csv.Escape.Validation"));

            RuleFor(x => x.Escape)
                .Must((model, x) => x.ToChar(true) != model.Delimiter.ToChar(true))
                .WithMessage(T("Admin.DataExchange.Csv.EscapeDelimiter.Validation"));

            RuleFor(x => x.Quote)
                .Must((model, x) => x.ToChar(true) != model.Delimiter.ToChar(true))
                .WithMessage(T("Admin.DataExchange.Csv.QuoteDelimiter.Validation"));
        });
    }
}

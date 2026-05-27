using Microsoft.AspNetCore.Http;

namespace Smartstore.Core.AI;

/// <summary>
/// Defines a contract for generating LLM (Large Language Model) metadata.
/// </summary>
public interface ILlmsGenerator
{
    /// <summary>
    /// Generates LLM (Large Language Model) metadata and writes it to the specified output stream.
    /// </summary>
    /// <param name="writer">The text writer to output the generated LLM metadata.</param>
    /// <param name="httpRequest">The HTTP request context used for generating the metadata.</param>
    Task GenerateLlms(TextWriter writer, HttpRequest httpRequest);
}

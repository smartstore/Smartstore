using System;
using System.Collections.Generic;

namespace Smartstore.Web.Infrastructure.Installation
{
    public class InstallationResult : ICloneable<InstallationResult>
    {
        public string ProgressMessage { get; set; }
        public bool Completed { get; set; }
        public bool Success { get; set; }
        public string RedirectUrl { get; set; }
        public List<string> Errors { get; private set; } = new();
        public bool HasErrors => this.Errors.Count > 0;

        object ICloneable.Clone()
            => this.Clone();

        public InstallationResult Clone()
        {
            var clone = new InstallationResult
            {
                ProgressMessage = ProgressMessage,
                Completed = Completed,
                RedirectUrl = RedirectUrl,
                Success = Success
            };

            clone.Errors.AddRange(Errors);

            return clone;
        }
    }
}

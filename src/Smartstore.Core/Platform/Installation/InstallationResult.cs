namespace Smartstore.Core.Installation
{
    public class InstallationResult : ICloneable<InstallationResult>
    {
        public InstallationResult(InstallationModel model)
        {
            Model = Guard.NotNull(model, nameof(model));
        }

        public InstallationModel Model { get; }
        
        public string ProgressMessage { get; set; }
        public bool Completed { get; set; }
        public bool Success { get; set; }
        public string RedirectUrl { get; set; }
        public List<string> Errors { get; private set; } = new();
        public bool HasErrors => Errors.Count > 0;

        object ICloneable.Clone()
            => Clone();

        public InstallationResult Clone()
        {
            var clone = new InstallationResult(Model)
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

namespace Smartstore.WebApi.Client
{
    internal static class Program
    {
        public static string AppName => "Smartstore Web API Client v.5.0";
        public static string ConsumerName => "My shopping data consumer v.5.0";

        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
using Smartstore.Core.Content.Menus;
using Smartstore.Events;
using Smartstore.Web.Rendering.Builders;

namespace Smartstore.Web.Infrastructure
{
    // TODO: (core) Remove this class later.
    internal class MainMenuBuiltEventConsumer : IConsumer
    {
        public void Handle(MenuBuiltEvent message)
        {
            if (!message.Name.EqualsNoCase("Main"))
            {
                return;
            }

            var cheatsheet = message.Root.Prepend(new MenuItem()
                .ToBuilder()
                .Text("Cheatsheet")
                .Url("javascript:void()")
                .AsItem());

            cheatsheet.Append(new MenuItem()
                .ToBuilder()
                .Text("Controls")
                .Action("Controls", "Home")
                .AsItem());

            cheatsheet.Append(new MenuItem()
                .ToBuilder()
                .Text("Privacy")
                .Route("Privacy")
                .AsItem());

            cheatsheet.Append(new MenuItem()
                .ToBuilder()
                .Text("Länder")
                .Action("Countries", "Home")
                .AsItem());

            cheatsheet.Append(new MenuItem()
                .ToBuilder()
                .Text("Settings")
                .Action("Settings", "Home")
                .AsItem());

            cheatsheet.Append(new MenuItem()
                .ToBuilder()
                .Text("Logs")
                .Action("Logs", "Home")
                .AsItem());

            cheatsheet.Append(new MenuItem()
                .ToBuilder()
                .Text("Files")
                .Action("Files", "Home")
                .AsItem());

            cheatsheet.Append(new MenuItem()
                .ToBuilder()
                .Text("Identity")
                .Route("Login")
                .AsItem());

            cheatsheet.Append(new MenuItem()
                .ToBuilder()
                .Text("Env")
                .Action("Env", "Home")
                .AsItem());

            cheatsheet.Append(new MenuItem()
                .ToBuilder()
                .Text("Messages")
                .Action("Messages", "Home")
                .AsItem());

            cheatsheet.Append(new MenuItem()
                .ToBuilder()
                .Text("Clear Cache")
                .Action("ClearCache", "Home")
                .AsItem());
        }
    }
}

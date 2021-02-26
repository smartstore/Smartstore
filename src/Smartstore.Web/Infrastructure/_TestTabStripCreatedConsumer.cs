using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Smartstore.Events;
using Smartstore.Web.Rendering.Events;

namespace Smartstore.Web.Infrastructure
{
    // TODO: (core) Remove this class later.
    public class TestTabStripCreatedConsumer : IConsumer
    {
        public Task HandleEvent(TabStripCreated message)
        {
            if (message.TabStripName != "yodele")
            {
                return Task.CompletedTask;
            }

            return message.TabFactory.AddAsync(builder => 
            {
                builder.Text("Content Slider")
                    .Name("tab-ContentSlider")
                    .Icon("far fa-images fa-lg fa-fw")
                    .LinkHtmlAttributes(new { data_tab_name = "ContentSlider" })
                    //.Content("<h4>Content Slider</h4>")
                    .Route("register")
                    .Ajax(false);
            });
        }
    }
}

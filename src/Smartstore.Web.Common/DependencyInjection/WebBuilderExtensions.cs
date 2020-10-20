using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Smartstore.Engine;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class WebBuilderExtensions
    {
        public static IApplicationBuilder UseSmartstoreMvc(this IApplicationBuilder app, IApplicationContext appContext)
        {
            if (appContext.HostEnvironment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            //app.UseHttpsRedirection();
            app.UseStaticFiles(); // TODO: (core) Set StaticFileOptions
            app.UseRouting();
            // TODO: (core) Use Swagger
            app.UseCookiePolicy(); // TODO: (core) Configure cookie policy
            app.UseAuthorization(); // TODO: (core) Configure custom auth with Identity Server
            // TODO: (core) Use request localization
            // TODO: (core) Use SEO url rewriter
            // TODO: (core) Use media middleware
            app.UseRequestLocalization(); // TODO: (core) Configure request localization
            //app.UseSession(); // TODO: (core) Configure session

            return app;
        }
    }
}

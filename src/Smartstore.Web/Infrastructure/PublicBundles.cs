using System;
using System.Collections.Generic;
using System.Linq;
using Smartstore.Engine;
using Smartstore.Web.Bundling;
using WebOptimizer;

namespace Smartstore.Web.Infrastructure
{
    internal class PublicBundles : IBundleProvider
    {
        public int Priority => 0;

        public void RegisterBundles(IApplicationContext appContext, IAssetPipeline assetPipeline)
        {
            if (!appContext.IsInstalled)
            {
                return;
            }

            var lib = "/lib/";
            var js = "/js/";


            /* (TEST) FLEX Sass theme --> /themes/flex.css
            -----------------------------------------------------*/
            // TODO: (core) Make dynamic registration for these in BundleMiddleware
            assetPipeline.RegisterCssBundle("/themes/flex/theme.css", "/Themes/Flex/theme.scss");
            assetPipeline.RegisterCssBundle("/themes/flex/theme-rtl.css", "/Themes/Flex/theme-rtl.scss");
            assetPipeline.RegisterCssBundle("/themes/flex-black/theme.css", "/Themes/FlexBlack/theme.scss");
            assetPipeline.RegisterCssBundle("/themes/flex-black/theme-rtl.css", "/Themes/FlexBlack/theme-rtl.scss");
            assetPipeline.RegisterCssBundle("/themes/flex-blue/theme.css", "/Themes/FlexBlue/theme.scss");
            assetPipeline.RegisterCssBundle("/themes/flex-blue/theme-rtl.css", "/Themes/FlexBlue/theme-rtl.scss");


            /* Public Common CSS --> /bundle/css/site-common.css
            -----------------------------------------------------*/
            assetPipeline.RegisterCssBundle("/bundle/css/site-common.css",
                lib + "fa5/css/all.css", // TODO: (core) Consider "fa-use-pro" theme variable somehow
                lib + "fontastic/fontastic.css",
                lib + "pnotify/css/pnotify.css",
                lib + "pnotify/css/pnotify.mobile.css",
                lib + "pnotify/css/pnotify.buttons.css");


            /* Public Main --> /bundle/js/site.js
            -----------------------------------------------------*/
            assetPipeline.RegisterJsBundle("/bundle/js/site.js",
                // Vendors
                lib + "underscore/underscore.js",
                lib + "underscore/underscore.string.js",
                lib + "jquery/jquery.addeasing.js",
                lib + "jquery-ui/effect.js",
                lib + "jquery-ui/effect-shake.js",
                lib + "jquery/jquery.unobtrusive-ajax.js",
                lib + "jquery/jquery.validate.js",
                lib + "jquery/jquery.validate.unobtrusive.js",
                lib + "jquery/jquery.ba-outside-events.js",
                lib + "jquery/jquery.scrollTo.js",
                lib + "moment/moment.js",
                lib + "datetimepicker/js/tempusdominus-bootstrap-4.js",
                lib + "select2/js/select2.js",
                lib + "pnotify/js/pnotify.js",
                lib + "pnotify/js/pnotify.mobile.js",
                lib + "pnotify/js/pnotify.buttons.js",
                lib + "pnotify/js/pnotify.animate.js",
                lib + "slick/slick.js",
                lib + "touchspin/jquery.bootstrap-touchspin.js",
                lib + "aos/js/aos.js",
                lib + "bs4/js/bootstrap.bundle.js",
                // Common
                js + "underscore.mixins.js",
                js + "smartstore.system.js",
                js + "smartstore.touchevents.js",
                js + "smartstore.jquery.utils.js",
                js + "smartstore.globalization.js",
                js + "jquery.validate.unobtrusive.custom.js",
                js + "smartstore.viewport.js",
                js + "smartstore.doajax.js",
                js + "smartstore.eventbroker.js",
                js + "smartstore.common.js",
                js + "smartstore.dialogs.js",
                js + "smartstore.selectwrapper.js",
                js + "smartstore.throbber.js",
                js + "smartstore.thumbzoomer.js",
                js + "smartstore.responsiveNav.js",
                js + "smartstore.keynav.js",
                js + "smartstore.articlelist.js",
                js + "smartstore.megamenu.js",
                js + "smartstore.offcanvas.js",
                js + "smartstore.parallax.js",
                js + "smartstore.media.js",
                // Shop
                js + "public.common.js",
                js + "public.search.js",
                js + "public.offcanvas-cart.js",
                js + "public.offcanvas-menu.js",
                js + "public.product.js");
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Smartstore.Engine;
using Smartstore.Web.Optimization;
using WebOptimizer;

namespace Smartstore.Web.Infrastructure
{
    internal class AdminBundles : IBundleProvider
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
            var adminJs = "/admin/js/";
            var components = "/components/";


            /* Admin Common CSS --> /bundle/css/admin-common.css
            -----------------------------------------------------*/
            assetPipeline.RegisterCssBundle("/bundle/css/admin-common.css",
                lib + "fontastic/fontastic.css",
                lib + "fa5/css/all.css",
                lib + "pnotify/css/pnotify.css",
                lib + "pnotify/css/pnotify.mobile.css",
                lib + "pnotify/css/pnotify.buttons.css");


            /* Admin Main --> /bundle/js/admin.js
            -----------------------------------------------------*/
            assetPipeline.RegisterJsBundle("/bundle/js/admin.js",
                adminJs + "jquery-shims.js",
                // Lib
                lib + "underscore/underscore.js",
                lib + "underscore/underscore.string.js",
                lib + "jquery/jquery.addeasing.js", 
                lib + "jquery-ui/effect.js",
                lib + "jquery-ui/effect-transfer.js",
                lib + "jquery-ui/position.js",
                lib + "jquery/jquery.unobtrusive-ajax.js",
                lib + "jquery/jquery.validate.js",
                lib + "jquery/jquery.validate.unobtrusive.js",
                lib + "jquery/jquery.scrollTo.js",
                lib + "jquery/jquery.serializeToJSON.min.js",
                lib + "sortable/sortable.js",
                lib + "sortable/jquery-sortable.js",
                lib + "moment/moment.js",
                lib + "datetimepicker/js/tempusdominus-bootstrap-4.js",
                lib + "colorpicker/js/bootstrap-colorpicker.js",
                lib + "colorpicker/js/bootstrap-colorpicker-globalinit.js",
                lib + "select2/js/select2.js",
                lib + "pnotify/js/pnotify.js",
                lib + "pnotify/js/pnotify.mobile.js",
                lib + "pnotify/js/pnotify.buttons.js",
                lib + "pnotify/js/pnotify.animate.js",
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
                js + "smartstore.entitypicker.js",
                js + "smartstore.rangeslider.js",
                js + "smartstore.tree.js",
                js + "smartstore.media.js",
                js + "smartstore.editortemplates.js",
                // Admin
                adminJs + "admin.common.js",
                adminJs + "admin.media.js",
                adminJs + "admin.globalinit.js");


            /* Chart.js --> /bundle/js/chart.js
            -----------------------------------------------------*/
            assetPipeline.RegisterJsBundle("/bundle/js/chart.js",
                lib + "Chart.js/Chart.js");


            /* DataGrid --> /bundle/js/datagrid.js
            -----------------------------------------------------*/
            // Script
            assetPipeline.RegisterJsBundle("/bundle/js/datagrid.js",
                components + "datagrid/datagrid.js",
                components + "datagrid/datagrid-pager.js",
                components + "datagrid/datagrid-tools.js");
            // Scss (Move as partial to main file later)
            //assetPipeline.CompileScssFiles(null, components + "datagrid/datagrid.scss").MinifyCss();
            assetPipeline.RegisterSassFile(components + "datagrid/datagrid.scss");

            // TEST
            assetPipeline.RegisterSassFile(lib + "bs4/scss/bootstrap.scss");
            //assetPipeline.AddFiles("text/css; charset=UTF-8", lib + "bs4/scss/bootstrap.scss")
            //    .AddSassProcessor()
            //    .FingerprintUrls()
            //    .AddResponseHeader("X-Content-Type-Options", "nosniff")
            //    .MinifyCss();
            //assetPipeline.CompileScssFiles(null, lib + "bs4/scss/bootstrap.scss").MinifyCss();

            /* Summernote--> /bundle/js/summernote.js
			------------------------------------------------------*/
            var summernote = "/lib/editors/summernote/";
            assetPipeline.RegisterJsBundle("/bundle/js/summernote.js",
                summernote + "summernote-bs4.min.js",
                summernote + "plugins/smartstore.image.js",
                summernote + "plugins/smartstore.link.js",
                summernote + "plugins/smartstore.tablestyles.js",
                summernote + "plugins/smartstore.cssclass.js",
                lib + "beautify/beautify.min.js",
                lib + "beautify/beautify-css.min.js",
                lib + "beautify/beautify-html.min.js",
                summernote + "globalinit.js");


            /* CodeMirror (V 5.3.3) --> /bundle/js/codemirror.js
            -----------------------------------------------------*/
            var cm = "/lib/editors/CodeMirror/";
            // Script
            assetPipeline.RegisterJsBundle("/bundle/js/codemirror.js",
                cm + "codemirror.min.js",
                cm + "addon/fold/xml-fold.min.js",
                cm + "addon/hint/show-hint.min.js",
                cm + "addon/hint/xml-hint.min.js",
                cm + "addon/hint/html-hint.min.js",
                cm + "addon/hint/css-hint.min.js",
                cm + "addon/hint/javascript-hint.min.js",
                cm + "addon/edit/closetag.min.js",
                cm + "addon/edit/closebrackets.min.js",
                cm + "addon/edit/matchtags.min.js",
                cm + "addon/edit/matchbrackets.min.js",
                cm + "addon/mode/multiplex.min.js",
                cm + "addon/mode/overlay.min.js",
                cm + "addon/display/fullscreen.min.js",
                cm + "addon/selection/active-line.min.js",
                cm + "mode/xml/xml.min.js",
                cm + "mode/javascript/javascript.min.js",
                cm + "mode/css/css.min.js",
                cm + "mode/htmlmixed/htmlmixed.min.js",
                cm + "mode/liquid/liquid.js");
            // CSS
            assetPipeline.RegisterCssBundle("/bundle/css/codemirror.css",
                cm + "codemirror.min.css",
                cm + "codemirror.custom.css",
                cm + "addon/hint/show-hint.min.css",
                cm + "addon/display/fullscreen.css",
                cm + "theme/eclipse.min.css",
                cm + "mode/liquid/liquid.css");


            /* Roxy File Manager--> /bundle/js/filemanager.js
			------------------------------------------------------*/
            var roxy = "/lib/roxyfm/js/";
            assetPipeline.RegisterJsBundle("/bundle/js/roxyfm.js",
                roxy + "jquery-2.1.1.min.js",
                roxy + "jquery-ui-1.10.4.custom.min.js",
                roxy + "filetypes.js",
                roxy + "custom.js",
                roxy + "main.js",
                roxy + "utils.js",
                roxy + "file.js",
                roxy + "directory.js",
                roxy + "jquery-dateFormat.min.js");

        }
    }
}

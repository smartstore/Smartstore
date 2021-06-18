using System;
using System.Collections.Generic;
using System.Linq;
using Smartstore.Engine;
using Smartstore.Web.Bundling;
using WebOptimizer;

namespace Smartstore.Web.Infrastructure
{
    internal class CommonBundles : IBundleProvider
    {
        public int Priority => 0;

        public void RegisterBundles(IApplicationContext appContext, IAssetPipeline assetPipeline)
        {
            var lib = "/lib/";
            var js = "/js/";

            /* Modernizr + jQuery --> /bundle/js/jquery.js
            -----------------------------------------------------*/
            assetPipeline.RegisterJsBundle("/bundle/js/jquery.js",
                lib + "modernizr/modernizr.js",
                lib + "jquery/jquery-3.4.1.js");


            if (!appContext.IsInstalled)
            {
                return;
            }

            /* Vue --> /bundle/js/vue.js
			-----------------------------------------------------*/
            assetPipeline.RegisterJsBundle("/bundle/js/vue.js",
                lib + "vue/vue.js");


            /* File uploader --> /bundle/js/fileuploader.js
			------------------------------------------------------*/
            assetPipeline.RegisterJsBundle("/bundle/js/fileuploader.js",
                lib + "dropzone/js/dropzone.js",
                js + "smartstore.dropzoneWrapper.js");


            /* Image Gallery --> /bundle/js/smart-gallery.js
			------------------------------------------------------*/
            assetPipeline.RegisterJsBundle("/bundle/js/smart-gallery.js",
                lib + "drift/Drift.js",
                lib + "photoswipe/photoswipe.js",
                lib + "photoswipe/photoswipe-ui-default.js",
                js + "smartstore.gallery.js");
        }
    }
}

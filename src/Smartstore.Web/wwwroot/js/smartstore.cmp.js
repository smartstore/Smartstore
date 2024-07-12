/*
*  Project: Consent Management Platform (CMP)
*  Author: Michael Herzog, SmartStore AG
*  Description: This script provides a Consent Management Platform (CMP) that allows scripts to be loaded and executed only after the user has given his consent.
*/

"use strict";

function ConsentManagementPlatform() {
    // Private methods

    // Scripts which require consent are included with the data-src attribute if no consentt is given yet.
    // If consent is given, we replace the data-src attribute with the src attribute.
    function loadScriptFromUrl(script) {
        const src = script.getAttribute('data-src');
        if (src) {
            // Load script now.
            script.src = src;
            script.removeAttribute('data-consent-required');
            script.removeAttribute('data-src');
        }
    }
    // We assume scripts which require specific consent are included with the type attribute text/plain.
    // Thus they won't be executed until we inject them into the DOM.
    // If consent is given, we replace the script tag with a new script tag that contains the actual script code.
    function loadInlineScript(script) {
        var newScript = document.createElement('script');
        newScript.innerHTML = script.innerHTML;
        script.parentNode.replaceChild(newScript, script);
        //document.body.appendChild(newScript);
    }

    // Topics which depend on cookie consent before they can be loaded are wrapped in template tags. 
    // If consent is given, we replace the template tags with the actual content.
    function loadTemplateContent(template) {
        var clone = template.content.cloneNode(true);
        template.parentNode.replaceChild(clone, template);
    }

    // Returns the cookie value by name.
    function getCookie(name) {
        let match = document.cookie.match(new RegExp('(^| )' + name + '=([^;]+)'));
        return match ? match[2] : null;
    }

    // Public properties
    this.CookieManagerDialog = null;
    this.Form = null;
    this.ConsentCookie = null;
    this.ConsentData = null;
    this.ConsentType = {
        Required: "required",
        Analytics: "analytics",
        ThirdParty: "thirdparty",
        ConsentAdUserData: "consentaduserdata",
        ConsentAdPersonalization: "consentadpersonalization"
    };

    // Public methods
    this.init = function () {
        var self = this;

        self.initCookieData();
        self.loadScripts();

        // Open consent dialog when user clicks on cookie manager link.
        $(document).on("click", ".cookie-manager", function (e) {
            e.preventDefault();

            self.showConsentDialog($(this).attr("href"));
        });
    }
    this.initCookieData = function () {
        var self = this;
        self.ConsentCookie = getCookie('.Smart.CookieConsent');

        if (self.ConsentCookie) {
            self.ConsentData = JSON.parse(decodeURIComponent(self.ConsentCookie));
        }
        else {
            return;
        }
    }
    this.loadScripts = function () {
        var self = this;

        for (let prop in self.ConsentType) {
            // Check to make sure the property is not from the prototype chain
            if (self.ConsentType.hasOwnProperty(prop)) {

                //console.log("Check for prop " + prop + ". Type:" + this.ConsentType[prop] + ". Result:" + self.checkConsent(this.ConsentType[prop]));

                if (self.checkConsent(self.ConsentType[prop])) {
                    // Load scripts included via URL.
                    var scripts = document.querySelectorAll('script[data-consent="' + self.ConsentType[prop] + '"][data-src]');
                    scripts.forEach(function (script) {
                        loadScriptFromUrl(script);
                    });

                    // Load scripts included via inline code.
                    var inlineScripts = document.querySelectorAll('script[data-consent="' + self.ConsentType[prop] + '"][type="text/plain"]');
                    inlineScripts.forEach(function (script) {
                        loadInlineScript(script);
                    });

                    // Inject HTML from template tags into DOM.
                    var templates = document.querySelectorAll('template[data-consent="' + self.ConsentType[prop] + '"]');
                    templates.forEach(function (template) {
                        loadTemplateContent(template);
                    });
                }
            }
        }
    }
    this.checkConsent = function (consentType) {
        // If cookie is not set, user has not given his consent. So nothing is allowed.
        if (!this.ConsentData) {
            return false; 
        }

        switch (consentType) {
            case "required":
                return true;
            case "analytics":
                return this.ConsentData.AllowAnalytics;
            case "thirdparty":
                return this.ConsentData.AllowThirdParty;
            case "consentaduserdata":
                return this.ConsentData.AdUserDataConsent;
            case "consentadpersonalization":
                return this.ConsentData.AdPersonalizationConsent;
            default:
                return false;        
        }
    }

    // Used by CookieManager Dialog to update the UI state of a checkbox.
    this.updateCheckboxUIState = function (elem) {
        var checked = elem.is(":checked");

        if (!checked) {
            elem.trigger("click");
        }
    }
    // Is called when user clicks on the "Save" button in the cookie manager dialog.
    this.onConsented = function () {
        var self = Smartstore.Cmp;

        // Update cookie data & load all scripts according to the new consent.
        self.initCookieData();
        self.loadScripts();

        self.hideConsentDialog();
    }
    // Is called when user clicks on a cookie manager link.
    this.showConsentDialog = function (loadDialogAjaxLink) {
        var self = Smartstore.Cmp;
        var dialog = $("#cookie-manager-window");

        if (dialog.length > 0) {
            // Dialog was already loaded > just open dialog.
            initAndShowConsentDialog(dialog);
        }
        else {
            // Dialog wasn't loaded yet > get view via ajax call and append to body.
            $.ajax({
                cache: false,
                type: "POST",
                url: loadDialogAjaxLink,
                data: {},
                success: function (data) {
                    $("body").append(data);
                    initAndShowConsentDialog($("#cookie-manager-window"));
                }
            });
        }
        function initAndShowConsentDialog(dialog) {
            self.CookieManagerDialog = dialog;
            self.Form = dialog.find("#cookie-manager-consent");
            self.CookieManagerDialog.modal('show');
        }
    }
    this.hideConsentDialog = function () {
        this.CookieManagerDialog.modal('hide');
    }

    this.init();
}

(function ($, window, document, undefined) {
    Smartstore.Cmp = new ConsentManagementPlatform();
} (jQuery, this, document));
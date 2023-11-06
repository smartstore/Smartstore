(function ($, window, document, undefined) {

    // TODO: Implement ValidationAttributes in Smartstore.Validation namespace

    // Message localization
    if ($.validator.messages && window.Res) {
        let r = window.Res;
        $.extend($.validator.messages, {
            required: r["Jquery.Validate.Required"],
            remote: r["Jquery.Validate.Remote"],
            email: r["Jquery.Validate.Email"],
            url: r["Jquery.Validate.Url"],
            date: r["Jquery.Validate.Date"],
            dateISO: r["Jquery.Validate.DateISO"],
            number: r["Jquery.Validate.Number"],
            digits: r["Jquery.Validate.Digits"],
            creditcard: r["Jquery.Validate.Creditcard"],
            equalTo: r["Jquery.Validate.Equalto"],
            extension: r["Jquery.Validate.Extension"],
            maxlength: $.validator.format(r["Jquery.Validate.Maxlength"]),
            minlength: $.validator.format(r["Jquery.Validate.Minlength"]),
            rangelength: $.validator.format(r["Jquery.Validate.Rangelength"]),
            range: $.validator.format(r["Jquery.Validate.Range"]),
            max: $.validator.format(r["Jquery.Validate.Max"]),
            min: $.validator.format(r["Jquery.Validate.Min"])
        });
    }

    // FileExtensions validation
    $.validator.unobtrusive.adapters.add('fileextensions', ['extensions'], function (options) {
        var params = {
            extensions: options.params.extensions.split(',')
        };

        options.rules['fileextensions'] = params;
        if (options.message) {
            options.messages['fileextensions'] = options.message;
        }
    });

    $.validator.addMethod("fileextensions", function (value, element, param) {
        if (!value)
            return true;

        var extension = getFileExtension(value);
        var validExtension = $.inArray(extension, param.extensions) !== -1;
        return validExtension;
    });

    function getFileExtension(fileName) {
        var extension = (/[.]/.exec(fileName)) ? /[^.]+$/.exec(fileName) : undefined;
        if (extension != undefined) {
            return extension[0];
        }
        return extension;
    };


    // FileSize validation
    jQuery.validator.unobtrusive.adapters.add('filesize', ['maxbytes'], function (options) {
        // Set up test parameters
        var params = {
            maxbytes: options.params.maxbytes
        };

        // Match parameters to the method to execute
        options.rules['filesize'] = params;
        if (options.message) {
            // If there is a message, set it for the rule
            options.messages['filesize'] = options.message;
        }
    });

    jQuery.validator.addMethod("filesize", function (value, element, param) {
        if (value === "") {
            // no file supplied
            return true;
        }

        var maxBytes = parseInt(param.maxbytes);

        // use HTML5 File API to check selected file size
        // https://developer.mozilla.org/en-US/docs/Using_files_from_web_applications
        // http://caniuse.com/#feat=fileapi
        if (element.files != undefined && element.files[0] != undefined && element.files[0].size != undefined) {
            var filesize = parseInt(element.files[0].size);

            return filesize <= maxBytes;
        }

        // if the browser doesn't support the HTML5 file API, just return true
        // since returning false would prevent submitting the form 
        return true;
    });


    // MustBeTrue validation
    jQuery.validator.unobtrusive.adapters.addBool("mustbetrue");
    jQuery.validator.addMethod("mustbetrue", function (value, element, param) {
        return element.checked;
    });

    // Creditcard validation
    jQuery.validator.unobtrusive.adapters.add('creditcard', ['cardtype'], function (options) {
        options.rules['creditcard'] = {
            cardtype: options.params.cardtype
        };
        if (options.message) {
            options.messages['creditcard'] = options.message;
        }
    });

    jQuery.validator.addMethod('creditcard', function (value, element, params) {
        return isValidCreditCard(value);
    });

    function isValidCreditCard(value) {
        value = value.replace(/\s|-/g, '');

        if (!/^\d+$/.test(value)) {
            return false;
        }

        var reversed = value.split('').reverse().map(Number);

        // Execute Luhn-Algorithm
        var sum = 0;
        for (var i = 0; i < reversed.length; i++) {
            if (i % 2 === 1) {
                reversed[i] *= 2;
                if (reversed[i] > 9) {
                    reversed[i] -= 9;
                }
            }
            sum += reversed[i];
        }

        return sum % 10 === 0;
    }

    // Validator <> Bootstrap
    function setControlFeedback(ctl, success) {
        if (ctl.is(':checkbox') || ctl.is(':radio')) {
            return;
        }

        if (success) {
            ctl.addClass('is-valid')
                .removeClass('is-invalid')
                .find('+ span[data-valmsg-for]')
                .addClass('valid-feedback');
        }
        else {
            ctl.removeClass('is-valid')
                .addClass('is-invalid')
                .find('+ span[data-valmsg-for]')
                .addClass('invalid-feedback');
        }
    }

    $.validator.setDefaults({
        onfocusout: function (el, e) {
            if ($(el).is(".is-valid, .is-invalid")) {
                $(el).valid();
            }
        },
        onkeyup: function (el, e) {
            if ($(el).is(".is-valid, .is-invalid")) {
                $(el).valid();
            }
        },
        onclick: false,
        highlight: function (el, errorClass, validClass) {
            //$(el).addClass('is-invalid').removeClass('is-valid');
            setControlFeedback($(el), false);
        },
        unhighlight: function (el, errorClass, validClass) {
            if ($(el).is(".is-invalid")) {
                setControlFeedback($(el), true);
            }
        },
        ignore: "input[type=date]"
    });
})(jQuery, this, document);
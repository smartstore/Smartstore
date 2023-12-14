
(function (smApiConsumer, $, undefined) {

	smApiConsumer.settings = {
		publicKey: '',
		secretKey: '',
		url: '',
		odataService: '/odata/v1',
		httpAcceptType: 'application/json'
	};

	smApiConsumer.init = function (settings) {
		$.extend(this.settings, settings);

		ensureIso8601Date();
	};

	smApiConsumer.createAuthorizationHeader = function () {
		var keys = this.settings.publicKey + ':' + this.settings.secretKey;
		return 'Basic ' + window.btoa(keys);
	};

	smApiConsumer.startRequest = function (options) {
		var self = this;
		var ajaxOptions = {
			url: this.settings.url + options.resource,
			type: options.method || 'GET',
			accepts: this.settings.httpAcceptType,
			cache: false,
			headers: {
				"Accept": this.settings.httpAcceptType
			},
			beforeSend: function (jqXHR, settings) {
				Callback(options.beforeSend, jqXHR, settings);
			}
		};

		if (options.content) {
			var data = options.content;

			if (typeof(data) === 'object') {
				data = JSON.stringify(data);
			}

			$.extend(ajaxOptions, {
				contentType: 'application/json',
				dataType: 'json',
				data: data
			});
		}

		$.extend(ajaxOptions.headers, {
			"Authorization": this.createAuthorizationHeader()
		});

		$.ajax(ajaxOptions)
			.done(function (data, textStatus, jqXHR) {
				Callback(options.done, data, textStatus, jqXHR);
			})
			.fail(function (jqXHR, textStatus, errorThrown) {
				Callback(options.fail, jqXHR, textStatus, errorThrown);
			});
	}


	// see https://developer.mozilla.org/en-US/docs/JavaScript/Reference/Global_Objects/Date/toISOString
	function ensureIso8601Date() {
		if (Date.prototype.toISOString)
			return;

		function pad(number) {
			return (number < 10 ? '0' + number : number);
		}

		// fallback
		Date.prototype.toISOString = function () {
			return this.getUTCFullYear() +
				'-' + pad(this.getUTCMonth() + 1) +
				'-' + pad(this.getUTCDate()) +
				'T' + pad(this.getUTCHours()) +
				':' + pad(this.getUTCMinutes()) +
				':' + pad(this.getUTCSeconds()) +
				'.' + (this.getUTCMilliseconds() / 1000).toFixed(3).slice(2, 5) +
				'Z';
		};
	}

	function Callback(func) {
		if (typeof func === 'function')
			return func.apply(this, Array.prototype.slice.call(arguments, 1));
		return null;
	}

}(window.smApiConsumer = window.smApiConsumer || {}, jQuery));
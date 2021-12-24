(function ($, window, document, undefined) {
	window.initGoogleProductTaxonomy = function (el) {
		el = $(el);
		el.select2({
			//width: 'style',
			dropdownAutoWidth: true,
			allowClear: true,
			theme: 'bootstrap',
			closeOnSelect: true,
			placeholder: el.data("default-category"),
			language: el.data("language-seo-code"),
			ajax: {
				delay: 300,
				cache: true,
				dataType: 'json',
				type: 'GET',
				url: el.data("select-url"),
				data: function (params) {
					var query = {
						search: params.term || "",
						page: params.page || 1
					}
					return query;
				},
				processResults: function (data, params) {
					params.page = params.page || 1;

					return {
						results: data.results,
						pagination: {
							more: data.hasMoreItems
						}
					};
				}
			}
		});
    };
})( jQuery, this, document );
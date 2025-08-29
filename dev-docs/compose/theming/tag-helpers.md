# ✔️ Tag Helpers

Tag Helpers are extensions of existing HTML tags. They can extend the functionality of a tag or even create completely new tags. Smartstore has created a lot of Tag Helpers to write simple and clear code and for better productivity.

## How To Use Tag Helpers

To use the Smartstore Tag Helpers in your Views, you'll need to reference specific libraries. You can do this either in your View file or by using `_ViewImports.cshtml` (recommended). Add the following lines and IntelliSense should recognise the Tag Helpers.

```
//for public Tag Helpers
@addTagHelper Smartstore.Web.TagHelpers.Public.*, Smartstore.Web.Common

//for shared Tag Helpers
@addTagHelper Smartstore.Web.TagHelpers.Shared.*, Smartstore.Web.Common

//for admin Tag Helpers
@addTagHelper Smartstore.Web.TagHelpers.Admin.*, Smartstore.Web.Common
```

{% hint style="info" %}
To learn more about \_ViewImports.cshtml, check out this [tutorial](https://learn.microsoft.com/en-us/aspnet/core/mvc/views/tag-helpers/intro?view=aspnetcore-6.0#addtaghelper-makes-tag-helpers-available).
{% endhint %}

{% hint style="info" %}
Try it out!

Type: <mark style="color:green;">`<span s`</mark> You should see a suggestion for `sm-if`, which is part of Smartstore's `IfTagHelper`.
{% endhint %}

A quick example:

```cshtml
<span sm-if="@Model.visible">Here I am!</span>
```

## Smartstore Tag Helpers

Smartstore differentiates between three kinds of Tag Helpers.

* Public: Used in frontend
* Shared: Used in both front- and backend
* Admin: Used in backend

You can find the code to Smartstore Tag Helpers in _Smartstore.Web.Common/TagHelpers/_.

### Public

<details>

<summary>Captcha Tag Helper</summary>

The `CaptchaTagHelper` creates a [CAPTCHA](https://en.wikipedia.org/wiki/CAPTCHA).

```cshtml
<captcha sm-enabled="Model.DisplayCaptcha" />
```

It also supports the `sm-enabled` attribute.

</details>

<details>

<summary>ConsentScript Tag Helper</summary>

The `ConsentScriptTagHelper` allows a script to be loaded immediately after cookie consent is given. Otherwise it is loaded after a page refresh.

```cshtml
<script sm-consent-type="Analytics">
```

* `sm-consent-type`: The type of cookie the user needs to accept.

</details>

<details>

<summary>Honeypot Tag Helper</summary>

The `HoneypotTagHelper` is Smartstore's cyber-security implementation of the [Honeypot](https://en.wikipedia.org/wiki/Honeypot_\(computing\)) mechanism.

```cshtml
<honeypot />
```

It also supports the `sm-enabled` attribute.

</details>

### Shared

<details>

<summary>Attributes Tag Helper</summary>

The `AttributesTagHelper` adds attributes to the element. It can be used in two ways:

* Adding a collection of attributes to the element.
* Adding an attribute, if it evaluates to true.

```cshtml
@{
    var attributes = new AttributeDictionary().Merge(ConvertUtility.ObjectToDictionary(ViewData["htmlAttributes"] ?? new object()));
}
<span attrs="attributes">I might have some attributes</span>

//or

<input type="checkbox" attr-checked='(node.HasChildren, "checked")' />
```

</details>

<details>

<summary>CollapsedContent Tag Helper</summary>

The `CollapsedContentTagHelper` collapses the element to a maximum height. It also adds _Show more_ or _Show less_ to the element.

```cshtml
<collapsed-content sm-max-height="50">
    Odit non aspernatur sunt ipsum dolorem nihil quibusdam earum.<br />
    Eius nulla magni cum cum delectus sit omnis. Quam aut itaque ut.<br />
    Adipisci nihil enim aut eos voluptas et. Iure ut maxime ut qui.<br />
    Impedit adipisci laborum quia pariatur. Laboriosam voluptatibus<br />
    atque qui minima et ut deleniti.<br />
    <br />
    Debitis beatae aut aut iusto non consequuntur. Et inventore placeat<br />
    alias ut consequatur corrupti. Ut qui laboriosam amet tempora velit<br />
    sed est. Dolorem doloremque reiciendis voluptatem quasi nemo<br />
    perferendis quo. Voluptas exercitationem consequatur dolorum omnis<br />
    porro necessitatibus dignissimos qui.<br />
    <br />
    Consectetur et corporis vel voluptas autem libero magnam. Mollitia<br />
    pariatur placeat ut. Dolores quidem molestiae dolore ut accusamus<br />
    quam dolorem iure. Nihil optio voluptatibus eum quis.<br />
    <br />
    Officia a accusantium nihil voluptas et. Error aut labore est qui<br />
    rem. Fugiat perspiciatis repellendus voluptatem aut qui dolorem.
</collapsed-content>
```

To specify the maximum number of pixel you want to show, add the `sm-max-height` attribute. Otherwise the catalog's default setting will be used.

</details>

<details>

<summary>Confirm Tag Helper</summary>

The `ConfirmTagHelper` adds a confirm button. There are many ways to customise it.

```cshtml
<confirm button-id="entry-delete" />

<confirm message="@T("Common.AreYouSure")" button-id="entry-delete" icon="fas fa-question-circle" action="EntryDelete" type="Action" />
```

It also supports these attributes:

* `action`: Action to execute, if confirmed. Default: `Delete`
* `controller`: Controller to search for `action`. Default: `ViewContext.RouteData.Values.GetcontrollerName()`
* `form-post-url`
* `type`: Type of confirm action. Default: `Delete`
* `backdrop`: Button has a backdrop. Default: `true`
* `title`: Title of the dialog.
* `accept-button-color`: Color of the accept button.
* `accept-text`: Custom accept button text.
* `cancel-text`: Custom cancel button text.
* `center`: Dialog is centered vertically. Default: `true`
* `center-content`: Dialog is centered. Default: `false`
* `size`: Size of the dialog. Possible values: Small, Medium, Large, Flex, FlexSmall. Default: `Medium`
* `message`: Custom display message.
* `icon`: Icon class.
* `icon-color`: Custom icon color.

</details>

<details>

<summary>FileIcon Tag Helper</summary>

The `FileIconTagHelper` display a file icon.

```cshtml
<file-icon file-extension="@Model.FileExtension" show-label="true" badge-class="badge-info"/>
```

It also supports these attributes:

* `label`: Custom label. Default: the files extension

</details>

<details>

<summary>FileUploader Tag Helper</summary>

The `FileUploaderTagHelper` adds a highly customisable way to upload files.

Here is an excerpt from _Smartstore.Web/Views/Customer/Avatar.cshtml_ to show this.

{% code title="Avatar.cshtml" %}
```cshtml
<file-uploader 
    file-uploader-name="uploadedFile"
    upload-url='@Url.Action("UploadAvatar", "Customer")'
    type-filter="image"
    display-browse-media-button="false"
    display-remove-button="fileId != 0"
    display-remove-button-after-upload="true"
    upload-text='@T("Common.FileUploader.UploadAvatar")'
    onuploadcompleted="onAvatarUploaded"
    onfileremoved="onAvatarRemoved"
    multi-file="false"
    has-template-preview="true" />
```
{% endcode %}

The default value for the attribute `media-path` is `SystemAlbumProvider.Files`.

</details>

<details>

<summary>If Tag Helper</summary>

The `IfTagHelper` adds a conditional attribute to the element. The output is suppressed, if the condition evaluates to `false`.

```cshtml
<span sm-if="@Model.visible">Here I am!</span>
```

</details>

<details>

<summary>MinifyTagHelper Tag Helper</summary>

The `MinifyTagHelper` allows an inline script to be minified automatically, before being loaded. Default: `false`

```cshtml
<script sm-minify="true">
```

This should not be used if the script contains dynamic content like:

* SessionId
* A randomly generated ID
* User related data
* Model data

The Tag-Helper has no effect in combination with the `src` attribute.

</details>

<details>

<summary>SuppressIfEmpty Tag Helper</summary>

The `SuppressIfEmptyTagHelper` adds a conditional attribute to the element. The output is suppressed, if the condition is `true` and the element is empty or only contains whitespaces.

```cshtml
@{
    bool condition = true;
}

<div id="div1" sm-suppress-if-empty="condition">
    @* I will be suppressed *@
</div>

<div id="div2" sm-suppress-if-empty="condition">
    I won't be suppressed!
</div>

<div id="div3" sm-suppress-if-empty="!condition">
    @* I won't be suppressed *@
</div>
```

</details>

<details>

<summary>SuppressIfEmptyZone Tag Helper</summary>

The `SuppressIfEmptyZoneTagHelper` suppresses the output, if the targeted zone is empty or only contains whitespaces.

Here is an excerpt from _Smartstore.Web/Views/ShoppingCart/Partials/OffCanvasShoppingCart.cshtml_ to show this.

{% code title="" %}
```cshtml
<div sm-suppress-if-empty-zone="offcanvas_cart_summary" class="offcanvas-cart-external-checkout">
    <div class="heading heading-center py-0">
        <h6 class="heading-title fs-h5 fwn">@T("Common.Or")</h6>
    </div>
    <div class="d-flex justify-content-center align-items-center flex-wrap flex-column">
        <zone name="offcanvas_cart_summary" />
    </div>
</div>
```
{% endcode %}

</details>

<details>

<summary>TagName Tag Helper</summary>

The `TagNameTagHelper` changes the tag at runtime.

```cshtml
<span sm-tagname="div">I always wanted to be a div...</span>
```

</details>

<details>

<summary>Widget Tag Helper</summary>

The `WidgetTagHelper` adds HTML content and injects it into a [zone](../../framework/content/widgets.md#zones). More information can be found in [Widgets](../../framework/content/widgets.md#widget-tag-helper).

```cshtml
<widget target-zone="my_widget_zone">
    <span>Widget content</span>
</widget>
```

The `key` attribute makes sure only one instance of the widget is included in the zone.

</details>

<details>

<summary>Zone Tag Helper</summary>

The `ZoneTagHelper` defines an zone for widgets to inject content. More information can be found in [Widgets](../../framework/content/widgets.md#zones).

```cshtml
<zone name="a_widget_drop_zone_name"/>
```

It also supports these attributes:

* `model`: Declare what model to use within the zone.
* `replace-content`: Replace content, if at least one widget is rendered.
* `remove-if-empty`: Remove the root zone tag, if it has no content. Default: `false`
* `preview-disabled`: If true, the zone preview will not be rendered. This is important for script or style zones that should not be printed. Default: `false`
* `preview-class`: Additional CSS classes for the span tag. For example, `position-absolute` to better control the layout flow.
* `preview-style`: CSS style definitions. For example, to set an individual `max-width`.

</details>

#### Controls

<details>

<summary>EntityPicker Tag Helper</summary>

The `EntityPickerTagHelper` adds an element to pick one or more entities from a larger group.

```cshtml
<entity-picker asp-for="Rotator" max-items="100" entity-type="product" dialog-title="Search" />
```

It also supports these attributes:

* `entity-type`: Entity type to be picked. Default: `product`
* `target-input-selector`: Identifier of the target input, defined by `field-name`.
* `caption`: Caption of the dialog.
* `icon-css-class`: Icon of the button that opens the dialog. Default: `fa fa-search`
* `dialog-title`: Title of the dialog.
* `disable-grouped-products`: Disable search for grouped products.
* `disable-bundle-products`: Disable search for bundle products.
* `disabled-entity-ids`: Ids of disabled entities.
* `selected`: Ids of selected entities.
* `enable-thumb-zoomer`: Enables the thumb zoomer.
* `highlight-search-term`: Highlight search term in search results. Default: `true`
* `max-items`: Maximum number of selectable items.
* `append-mode`: Append selected entity ids to already chosen entities. Default: `true`
* `delimiter`: Entity id delimiter. Default: `,`
* `field-name`: Fieldname of \[target-input-selector] to paste selected ids. Default: `id`
* `ondialogloading`: JS function called _before_ dialog is loaded.
* `ondialogloaded`: JS function called _after_ dialog is loaded.
* `onselectioncompleted`: JS function called _after_ selection.

</details>

<details>

<summary>Menu Tag Helper</summary>

The `MenuTagHelper` adds a menu widget.

```
<menu name="Main" template="Categories"></menu>
```

</details>

<details>

<summary>Modal Tag Helper</summary>

The `ModalTagHelper` adds a customisable modal dialog. It works in combination with:

* `ModalHeaderTagHelper`
* `ModalBodyTagHelper`
* `ModalFooterTagHelper`

Here is an excerpt from _Smartstore.Web/Areas/Admin/Views/Theme/Configure.cshtml_ to show this.

{% code title="Configure.cshtml" %}
```cshtml
<modal id="importvariables-window">
    <modal-header sm-title="@T("Admin.Configuration.Themes.ImportVars")"></modal-header>
    <modal-body>
        <form enctype="multipart/form-data" method="post" asp-action="ImportVariables" asp-route-theme="@Model.ThemeName" asp-route-storeId="@Model.StoreId">
            <p class="text-muted">
                @T("Admin.Configuration.Themes.ImportVars.Note")
            </p>
            <div>
                @T("Admin.Configuration.Themes.ImportVars.XmlFile"): <input type="file" id="importxmlfile" name="importxmlfile" />
            </div>
        </form>
    </modal-body>
    <modal-footer>
        <button type="button" class="btn btn-secondary btn-flat" data-dismiss="modal">
            <span>@T("Admin.Common.Cancel")</span>
        </button>
        <button id="importxmlsubmit" type="button" class="btn btn-primary">
            <span>@T("Common.Import")</span>
        </button>
    </modal-footer>
</modal>
```
{% endcode %}

The `ModalTagHelper` also supports these attributes:

* `sm-size`: Size of the dialog. Possible values: Small, Medium, Large, Flex, FlexSmall. Default: `Medium`
* `sm-fade`: Show fade animation. Default: `true`
* `sm-focus`: Has focus. Default: `true`
* `sm-backdrop`: Backgrop behaviour. Possible values: Show, Hide, Static, Inverse, Invisible. Default: `Show`
* `sm-show`: Show the dialog immidiately. Default: `true`
* `sm-close-on-escape-press`: Dialog closes on pressing `Esc`. Default: `true`
* `sm-center-vertically`: Center content vertically. Default: `false`
* `sm-center-content`: Center content. Default: `false`
* `sm-render-at-page-end`: Insert dialog at the end of the page. Default: `true`

The `ModalHeaderTagHelper` also supports these attributes:

* `sm-title`: The dialog title.
* `sm-show-close`: Show close button. Default: true

The `ModalBodyTagHelper` also supports these attributes:

* `sm-content-url`: URL to content. Content is included via iframe.

</details>

<details>

<summary>Pagination Tag Helper</summary>

The `PaginationTagHelper` adds [pagination](https://en.wikipedia.org/wiki/Pagination).

```cshtml
<pagination sm-list-items="Model.MySubscriptions" />
```

Pass an `IPageable` object that should be _paged_, using the `sm-list-items` attribute.

It also supports these attributes:

* `sm-list-items`
* `sm-alignment`: Element alignment. Possible values: Left, Centered, Right. Default: `Centered`
* `sm-size`: Element size. Possible values: Mini, Small, Medium, Large. Default: `Medium`
* `sm-style`: Element style. Possible values: Pagination, Blog
* `sm-show-first`: Always show first page. Default: `false`
* `sm-show-last`: Always show last page. Default: `false`
* `sm-show-next`: Always show next page. Default: `true`
* `sm-show-previous`: Always show previous page. Default: `true`
* `sm-max-pages`: Maximum number of displayed pages. Default: `8`
* `sm-skip-active-state`
* `sm-item-title-format-string`
* `sm-query-param`: Default: `page`

</details>

<details>

<summary>TabStrip Tag Helper</summary>

The `TabStripTagHelper` adds a tab strip. It works with the `TabTagHelper`.

```cshtml
<tabstrip id="tab-example" sm-nav-style="Material" sm-nav-position="Top">
    <tab sm-title="First Tab" sm-selected="true">
        <partial name="Tab1" model="TabModel" />
    </tab>
    <tab sm-title="Second Tab" sm-selected="true">
        <partial name="Tab2" model="TabModel" />
        </tab>
</tabstrip>
```

The `TabStripTagHelper` also supports these attributes:

* `sm-hide-single-item`: Hide navigation on single tab. Default: `true`
* `sm-responsive`: Collapse navigation on smaller screens. Default: `false`
* `sm-nav-position`: Position of the navigation. Possible values: Top, Right, Below, Left Default: `Top`
* `sm-nav-style`: Style of the navigation. Possible values: Tabs, Pills, Material.
* `sm-fade`: Add fade animation. Default: `true`
* `sm-smart-tab-selection`: Reselect active tab on reload. Default: `true`
* `sm-onajaxbegin`
* `sm-onajaxsuccess`
* `sm-onajaxfailure`
* `sm-onajaxcomplete`
* `sm-publish-event`: Fires the `TabStripCreated` event. Default: `true`

The `TabTagHelper` also supports these attributes:

* `sm-name`
* `sm-title`
* `sm-selected`
* `sm-disabled`
* `sm-visible`: Tab visibility. Default: `true`
* `sm-hide-if-empty`: Default: `false`
* `sm-ajax`: Load content with AJAX
* `sm-icon`
* `sm-icon-class`
* `sm-badge-text`
* `sm-badge-style`: Badge Style. Possible values: Secondary, Primary, Success, Info, Warning, Danger, Light, Dark.
* `sm-image-url`
* `sm-adaptive-height`: Responsive height. Default: `false`

</details>

#### Forms

<details>

<summary>AjaxForm Tag Helper</summary>

The `AjaxFormTagHelper` adds unobtrusive AJAX to a form.

```cshtml
<form sm-ajax method="post" asp-area="" asp-action="Do" sm-onsuccess="OnDo@(Model.Id)" sm-loading-element-id="#do-prog-@(Model.Id)">
```

It also supports these attributes:

* `sm-ajax`: The form is an unobrusive AJAX form.
* `sm-confirm`: Custom confirm message.
* `sm-onbegin`
* `sm-oncomplete`
* `sm-onfailure`
* `sm-onsuccess`
* `sm-allow-cache`
* `sm-loading-element-id`
* `sm-loading-element-duration`
* `sm-update-target-id`
* `sm-insertion-mode`: Mode of insertion of the response. Possible values: Replace, InsertBefore, InsertAfter, ReplaceWith.

Further information can be found in this [explanation](https://www.learnrazorpages.com/razor-pages/ajax/unobtrusive-ajax).

</details>

<details>

<summary>ColorBox Tag Helper</summary>

The `ColorBoxTagHelper` adds a color-picker. It is used under the hood of the `SettingEditorTagHelper` to display colors.

```cshtml
<colorbox asp-for="MyColour" sm-default-color="#ff2030" />
```

</details>

<details>

<summary>Editor Tag Helper</summary>

The `EditorTagHelper` adds a customisable input field. It works similiar to the `SettingEditorTagHelper`.

```cshtml
<editor asp-for="PriceInclTax" sm-postfix="@primaryStoreCurrencyCode" />
```

To add more `ViewData` attributes, use `asp-additional-viewdata`.

</details>

<details>

<summary>FormControl Tag Helper</summary>

The `FormControlTagHelper` adds labels and CSS classes to form elements.

```cshtml
<input type="text" asp-for="Name" />
<input type="checkbox" asp-for="MyBool" sm-switch />
```

It also supports these attributes:

* `sm-append-hint`
* `sm-ignore-label`
* `sm-switch`: Process checkboxes as switches. Default: `true`
* `sm-control-size`: Size of the element. Default: `Medium`
* `sm-plaintext`: View as plaintext.
* `sm-required`

</details>

<details>

<summary>Hint Tag Helper</summary>

The `HintTagHelper` displays the localised Hint of resource passed in `asp-for`.

```cshtml
<div><span>John Smith</span><hint asp-for="Name" /></div>
```

</details>

<details>

<summary>InputPassword Tag Helper</summary>

The `InputPasswordTagHelper` adds the ability to toggle the visibility of a password input.

```cshtml
<input type="password" sm-enable-visibility-toggle="false"/>
```

* `sm-enable-visibility-toggle`: Displays an eye icon to toggle the visibility of the password. Default: `true`

</details>

<details>

<summary>NumberInput Tag Helper</summary>

The `NumberInputTagHelper` extends the number-input's customisability and styles the element.

```cshtml
<input type="number" sm-decimals="2" sm-numberinput-style="centered" asp-for="price"/>
```

</details>

<details>

<summary>TripleDatePicker Tag Helper</summary>

The `TripleDatePickerTagHelper` adds a customisable date-picker displaying the day, month and year.

{% code overflow="wrap" %}
```cshtml
<triple-date-picker day-name="@(controlId + "-day")" month-name="@(controlId + "-month")" year-name="@(controlId + "-year")" day="Model.SelectedDay" month="Model.SelectedMonth" year="Model.SelectedYear" begin-year="Model.BeginYear" end-year="Model.EndYear" disabled="Model.IsDisabled" />
```
{% endcode %}

The default value for `control-size` is `Medium`.

</details>

#### Media

All media Tag Helpers support the following attributes: `sm-file`, `sm-file-id`, `sm-url-host`.

All image-based Tag Helpers (`ImageTagHelper`, `MediaTagHelper`, `ThumbnailTagHelper`, `VideoTagHelper`) support the following attributes: `sm-model`, `sm-size`, `sm-width`, `sm-height`, `sm-resize-mode`, `sm-anchor-position` and `sm-no-fallback`.

<details>

<summary>Audio Tag Helper</summary>

The `AudioTagHelper` adds an audio element.

```cshtml
<audio sm-file="AudioFile" />
```

</details>

<details>

<summary>Image Tag Helper</summary>

The `ImageTagHelper` adds an image with attributes used in `Model` or the File.

```cshtml
<img sm-file="JPGFile"/>
```

</details>

<details>

<summary>Media Tag Helper</summary>

The `MediaTagHelper` adds a suitable tag for a given media type.

```cshtml
<media sm-file="Model.CurrentFile" sm-size="Model.ThumbSize" alt="@picAlt" title="@picTitle" />
```

</details>

<details>

<summary>Thumbnail Tag Helper</summary>

The `ThumbnailTagHelper` adds a thumbnail of a media file.

```cshtml
<media-thumbnail sm-file="MediaFile" sm-size="ThumbSize" />
```

</details>

<details>

<summary>Video Tag Helper</summary>

The `VideoTagHelper` adds a video element.

```cshtml
<video sm-file="VideoFile" controls preload="metadata" />
```

</details>

### Admin

<details>

<summary>BackTo Tag Helper</summary>

The `BackToTagHelper` adds a left arrow icon to a link.

```cshtml
<a href="#" sm-backto>Go back</a>
```

</details>

<details>

<summary>DataGrid Tag Helper</summary>

There are different types of DataGrid Tag Helpers. They all work together to extend the functionality of the Grid Tag Helper.

Here is an excerpt from _Smartstore.Web/Areas/Admin/Views/ActivityLog\_Grid.ActivityLogs.cshtml_ to show this.

{% code title="_Grid.ActivityLogs.cshtml" %}
```cshtml
<datagrid allow-resize="true" allow-row-selection="true" allow-column-reordering="true">
    <datasource read="@Url.Action("ActivityLogList", "ActivityLog")"
                delete="@Url.Action("ActivityLogDelete", "ActivityLog")" />
    <sorting enabled="true">
        <sort by="CreatedOn" by-entity-member="CreatedOnUtc" descending="true" />
    </sorting>
    <paging position="Bottom" show-size-chooser="true" />
    <toolbar>
        <toolbar-group>
            <button datagrid-action="DataGridToolAction.ToggleSearchPanel" type="button" class="btn btn-light btn-icon">
                <i class="fa fa-fw fa-filter"></i>
            </button>
        </toolbar-group>
        <zone name="datagrid_toolbar_alpha"></zone>
        <toolbar-group class="omega"></toolbar-group>
        <zone name="datagrid_toolbar_omega"></zone>
        <toolbar-group>
            <button type="submit" name="delete-all" id="delete-all" value="clearall" class="btn btn-danger no-anims btn-flat">
                <i class="far fa-trash-alt"></i>
                <span>@T("Admin.Common.DeleteAll")</span>
            </button>
            <button datagrid-action="DataGridToolAction.DeleteSelectedRows" type="button" class="btn btn-danger no-anims btn-flat">
                <i class="far fa-trash-alt"></i>
                <span>@T("Admin.Common.Delete.Selected")</span>
            </button>
        </toolbar-group>
    </toolbar>
    <search-panel>
        <partial name="_Grid.ActivityLogs.Search" model="parentModel" />
    </search-panel>
    <columns>
        <column for="ActivityLogTypeName" entity-member="ActivityLogType.Name" hideable="false" />
        <column for="Comment" wrap="true" />
        <column for="CustomerEmail" entity-member="Customer.Email" type="string">
            <display-template>
                <a :href="item.row.CustomerEditUrl" class="text-truncate">
                    {{ item.value }}
                </a>
            </display-template>
        </column>
        <column for="IsSystemAccount" halign="center" sortable="false" />
        <column for="CreatedOn" entity-member="CreatedOnUtc" />
    </columns>
    <row-commands>
        <a datarow-action="DataRowAction.Delete">@T("Common.Delete")</a>
    </row-commands>
</datagrid>
```
{% endcode %}

</details>

<details>

<summary>SettingEditor Tag Helper</summary>

The `SettingEditorTagHelper` provides automatic HTML-Input type Mapping.

```cshtml
<setting-editor asp-for="Name"></setting-editor>
```

It automatically checks the type of the variable passed in `asp-for` and looks for an appropriate HTML input. Additionally it offers model binding and matching.

</details>

<details>

<summary>SmartLabel Tag Helper</summary>

The `SmartLabelTagHelper` displays a label and an optional hint.

```cshtml
<smart-label asp-for="Name" />
```

It also supports these attributes:

* `sm-ignore-hint`: Hint will be ignored. Default: `true`
* `sm-text`: Custom label text.
* `sm-hint`: Custom label hint.

</details>

## Further Reading

If you are interested in writing your own `TagHelper` or learning more about them, follow this [Microsoft Tag Helper tutorial](https://learn.microsoft.com/en-us/aspnet/core/mvc/views/tag-helpers/intro?view=aspnetcore-6.0).

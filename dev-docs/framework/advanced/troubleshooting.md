# 🐣 Troubleshooting

When the database contains long texts, especially those obtained by copying content from the Web with embedded Base64 images, Smartstore may behave unpredictably and become unresponsive. The following solutions can help identify and resolve a variety of issues.

## Extremely long text and MSSQL

`UseSequentialDbDataReader` is a database system preference located in _appsettings.json_. When set to `true`, it can solve massive performance problems with databases overloaded with very long text entities. Just one entity with a field containing long text can be catastrophic. It can cause all database requests associated with the entity to become extremely slow and unresponsive.

Set `UseSequentialDbDataReader` to `true` to solve this problem.

{% hint style="info" %}
Postgre and MySQL are not affected by the large text problem.
{% endhint %}

## Offload embedded images

Long text content can be caused by embedded Base64 images in the HTML code. These should be removed from the text and stored in the file system. The **Offload embedded images** function has been created for this purpose. To accomplish this, it does the following:

1. Scans long texts for Base64 encoded images.
2. Extracts the images.
3. Places the images in the media storage.
4. Replaces the Base64 in the long text with a link to the image.

There is no UI for this, it's invoked via the URL: _/admin/maintenance/offloadembeddedimages/_. Admin access is required, and the `UseSequentialDbDataReader` setting must be enabled before using this function to ensure that the entities can be loaded with high performance.

This process can take a long time and use a lot of CPU. Therefore, the default setting is to process a maximum of `200` images per cycle. If you want to process more images at once, avoiding to repeat the task multiple times, add the `take` URL-Parameter: `?take=<MaxNumOfImagesToProcess>`

{% hint style="warning" %}
If you get a timeout error, you'll need to reduce the number of images to process.
{% endhint %}

When all embedded images have been successfully offloaded, set the `UseSequentialDbDataReader` setting back to `false`.

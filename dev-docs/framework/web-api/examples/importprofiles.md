# ✔ Import Profiles

### Upload import files and start import

```
POST http://localhost:59318/odata/v1/ImportProfiles(16)/SaveFiles

Content-Type: text/plain; charset=utf-8
Content-Disposition: form-data; name=deleteFiles
True

Content-Type: text/plain; charset=utf-8
Content-Disposition: form-data; name=startImport
True

Content-Type: application/octet-stream
Content-Disposition: form-data; name="my-file-1"; filename="produktcsvexport.csv"
<Binary data for produktcsvexport.csv here (length 6645 bytes)…>
```

The file(s) are uploaded using a multipart form data POST request. You must specify an import profile ID for which the import files are intended. This can be the profile ID (see above) or the profile name passed via the **name** query string parameter.

You can also upload ZIP files which is useful for very large import files. ZIP files are always unzipped into the import folder of the profile.

**deleteExisting** specifies whether to delete all existing files including subfolders. **startImport** specifies whether to start the import.

{% hint style="info" %}
Existing import files are always overwritten.
{% endhint %}

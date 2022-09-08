### Breaking changes

- For PUT and PATCH requests, the HTTP header **Prefer** with the value **return=representation** must be sent to get a 
status code 200 with entity content response. This is the default behavior of ASP.NET Core OData 8.0.
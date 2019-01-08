# Cake FTP-Folder upload
This is a Cake addin to help upload the contents of a folder to a ftp server. 

Examples 


Uploading from a folder
```csharp
 FtpUploadFolder("./Artifacts/Api","ftp://somehost/somepath", "username", "p@ssword"); 
 ```

 Using simple sorting to ensure upload of web.config file first
 ```csharp
Func<IFile,object> sorting = x => (x.Path.Segments?.LastOrDefault()?.EndsWith("web.config") == true ? -5 : 10); 

FtpUploadFolder("./Artifacts/Api","ftp://somehost/somepath", "username", "p@ssword", sortPredicate: sorting ); 
 ```

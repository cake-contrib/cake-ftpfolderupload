using System;
using System.Collections.Generic;
using System.Linq;
using Cake.Core;
using Cake.Core.Diagnostics;
using Cake.Core.IO;

namespace Cake.FtpFolderUpload
{
    internal class FtpFolderUpload
    {
        private static readonly string[] ValidFtpCreateStatuses = new[] {"TRANSFER COMPLETE", "FILE RECEIVE OK"};

        private readonly IFileSystem _fileSystem;
        private readonly ICakeEnvironment _cakeEnvironment;
        private readonly ICakeLog _log;
        
        private readonly IEqualityComparer<string> _comparer;

        public FtpFolderUpload(IFileSystem fileSystem, ICakeEnvironment cakeEnvironment, ICakeLog log)
        {
            _fileSystem = fileSystem;
            _cakeEnvironment = cakeEnvironment;
            _log = log;
            _comparer = cakeEnvironment.Platform.IsUnix()
                ? StringComparer.Ordinal
                : StringComparer.OrdinalIgnoreCase;
        }

        public void UploadFolder(DirectoryPath folder, string ftpServer, string userName, string password,
            Func<IFile, bool> filterPredicate = null, Func<IFile, object> sort = null)
        {
            if (folder == null) throw new ArgumentNullException(nameof(folder));
            if (ftpServer == null) throw new ArgumentNullException(nameof(ftpServer));
            if (userName == null) throw new ArgumentNullException(nameof(userName));
            if (password == null) throw new ArgumentNullException(nameof(password));

            if (!Uri.TryCreate(ftpServer, UriKind.Absolute, out Uri uri))
            {
                throw new ArgumentException($"Value {ftpServer} is not a valid url");
            }

            if (uri.Scheme != Uri.UriSchemeFtp)
            {
                throw new ArgumentException("ftpServer is not using ftp protocol");
            }

            var files = _fileSystem.GetDirectory(folder).GetFiles("*.*", SearchScope.Recursive);
            if (filterPredicate != null)
            {
                files = files.Where(filterPredicate);
            }

            if (sort != null)
            {
                files = files.OrderBy(sort);
            }

            var rootPath = folder.IsRelative ? folder.MakeAbsolute(_cakeEnvironment) : folder;
            // We keep track of the visited directories so we only try 
            // to create them once (We cannot check to see if a directory exist)
            HashSet<string> directoriesVisited = new HashSet<string>(_comparer);

            foreach (var file in files)
            {
                var relativePath = rootPath.GetRelativePath(file.Path);
                var directory = relativePath.GetDirectory().FullPath;
                if (!string.IsNullOrWhiteSpace(directory) && !directoriesVisited.Contains(directory))
                {
                    _log.Verbose($"Trying to Ensure folderpath {directory }");
                    EnsureFolder(ftpServer,userName,password,directory);
                    directoriesVisited.Add(directory);
                }

                var ftpPath = $"{uri.ToString().TrimEnd('/')}/{relativePath.FullPath}";

                if (UploadFile(ftpPath, userName, password, file.Path))
                {
                    _log.Information($"Uploaded file {file.Path} to {ftpPath}");
                }
            }
        }

        private bool UploadFile(string ftpPath, string user, string password, FilePath filePath)
        {
            _log.Verbose($"Creating webrequest for {ftpPath}");

            if (!(System.Net.WebRequest.Create(ftpPath) is System.Net.FtpWebRequest ftpUpload))
            {
                throw new InvalidOperationException("Failed to create WebRequest for path {ftpPath}");

            }

            _log.Verbose($"Using credentials user : {user}, password: {password}");
            ftpUpload.Credentials = new System.Net.NetworkCredential(user, password);
            _log.Verbose("Setting KeepAlive:false, UseBinary : true");
            ftpUpload.KeepAlive = false;
            ftpUpload.UseBinary = true;

            ftpUpload.Method = System.Net.WebRequestMethods.Ftp.UploadFile;
            using (System.IO.Stream
                sourceStream = _fileSystem.GetFile(filePath).OpenRead(),
                uploadStream = ftpUpload.GetRequestStream())
            {
                sourceStream.CopyTo(uploadStream);
                uploadStream.Close();
            }

            System.Net.FtpWebResponse uploadResponse = null;
            try
            {
                uploadResponse = (System.Net.FtpWebResponse) ftpUpload.GetResponse();
                var uploadResponseStatus = (uploadResponse.StatusDescription ?? string.Empty).Trim().ToUpper();
                if (!ValidFtpCreateStatuses.Any(x => uploadResponseStatus.IndexOf(x,StringComparison.OrdinalIgnoreCase) > -1))
                {
                    throw new InvalidOperationException(
                        $"Failed to upload file. Returned status {uploadResponseStatus}");
                }

                return true;
            }
            finally
            {
                uploadResponse?.Close();
            }
        }


        private void EnsureFolder(
            string ftpUri,
            string user,
            string password,
            string folder)
        {
            _log.Verbose($"Getting folders for {folder}");
            var folders = folder.Split('/');

            var dir = "";


            foreach (var currentFolder in folders)
            {
                dir += "/" + currentFolder;
                string uploadResponseStatus = null;
                var ftpFullPath = string.Format(
                    "{0}{1}",
                    ftpUri.TrimEnd('/'),
                    dir
                );
                _log.Verbose($"Try creating folder {dir} with {ftpFullPath}");

                var ftpUpload = System.Net.WebRequest.Create(ftpFullPath) as System.Net.FtpWebRequest;

                if (ftpUpload == null)
                {
                    throw new InvalidOperationException($"Failed to create folder {dir} on {ftpFullPath}");
                }
                
                    try
                    {

                        _log.Verbose($"Using credentials user: {user} password: {password}");
                        ftpUpload.Credentials = new System.Net.NetworkCredential(user, password);
                        _log.Verbose("KeepAlive:false, UseBinary:true");
                        ftpUpload.KeepAlive = false;
                        ftpUpload.UseBinary = true;

                        ftpUpload.Method = System.Net.WebRequestMethods.Ftp.MakeDirectory;
                        var uploadResponse = (System.Net.FtpWebResponse) ftpUpload.GetResponse();
                        uploadResponseStatus = (uploadResponse.StatusDescription ?? string.Empty).Trim().ToUpper();
                        _log.Verbose($"Response {uploadResponseStatus}");
                        uploadResponse.Close();
                    }
                    catch (Exception ex)
                    {
                        _log.Verbose($"Folder not created due to exception {ex}. This is most likely ok");
                    }
                }
            }
        }
    }


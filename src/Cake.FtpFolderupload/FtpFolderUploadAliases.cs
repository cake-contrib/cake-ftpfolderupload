using System;
using Cake.Core;
using Cake.Core.Annotations;
using Cake.Core.IO;

namespace Cake.FtpFolderUpload
{
    /// <summary>
    /// Contains functionality for working with FTP upload from a folder
    /// </summary>
    [CakeAliasCategory("Ftp")]
    public static class FtpFolderUploadAliases
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="context">The context</param>
        /// <param name="folder">The folder to upload</param>
        /// <param name="ftpServer">The ftp server path</param>
        /// <param name="userName">The user name to connect to the ftp server</param>
        /// <param name="password">The password for connecting to the ftp server</param>
        /// <param name="filterPredicate">A filter to limit the files to upload. If null all files are uploaded</param>
        /// <param name="sortPredicate"></param>
        /// <code></code>
        [CakeMethodAlias]
        public static void FtpUploadFolder(this ICakeContext context, DirectoryPath folder, string ftpServer,
            string userName, string password, Func<IFile, bool> filterPredicate = null,
            Func<IFile, object> sortPredicate = null)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            var upload = new FtpFolderUpload(context.FileSystem, context.Environment, context.Log);
            upload.UploadFolder(folder,ftpServer, userName, password, filterPredicate, sortPredicate);
        }
    }
}
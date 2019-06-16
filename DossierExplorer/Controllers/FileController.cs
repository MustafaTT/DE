using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ElFinder;
using System.IO;
using Microsoft.AspNet.Identity;

namespace DossierExplorer.Controllers
{
    public partial class FileController : Controller
    {
        public virtual ActionResult Index(string folder, string subFolder)
        {
            System.Web.HttpContext currentContext = System.Web.HttpContext.Current;
            bool login = currentContext.User.Identity.IsAuthenticated;
            if (login)
            {


                string userid = currentContext.User.Identity.GetUserId().ToString();
                
                string RootPath = Path.Combine("UsersData", userid, "Files", "Root");
                
                string UserRootFullPath =Path.Combine(GlobalVariables.MyAppPath, RootPath);
                bool exists = System.IO.Directory.Exists(UserRootFullPath);
                
                if (!exists)
                {
                    System.IO.Directory.CreateDirectory(Path.Combine(UserRootFullPath));
                  
                }
                FileSystemDriver driver = new FileSystemDriver();

                var root = new Root(
                        new DirectoryInfo(Path.Combine(GlobalVariables.MyAppPath, "UsersData", userid,"Files",folder)),//File Browserın gördüğü kullanıcı klasörü yolu
                        "http://" + Request.Url.Authority + "/" + "UsersData" + "/" + userid + "/" + "Files" + "/" + folder)// Önizleme yapmak için dosyanın url i
                {
                    // Sample using ASP.NET built in Membership functionality...
                    // Only the super user can READ (download files) & WRITE (create folders/files/upload files).
                    // Other users can only READ (download files)
                    // IsReadOnly = !User.IsInRole(AccountController.SuperUser)

                    IsReadOnly = false, // Can be readonly according to user's membership permission
                    Alias = "Root", // Beautiful name given to the root/home folder
                    UploadOverwrite=false,
                    MaxUploadSizeInKb = 2000000, // Limit imposed to user uploaded file <= 500 KB
                    LockedFolders = new List<string>(new string[] { "Folder1" })
                };

                // Was a subfolder selected in Home Index page?
                if (!string.IsNullOrEmpty(subFolder))
                {
                    root.StartPath = new DirectoryInfo(Path.Combine(GlobalVariables.MyAppPath,"UsersData", userid, "Files", folder,subFolder)); //arama sonucundan dosyanın klasörüne yönlendirme yapmak için başlangıç klasörü
                }

                driver.AddRoot(root);

                var connector = new Connector(driver);

                return connector.Process(this.HttpContext.Request);
            }

            //TODO :RETURN REDIRECT LOGIN
            return null;
        }

        public virtual ActionResult SelectFile(string target)
        {
            System.Web.HttpContext currentContext = System.Web.HttpContext.Current;
            bool login = currentContext.User.Identity.IsAuthenticated;
            if (login)
            {


                string userid = currentContext.User.Identity.GetUserId().ToString();
                FileSystemDriver driver = new FileSystemDriver();

                driver.AddRoot(
                    new Root(
                        new DirectoryInfo(Path.Combine(GlobalVariables.MyAppPath, "UsersData", userid, "Files")),
                        "http://" + Request.Url.Authority + "/" + "UsersData" + "/" + userid + "/" + "Files")
                    { IsReadOnly = false });

                var connector = new Connector(driver);

                return Json(connector.GetFileByHash(target).FullName);
            }
            //TODO :RETURN REDIRECT LOGIN
            return null;
        }
    }
}
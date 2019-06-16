using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using AttributeRouting.Web.Mvc;
using System.IO;
using DossierExplorer.ViewModels;

namespace DossierExplorer.Controllers
{
    public partial class HomeController : Controller
    {
        [GET("Index")]
        public virtual ActionResult Index()
        {
            return View();
        }


       
        //[GET("Browser")]
        //public virtual ActionResult Browser()
        //{
        //    DirectoryInfo di = new DirectoryInfo(Server.MapPath("~/Files/MyFolder"));
        //    // Enumerating all 1st level directories of a given root folder (MyFolder in this case) and retrieving the folders names.
        //    var folders = di.GetDirectories().ToList().Select(d => d.Name);

        //    return View(folders);
        //}
        [Authorize]
        [GET("FileManager/{subFolder?}")]
       
        public virtual ActionResult Files(string subFolder)
        {        // FileViewModel contains the root "Root" and the selected subfolder if any
            FileViewModel model = new FileViewModel() { Folder = "Root", SubFolder = subFolder };

            return View(model);
        }
        public virtual ActionResult Folder(string subFolder)
        {
            string RootName = "Root";
            if (subFolder != RootName)
            {
                subFolder = subFolder.Substring(RootName.Length + 1);
                subFolder = subFolder.Replace("\\", "\\" + "\\");
                //Jqueryde tek parantez boşluk olarak algılandığı için dosya yolunu 4 parantez olarak gönderdiğimizde 2 parantez olarak gidiyor .
                //Dosya yolunu bu şekilde göndermeliyiz. Aksi taktirde alt klaösere yönlendirme yapamayız. 
            }
               
            else
                subFolder = null;
            // FileViewModel contains the root MyFolder and the selected subfolder if any
            FileViewModel model = new FileViewModel() { Folder = RootName, SubFolder = subFolder };

            var x = model;
            return View(model);
        }

        public virtual ActionResult Dashboard()
        {

            return Redirect("~/dashboard");
        }

    }
}
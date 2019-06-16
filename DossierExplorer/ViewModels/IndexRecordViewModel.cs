using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DossierExplorer.ViewModels
{
    public class IndexRecordViewModel
    {
        //public string userid { get; set; }
        //public string md5 { get; set; }

        public string name { get; set; }
        //public string directory { get; set; }
        public string display_directory { get; set; }
        public string fullpath { get; set; }
        //public string extension { get; set; }

        //public Nullable<int> image_width { get; set; }
        //public Nullable<int> image_height { get; set; }
        //public Nullable<int> image_hres { get; set; }
        //public Nullable<int> image_vres { get; set; }
        public string size { get; set; }
        public string create_time { get; set; }
        public string laccess_time { get; set; }
        public string lmodified_time { get; set; }
        //public string content { get; set; }
        ////public string tag { get; set; }
    }
}
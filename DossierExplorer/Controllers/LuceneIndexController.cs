using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.IO;
using Lucene.Net.Analysis.Standard;

using Lucene.Net.Index;

using Lucene.Net.Store;
using PagedList;
using Microsoft.AspNet.Identity;
using Lucene.Net.Analysis;
using DossierExplorer.LuceneIndex.Models;
using Hangfire;
using System.Threading.Tasks;
using System.Threading;
using System.Text;
using DossierExplorer.ViewModels;
using DossierExplorer.LuceneIndex;
namespace DossierExplorer.Controllers
{
    public partial class LuceneIndexController : Controller
    {
      
        [Authorize]
        public virtual ActionResult LuceneIndex()
        {
            
            return View();
        }

        [HttpPost]
        [Authorize]
        public virtual PartialViewResult _PartialUserSearchFiles(string SearchTerm, string SearchField, string RecordCount, int? Page, string SortOrder,string[] Tags)
        {
            System.Web.HttpContext currentContext = System.Web.HttpContext.Current;

            
            if (SearchField == "DisplayDirectory"&& SearchTerm != "") 
            {
                SearchTerm = SearchTerm.Replace("\\", "\\" + "\\");
               
            }


            string userid = currentContext.User.Identity.GetUserId().ToString();
            if (SearchTerm != "")
                SearchTerm = "(" + SearchTerm + ")" + "AND userid:" + userid;
            string searchTags = null;
            if (Tags != null)
            {
                

                foreach (var item in Tags)
                {
                    searchTags = searchTags+"Tag:" + item + " OR ";
                }
                searchTags = searchTags.Substring(0, searchTags.Length - 4);
                if (SearchTerm != "")
                    SearchTerm = "(" + "(" + SearchTerm + ")" + "AND userid:" + userid + ")"+" AND "+ searchTags;
            }


            //// create default Lucene search index directory
            //if (!Directory.Exists(DMSIndex.DMSIndex._luceneDir)) Directory.CreateDirectory(DMSIndex.DMSIndex._luceneDir);

            // perform Lucene search
            int RecordCount1 = 1;
            int FileCount = IndexCRUD.GetAllUserIndexRecords(userid).Count();
            if (RecordCount != null && RecordCount != "All")
            { RecordCount1 = Int32.Parse(RecordCount); }
            else RecordCount1 = FileCount;

            int PageSize = (RecordCount1); // sayfadaki veri sayısı
            int PageNumber = (Page ?? 1); // sayfa seçilmediyse page değişkenine 1 ata
            IEnumerable<IndexModel> _searchResults = IndexCRUD.SearchDefault(SearchTerm, SearchField);

            if (SearchField == "") _searchResults = IndexCRUD.SearchDefault(SearchTerm);

            else IndexCRUD.SearchDefault(SearchTerm, SearchField);



            if (string.IsNullOrEmpty(SearchTerm)&& searchTags == null)
                _searchResults = IndexCRUD.GetAllUserIndexRecords(userid);
            else if(string.IsNullOrEmpty(SearchTerm) && searchTags != null)
                _searchResults = IndexCRUD.GetAllUserIndexRecordsCategorised(userid, searchTags);
            var x = _searchResults;
            var files = from s in _searchResults
                        select s;

            switch (SortOrder)
            {
                case "ZA":
                    files = files.OrderByDescending(s => s.name);

                    break;
                case "AZ":
                    files = files.OrderBy(s => s.name);

                    break;
                case "S-":
                    files = files.OrderByDescending(s => s.size);

                    break;
                case "S+":
                    files = files.OrderBy(s => s.size);

                    break;
                case "CT-":
                    files = files.OrderByDescending(s => s.create_time);

                    break;
                case "CT+":
                    files = files.OrderBy(s => s.create_time);

                    break;
                case "MT-":
                    files = files.OrderByDescending(s => s.lmodified_time);

                    break;
                case "MT+":
                    files = files.OrderBy(s => s.lmodified_time);

                    break;

                default:
                    files = files.OrderBy(s => s.name);

                    break;
            }
            var viewmodel = new List<IndexRecordViewModel>();
           
            foreach (var item in files)

            {
                var temp = new IndexRecordViewModel();
                temp.fullpath=item.fullpath;
                temp.name = item.name;
                temp.display_directory = item.display_directory;
                temp.laccess_time = item.laccess_time;
                temp.lmodified_time = item.lmodified_time;
                temp.create_time = item.create_time;

                temp.size = FormatByteSize(item.size);
                viewmodel.Add(temp);
            }



           IPagedList Pagedfiles = viewmodel.ToPagedList(PageNumber, PageSize);
            return PartialView("_PartialUserSearchFiles", Pagedfiles);




        }

        public static string FormatByteSize(long? bytes)
        {
            decimal bytesd = Convert.ToDecimal(bytes);
            string []suffix = { "B", "KB", "MB", "GB", "TB" };
            int index = 0;
            do { bytesd /= 1024.0m; index++; }
            while (bytesd >= 1024.0m);
            return String.Format("{0}{1}", Math.Round(bytesd, 2), suffix[index]);
        }

        [HttpGet]
        public virtual PartialViewResult _PartialContent(string fullpath)
        {
            var result = IndexCRUD.SearchNotAnalyzed(fullpath, "FullPath").FirstOrDefault();

            var model = new IndexContentViewModel();
            model.content = result.content;
            model.name = result.name;

            return PartialView(model);




        }
       
        [HttpPost]
        public virtual ActionResult AddTag(string[] paths, string tag)
        {

            if (tag != "")
            {
                while (true)
                {
                    try
                    {
                        var analyzer = new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_30);
                        using (var writer = new IndexWriter(IndexCRUD._directory, analyzer, IndexWriter.MaxFieldLength.UNLIMITED))
                        {
                            foreach (var path in paths)
                            {
                                var query = IndexCRUD.SearchNotAnalyzed(path, "FullPath").FirstOrDefault();
                                var model = new IndexModel();
                                model.name = query.name;
                                model.md5 = query.md5;
                                model.size = query.size;
                                model.userid = query.userid;
                                model.laccess_time = query.laccess_time;
                                model.display_directory = query.display_directory;
                                model.directory = query.directory;
                                model.create_time = query.create_time;
                                model.content = query.content;
                                model.lmodified_time = query.lmodified_time;
                                model.fullpath = query.fullpath;
                                model.extension = query.extension;
                                if (query.tag == null)
                                    model.tag = " <> " + tag;
                                else model.tag = query.tag + " <> " + tag;
                                var record = IndexCRUD.IndexModelToDoc(model);

                                writer.UpdateDocument(new Term("FullPath", path), record);
                            }

                            analyzer.Close();
                            writer.Dispose();
                            break;
                        }
                    }
                    catch (LockObtainFailedException)

                    {

                    }

                }




            }
            return new EmptyResult();
        }

        [HttpPost]
        public virtual ActionResult DeleteTag(string[] paths, string tag)
        {

            if (tag != "")
            {

                while (true)
                {


                    try
                    {


                        var analyzer = new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_30);
                        using (var writer = new IndexWriter(IndexCRUD._directory, analyzer, IndexWriter.MaxFieldLength.UNLIMITED))
                        {
                            foreach (var path in paths)
                            {
                                var query = IndexCRUD.SearchNotAnalyzed(path, "FullPath").FirstOrDefault();
                                if (query.tag != null)
                                {
                                    var model = new IndexModel();
                                    model.name = query.name;
                                    model.md5 = query.md5;
                                    model.size = query.size;
                                    model.userid = query.userid;
                                    model.laccess_time = query.laccess_time;
                                    model.display_directory = query.display_directory;
                                    model.directory = query.directory;
                                    model.create_time = query.create_time;
                                    model.content = query.content;
                                    model.lmodified_time = query.lmodified_time;
                                    model.fullpath = query.fullpath;
                                    model.extension = query.extension;

                                    string updatedtag = query.tag.Replace(String.Format(" <> {0}", tag), "");
                                    model.tag = updatedtag;




                                    var record = IndexCRUD.IndexModelToDoc(model);

                                    writer.UpdateDocument(new Term("FullPath", path), record);
                                }
                            }

                            analyzer.Close();
                            writer.Dispose();
                            break;
                        }
                    }
                    catch (LockObtainFailedException)

                    {

                    }

                }

            }

            return new EmptyResult();
        }

        public virtual ActionResult SearchInfo()
        {
            return View();
        }

        public virtual ActionResult FileBrowserInfo()
        {
            return View();
        }

        public  virtual PartialViewResult _PartialTags()
        {
            System.Web.HttpContext currentContext = System.Web.HttpContext.Current;

            string userid = currentContext.User.Identity.GetUserId().ToString();
            var records =IndexCRUD.GetAllUserIndexRecords(userid).ToList();
            var tagrecords = records.Select(item => item.tag).Distinct().ToList();
            var targetsString = String.Concat(tagrecords);
            string[] taglist = targetsString.Split(new string[] { " <> " }, StringSplitOptions.None);
            var distingtaglist = taglist.Skip(1).Distinct();
            var tags = new List<TagViewModel>();
            
            foreach (var item in distingtaglist)
                {
                var tmp = new TagViewModel();
                tmp.tag = item;
                tags.Add(tmp);          
                }

            //var tags =
            //var tags = new List<TagViewModel>() ;
            //string tagstring = null;
            //foreach (var item in records)

            //{

            //    tagstring = tagstring + item.tag;

            //}

            return PartialView(tags);

        }

    }

   

    }

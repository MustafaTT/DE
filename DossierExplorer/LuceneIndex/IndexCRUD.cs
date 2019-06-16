using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.IO;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Version = Lucene.Net.Util.Version;
using Directory = System.IO.Directory;
using System.Security.Cryptography;
using TikaOnDotNet.TextExtraction;
using System.Text.RegularExpressions;

using DossierExplorer.LuceneIndex.Models;
using Hangfire;
using System.Threading;
using System.Text;


namespace DossierExplorer.LuceneIndex
{
    public class IndexCRUD
    {
        public static bool ValidType(string fileExt)
        {
            string filetype = fileExt.ToLower();
            if (filetype == ".jpg" || filetype == ".tif" || filetype == ".tıf" || filetype == ".tıff" || filetype == ".jpeg" || filetype == ".xml" || filetype == ".bmp" || filetype == ".png" || filetype == ".tiff" || filetype == ".pdf" || filetype == ".doc" || filetype == ".docx" || filetype == ".xls" || filetype == ".xlsx" || filetype == ".ppt" || filetype == ".pptx" || filetype == ".rtf" || filetype == ".txt")
            {
                return true;
            }
            else
                return false;
        }
        public static string FindUseridInPath(string path)
        {
            try
            {
                string match = Regex.Matches(path, @"(\\){1,2}[0-9a-fA-F]{8}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{12}(\\){1,2}")[0].Value;
                string result = Regex.Matches(match, @"[0-9a-fA-F]{8}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{12}")[0].Value;
                return result;
            }
            catch (System.ArgumentOutOfRangeException)
            {
                return null;
            }
        }
        public static string DisplayDirectory(string path)
        {
            string pathnull = FindUseridInPath(path);

            if (pathnull != null)
            {
                string getAfter = Path.Combine(FindUseridInPath(path), "Files") + @"\";


                string display = path.Substring(path.IndexOf(getAfter) + getAfter.Length);

                return display;
            }
            return null;
        }
        public static string CalculateMD5(string filename)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = System.IO.File.OpenRead(filename))
                {
                    var hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
        }
        public static string GenerateMD5FromPathString(string path)
        {

            using (MD5 md5 = MD5.Create())
            {
                var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(path));
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }



        public static string _luceneDir = Path.Combine(GlobalVariables.MyAppPath, "LuceneIndexFiles");
        private static FSDirectory _directoryTemp;
        public static FSDirectory _directory
        {
            get
            {
                if (_directoryTemp == null) _directoryTemp = FSDirectory.Open(new DirectoryInfo(_luceneDir));
                //if (IndexWriter.IsLocked(_directoryTemp)) IndexWriter.Unlock(_directoryTemp);
                //var lockFilePath = Path.Combine(_luceneDir, "write.lock");
                //if (System.IO.File.Exists(lockFilePath)) System.IO.File.Delete(lockFilePath);
                return _directoryTemp;
            }
        }
        public static void Optimize()
        {
            var analyzer = new StandardAnalyzer(Version.LUCENE_30);
            using (var writer = new IndexWriter(_directory, analyzer, IndexWriter.MaxFieldLength.UNLIMITED))
            {
                analyzer.Close();
                writer.Optimize();
                writer.Dispose();
            }
        }
        // search methods
        public static IEnumerable<IndexModel> GetAllIndexRecords()
        {
            // validate search index
            if (!System.IO.Directory.EnumerateFiles(_luceneDir).Any()) return new List<IndexModel>();

            // set up lucene searcher
            var searcher = new IndexSearcher(_directory, false);
            var reader = IndexReader.Open(_directory, false);
            var docs = new List<Document>();
            var term = reader.TermDocs();
            // v 2.9.4: use 'hit.Doc()'
            // v 3.0.3: use 'hit.Doc'
            while (term.Next()) docs.Add(searcher.Doc(term.Doc));
            reader.Dispose();
            searcher.Dispose();
            return _mapLuceneToDataList(docs);
        }

        public static IEnumerable<IndexModel> GetAllUserIndexRecords(string userid)
        {
            return SearchNotAnalyzed(userid, "userid");
        }
        public static IEnumerable<IndexModel> GetAllUserIndexRecordsCategorised(string userid, string tags)
        {
            string term = "userid:" + userid + " AND " + "(" + tags + ")";
            return SearchDefault(term);
        }
        public static IEnumerable<IndexModel> SearchDefault(string input, string fieldName = "")
        {
            return string.IsNullOrEmpty(input) ? new List<IndexModel>() : _search(input, fieldName);
        }
        // main search method
        private static IEnumerable<IndexModel> _search(string searchQuery, string searchField = "")
        {
            // validation
            if (string.IsNullOrEmpty(searchQuery.Replace("*", "").Replace("?", ""))) return new List<IndexModel>();
            // set up lucene searcher
            using (var searcher = new IndexSearcher(_directory, false))
            {
                var hits_limit = 214483647;
                var analyzer = new StandardAnalyzer(Version.LUCENE_30);
                // search by single field
                if (!string.IsNullOrEmpty(searchField))
                {
                    var parser = new QueryParser(Version.LUCENE_30, searchField, analyzer);
                    var query = parseQuery(searchQuery, parser);
                    var hits = searcher.Search(query, hits_limit).ScoreDocs;
                    var results = _mapLuceneToDataList(hits, searcher);
                    analyzer.Close();
                    searcher.Dispose();
                    return results;
                }
                // search by multiple fields (ordered by RELEVANCE)
                else
                {
                    var parser = new MultiFieldQueryParser
                        (Version.LUCENE_30, new[] { "userid", "md5", "Name", "Directory", "DisplayDirectory", "FullPath", "Extension", "Size", "Created-time", "Lacces-time", "Lmodified-time", "Content", "Tag" }, analyzer);
                    var query = parseQuery(searchQuery, parser);
                    var hits = searcher.Search(query, null, hits_limit, Sort.INDEXORDER).ScoreDocs;
                    var results = _mapLuceneToDataList(hits, searcher);
                    analyzer.Close();
                    searcher.Dispose();
                    return results;
                }
            }
        }
        public static IEnumerable<IndexModel> SearchNotAnalyzed(string searchQuery, string searchField = "") // We use it for when we need exact same record value like id or fullpath of file
        {
            using (var searcher = new IndexSearcher(_directory, false))
            {
                var hits_limit = 214483647;
            
                var query = new TermQuery(new Term(searchField, searchQuery));
                var hits = searcher.Search(query, hits_limit).ScoreDocs;
                var results = _mapLuceneToDataList(hits, searcher);

                searcher.Dispose();
                return results;
         
            }
        }
        private static Query parseQuery(string searchQuery, QueryParser parser)
        {
            Query query;
            try
            {
                query = parser.Parse(searchQuery.Trim());
            }
            catch (ParseException)
            {
                query = parser.Parse(QueryParser.Escape(searchQuery.Trim()));
            }
            return query;
        }

        // map Lucene search index to data
        private static IEnumerable<IndexModel> _mapLuceneToDataList(IEnumerable<Document> hits)
        {
            return hits.Select(_mapLuceneDocumentToData).ToList();
        }
        private static IEnumerable<IndexModel> _mapLuceneToDataList(IEnumerable<ScoreDoc> hits, IndexSearcher searcher)
        {
            // v 2.9.4: use 'hit.doc'
            // v 3.0.3: use 'hit.Doc'
            return hits.Select(hit => _mapLuceneDocumentToData(searcher.Doc(hit.Doc))).ToList();
        }
        private static IndexModel _mapLuceneDocumentToData(Document doc)
        {
            return new IndexModel
            {
                userid = doc.Get("userid"),
                md5 = doc.Get("md5"),
                name = doc.Get("Name"),
                directory = doc.Get("Directory"),
                display_directory = doc.Get("DisplayDirectory"),
                fullpath = doc.Get("FullPath"),
                extension = doc.Get("Extension"),
                size = Convert.ToInt64(doc.Get("Size")),
                create_time = doc.Get("Created-time"),
                laccess_time = doc.Get("Lacces-time"),
                lmodified_time = doc.Get("Lmodified-time"),
                content = doc.Get("Content"),
                tag = doc.Get("Tag")
            };
        }

        // add/update/clear search index data 

        public static void CreateEmptyIndexFiles()//Create Index If Index Files If Does'nt Exist
        {
            string IndexFilesPath = Path.Combine(GlobalVariables.MyAppPath, "LuceneIndexFiles");
            var IndexFiles = Directory.GetFiles(IndexFilesPath, "*.cf*");
            if (!IndexFiles.Any())
            {
                // if any index not created create temp record.
                var emptymodel = new IndexModel();
                emptymodel.content = null;
                emptymodel.create_time = null;
                emptymodel.laccess_time = null;
                emptymodel.lmodified_time = null;
                emptymodel.userid = "temp";
                emptymodel.fullpath = null;
                emptymodel.directory = null;
                emptymodel.extension = null;
                emptymodel.fullpath = null;
                emptymodel.display_directory = null;
                emptymodel.md5 = null;
                emptymodel.name = null;
                emptymodel.size = null;
                emptymodel.tag = null;

                var analyzer = new StandardAnalyzer(Version.LUCENE_30);
                var writer = new IndexWriter(_directory, analyzer, IndexWriter.MaxFieldLength.UNLIMITED);

                _addToLuceneIndex(emptymodel, writer);

                var searchQuery = new TermQuery(new Term("userid", "temp"));
                writer.DeleteDocuments(searchQuery);

                // close handles
                analyzer.Close();
                writer.Dispose();
            }
        }
        public static void CreateFolders()//Create neccesary folders If Does'nt Exist
        {
            string root = GlobalVariables.MyAppPath;
            
            // If directory does not exist, create it. 
            if (!Directory.Exists(Path.Combine(root,"TempFolder")))
            {
                Directory.CreateDirectory(Path.Combine(root, "TempFolder"));
            }
            if (!Directory.Exists(Path.Combine(root, "TempFolder","TempFiles")))
            {
                Directory.CreateDirectory(Path.Combine(root, "TempFolder", "TempFiles"));

            }
            if (!Directory.Exists(Path.Combine(root, "TempFolder", "TempImages")))
            {
                Directory.CreateDirectory(Path.Combine(root, "TempFolder", "TempImages"));
            }
            if (!Directory.Exists(Path.Combine(root, "UsersData")))
            {
                Directory.CreateDirectory(Path.Combine(root, "UsersData"));
            }
            if (!Directory.Exists(Path.Combine(root, "LuceneIndexFiles")))
            {
                Directory.CreateDirectory(Path.Combine(root, "LuceneIndexFiles"));
            }

        }


            public static string CreateTempFiles(string path)
        {
            string extention = Path.GetExtension(path);
            //string tempname = Guid.NewGuid().ToString()+"-" + Path.GetFileName(path);
            string filemd5 = CalculateMD5(path);
            string temppath;
            if (extention.ToLower() == ".tiff" || extention.ToLower() == ".tif" || extention.ToLower() == ".tıff" || extention.ToLower() == ".tıf")
                temppath = Path.Combine(GlobalVariables.MyAppPath, "TempFolder", "TempFiles", filemd5 + ".jpeg");
            else
                temppath = Path.Combine(GlobalVariables.MyAppPath, "TempFolder", "TempFiles", filemd5 + extention);
            if (System.IO.File.Exists(temppath))
                System.IO.File.Delete(temppath);
            System.IO.File.Copy(path, temppath);
            return temppath;
        }
        //AddUpdateLuceneIndex   To Update ALL INDEX DATA DELETE DELETED Files And Add If New File Added .
        public static void AddUpdateLuceneIndex(string pathdir)
        {
            CreateEmptyIndexFiles();
            var list = GetAllIndexRecords();
            List<string> pathlistdirectory = new List<string>();
            string[] filePaths = Directory.GetFiles(pathdir, "*.*", SearchOption.AllDirectories);
            foreach (string item in filePaths)
            {
                var query = list.Where(x => x.fullpath == item).FirstOrDefault();
                if (query == null)
                {
                    AddFileDirectoryToIndex(Path.GetFullPath(item));

                }
                pathlistdirectory.Add(Path.GetFullPath(item));
            }
            List<string> pathlistindex = new List<string>();
            pathlistindex = GetAllIndexRecords().Where(x => x.fullpath != null).Select(y => y.fullpath).ToList();
            var NotExistFile = pathlistindex.Except(pathlistdirectory).ToList();
            while (true)
            {
                try
                {
                    var analyzer = new StandardAnalyzer(Version.LUCENE_30);
                    var writer = new IndexWriter(_directory, analyzer, IndexWriter.MaxFieldLength.UNLIMITED);
                    foreach (var item in NotExistFile)
                    {

                        var searchQuery = new TermQuery(new Term("FullPath", item));
                        writer.DeleteDocuments(searchQuery);

                    }
                    analyzer.Close();
                    writer.Dispose();
                    Optimize();//Optimize Index For Faster Search And Reduce Index Size
                    break;
                }
                catch
                {

                }
            }
        }

        //DELETE ALL INDEX RECORD

        public static bool ClearLuceneIndex()
        {
            try
            {
                var analyzer = new StandardAnalyzer(Version.LUCENE_30);
                using (var writer = new IndexWriter(_directory, analyzer, true, IndexWriter.MaxFieldLength.UNLIMITED))
                {
                    // remove older index entries
                    writer.DeleteAll();

                    // close handles
                    analyzer.Close();
                    writer.Dispose();
                }
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }



        public static Lucene.Net.Documents.Document IndexModelToDoc(IndexModel model)// convert IndexModel fields to lucene fields mapped  
        {
            Document doc = new Document();
            
            if (model.userid != null)
                doc.Add(new Field("userid", model.userid, Field.Store.YES, Field.Index.NOT_ANALYZED));
            if (model.md5 != null)
                doc.Add(new Field("md5", model.md5, Field.Store.YES, Field.Index.NOT_ANALYZED));
            if (model.name != null) doc.Add(new Field("Name", model.name, Field.Store.YES, Field.Index.ANALYZED));
            if (model.directory != null) doc.Add(new Field("Directory", model.directory, Field.Store.YES, Field.Index.NOT_ANALYZED));
            if (model.fullpath != null) doc.Add(new Field("FullPath", model.fullpath, Field.Store.YES, Field.Index.NOT_ANALYZED));
            if (model.display_directory != null) doc.Add(new Field("DisplayDirectory", model.display_directory, Field.Store.YES, Field.Index.ANALYZED));
            if (model.extension != null) doc.Add(new Field("Extension", model.extension, Field.Store.YES, Field.Index.ANALYZED));
            if (model.size != null) doc.Add(new Field("Size", model.size.ToString(), Field.Store.YES, Field.Index.ANALYZED));
            if (model.create_time != null) doc.Add(new Field("Created-time", model.create_time.ToString(), Field.Store.YES, Field.Index.ANALYZED));
            if (model.laccess_time != null) doc.Add(new Field("Lacces-time", model.laccess_time.ToString(), Field.Store.YES, Field.Index.ANALYZED));
            if (model.lmodified_time != null) doc.Add(new Field("Lmodified-time", model.lmodified_time.ToString(), Field.Store.YES, Field.Index.ANALYZED));
            if (model.content != null) doc.Add(new Field("Content", model.content, Field.Store.YES, Field.Index.ANALYZED));
            if (model.tag != null) doc.Add(new Field("Tag", model.tag, Field.Store.YES, Field.Index.ANALYZED));
            return doc;
        }
        // Add Record To Index From IndexMODEL
        private static void _addToLuceneIndex(IndexModel IndexModel, IndexWriter writer)
        {
            // remove older index entry
            //var searchQuery = new TermQuery(new Term("FullPath", IndexModel.fullpath));
            //writer.DeleteDocuments(searchQuery);

            // add new index entry
            var doc = IndexModelToDoc(IndexModel);

            // add entry to index
            writer.AddDocument(doc);
        }
        //public static string GetContent(string path)
        //{


        //    string extention = Path.GetExtension(path);
        //    //string tempname = Guid.NewGuid().ToString()+"-" + Path.GetFileName(path);
        //    string filemd5 = CalculateMD5(path);
        //    string temppath = Path.Combine(GlobalVariables.MyAppPath, "TempFolder", "TempFiles", filemd5);
        //    if (System.IO.File.Exists(temppath))
        //        System.IO.File.Delete(temppath);

        //    System.IO.File.Copy(path, temppath);





        //    if (extention.ToLower() == ".pdf")
        //    {


        //        string result = OCR.PdfToText(temppath);


        //        if (System.IO.File.Exists(temppath))
        //            System.IO.File.Delete(temppath);
        //        return result;
        //    }
        //    else if (extention.ToLower() == ".jpg" || Path.GetExtension(path).ToLower() == ".jpeg" || Path.GetExtension(path).ToLower() == ".png" || Path.GetExtension(path).ToLower() == ".bmp" || Path.GetExtension(path).ToLower() == ".tiff")
        //    {



        //        string result = OCR.OCRImage(temppath);




        //        return result;
        //    }
        //    else
        //    {
        //        try

        //        {


        //            string result = new TextExtractor().Extract(temppath).Text;


        //            System.IO.File.Delete(temppath);
        //            return result;
        //        }

        //        catch (TikaOnDotNet.TextExtraction.TextExtractionException exception)
        //        {
        //            Console.WriteLine(exception);
        //            System.IO.File.Delete(temppath);

        //            return null;

        //        }
        //    }


        //}//need to revise!!. --------DONT GONNA STOP PROCCESS if file exist another path but after all existed files deleteted not gonna stop proccess need to update code .

        [Queue("ahigh")]
        public static void StopBackgroundJob(string name, string md5DeletedFile, string methodname)
        {
            IndexModel query = null;
            if (md5DeletedFile != "")
                query = SearchNotAnalyzed(md5DeletedFile, "md5").Where(x => x.md5 == md5DeletedFile).FirstOrDefault();

            if (query == null)//if same file exist in another directory we still need to get text for that file so we shouldnt stop procces.
            {
                var mon = JobStorage.Current.GetMonitoringApi();
                var processingJobs = mon.ProcessingJobs(0, int.MaxValue);
                //var shudulefJobs = mon.ScheduledJobs(0, int.MaxValue);
                var jobInstanceIdsToDelete = new List<string>();           
                //processingJobs.Where(o => o.Value.Job.Method.Name == methodname && o.Value.Job.Args[0].ToString() == path).ToList().ForEach(x => jobInstanceIdsToDelete.Add(x.Key));//was check procces with fullpath
                processingJobs.Where(o => o.Value.Job.Method.Name == methodname && o.Value.Job.Args[1].ToString() == md5DeletedFile).ToList().ForEach(x => jobInstanceIdsToDelete.Add(x.Key));
                if (jobInstanceIdsToDelete.Any())
                {
                    foreach (var id in jobInstanceIdsToDelete)
                    {
                        BackgroundJob.Delete(id);
                    }
                }
                var JobReleatedTempFiles = Directory.GetFiles(Path.Combine(GlobalVariables.MyAppPath, "TempFolder", "TempFiles"), md5DeletedFile, SearchOption.TopDirectoryOnly);
                var JobReleatedTempFolder = Directory.GetDirectories(Path.Combine(GlobalVariables.MyAppPath, "TempFolder", "TempImages"), md5DeletedFile, SearchOption.TopDirectoryOnly);
                while (JobReleatedTempFiles.Any())
                {
                    try
                    {
                        foreach (var item in JobReleatedTempFiles) System.IO.File.Delete(item);
                        JobReleatedTempFiles = Directory.GetFiles(Path.Combine(GlobalVariables.MyAppPath, "TempFolder", "TempFiles"), md5DeletedFile, SearchOption.TopDirectoryOnly);
                        break;
                    }
                    catch when (JobReleatedTempFiles.Any())
                    {
                        Thread.Sleep(1000);
                    }
                }
                while (JobReleatedTempFolder.Any())
                {
                    try
                    {
                        foreach (var item in JobReleatedTempFolder) System.IO.Directory.Delete(item, true);
                        JobReleatedTempFolder = Directory.GetFiles(Path.Combine(GlobalVariables.MyAppPath, "TempFolder", "TempImages"), md5DeletedFile, SearchOption.TopDirectoryOnly);
                        break;
                    }
                    catch when (JobReleatedTempFolder.Any())
                    {
                        Thread.Sleep(1000);

                    }
                }
            }
        }
        public static void AddFileDirectoryToIndex(string path)
        {
            string filepath = path;
            string filetype = Path.GetExtension(filepath);
            string filename = Path.GetFileName(filepath);
            var file = new FileInfo(path);
            if (ValidType(filetype)) // check file type 
            {
                var query = SearchNotAnalyzed(path, "FullPath").Where(item => item.fullpath == filepath).FirstOrDefault();// check if file already exist in index 
                if (query == null)
                {
                    IndexModel tmp = new IndexModel();
                    string dispdir = DisplayDirectory(file.Directory.FullName);
                    tmp.userid = FindUseridInPath(file.Directory.FullName);
                    tmp.fullpath = file.FullName;
                    tmp.md5 = CalculateMD5(file.FullName);
                    tmp.name = file.Name;
                    tmp.size = file.Length;
                    tmp.extension = file.Extension;
                    tmp.directory = file.Directory.FullName;
                    tmp.display_directory = dispdir;
                    tmp.create_time = file.CreationTime.ToString("MM/dd/yyyy HH:mm:ss");
                    tmp.laccess_time = file.LastAccessTime.ToString("MM/dd/yyyy HH:mm:ss");
                    tmp.lmodified_time = file.LastWriteTime.ToString("MM/dd/yyyy HH:mm:ss");
                    tmp.content = null;
                    tmp.tag = null;
                    while (true)
                    {
                        try
                        {
                            var analyzer = new StandardAnalyzer(Version.LUCENE_30);
                            using (var writer = new IndexWriter(_directory, analyzer, IndexWriter.MaxFieldLength.UNLIMITED))
                            {
                                _addToLuceneIndex(tmp, writer);
                                analyzer.Close();
                                writer.Dispose();
                                break;
                            }

                        }
                        catch

                        {

                        }

                    }
                   //BackgroundJob.Enqueue(() => UpdateLuceneIndexContent(filename, tmp.md5));//adding md5 to method just for find proccess when file deleted to stop backround jo
                }
            }
        }
        //Get Text From Various Documents
        public static string GetContent(string temppath)
        {
            string extention = Path.GetExtension(temppath);
            if (extention.ToLower() == ".pdf")
            {
                string result = OCR.GetTextFromPDF(temppath);
                if (Regex.Matches(result, @"[a-zA-Z]").Count == 0)
                    result = OCR.OCRPdf(temppath);
                if (System.IO.File.Exists(temppath))
                    System.IO.File.Delete(temppath);
                return result;
            }
            else if (extention.ToLower() == ".tif" || extention.ToLower() == ".jpg" || extention.ToLower() == ".jpeg" || extention.ToLower() == ".png" || extention.ToLower() == ".bmp" || extention.ToLower() == ".tiff")
            {
                string result = OCR.OCRImage(temppath);
                return result;
            }
            else
            {
                try
                {
                    string result = new TextExtractor().Extract(temppath).Text;
                    System.IO.File.Delete(temppath);
                    return result;
                }
                catch (TikaOnDotNet.TextExtraction.TextExtractionException exception)
                {
                    Console.WriteLine(exception);
                    System.IO.File.Delete(temppath);
                    return null;
                }
            }
        }
        [Queue("clow")]
        public static void UpdateLuceneIndexContent(string name, string md5, string temppath)
        {
            var query = SearchNotAnalyzed(md5, "md5").Where(item => item.md5 == md5);
            if (query != null)
            {
                var temp = new IndexModel();
                temp.content = GetContent(temppath);//just change content
                temp.extension = query.FirstOrDefault().extension;
                temp.md5 = query.FirstOrDefault().md5;
                var queryAfterTextResult = SearchNotAnalyzed(md5, "md5").Where(item => item.md5 == md5);
                foreach (var item in queryAfterTextResult)
                {
                    temp.size = item.size;
                    temp.laccess_time = item.laccess_time;
                    temp.lmodified_time = item.lmodified_time;
                    temp.create_time = item.create_time;
                    temp.userid = item.userid;
                    temp.fullpath = item.fullpath;
                    temp.directory = item.directory;
                    temp.name = item.name;
                    temp.display_directory = item.display_directory;
                    temp.tag = item.tag;
                    while (true)
                    {
                        try
                        {
                   
                            var analyzer = new StandardAnalyzer(Version.LUCENE_30);
                            var writer = new IndexWriter(_directory, analyzer, IndexWriter.MaxFieldLength.UNLIMITED);
                            Document doc = IndexModelToDoc(temp);
                            //writer.UpdateDocument(new Term("FullPath", path), doc);
                            //After text extraction done we was just change exact files content value ,
                            //but now we searching index with md5 and change all content values for samefiles which is better solution but didnt fully tested yet !!
                            writer.UpdateDocument(new Term("FullPath", item.fullpath), doc);
                            analyzer.Close();
                            writer.Dispose();
                            break;
                        }
                        catch
                        {

                        }
                    }
                }
            }
        }




        [Queue("ahigh")]
        public static void OnUploadResponse(List<FileInfo> filelist)
        {
            if (filelist != null)
            {
                var doc = new List<Document>();
                string userid = FindUseridInPath(filelist.FirstOrDefault().FullName);
                var IndexRecords = GetAllUserIndexRecords(userid);
                foreach (var item in filelist)
                {
                    if (ValidType(item.Extension))
                    {
                        string md5 = CalculateMD5(item.FullName);
                        var query = IndexRecords.Where(x => x.fullpath == item.FullName).FirstOrDefault();
                        var sameFileExist = IndexRecords.Where(x => x.md5 == md5).FirstOrDefault();
                        IndexModel tmp = new IndexModel();
                        if (query == null && sameFileExist == null) //new file dont exist in index
                        {
                            tmp.userid = FindUseridInPath(item.Directory.FullName);
                            tmp.fullpath = item.FullName;
                            tmp.md5 = md5;
                            tmp.name = item.Name;
                            tmp.size = item.Length;
                            tmp.extension = item.Extension;
                            tmp.directory = item.Directory.FullName;
                            tmp.display_directory = DisplayDirectory(item.Directory.FullName);
                            tmp.create_time = item.CreationTime.ToString("MM/dd/yyyy HH:mm:ss");
                            tmp.laccess_time = item.LastAccessTime.ToString("MM/dd/yyyy HH:mm:ss");
                            tmp.lmodified_time = item.LastWriteTime.ToString("MM/dd/yyyy HH:mm:ss");
                            tmp.content = null;
                            tmp.tag = null;
                            doc.Add(IndexModelToDoc(tmp));
                        }
                        else if (sameFileExist != null) // we indexed file already dont need text extraction
                        {
                            string dispdir = DisplayDirectory(item.Directory.FullName);
                            tmp.userid = FindUseridInPath(item.Directory.FullName);
                            tmp.fullpath = item.FullName;
                            tmp.md5 = sameFileExist.md5;//same
                            tmp.name = item.Name;
                            tmp.size = sameFileExist.size;//same
                            tmp.extension = item.Extension;
                            tmp.directory = item.Directory.FullName;
                            tmp.display_directory = DisplayDirectory(item.Directory.FullName);
                            tmp.create_time = item.CreationTime.ToString("MM/dd/yyyy HH:mm:ss");
                            tmp.laccess_time = item.LastAccessTime.ToString("MM/dd/yyyy HH:mm:ss");
                            tmp.lmodified_time = item.LastWriteTime.ToString("MM/dd/yyyy HH:mm:ss");
                            //if(sameFileExist.content != null)
                            tmp.content = sameFileExist.content;
                            tmp.tag = sameFileExist.tag;//dont have to same but reasonable
                            doc.Add(IndexModelToDoc(tmp));
                        }
                    }
                }
                while (true)
                {
                    try
                    {
                        var analyzer = new StandardAnalyzer(Version.LUCENE_30);
                        using (var writer = new IndexWriter(_directory, analyzer, IndexWriter.MaxFieldLength.UNLIMITED))
                        {
                            foreach (var record in doc)
                                writer.AddDocument(record);
                            analyzer.Close();
                            writer.Dispose();
                            break;
                        }
                    }
                    catch
                    {



                    }
                }
                foreach (var item in filelist)
                {
                    if (ValidType(item.Extension))
                    {
                        string md5 = CalculateMD5(item.FullName);
                        string temppath= CreateTempFiles(item.FullName);
                        BackgroundJob.Enqueue(() => UpdateLuceneIndexContent(item.Name, md5,temppath));
                    }
                }
            }
        }
        [Queue("ahigh")]
        public static void OnDeleteResponse(List<string> pathlist)
        {
            string userid = FindUseridInPath(pathlist.FirstOrDefault());
            var records = GetAllUserIndexRecords(userid);
            var querydeleted = new List<IndexModel>();
            foreach (var path in pathlist)
            {
                var deletedfile = records.Where(item => item.fullpath == path).FirstOrDefault();
                querydeleted.Add(deletedfile);
            }
            while (true)
            {
                try
                {
                    var md5list = new List<String>();
                    var analyzer = new StandardAnalyzer(Version.LUCENE_30);
                    var writer = new IndexWriter(_directory, analyzer, IndexWriter.MaxFieldLength.UNLIMITED);
                    foreach (var path in pathlist)
                    {
                        string filetype = Path.GetExtension(path);
                       if (ValidType(filetype))
                        {
                            var searchQuery = new TermQuery(new Term("FullPath", path));
                            //We need md5. if Query has value in StopBackgroundjob method we should stop text extraction .
                            //if (!fileExistInIndex) throw new ArgumentNullException("Query cannot be null or empty");
                            writer.DeleteDocuments(searchQuery);
                        }
                    }
                    analyzer.Close();
                    writer.Dispose();
                    break;
                }
                //catch (ArgumentNullException)
                //{

                //    Thread.Sleep(1000);

                //}
                catch (LockObtainFailedException)
                {
                    Thread.Sleep(100);
                }
            }
            var recordsAfterDelete = GetAllUserIndexRecords(userid);
            foreach (var item in querydeleted)
            {
                var queryAfterDelete = recordsAfterDelete.Where(x => x.md5 == item.md5).FirstOrDefault();//To control file md5 is exist in server and that files have no content  value .

                bool fileExistInIndex = (queryAfterDelete == null) ? false : true;

                if (!fileExistInIndex)
                    BackgroundJob.Enqueue(() => StopBackgroundJob(item.name, item.md5, "UpdateLuceneIndexContent")); ///have to check exeption handling of ocr and text extraction of pdf---//haveto work on it gonna disable for now

            }

        }

        public static void OnRenameResponseFolder(List<string> pathsold, List<string> pathnew)
        {
            pathnew.Sort();
            pathsold.Sort();
            var pathChanger = pathsold.Join(pathnew, s => pathsold.IndexOf(s), i => pathnew.IndexOf(i), (s, i) => new { olds = s, news = i }).ToList().Where(ext=> ValidType(Path.GetExtension(ext.news)));




            var indexmodelrecords = new List<IndexModel>();

            foreach (var item in pathChanger)

            {

                string filepathold = item.olds;
                string filetype = Path.GetExtension(filepathold);
                var filenew = new FileInfo(item.news);

                if (ValidType(filetype))
                {
                    var query = SearchNotAnalyzed(item.olds, "FullPath").Where(xy => xy.fullpath == filepathold).FirstOrDefault();


                    if (query != null) //rename if exist file just not get error
                    {

                        IndexModel tmp = new IndexModel();
                        string dispdir = DisplayDirectory(filenew.Directory.FullName);
                        tmp.userid = FindUseridInPath(filenew.Directory.FullName);
                        tmp.fullpath = filenew.FullName;
                        tmp.md5 = query.md5;//same
                        tmp.name = filenew.Name;
                        tmp.size = query.size;//same
                        tmp.extension = query.extension;//must same if not there is something wrong with the name , cant do anything better keep that same ,even extention change its same file 
                        tmp.directory = filenew.Directory.FullName;
                        tmp.display_directory = dispdir;
                        tmp.create_time = filenew.CreationTime.ToString("MM/dd/yyyy HH:mm:ss");
                        tmp.laccess_time = filenew.LastAccessTime.ToString("MM/dd/yyyy HH:mm:ss");
                        tmp.lmodified_time = filenew.LastWriteTime.ToString("MM/dd/yyyy HH:mm:ss");
                        tmp.content = query.content;
                        tmp.tag = query.tag;

                        indexmodelrecords.Add(tmp);

                      



                    }



                }
            }
            while (true)
            {
                try
                {


                    var analyzer = new StandardAnalyzer(Version.LUCENE_30);
                    using (var writer = new IndexWriter(_directory, analyzer, IndexWriter.MaxFieldLength.UNLIMITED))
                    {
                        foreach(var paths in pathChanger)
                        {
                            var doc = new Document();
                            doc = IndexModelToDoc(indexmodelrecords.Where(pathmodel => pathmodel.fullpath == paths.news).FirstOrDefault());
                            writer.UpdateDocument(new Term("FullPath", paths.olds),doc) ;
                        }
                        
                        analyzer.Close();
                        writer.Dispose();
                        break;
                    }

                }
                catch
                {


                }


            }






        }

        public static void OnUpload(string path)
        {
            string filepath = path;
            string filetype = Path.GetExtension(filepath);
            var file = new FileInfo(path);
            string filemd5 = CalculateMD5(path);
            if (ValidType(filetype))
            {
                var query = SearchNotAnalyzed(path, "FullPath").Where(item => item.fullpath == filepath).FirstOrDefault();
                var sameFileExist = SearchNotAnalyzed(filemd5, "md5").Where(item => item.md5 == filemd5).FirstOrDefault();

                if (query == null && sameFileExist == null) //new file dont exist in index
                {
                    string filename = Path.GetFileName(filepath);
                    IndexModel tmp = new IndexModel();
                    string dispdir = DisplayDirectory(file.Directory.FullName);
                    tmp.userid = FindUseridInPath(file.Directory.FullName);
                    tmp.fullpath = file.FullName;
                    tmp.md5 = filemd5;
                    tmp.name = file.Name;
                    tmp.size = file.Length;
                    tmp.extension = file.Extension;
                    tmp.directory = file.Directory.FullName;
                    tmp.display_directory = dispdir;
                    tmp.create_time = file.CreationTime.ToString("MM/dd/yyyy HH:mm:ss");
                    tmp.laccess_time = file.LastAccessTime.ToString("MM/dd/yyyy HH:mm:ss");
                    tmp.lmodified_time = file.LastWriteTime.ToString("MM/dd/yyyy HH:mm:ss");
                    tmp.content = null;
                    tmp.tag = null;
                    while (true)
                    {

                        try
                        {
                            var analyzer = new StandardAnalyzer(Version.LUCENE_30);
                            using (var writer = new IndexWriter(_directory, analyzer, IndexWriter.MaxFieldLength.UNLIMITED))
                            {
                                _addToLuceneIndex(tmp, writer);
                                analyzer.Close();
                                writer.Dispose();
                                break;
                            }

                        }
                        catch
                        {



                        }

                    }

                    string temppath = CreateTempFiles(path);
                    BackgroundJob.Enqueue(() => UpdateLuceneIndexContent(filename, tmp.md5, temppath));//adding md5 to method just for find proccess when file deleted to stop backround job

                }
                else if (sameFileExist != null) // we indexed file already dont need text extraction
                {
                    IndexModel tmp = new IndexModel();
                    string dispdir = DisplayDirectory(file.Directory.FullName);
                    tmp.userid = FindUseridInPath(file.Directory.FullName);
                    tmp.fullpath = file.FullName;
                    tmp.md5 = sameFileExist.md5;//same
                    tmp.name = file.Name;
                    tmp.size = sameFileExist.size;//same
                    tmp.extension = file.Extension;
                    tmp.directory = file.Directory.FullName;
                    tmp.display_directory = dispdir;
                    tmp.create_time = file.CreationTime.ToString("MM/dd/yyyy HH:mm:ss");
                    tmp.laccess_time = file.LastAccessTime.ToString("MM/dd/yyyy HH:mm:ss");
                    tmp.lmodified_time = file.LastWriteTime.ToString("MM/dd/yyyy HH:mm:ss");
                    //if(sameFileExist.content != null)
                    tmp.content = sameFileExist.content;
                    tmp.tag = sameFileExist.tag;//dont have to same but reasonable
                    while (true)
                    {


                        try
                        {


                            var analyzer = new StandardAnalyzer(Version.LUCENE_30);
                            using (var writer = new IndexWriter(_directory, analyzer, IndexWriter.MaxFieldLength.UNLIMITED))
                            {
                                _addToLuceneIndex(tmp, writer);
                                analyzer.Close();
                                writer.Dispose();
                                break;
                            }
                        }
                        catch
                        {


                        }
                    }
                    //if (sameFileExist.content == null)//have to test
                    //    BackgroundJob.Enqueue(() => UpdateLuceneIndexContent(path));


                }


            }
        }

        public static void OnRename(string pathold, string pathnew)
        {
            string filepathold = pathold;
            string filetype = Path.GetExtension(filepathold);
            var filenew = new FileInfo(pathnew);

            if (ValidType(filetype))
            {
                var query = SearchNotAnalyzed(pathold, "FullPath").Where(item => item.fullpath == filepathold).FirstOrDefault();


                if (query != null) //rename if exist file just not get error
                {

                    IndexModel tmp = new IndexModel();
                    string dispdir = DisplayDirectory(filenew.Directory.FullName);
                    tmp.userid = FindUseridInPath(filenew.Directory.FullName);
                    tmp.fullpath = filenew.FullName;
                    tmp.md5 = query.md5;//same
                    tmp.name = filenew.Name;
                    tmp.size = query.size;//same
                    tmp.extension = query.extension;//must same if not there is something wrong with the name , cant do anything better keep that same ,even extention change its same file 
                    tmp.directory = filenew.Directory.FullName;
                    tmp.display_directory = dispdir;
                    tmp.create_time = filenew.CreationTime.ToString("MM/dd/yyyy HH:mm:ss");
                    tmp.laccess_time = filenew.LastAccessTime.ToString("MM/dd/yyyy HH:mm:ss");
                    tmp.lmodified_time = filenew.LastWriteTime.ToString("MM/dd/yyyy HH:mm:ss");
                    tmp.content = query.content;
                    tmp.tag = query.extension; ;

                    Document doc = IndexModelToDoc(tmp);

                    while (true)
                    {
                        try
                        {


                            var analyzer = new StandardAnalyzer(Version.LUCENE_30);
                            using (var writer = new IndexWriter(_directory, analyzer, IndexWriter.MaxFieldLength.UNLIMITED))
                            {
                                writer.UpdateDocument(new Term("FullPath", pathold), doc);
                                analyzer.Close();
                                writer.Dispose();
                                break;
                            }

                        }
                        catch
                        {


                        }


                    }



                }



            }
        }

        public static void OnDelete(string path)
        {
            string filetype = Path.GetExtension(path);
            if (ValidType(filetype))
            {

                var query = SearchNotAnalyzed(path, "FullPath").Where(item => item.fullpath == path).FirstOrDefault();//To control file md5 is exist in server and that files have no content  value .
                string filename = Path.GetFileName(path);
                bool fileExistInIndex = (query == null) ? false : true;
                string deletedfilemd5 = fileExistInIndex ? query.md5 : "";

                while (true)
                {


                    try
                    {
                        var searchQuery = new TermQuery(new Term("FullPath", path));
                        //We need md5. if Query has value in StopBackgroundjob method we should stop text extraction .

                        var analyzer = new StandardAnalyzer(Version.LUCENE_30);
                        var writer = new IndexWriter(_directory, analyzer, IndexWriter.MaxFieldLength.UNLIMITED);
                        var search = SearchNotAnalyzed(path, "FullPath");
                        if (!fileExistInIndex) throw new ArgumentNullException("Query cannot be null or empty");
                        writer.DeleteDocuments(searchQuery);
                        analyzer.Close();
                        writer.Dispose();
                        break;


                    }
                    catch (ArgumentNullException)
                    {

                        Thread.Sleep(1000);

                    }
                    catch
                    {

                        Thread.Sleep(100);


                    }
                }
                BackgroundJob.Enqueue(() => StopBackgroundJob(filename, deletedfilemd5, "UpdateLuceneIndexContent"));
                ///have to check exeption handling of ocr and text extraction of pdf---//haveto work on it gonna disable for now
            }

            //TODO STOP IF STILL TEXTEXTRACTION IN PROGRESS
        }
        //change  JUST Path RECORD When File Moved 
        public static void OnMove(string newpath, string oldpath, bool isCut)
        {
            //string oldfilepath = oldpath;
            var file = new FileInfo(newpath);
            string extention = Path.GetExtension(newpath);
            if (ValidType(extention))
            {
                while (true)
                {
                    try
                    {
                        var query = SearchNotAnalyzed(oldpath, "FullPath").Where(item => item.fullpath == oldpath).FirstOrDefault();
                        bool fileExistInIndex = (query == null) ? false : true;
                        if (!fileExistInIndex) throw new ArgumentNullException("Query can't be null");
                        IndexModel tmp = new IndexModel();
                        tmp.userid = FindUseridInPath(file.Directory.FullName);
                        tmp.fullpath = file.FullName;
                        tmp.md5 = query.md5;
                        tmp.name = file.Name;
                        tmp.size = query.size;
                        tmp.extension = query.extension;
                        tmp.directory = file.Directory.FullName;
                        tmp.display_directory = DisplayDirectory(file.Directory.FullName);
                        tmp.create_time = file.CreationTime.ToString("MM/dd/yyyy HH:mm:ss");
                        tmp.laccess_time = file.LastAccessTime.ToString("MM/dd/yyyy HH:mm:ss");
                        tmp.lmodified_time = file.LastWriteTime.ToString("MM/dd/yyyy HH:mm:ss");
                        tmp.content = query.content;
                        tmp.tag = query.tag;
                        Document doc = IndexModelToDoc(tmp);
                        if (isCut)//if cuted just update old record
                        {
                            while (true)
                            {
                                try
                                {
                                    var analyzer = new StandardAnalyzer(Version.LUCENE_30);
                                    using (var writer = new IndexWriter(_directory, analyzer, IndexWriter.MaxFieldLength.UNLIMITED))
                                    {

                                        writer.UpdateDocument(new Term("FullPath", oldpath), doc);
                                        analyzer.Close();
                                        writer.Dispose();
                                        break;
                                    }
                                }
                                catch

                                {
                                }

                            }
                        }
                        else//if copyed remain old record and add same record with new directory
                        {
                            while (true)
                            {
                                try
                                {
                                    var analyzer = new StandardAnalyzer(Version.LUCENE_30);
                                    using (var writer = new IndexWriter(_directory, analyzer, IndexWriter.MaxFieldLength.UNLIMITED))
                                    {

                                        writer.AddDocument(doc);
                                        analyzer.Close();
                                        writer.Dispose();
                                        break;
                                    }
                                }
                                catch
                                { }

                            }
                        }
                        break;
                    }
                    catch (ArgumentNullException)
                    {

                    }
                }
            }
        }
        public static void OnMovefolderAddFile(string fileinnewdir)

        {
            string md5movedfile = CalculateMD5(fileinnewdir);
            var query = SearchNotAnalyzed(md5movedfile, "md5").Where(item => item.md5 == md5movedfile).FirstOrDefault();
            var file = new FileInfo(fileinnewdir);
            if (query == null)
            {
                IndexModel tmp = new IndexModel();

                tmp.userid = FindUseridInPath(file.Directory.FullName);
                tmp.fullpath = file.FullName;
                tmp.md5 = query.md5;
                tmp.name = file.Name;
                tmp.size = query.size;
                tmp.extension = query.extension;
                tmp.directory = file.Directory.FullName;
                tmp.display_directory = DisplayDirectory(file.Directory.FullName);
                tmp.create_time = file.CreationTime.ToString("MM/dd/yyyy HH:mm:ss");
                tmp.laccess_time = file.LastAccessTime.ToString("MM/dd/yyyy HH:mm:ss");
                tmp.lmodified_time = file.LastWriteTime.ToString("MM/dd/yyyy HH:mm:ss");
                tmp.content = query.content;
                tmp.tag = query.tag;
                Document doc = IndexModelToDoc(tmp);
                while (true)
                {


                    try
                    {


                        var analyzer = new StandardAnalyzer(Version.LUCENE_30);
                        using (var writer = new IndexWriter(_directory, analyzer, IndexWriter.MaxFieldLength.UNLIMITED))
                        {

                            writer.AddDocument(doc);
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

        }
        public static void DeleteLuceneIndex(string path)
        {
            string myFilePath = path;
            string filetype = Path.GetExtension(myFilePath);
            string directory = Path.GetDirectoryName(myFilePath);
            string name = Path.GetFileName(myFilePath);

            if (ValidType(filetype))
            {
                var searchQuery = new TermQuery(new Term("FullPath", path));
                if (searchQuery != null)
                {
                    while (true)
                    {
                       try
                        {
                            var analyzer = new StandardAnalyzer(Version.LUCENE_30);
                            var writer = new IndexWriter(_directory, analyzer, IndexWriter.MaxFieldLength.UNLIMITED);
                            writer.DeleteDocuments(searchQuery);
                            analyzer.Close();
                            writer.Dispose();
                            break;
                        }
                        catch
                        {
                        }
                    }
                }

            }
        }




    }




















}

















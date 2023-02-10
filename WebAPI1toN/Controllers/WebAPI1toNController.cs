using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SecuGen.SecuSearchSDK3;
using WebAPI1toN.Interfaces;
using Newtonsoft.Json;
using WebAPI1toN.Models;
using System.Text;
using System.Runtime;


// MVC ASP.NET Core 6 Web API -> 9/16/22
// SS3 controller is the heart of the web site.
// This controller continiously loads/saves SS3 DB due to the requirement of WebAPI1toN must have many individual FP DBs
// Most of the other installations only require one FPDB.


namespace WebAPI1toN.Controllers
{
    public class WebAPI1toNController : Controller
    {
        private readonly ILogger<WebAPI1toNController> _logger;
        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly ISecuSearch3 _secuSearch3;
        private readonly string SS3_FP_DATA = "FingerprintData_{0}.tdb";
        private readonly string DIRECTORY_LOCATION = "Data";
        private readonly string TEMPLATE_FILENAME_BASE = "Template_";
        private const string SG400_TEMPLATE_FILE_EXTENSION = ".min";
        private const string ISO_TEMPLATE_FILE_EXTENSION = ".iso";
        private IImageContainer _imageContainer;
        private IDataPartitioning _dataPartitioning;

        public WebAPI1toNController(ILogger<WebAPI1toNController> logger
            , ISecuSearch3 secuSearch3
            , IWebHostEnvironment webHostEnvironment
            , IImageContainer imageContainer
            , IDataPartitioning dataPartitioning)
        {
            _logger = logger;
            _secuSearch3 = secuSearch3;
            _hostingEnvironment = webHostEnvironment;
            _imageContainer = imageContainer;
            _dataPartitioning = dataPartitioning;
        }


        // GET: SS3Controller
        public ActionResult Index()
        {
            return View();
        }

        public IActionResult Enroll()
        {
            return View();
        }

        public IActionResult Search(string sessionid, SearchResultsModel? searchresultsmodel = null)
        {
            SearchResultsModel model = searchresultsmodel ?? new SearchResultsModel();
            ViewData["AllImages"] = _imageContainer.getBMPBase64Image(GetPathToDataDirectory(), sessionid);
            ViewData["SessionInfo"] = sessionid;
            return View(model);
        }

        [HttpPost]
        public JsonResult Register(UInt32 userid, string isotemplatebase64, string bmpbase64image, string sessionid)
        {
            string strError = "Fingerprint registered successfully";
            bool bSuccess = false;
            SSError error = SSError.NONE;
            byte[] sg400Template = new byte[SSConstants.TEMPLATE_SIZE];
            byte[] isoTemplateData = Convert.FromBase64String(isotemplatebase64);
            // FileDownTemplate(userid.ToString(), ref isoTemplateData, ISO_TEMPLATE_FILE_EXTENSION);

            if (sessionid == "0")
            {
                _secuSearch3.ClearFPDB();
                _dataPartitioning.setSessionInfo();
                sessionid = _dataPartitioning.GetClientSessionInfo();
                error = _secuSearch3.SaveFPDB(GetFingprintTdbFilewithPath(sessionid));
                if (error != SSError.NONE)
                {
                    strError = string.Format("Save Empty FPDB FAILURE, error[{0}]", error);
                    return Json(new {bRegister= (bSuccess ? "true" : "false")
                        , strSessionID=sessionid
                        , error=strError});
                }
            } // end if sessionid = "0"

            error = _secuSearch3.LoadFPDB(GetFingprintTdbFilewithPath(sessionid));
            if (error == SSError.NONE)
            {
                error = _secuSearch3.ExtractTemplate(isoTemplateData, SSTemplateType.ISO19794, 0, sg400Template);
                if (error == SSError.NONE)
                {
                    // can file down individual templates
                    // FileDownTemplate(userid.ToString(),ref sg400Template, SG400_TEMPLATE_FILE_EXTENSION);
                    error = _secuSearch3.RegisterFP(sg400Template, userid);
                    if (error != SSError.NONE)
                    {
                        strError = string.Format("Failed to insert into SS3; Error[{0}]", error.ToString());
                    } // end if error != SSError.NONE
                    else
                    {
                        error = _secuSearch3.SaveFPDB(GetFingprintTdbFilewithPath(sessionid));
                        if (error != SSError.NONE)
                        {
                            strError = string.Format("Failure to save SS3 DB[{0}]\nError[{1}]"
                                , GetFingprintTdbFilewithPath(sessionid), error.ToString());
                        } // if (error != SSError.NONE)
                        else
                        {
                            // Write data to image container service
                            _imageContainer.storeBMPBase64Image(GetPathToDataDirectory(), sessionid, bmpbase64image);
                            bSuccess = true;
                        }
                    } // end else error == SSError.NONE
                } // if error == SSError.NONE
                else
                {
                    strError = string.Format("Failed to extract template; Error[{0}]", error.ToString());
                }
            }
            else
            {
                strError = string.Format("LoadFPDB failure, error[{0}]", error);
            } // end else loadfpdb was successful
            return Json(new {bRegister= (bSuccess ? "true" : "false")
                , strSessionID=sessionid
                , error=strError});
        }

        [HttpPost]
        public JsonResult IdentifyPersonMatch(string isotemplatebase64, string sessionid)
        {
            string strError = "Identify Person Match successfully";
            bool bSuccess = false;
            var matchedPersonTemplateId = 0;
            SearchResultsModel searchResultsModel = new SearchResultsModel();

            if (sessionid != string.Empty)
                _dataPartitioning.SetClientSessionInfo(sessionid);
            else
                return Json(new {found=(bSuccess ? "true" : "false")
                    , match=searchResultsModel.SearchResultIdentity
                    , error=string.Format("SessionID field empty; [{0}]", sessionid)});

            var error = _secuSearch3.LoadFPDB(GetFingprintTdbFilewithPath(sessionid));
            if (error == SSError.NONE)
            {
                byte[] sg400Template = new byte[SSConstants.TEMPLATE_SIZE];
                byte[] webApiFingerprintData = Convert.FromBase64String(isotemplatebase64);
                error = _secuSearch3.ExtractTemplate(webApiFingerprintData, SSTemplateType.ISO19794, 0, sg400Template);
                if (error == SSError.NONE)
                {
                    searchResultsModel.SearchIdentity = true;
                    // File down template that is used as matcher
                    // FileDownTemplate("Identify", ref sg400Template);
                    UInt32 foundMatchTemplateId = 0;
                    error = _secuSearch3.IdentifyFP(sg400Template, SSConfLevel.NORMAL, ref foundMatchTemplateId);
                    if (error == SSError.NONE)
                    {
                        matchedPersonTemplateId = (Int32)foundMatchTemplateId;
                        searchResultsModel.SearchResultIdentity = (Int32)foundMatchTemplateId;
                        bSuccess = true;
                    }
                    else
                    {
                        strError = string.Format("IdentifyFP failure; error [{0}];", error.ToString());
                    }
                }
                else
                {
                    strError = string.Format("ExtractTemplate failure; error [{0}];", error.ToString());
                }
            } // end if LoadFPDB failure
            else
            {
                strError = string.Format("Load FP DB failure; error [{0}];", error.ToString());
            }

            ViewData["AllImages"] = _imageContainer.getBMPBase64Image(GetPathToDataDirectory(), sessionid);
            return Json(new {found=(bSuccess ? "true" : "false")
                , match=searchResultsModel.SearchResultIdentity
                , error=strError});
        }

        [HttpPost]
        public JsonResult SearchMatches(string isotemplatebase64, string sessionid)
        {
            bool bSuccess = false;
            var foundList = new SSCandList();
            string str;
            string strResults = string.Empty;
            string strError = string.Empty;
            SearchResultsModel searchResultsModel = new SearchResultsModel();

            if (sessionid != string.Empty)
                _dataPartitioning.SetClientSessionInfo(sessionid);
            else
                return Json(new {found=(bSuccess ? "true" : "false")
                    , match=searchResultsModel.SearchResultIdentity
                    , error=string.Format("SessionID field empty; [{0}]", sessionid)});

            string fingerprintDBFullPath = GetFingprintTdbFilewithPath(sessionid);
            var error = _secuSearch3.LoadFPDB(fingerprintDBFullPath);

            if (error == SSError.NONE)
            {
                byte[] sg400Template = new byte[SSConstants.TEMPLATE_SIZE];
                byte[] webApiFingerprintData = Convert.FromBase64String(isotemplatebase64);
                error = _secuSearch3.ExtractTemplate(webApiFingerprintData, SSTemplateType.ISO19794, 0, sg400Template);
                if (error == SSError.NONE)
                {
                    // File down template used as matcher
                    // FileDownTemplate("Cand_List", ref sg400Template);
                    error = _secuSearch3.SearchFP(sg400Template, ref foundList);
                    if (error == SSError.NONE)
                    {
                        bSuccess = true;
                        searchResultsModel.SearchMatches = true;
                        for (int i = 0; i < foundList.Count; i++)
                        {
                            str = string.Format("{0} -- {1} -- {2}\n"
                                , foundList.Candidates[i].Id.ToString()
                                , foundList.Candidates[i].MatchScore.ToString()
                                , foundList.Candidates[i].ConfidenceLevel.ToString());
                            searchResultsModel.AddResultCandidate(str);
                            strResults += str;
                        } // end for int i < foundList.Count
                    }
                    else
                    {
                        // Log Error
                        strError = string.Format("SearchMatches; SearchFP failure; error [{0}];", error.ToString());
                    } // end if error != SSError.NONE
                }
                else
                {
                    strError = string.Format("SearchMatches; ExtractTemplate failure; error [{0}];", error.ToString());

                } // end if //Extract Template error
            }

            //return JsonConvert.SerializeObject(foundList);
            ViewData["AllImages"] = _imageContainer.getBMPBase64Image(GetPathToDataDirectory(), sessionid);
            return Json(new {found=(bSuccess ? "true" : "false")
                , match=strResults
                , error=strError});

        }

        private string GetPathToDataDirectory()
        {
            string contentRootPath = _hostingEnvironment.ContentRootPath;
            string dataContent = Path.Combine(contentRootPath, DIRECTORY_LOCATION);

            if (!Directory.Exists(dataContent))
            {
                Directory.CreateDirectory(dataContent);
            }

            return dataContent;
        } // end GetFingerprintTbdFileWithPath


        private string GetFingprintTdbFilewithPath(string strSessionID)
        {
            string dataContent = GetPathToDataDirectory();
            return Path.Combine(dataContent, string.Format(SS3_FP_DATA, strSessionID));
        } // end GetFingerprintTbdFileWithPath

        private void FileDownTemplate(string userid, ref byte[] sg400Template, string strTemplateExtension)
        {
            string dataPath = GetPathToDataDirectory();

            string strTemplateName = Path.Combine(dataPath, TEMPLATE_FILENAME_BASE + userid + strTemplateExtension);
            using (FileStream fs = System.IO.File.Create(strTemplateName))
            {
                fs.Write(sg400Template, 0, SSConstants.TEMPLATE_SIZE);
            }
        }

    } // end SS3Controller
}

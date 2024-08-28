using CustomerService.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace CustomerService.Controllers
{
    public class CaseController : Controller
    {
        private readonly DataverseService _dataverseService;

        public CaseController(DataverseService dataverseService)
        {
            _dataverseService = dataverseService;
        }

        
        public IActionResult Index()
        {
            var columns = new ColumnSet("title","description", "ticketnumber", "prioritycode", "statuscode", "ownerid", "createdon");

            var cases = _dataverseService.RetrieveEtities("incident", columns);

            var caseList = cases.Entities.Select(e => new CaseModel
            {
                CaseId=e.Id,
                Title = e.GetAttributeValue<string>("title"),
                Description = e.GetAttributeValue<string>("description"),
                CaseNumber = e.GetAttributeValue<string>("ticketnumber"),
                Priority = e.FormattedValues.ContainsKey("prioritycode") ? e.FormattedValues["prioritycode"] : string.Empty,
                Status = e.FormattedValues.ContainsKey("statuscode") ? e.FormattedValues["statuscode"] : string.Empty,
                Owner = e.GetAttributeValue<EntityReference>("ownerid")?.Name,
                CreatedOn = e.GetAttributeValue<DateTime>("createdon")
               

            }).ToList();
            
            return View(caseList);
        }

        public IActionResult Create()
        {
            var accounts = _dataverseService.RetrieveEtities("account", new ColumnSet("name", "accountid"));

            ViewBag.Accounts = new SelectList(accounts.Entities.Select(a => new { 
                Id = a.Id,
                Name = a.GetAttributeValue<string>("name")
            }), "Id","Name");

            return View();
        }

        [HttpPost]
        public IActionResult Create(CaseModel model)
        {
           
                Entity newCase = new Entity("incident");
                newCase["title"] = model.Title;
                newCase["description"] = model.Description;
                newCase["prioritycode"] = new OptionSetValue(int.Parse(model.Priority));
                if (!string.IsNullOrEmpty(model.Customer))
                {
                    newCase["customerid"] = new EntityReference("account",Guid.Parse(model.Customer));    
                }
                
                var caseId = _dataverseService.CreateEntity(newCase);



            return RedirectToAction("Index");
        }

        public IActionResult Edit()
        {
            var accounts = _dataverseService.RetrieveEtities("account", new ColumnSet("name", "accountid"));

            ViewBag.Accounts = new SelectList(accounts.Entities.Select(a => new {
                Id = a.Id,
                Name = a.GetAttributeValue<string>("name")
            }), "Id", "Name");

            return View();
        }

        [HttpPost]
        public IActionResult Edit(CaseModel model)
        {

            Entity newCase = new Entity("incident");
            newCase["title"] = model.Title;
            newCase["description"] = model.Description;
            newCase["prioritycode"] = new OptionSetValue(int.Parse(model.Priority));
            if (!string.IsNullOrEmpty(model.Customer))
            {
                newCase["customerid"] = new EntityReference("account", Guid.Parse(model.Customer));
            }

            var caseId = _dataverseService.CreateEntity(newCase);



            return RedirectToAction("Index");
        }
    }
}

using CustomerService.Models;
using Microsoft.AspNetCore.Mvc;
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
    }
}

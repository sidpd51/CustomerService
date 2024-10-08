﻿using CustomerService.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Options;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System.Security.Claims;
using System.Linq.Dynamic.Core;
using CustomerService.Service;

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
            
            return View();
        }

        
        
        [HttpPost]
        public JsonResult GetAllCases()
        {

            int totalRecord = 0;
            int filterRecord = 0;
            var draw = Request.Form["draw"].FirstOrDefault();
            var sortColumn = Request.Form["columns[" + Request.Form["order[0][column]"].FirstOrDefault() + "][name]"].FirstOrDefault();
            var sortColumnDirection = Request.Form["order[0][dir]"].FirstOrDefault();
            var searchValue = Request.Form["search[value]"].FirstOrDefault();
            int pageSize = Convert.ToInt32(Request.Form["length"].FirstOrDefault() ?? "0");
            int skip = Convert.ToInt32(Request.Form["start"].FirstOrDefault() ?? "0");

            var columns = new ColumnSet("title", "ticketnumber", "prioritycode", "statuscode", "ownerid", "createdon");
          
            var getUserId = Convert.ToString(CurrentUser());

            var result = _dataverseService.QualiableData("incident", skip, pageSize, columns, searchValue, sortColumn, sortColumnDirection);
            var data = result.Entities.Select(e => new
            {
                CaseId = e.Id,
                Title = e.GetAttributeValue<string>("title"),
                CaseNumber = e.GetAttributeValue<string>("ticketnumber"),
                Priority = e.FormattedValues.ContainsKey("prioritycode") ? e.FormattedValues["prioritycode"] : string.Empty,
                Status = e.FormattedValues.ContainsKey("statuscode") ? e.FormattedValues["statuscode"] : string.Empty,
                OwnerId = e.GetAttributeValue<EntityReference>("ownerid")?.Id.ToString(),
                CreatedOn = e.GetAttributeValue<DateTime>("createdon"),
                UserId = Convert.ToString(getUserId)
            });

            //get total count of data in table
            totalRecord = result.TotalRecordCount;
            // search data when search value found
            
            // get total count of records after search
            filterRecord = result.TotalRecordCount;
            
            var caseList = data.ToList();
            var returnObj = new
            {
                draw = draw,
                recordsTotal = totalRecord,
                recordsFiltered = filterRecord,
                data = caseList
            };

            return Json(returnObj);
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
            ModelState.Remove(nameof(CaseModel.Owner));
            ModelState.Remove(nameof(CaseModel.CreatedOn));
            ModelState.Remove(nameof(CaseModel.Status));
            ModelState.Remove(nameof(CaseModel.CaseNumber));
            ClaimsIdentity? cIdentity = User.Identity as ClaimsIdentity;

            if (ModelState.IsValid)
            {
              

                    Entity newCase = new Entity("incident");
                    newCase["title"] = model.Title;
                    newCase["description"] = model.Description;
                    newCase["prioritycode"] = new OptionSetValue(int.Parse(model.Priority));
                    newCase["ownerid"] = new EntityReference("systemuser", CurrentUser());
                    if (!string.IsNullOrEmpty(model.Customer))
                    {
                        newCase["customerid"] = new EntityReference("account", Guid.Parse(model.Customer));
                    }

                    var caseId = _dataverseService.CreateEntity(newCase);
                    TempData["success"] = "Case created successfully!";


                    return RedirectToAction("Index");
                
               
            }

            var accounts = _dataverseService.RetrieveEtities("account", new ColumnSet("name", "accountid"));

            ViewBag.Accounts = new SelectList(accounts.Entities.Select(a => new {
                Id = a.Id,
                Name = a.GetAttributeValue<string>("name")
            }), "Id", "Name");


            return View(model);
        }

        public Guid CurrentUser()
        {
            try
            {
                ClaimsIdentity? cIdentity = User.Identity as ClaimsIdentity;

                
                
                var azureId = cIdentity.Claims.FirstOrDefault(x => x.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value;
                return _dataverseService.GetCurrentUserGuid(azureId);
                
            }
            catch (Exception)
            {

                throw;
            }
            
            
        }

        public IActionResult Edit(Guid id)
        {
            
            if (id == null)
            {

                return NotFound();

            }

            var caseEntity = _dataverseService.RetrieveEntity("incident",id, new ColumnSet("title", "description", "prioritycode", "customerid", "ticketnumber"));

            if(caseEntity == null)
            {
                return NotFound();
            }

            var model = new CaseModel
            {
                CaseId = id,
                Title = caseEntity.GetAttributeValue<string>("title"),
                Description = caseEntity.GetAttributeValue<string>("description"),
                Priority = caseEntity.GetAttributeValue<OptionSetValue>("prioritycode").Value.ToString(),
                Customer = caseEntity.GetAttributeValue<EntityReference>("customerid")?.Id.ToString(),
                CaseNumber = caseEntity.GetAttributeValue<string>("ticketnumber")
            };

            var accounts = _dataverseService.RetrieveEtities("account", new ColumnSet("name", "accountid"));

            ViewBag.Accounts = new SelectList(accounts.Entities.Select(a => new
            {
                Id = a.Id,
                Name = a.GetAttributeValue<string>("name"), 
            }), "Id", "Name", model.Customer);

            return View(model);
        }

        [HttpPost]
        public IActionResult Edit(CaseModel model)
        {
            ModelState.Remove(nameof(CaseModel.Owner));
            ModelState.Remove(nameof(CaseModel.CreatedOn));
            ModelState.Remove(nameof(CaseModel.Status));
            ModelState.Remove(nameof(CaseModel.CaseNumber));


            if (ModelState.IsValid)
            {
                Entity updateCase = new Entity("incident")
                {
                    Id = model.CaseId
                };

                updateCase["title"] = model.Title;
                updateCase["description"] = model.Description;
                updateCase["prioritycode"] = new OptionSetValue(int.Parse(model.Priority));
                
                if(!string.IsNullOrEmpty(model.Customer))
                {
                    updateCase["customerid"] = new EntityReference("account", Guid.Parse(model.Customer));
                }else
                {
                    return NotFound();
                }


                _dataverseService.UpdateEntity(updateCase);
                TempData["success"] = "Case updated successfully!";

                return RedirectToAction("Index");

            }

            var accounts = _dataverseService.RetrieveEtities("account", new ColumnSet("name", "accountid"));
            ViewBag.Accounts = new SelectList(accounts.Entities.Select(a => new
            {
                Id = a.Id,
                Name = a.GetAttributeValue<string>("name"),
            }), "Id", "Name", model.Customer);

            return View(model);
        }

        public IActionResult Delete(Guid id)
        {
            if (id == null)
            {

                return NotFound();

            }

            try
            {
                _dataverseService.DeleteEntity("incident", id);
                TempData["success"] = "Case deleted successfully!";

                return RedirectToAction("Index");
            }catch(Exception ex)
            {
                return NotFound();
            }

        }

        public IActionResult Resolve(Guid id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                // Problem Solved
                _dataverseService.SetCaseStatus(id, 5);
                TempData["success"] = "Case resolved successfully!";
                return RedirectToAction("Index");

            }
            catch (Exception ex)
            {
                return NotFound();
            }
        }

        public IActionResult Cancel(Guid id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                // Cancelled
                _dataverseService.SetCaseStatus(id, 6);
                TempData["success"] = "Case cancelled successfully!";
                return RedirectToAction("Index");

            }
            catch (Exception ex)
            {
                return NotFound();
            }
        }

        public IActionResult Reactivate(Guid id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                _dataverseService.SetCaseStatus(id, 1);
                TempData["success"] = "Case Reactivated successfully!";
                return RedirectToAction("Index");
            }
            catch (Exception)
            {

                return NotFound();
            }
        }
    }
}

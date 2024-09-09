using Microsoft.AspNetCore.Mvc;
using CustomerService.Models;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk;
using CustomerService.Service;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Crm.Sdk.Messages;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.AspNetCore.Server.Kestrel.Transport.NamedPipes;

namespace CustomerService.Controllers
{
    public class ContactController : Controller
    {
        private readonly DataverseService _dataverseService;

        public ContactController(DataverseService dataverseService)
        {
            _dataverseService = dataverseService;
        }


        public IActionResult Index()
        {

            var contact = _dataverseService.RetrieveEntity("contact", Guid.Parse("8bf926f7-156c-ef11-a670-002248d66802"), new ColumnSet("sid_uploaddoc"));
            var columns = new ColumnSet("firstname","createdon", "lastname", "parentcustomerid", "mobilephone", "emailaddress1", "sid_interest", "entityimage");
            EntityCollection entityCollection = _dataverseService.RetrieveContacts(columns);
            var contactList = entityCollection.Entities.Select(e => new ContactModel
            {
                Id = e.Id,
                FirstName = e.GetAttributeValue<string>("firstname"),
                LastName = e.GetAttributeValue<string>("lastname"),
                Account = e.FormattedValues.ContainsKey("parentcustomerid") ? e.FormattedValues["parentcustomerid"]:string.Empty,
                ShowInterest = e.FormattedValues.ContainsKey("sid_interest")? e.FormattedValues["sid_interest"]: string.Empty,
                Email = e.GetAttributeValue<string>("emailaddress1"),
                Phone = e.GetAttributeValue<string>("mobilephone"),
                CreatedOn = e.GetAttributeValue<DateTime>("createdon"),
                EntityImage = e.GetAttributeValue<byte[]>("entityimage")
            }).ToList();
            
            return View(contactList);
        }

        public IActionResult Create()
        {
            var accounts = _dataverseService.RetrieveEtities("account", new ColumnSet("name", "accountid"));

            ViewBag.Accounts = new SelectList(accounts.Entities.Select(a => new {
                Id = a.Id,
                Name = a.GetAttributeValue<string>("name")
            }), "Id", "Name");

            return View();
        }

        [HttpPost]
        public IActionResult Create(ContactModel model, IFormFile anyDoc)
        {
            try
            {
                ModelState.Remove(nameof(ContactModel.CreatedOn));
                ModelState.Remove(nameof(ContactModel.ShowInterest));
                ModelState.Remove(nameof(ContactModel.Interest));
                if (ModelState.IsValid)
                {
                    Entity newContact = new Entity("contact");
                    newContact["firstname"] = model.FirstName;
                    newContact["lastname"] = model.LastName;
                    newContact["mobilephone"] = model.Phone;
                    newContact["emailaddress1"] = model.Email;
                    
                    if (!string.IsNullOrEmpty(model.Account))
                    {
                        newContact["parentcustomerid"] = new EntityReference("account", Guid.Parse(model.Account));
                    }
                    
                    newContact["sid_interest"] = model.Interest.Length == 0 ? null : new OptionSetValueCollection(Array.ConvertAll(model.Interest, value => new OptionSetValue(value)));
                    
                    if (anyDoc != null && anyDoc.Length > 0)
                    {
                        using (var ms = new MemoryStream())
                        {
                            anyDoc.CopyTo(ms);
                            model.AnyDoc = ms.ToArray();
                            if (anyDoc.ContentType == "image/jpeg"|| anyDoc.ContentType == "image/png"|| anyDoc.ContentType == "image/jpg")
                            {
                                newContact["entityimage"] = model.AnyDoc;
                                var contactGuid = _dataverseService.CreateEntity(newContact);
                            }
                            else
                            {
                                var contactGuid = _dataverseService.CreateEntity(newContact);
                                string fileName = anyDoc.FileName;
                                var blockSize = 4194304; // Block size for uploading 4194304 ~ 4MB
                                var blockIds = new List<string>();
                                EntityReference entityReference = new EntityReference("contact", contactGuid);
                                var initializeFileUploadRequest = new InitializeFileBlocksUploadRequest
                                {
                                    FileAttributeName = "sid_uploaddoc", // Replace with the actual field schema name
                                    Target = entityReference,
                                    FileName = fileName
                                };

                                var fileUploadResponse = _dataverseService.ExecuteFileBlockUpload(initializeFileUploadRequest);
                                for (int i = 0; i < Math.Ceiling(model.AnyDoc.Length / Convert.ToDecimal(blockSize)); i++)
                                {
                                    var blockId = Convert.ToBase64String(Encoding.UTF8.GetBytes(Guid.NewGuid().ToString()));
                                    blockIds.Add(blockId);

                                    var blockData = model.AnyDoc.Skip(i * blockSize).Take(blockSize).ToArray();
                                    var blockRequest = new UploadBlockRequest()
                                    {
                                        FileContinuationToken = fileUploadResponse.FileContinuationToken,
                                        BlockId = blockId,
                                        BlockData = blockData
                                    };

                                    
                                    var blockResponse = _dataverseService.ExecuteUploadBlock(blockRequest);
                                }
                               
                                _dataverseService.CommitOperation(blockIds, fileUploadResponse.FileContinuationToken, fileName, anyDoc.ContentType);
                            }
                        }
                        
                    }

                    
                    TempData["success"] = "Contact created successfully!";
                    return RedirectToAction("Index");
                }
               
                var accounts = _dataverseService.RetrieveEtities("account", new ColumnSet("name", "accountid"));

                ViewBag.Accounts = new SelectList(accounts.Entities.Select(a => new {
                    Id = a.Id,
                    Name = a.GetAttributeValue<string>("name")
                }), "Id", "Name");


                return View(model);
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

            var contactEntity = _dataverseService.RetrieveEntity("contact", id, new ColumnSet("firstname", "createdon", "entityimage", "lastname", "parentcustomerid", "sid_uploaddoc", "sid_uploaddoc_name", "mobilephone", "emailaddress1", "sid_interest"));
            if (contactEntity == null)
            {
                return NotFound();
            }
            
            var optionSetCollection = contactEntity.GetAttributeValue<OptionSetValueCollection>("sid_interest");

            int[] interestArray = optionSetCollection != null
                ? optionSetCollection.Select(option => option.Value).ToArray()
                : new int[] { };

            EntityReference er = new EntityReference("contact", id);
            
            var model = new ContactModel
            {
                Id = id,
                FirstName = contactEntity.GetAttributeValue<string>("firstname"),
                LastName = contactEntity.GetAttributeValue<string>("lastname"),
                Account = contactEntity.GetAttributeValue<EntityReference>("parentcustomerid").Id.ToString(),
                Email = contactEntity.GetAttributeValue<string>("emailaddress1"),
                Phone = contactEntity.GetAttributeValue<string>("mobilephone"),
                EntityImage = contactEntity.GetAttributeValue<byte[]>("entityimage"),
                Interest = interestArray
             };

            if (contactEntity.GetAttributeValue<Guid>("sid_uploaddoc").ToString() != string.Empty)
            {
                model.DocName = contactEntity.GetAttributeValue<string>("sid_uploaddoc_name");
            }

            var accounts = _dataverseService.RetrieveEtities("account", new ColumnSet("name", "accountid"));

            ViewBag.Accounts = new SelectList(accounts.Entities.Select(a => new
            {
                Id = a.Id,
                Name = a.GetAttributeValue<string>("name")
            }), "Id", "Name");

            return View(model);
        }
        [HttpPost]
        public IActionResult Edit(ContactModel model, IFormFile anyDoc)
        {
            try
                    {
                ModelState.Remove(nameof(ContactModel.CreatedOn));
                ModelState.Remove(nameof(ContactModel.ShowInterest));
                ModelState.Remove(nameof(ContactModel.Interest));
                ModelState.Remove(nameof(ContactModel.DocName));
                ModelState.Remove(nameof(ContactModel.AnyDoc));
                if (ModelState.IsValid)
                {
                    Entity newContact = new Entity("contact");
                    newContact["contactid"] = model.Id;
                    newContact["firstname"] = model.FirstName;
                    newContact["lastname"] = model.LastName;
                    newContact["mobilephone"] = model.Phone;
                    newContact["emailaddress1"] = model.Email;
                    if (!string.IsNullOrEmpty(model.Account))
                    {
                        newContact["parentcustomerid"] = new EntityReference("account", Guid.Parse(model.Account));
                    }
                    newContact["sid_interest"] = model.Interest.Length == 0 ? null : new OptionSetValueCollection(Array.ConvertAll(model.Interest, value => new OptionSetValue(value)));

                    if (anyDoc != null && anyDoc.Length > 0)
                    {
                        using (var ms = new MemoryStream())
                        {
                            anyDoc.CopyTo(ms);
                            model.AnyDoc = ms.ToArray();
                            if (anyDoc.ContentType == "image/jpeg" || anyDoc.ContentType == "image/png" || anyDoc.ContentType == "image/jpg")
                            {
                                newContact["entityimage"] = model.AnyDoc;
                                _dataverseService.UpdateEntity(newContact);
                            }
                            else
                            {
                                string fileName = anyDoc.FileName;
                                var blockSize = 4194304; // Block size for uploading 4194304 ~ 4MB
                                var blockIds = new List<string>();
                                EntityReference entityReference = new EntityReference("contact", model.Id);
                                var initializeFileUploadRequest = new InitializeFileBlocksUploadRequest
                                {
                                    FileAttributeName = "sid_uploaddoc", // Replace with the actual field schema name
                                    Target = entityReference,
                                    FileName = fileName
                                };

                                var fileUploadResponse = _dataverseService.ExecuteFileBlockUpload(initializeFileUploadRequest);
                                for (int i = 0; i < Math.Ceiling(model.AnyDoc.Length / Convert.ToDecimal(blockSize)); i++)
                                {
                                    var blockId = Convert.ToBase64String(Encoding.UTF8.GetBytes(Guid.NewGuid().ToString()));
                                    blockIds.Add(blockId);

                                    var blockData = model.AnyDoc.Skip(i * blockSize).Take(blockSize).ToArray();
                                    var blockRequest = new UploadBlockRequest()
                                    {
                                        FileContinuationToken = fileUploadResponse.FileContinuationToken,
                                        BlockId = blockId,
                                        BlockData = blockData
                                    };


                                    var blockResponse = _dataverseService.ExecuteUploadBlock(blockRequest);
                                }

                                _dataverseService.CommitOperation(blockIds, fileUploadResponse.FileContinuationToken, fileName, anyDoc.ContentType);
                                _dataverseService.UpdateEntity(newContact);
                            }
                        }

                    }

                    TempData["success"] = "Contact updated successfully!";
                    return RedirectToAction("Index");
                }

                var accounts = _dataverseService.RetrieveEtities("account", new ColumnSet("name", "accountid"));

                ViewBag.Accounts = new SelectList(accounts.Entities.Select(a => new {
                    Id = a.Id,
                    Name = a.GetAttributeValue<string>("name")
                }), "Id", "Name");


                return View(model);
            }
            catch (Exception)
            {

                throw;
            }
        }

        public IActionResult Delete(Guid id)
        {
            try
            {
                if(id==null)
                {
                    NotFound();
                }
                _dataverseService.DeleteEntity("contact", id);
                TempData["success"] = "Contact deleted successfully!";
                return RedirectToAction("Index");
            }
            catch (Exception)
            {

                throw;
            }
        }


        public IActionResult Download(Guid id)
        {
            if (id == Guid.Empty)
            {
                return NotFound();
            }

            var contactEntity = _dataverseService.RetrieveEntity("contact", id, new ColumnSet("sid_uploaddoc", "sid_uploaddoc_name"));
            EntityReference er = new EntityReference("contact", id);

            byte[] fileBytes = _dataverseService.DownloadFile(er, "sid_uploaddoc");

            if(fileBytes == null || fileBytes.Length ==0)
            {
                return NotFound("file not found or is Empty!");
            }

            string fileName = contactEntity.GetAttributeValue<string>("sid_uploaddoc_name");

            return File(fileBytes, "application/octet-stream", fileName);
        } 
    }
}

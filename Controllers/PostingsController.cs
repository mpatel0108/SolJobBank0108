using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using sol_Job_Bank.Utilities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using sol_Job_Bank.Data;
using sol_Job_Bank.Models;
using Microsoft.AspNetCore.Authorization;
using sol_Job_Bank.ViewModels;

namespace sol_Job_Bank.Controllers
{
    [Authorize]
    public class PostingsController : Controller
    {
        //for sending email
        private readonly IMyEmailSender _emailSender;
        private readonly JobBankContext _context;

        public PostingsController(JobBankContext context, IMyEmailSender emailSender)
        {
            _context = context;
            _emailSender = emailSender;
        }

        // GET: Postings
        public async Task<IActionResult> Index(int? page, int? pageSizeID)
        {
            //Clear the sort/filter/paging URL Cookie
            CookieHelper.CookieSet(HttpContext, "PostingsURL", "", -1);

            var postings = _context.Postings
                .Include(p => p.Position)
                .Include(p => p.Applications)
                .OrderBy(p=>p.Position.Name);

            //Handle Paging
            int pageSize;//This is the value we will pass to PaginatedList
            if (pageSizeID.HasValue)
            {
                //Value selected from DDL so use and save it to Cookie
                pageSize = pageSizeID.GetValueOrDefault();
                CookieHelper.CookieSet(HttpContext, "pageSizeValue", pageSize.ToString(), 30);
            }
            else
            {
                //Not selected so see if it is in Cookie
                pageSize = Convert.ToInt32(HttpContext.Request.Cookies["pageSizeValue"]);
            }
            pageSize = (pageSize == 0) ? 5 : pageSize;//Neither Selected or in Cookie so go with default
            ViewData["pageSizeID"] =
                new SelectList(new[] { "3", "5", "10", "20", "30", "40", "50", "100", "500" }, pageSize.ToString());
            var pagedData = await PaginatedList<Posting>.CreateAsync(postings.AsNoTracking(), page ?? 1, pageSize);

            return View(pagedData);
        }

        // GET/POST: MedicalTrials/Notification/5
        public async Task<IActionResult> Notification(int? id, string Subject, string emailContent, string PostingSummary)
        {
            if (id == null)
            {
                return NotFound();
            }

            ViewData["id"] = id;
            ViewData["PostingSummary"] = PostingSummary;

            if (string.IsNullOrEmpty(Subject) || string.IsNullOrEmpty(emailContent))
            {
                ViewData["Message"] = "You must enter both a Subject and some message Content before sending the message.";
            }
            else
            {
                int folksCount = 0;
                try
                {
                    //Send a Notice.
                    List<EmailAddress> folks = (from p in _context.Applicants.Include(a => a.Applications)
                                                where p.Applications.Any(a => a.PostingID == id)
                                                select new EmailAddress
                                                {
                                                    Name = p.FullName,
                                                    Address = p.eMail
                                                }).ToList();
                    folksCount = folks.Count();
                    if (folksCount > 0)
                    {
                        var msg = new EmailMessage()
                        {
                            ToAddresses = folks,
                            Subject = Subject,
                            Content = "<p>" + emailContent + 
                                "</p><p>Note: You are receiving this message becasue you applied to our Job Posting for " +
                                PostingSummary + "</p>"

                        };
                        await _emailSender.SendToManyAsync(msg);
                        ViewData["Message"] = "Message sent to " + folksCount + " Applicant"
                            + ((folksCount == 1) ? "." : "s.");
                    }
                    else
                    {
                        ViewData["Message"] = "Message NOT sent!  No Applicants have applied to this Job Posting.";
                    }
                }
                catch (Exception ex)
                {
                    string errMsg = ex.GetBaseException().Message;
                    ViewData["Message"] = "Error: Could not send email message to the " + folksCount + " Patient"
                        + ((folksCount == 1) ? "" : "s") + " in the trial.";
                }
            }
            return View();
        }

        // GET: Postings/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            //Get the URL with the last filter, sort and page parameters
            ViewData["returnURL"] = MaintainURL.ReturnURL(HttpContext, "Postings");

            var posting = await _context.Postings
                .Include(p => p.Position)
                .Include(p => p.PostingDocuments)
                .Include(p => p.Applications).ThenInclude(p => p.Applicant)
                .FirstOrDefaultAsync(m => m.ID == id);
            if (posting == null)
            {
                return NotFound();
            }

            return View(posting);
        }

        // GET: Postings/Create
        [Authorize(Roles = "Admin,Supervisor,Staff")]
        public IActionResult Create()
        {
            //Get the URL with the last filter, sort and page parameters
            ViewData["returnURL"] = MaintainURL.ReturnURL(HttpContext, "Postings");

            PopulateDropDownLists();
            return View();
        }

        // POST: Postings/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Supervisor,Staff")]
        public async Task<IActionResult> Create([Bind("ID,NumberOpen,ClosingDate,StartDate,PositionID")] Posting posting
            , List<IFormFile> theFiles)
        {
            //Get the URL with the last filter, sort and page parameters
            ViewData["returnURL"] = MaintainURL.ReturnURL(HttpContext, "Postings");
            try
            {
                if (ModelState.IsValid)
                {
                    await AddDocumentsAsync(posting, theFiles);
                    _context.Add(posting);
                    await _context.SaveChangesAsync();
                    return RedirectToAction("Details", new { posting.ID });
                }
            }
            catch (RetryLimitExceededException /* dex */)
            {
                ModelState.AddModelError("", "Unable to save changes after multiple attempts. Try again, and if the problem persists, see your system administrator.");
            }
            catch (DbUpdateException dex)
            {
                if (dex.GetBaseException().Message.Contains("UNIQUE constraint failed"))
                {
                    ModelState.AddModelError("", "Unable to save changes. Remember, you cannot have multiple postings for the same position with the same closing date.");
                }
                else
                {
                    ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists see your system administrator.");
                }
            }

            PopulateDropDownLists(posting);
            return View(posting);
        }

        // GET: Postings/Edit/5
        [Authorize(Roles = "Admin,Supervisor,Staff")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            //Get the URL with the last filter, sort and page parameters
            ViewData["returnURL"] = MaintainURL.ReturnURL(HttpContext, "Postings");

            var posting = await _context.Postings
                .Include(p=>p.PostingDocuments)
                .SingleOrDefaultAsync(p=>p.ID==id);
            if (posting == null)
            {
                return NotFound();
            }

            //Staff can only edit records they created
            if (User.IsInRole("Staff") && (User.Identity.Name!=posting.CreatedBy))
            {
                ModelState.AddModelError("", "NOTE: You cannot save changes to this record since you did not enter it into the system.");
            }

            PopulateDropDownLists(posting);
            return View(posting);
        }

        // POST: Postings/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Supervisor,Staff")]
        public async Task<IActionResult> Edit(int id, List<IFormFile> theFiles)
        {
            //Get the URL with the last filter, sort and page parameters
            ViewData["returnURL"] = MaintainURL.ReturnURL(HttpContext, "Postings");

            var postingToUpdate = await _context.Postings
                .Include(p => p.PostingDocuments)
                .SingleOrDefaultAsync(p => p.ID == id);
            if (postingToUpdate == null)
            {
                return NotFound();
            }

            //Staff can only edit records they created
            if (User.IsInRole("Staff") && (User.Identity.Name != postingToUpdate.CreatedBy))
            {
                return Redirect(ViewData["returnURL"].ToString());
            }

            if (await TryUpdateModelAsync<Posting>(postingToUpdate, "",
                d => d.NumberOpen, d => d.ClosingDate, d => d.StartDate, d => d.PositionID))
            {
                try
                {
                    await AddDocumentsAsync(postingToUpdate, theFiles);
                    await _context.SaveChangesAsync();
                    return RedirectToAction("Details", new { postingToUpdate.ID });
                }
                catch (RetryLimitExceededException /* dex */)
                {
                    ModelState.AddModelError("", "Unable to save changes after multiple attempts. Try again, and if the problem persists, see your system administrator.");
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PostingExists(postingToUpdate.ID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                catch (DbUpdateException dex)
                {
                    if (dex.GetBaseException().Message.Contains("UNIQUE constraint failed"))
                    {
                        ModelState.AddModelError("", "Unable to save changes. Remember, you cannot have multiple postings for the same position with the same closing date.");
                    }
                    else
                    {
                        ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists see your system administrator.");
                    }
                }
            }
            PopulateDropDownLists(postingToUpdate);
            return View(postingToUpdate);
        }

        // GET: Postings/Delete/5
        [Authorize(Roles = "Admin,Supervisor")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            //Get the URL with the last filter, sort and page parameters
            ViewData["returnURL"] = MaintainURL.ReturnURL(HttpContext, "Postings");

            var posting = await _context.Postings
                .Include(p => p.Position)
                .FirstOrDefaultAsync(m => m.ID == id);
            if (posting == null)
            {
                return NotFound();
            }

            return View(posting);
        }

        // POST: Postings/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Supervisor")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            //Get the URL with the last filter, sort and page parameters
            ViewData["returnURL"] = MaintainURL.ReturnURL(HttpContext, "Postings");

            var posting = await _context.Postings.FindAsync(id);
            try
            {
                _context.Postings.Remove(posting);
                await _context.SaveChangesAsync();
                return Redirect(ViewData["returnURL"].ToString());
            }
            catch (DbUpdateException dex)
            {
                if (dex.GetBaseException().Message.Contains("FOREIGN KEY constraint failed"))
                {
                    ModelState.AddModelError("", "Unable to delete Posting. Remember, you cannot delete a Posting once applications have been submitted.");
                }
                else if (dex.GetBaseException().Message.Contains("Cannot delete seeded data"))
                {
                    ModelState.AddModelError("", "Unable to delete. Remember, you cannot delete originally seeded data.");
                }
                else
                {
                    ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists see your system administrator.");
                }
            }
            return View(posting);

        }

        public FileContentResult Download(int id)
        {
            var theFile = _context.PostingDocuments
                .Include(d => d.FileContent)
                .Where(f => f.ID == id)
                .SingleOrDefault();
            return File(theFile.FileContent.Content, theFile.MimeType, theFile.FileName);
        }

        private async Task AddDocumentsAsync(Posting posting, List<IFormFile> theFiles)
        {
            foreach (var f in theFiles)
            {
                if (f != null)
                {
                    string mimeType = f.ContentType;
                    string fileName = Path.GetFileName(f.FileName);
                    long fileLength = f.Length;
                    //Note: you could filter for mime types if you only want to allow
                    //certain types of files.  I am allowing everything.
                    if (!(fileName == "" || fileLength == 0))//Looks like we have a file!!!
                    {
                        PostingDocument d = new PostingDocument();
                        using (var memoryStream = new MemoryStream())
                        {
                            await f.CopyToAsync(memoryStream);
                            d.FileContent.Content = memoryStream.ToArray();
                        }
                        d.MimeType = mimeType;
                        d.FileName = fileName;
                        posting.PostingDocuments.Add(d);
                    };
                }
            }
        }

        //Broke the Populate approach down into 2 methods for future use.
        private void PopulateDropDownLists(Posting posting = null)
        {
            ViewData["PositionID"] = PositionSelectList(posting?.PositionID);
        }
        private SelectList PositionSelectList(int? id)
        {
            var dQuery = from d in _context.Positions
                         orderby d.Name
                         select d;
            return new SelectList(dQuery, "ID", "Name", id);
        }

        private bool PostingExists(int id)
        {
            return _context.Postings.Any(e => e.ID == id);
        }
    }
}

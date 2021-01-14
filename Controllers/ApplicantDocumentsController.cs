using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using sol_Job_Bank.Data;
using sol_Job_Bank.Models;

namespace sol_Job_Bank.Controllers
{
    public class ApplicantDocumentsController : Controller
    {
        private readonly JobBankContext _context;

        public ApplicantDocumentsController(JobBankContext context)
        {
            _context = context;
        }

        // GET: ApplicantDocuments
        public IActionResult Index()
        {
            return RedirectToAction("Index", "Applicants", null);
        }

        // GET: ApplicantDocuments/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var applicantDocument = await _context.ApplicantDocuments
                .Include(d=>d.Applicant).FirstOrDefaultAsync(d => d.ID==id);
            if (applicantDocument == null)
            {
                return NotFound();
            }
            return View(applicantDocument);
        }

        // POST: ApplicantDocuments/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id)
        {
            var applicantDocumentToUpdate = await _context.ApplicantDocuments
                .Include(d => d.Applicant).FirstOrDefaultAsync(d => d.ID == id);
            //Check that you got it or exit with a not found error
            if (applicantDocumentToUpdate == null)
            {
                return NotFound();
            }

            if (await TryUpdateModelAsync<ApplicantDocument>(applicantDocumentToUpdate, "",
                d => d.FileName, d => d.Description))
            {
                try
                {
                    await _context.SaveChangesAsync();
                    return RedirectToAction("Details","Applicants",new { ID=applicantDocumentToUpdate.ApplicantID });
                }
                catch (DbUpdateException)
                {
                    ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists see your system administrator.");
                }
            }
            return View(applicantDocumentToUpdate);
        }

        // GET: ApplicantDocuments/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var applicantDocument = await _context.ApplicantDocuments
                .Include(a => a.Applicant)
                .FirstOrDefaultAsync(m => m.ID == id);
            if (applicantDocument == null)
            {
                return NotFound();
            }

            return View(applicantDocument);
        }

        // POST: ApplicantDocuments/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var applicantDocument = await _context.ApplicantDocuments.FindAsync(id);
            try
            {
                _context.ApplicantDocuments.Remove(applicantDocument);
                await _context.SaveChangesAsync();
                return RedirectToAction("Details", "Applicants", new { ID=applicantDocument.ApplicantID });
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists see your system administrator.");
            }
            return View(applicantDocument);
        }
    }
}

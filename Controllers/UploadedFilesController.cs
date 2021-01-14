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
    public class UploadedFilesController : Controller
    {
        private readonly JobBankContext _context;

        public UploadedFilesController(JobBankContext context)
        {
            _context = context;
        }

        // GET: UploadedFiles
        public async Task<IActionResult> Index()
        {
            var uploadedFiles = from f in _context.UploadedFiles
                                select f;
            return View(await uploadedFiles.ToListAsync());
        }

        // GET: UploadedFiles/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var uploadedFile = await _context.UploadedFiles
                .FirstOrDefaultAsync(m => m.ID == id);
            if (uploadedFile == null)
            {
                return NotFound();
            }

            return View(uploadedFile);
        }

        // GET: UploadedFiles/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: UploadedFiles/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ID,MimeType,FileName,Description")] UploadedFile uploadedFile)
        {
            if (ModelState.IsValid)
            {
                _context.Add(uploadedFile);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(uploadedFile);
        }

        // GET: UploadedFiles/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var uploadedFile = await _context.UploadedFiles.FindAsync(id);
            if (uploadedFile == null)
            {
                return NotFound();
            }
            return View(uploadedFile);
        }

        // POST: UploadedFiles/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ID,MimeType,FileName,Description")] UploadedFile uploadedFile)
        {
            if (id != uploadedFile.ID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(uploadedFile);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UploadedFileExists(uploadedFile.ID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(uploadedFile);
        }

        // GET: UploadedFiles/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var uploadedFile = await _context.UploadedFiles
                .FirstOrDefaultAsync(m => m.ID == id);
            if (uploadedFile == null)
            {
                return NotFound();
            }

            return View(uploadedFile);
        }

        // POST: UploadedFiles/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var uploadedFile = await _context.UploadedFiles.FindAsync(id);
            _context.UploadedFiles.Remove(uploadedFile);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool UploadedFileExists(int id)
        {
            return _context.UploadedFiles.Any(e => e.ID == id);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using sol_Job_Bank.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using sol_Job_Bank.Data;
using sol_Job_Bank.Models;
using Microsoft.AspNetCore.Authorization;

namespace sol_Job_Bank.Controllers
{
    [Authorize(Roles = "Admin,Supervisor")]
    public class SkillsController : Controller
    {
        private readonly JobBankContext _context;

        public SkillsController(JobBankContext context)
        {
            _context = context;
        }

        // GET: Skills
        public async Task<IActionResult> Index(int? page, int? pageSizeID)
        {
            //Clear the sort/filter/paging URL Cookie
            CookieHelper.CookieSet(HttpContext, "SkillsURL", "", -1);

            var skills = _context.Skills
                .OrderBy(s=>s.Name);

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
            var pagedData = await PaginatedList<Skill>.CreateAsync(skills.AsNoTracking(), page ?? 1, pageSize);

            return View(pagedData);
        }

        // GET: Skills/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            //Get the URL with the last filter, sort and page parameters
            ViewData["returnURL"] = MaintainURL.ReturnURL(HttpContext, "Skills");

            var skill = await _context.Skills
                .FirstOrDefaultAsync(m => m.ID == id);
            if (skill == null)
            {
                return NotFound();
            }

            return View(skill);
        }

        // GET: Skills/Create
        public IActionResult Create()
        {
            //Get the URL with the last filter, sort and page parameters
            ViewData["returnURL"] = MaintainURL.ReturnURL(HttpContext, "Skills");

            return View();
        }

        // POST: Skills/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ID,Name")] Skill skill)
        {
            //Get the URL with the last filter, sort and page parameters
            ViewData["returnURL"] = MaintainURL.ReturnURL(HttpContext, "Skills");

            try
            {
                if (ModelState.IsValid)
                {
                    _context.Add(skill);
                    await _context.SaveChangesAsync();
                    return RedirectToAction("Details", new { skill.ID });
                }
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists see your system administrator.");
            }

            return View(skill);
        }

        // GET: Skills/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            //Get the URL with the last filter, sort and page parameters
            ViewData["returnURL"] = MaintainURL.ReturnURL(HttpContext, "Skills");

            var skill = await _context.Skills.FindAsync(id);
            if (skill == null)
            {
                return NotFound();
            }
            return View(skill);
        }

        // POST: Skills/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id)
        {
            var skillToUpdate = await _context.Skills.FindAsync(id);
            //Check that you got it or exit with a not found error
            if (skillToUpdate == null)
            {
                return NotFound();
            }

            //Get the URL with the last filter, sort and page parameters
            ViewData["returnURL"] = MaintainURL.ReturnURL(HttpContext, "Skills");

            if (await TryUpdateModelAsync<Skill>(skillToUpdate, "",
                o => o.Name))
            {
                try
                {
                    await _context.SaveChangesAsync();
                    return RedirectToAction("Details", new { skillToUpdate.ID });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SkillExists(skillToUpdate.ID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                catch (DbUpdateException)
                {
                    ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists see your system administrator.");
                }

            }
            return View(skillToUpdate);
        }

        // GET: Skills/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            //Get the URL with the last filter, sort and page parameters
            ViewData["returnURL"] = MaintainURL.ReturnURL(HttpContext, "Skills");

            var skill = await _context.Skills
                .FirstOrDefaultAsync(m => m.ID == id);
            if (skill == null)
            {
                return NotFound();
            }

            return View(skill);
        }

        // POST: Skills/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            //Get the URL with the last filter, sort and page parameters
            ViewData["returnURL"] = MaintainURL.ReturnURL(HttpContext, "Skills");

            var skill = await _context.Skills.FindAsync(id);
            try
            {
                _context.Skills.Remove(skill);
                await _context.SaveChangesAsync();
                return Redirect(ViewData["returnURL"].ToString());
            }
            catch (DbUpdateException dex)
            {
                if (dex.GetBaseException().Message.Contains("FOREIGN KEY constraint failed"))
                {
                    ModelState.AddModelError("", "Unable to delete Skill. Remember, you cannot delete a Skill noted for any Applicants.");
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
            return View(skill);
        }

        private bool SkillExists(int id)
        {
            return _context.Skills.Any(e => e.ID == id);
        }
    }
}

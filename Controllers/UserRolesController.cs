using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using sol_Job_Bank.Data;
using sol_Job_Bank.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace sol_Job_Bank.Controllers
{
    [Authorize(Roles = "Security")]
    public class UserRolesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public UserRolesController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }
        // GET: User
        public async Task<IActionResult> Index()
        {
            var users = await (from u in _context.Users
                               .OrderBy(u => u.UserName)
                               select new UserVM
                               {
                                   Id = u.Id,
                                   UserName = u.UserName
                               }).ToListAsync();
            foreach (var u in users)
            {
                var user = await _userManager.FindByIdAsync(u.Id);
                u.UserRoles = await _userManager.GetRolesAsync(user);
            };
            return View(users);
        }

        // GET: Users/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null)
            {
                return new BadRequestResult();
            }
            var _user = await _userManager.FindByIdAsync(id);//IdentityRole
            if (_user == null)
            {
                return NotFound();
            }
            UserVM user = new UserVM
            {
                Id = _user.Id,
                UserName = _user.UserName,
                UserRoles = await _userManager.GetRolesAsync(_user)
            };
            PopulateAssignedRoleData(user);

            if(user.UserName==User.Identity.Name) //Current User
            {
                ModelState.AddModelError("", "NOTE: You cannot save changes to your own role assignments.");
            }

            return View(user);
        }

        // POST: Users/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string Id, string[] selectedOptions)
        {
            var _user = await _userManager.FindByIdAsync(Id);//IdentityRole

            if (_user.UserName == User.Identity.Name) //Current User
            {
                return RedirectToAction("Index");
            }

            UserVM user = new UserVM
            {
                Id = _user.Id,
                UserName = _user.UserName,
                UserRoles = await _userManager.GetRolesAsync(_user)
            };
            try
            {
                await UpdateUserRoles(selectedOptions, user);
                return RedirectToAction("Index");
            }
            catch (Exception)
            {
                ModelState.AddModelError(string.Empty,
                                "Unable to save changes.");
            }
            PopulateAssignedRoleData(user);
            return View(user);
        }

        private void PopulateAssignedRoleData(UserVM user)
        {//Prepare to lists for roles, selected and available
            var allRoles = _context.Roles;
            var currentRoles = user.UserRoles;
            var selected = new List<RoleVM>();
            var available = new List<RoleVM>();
            foreach (var r in allRoles)
            {
                if (currentRoles.Contains(r.Name))
                {
                    selected.Add(new RoleVM
                    {
                        ID = r.Name,
                        DisplayText = r.Name
                    });
                }
                else
                {
                    available.Add(new RoleVM
                    {
                        ID = r.Name,
                        DisplayText = r.Name
                    });
                }
            }
            ViewData["selOpts"] = new MultiSelectList(selected.OrderBy(s => s.DisplayText), "ID", "DisplayText");
            ViewData["availOpts"] = new MultiSelectList(available.OrderBy(s => s.DisplayText), "ID", "DisplayText");
        }

        private async Task UpdateUserRoles(string[] selectedOptions, UserVM userToUpdate)
        {
            var userRoles = userToUpdate.UserRoles;//Current roles use is in
            var _user = await _userManager.FindByIdAsync(userToUpdate.Id);//IdentityUser

            if (selectedOptions == null)
            {
                //No roles selected so just remove any currently assigned
                foreach (var r in userRoles)
                {
                    await _userManager.RemoveFromRoleAsync(_user, r);
                }
            }
            else
            {
                //At least one role checked so loop through all the roles
                //and add or remove as required

                //We need to do this next line because foreach loops don't always work well
                //for data returned by EF when working async.  Pulling it into an IList<>
                //first means we can safely loop over the colleciton making async calls and avoid
                //the error 'New transaction is not allowed because there are other threads running in the session'
                IList<IdentityRole> allRoles = _context.Roles.ToList<IdentityRole>();

                foreach (var r in allRoles)
                {
                    if (selectedOptions.Contains(r.Name))
                    {
                        if (!userRoles.Contains(r.Name))
                        {
                            await _userManager.AddToRoleAsync(_user, r.Name);
                        }
                    }
                    else
                    {
                        if (userRoles.Contains(r.Name))
                        {
                            await _userManager.RemoveFromRoleAsync(_user, r.Name);
                        }
                    }
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _context.Dispose();
                _userManager.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}

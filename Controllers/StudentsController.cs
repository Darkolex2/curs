using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Workforce.Data;
using Workforce.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Workforce.Controllers
{
    public class StudentsController : Controller
    {
        private readonly SchoolContext _context;

        public StudentsController(SchoolContext context)
        {
            _context = context;
        }

        // GET: Students
        public async Task<IActionResult> Index(string sortOrder, string currentFilter, string searchString, int? pageNumber)
        {
            ViewData["CurrentSort"] = sortOrder;
            ViewData["NameSortParm"] = string.IsNullOrEmpty(sortOrder) ? "LastName_desc" : "";
            ViewData["DateSortParm"] = sortOrder == "EnrollmentDate" ? "EnrollmentDate_desc" : "EnrollmentDate";

            if (searchString != null) pageNumber = 1;
            else searchString = currentFilter;

            ViewData["CurrentFilter"] = searchString;

            var students = _context.Students.AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                students = students.Where(s => s.LastName.Contains(searchString) || s.FirstMidName.Contains(searchString));
            }

            if (string.IsNullOrEmpty(sortOrder)) sortOrder = "LastName";

            bool descending = sortOrder.EndsWith("_desc");
            if (descending) sortOrder = sortOrder[..^5];

            students = descending
                ? students.OrderByDescending(e => EF.Property<object>(e, sortOrder))
                : students.OrderBy(e => EF.Property<object>(e, sortOrder));

            return View(await PaginatedList<Student>.CreateAsync(students.AsNoTracking(), pageNumber ?? 1, 3));
        }

        // GET: Students/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            var student = await _context.Students
                .Include(s => s.Enrollments).ThenInclude(e => e.Course)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.ID == id);

            return student == null ? NotFound() : View(student);
        }

        // GET: Students/Create
        public IActionResult Create() => View();

        // POST: Students/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("EnrollmentDate,FirstMidName,LastName")] Student student)
        {
            if (!ModelState.IsValid) return View(student);

            _context.Add(student);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Students/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            var student = await _context.Students.FindAsync(id);
            return student == null ? NotFound() : View(student);
        }

        // POST: Students/Edit/5
        [HttpPost, ActionName("Edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPost(int? id)
        {
            var student = await _context.Students.FirstOrDefaultAsync(s => s.ID == id);
            if (student == null) return NotFound();

            if (await TryUpdateModelAsync(student, "", s => s.FirstMidName, s => s.LastName, s => s.EnrollmentDate))
            {
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            return View(student);
        }

        // GET: Students/Delete/5
        public async Task<IActionResult> Delete(int? id, bool? saveChangesError = false)
        {
            var student = await _context.Students.AsNoTracking().FirstOrDefaultAsync(m => m.ID == id);
            if (student == null) return NotFound();

            if (saveChangesError.GetValueOrDefault())
                ViewData["ErrorMessage"] = "Delete failed. Try again.";

            return View(student);
        }

        // POST: Students/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var student = await _context.Students.FindAsync(id);
            if (student != null)
            {
                _context.Students.Remove(student);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool StudentExists(int id) => _context.Students.Any(e => e.ID == id);
    }
}

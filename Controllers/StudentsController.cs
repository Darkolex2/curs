using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Workforce.Data;
using Workforce.Models;

namespace Workforce.Controllers
{
    public class StudentsController : Controller
    {
        private readonly SchoolContext _context;

        public StudentsController(SchoolContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            return View(await _context.Students.Include(s => s.Enrollments).ThenInclude(e => e.Course).ToListAsync());
        }

        public IActionResult Create()
        {
            PopulateCoursesDropDownList();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("LastName,FirstMidName,EnrollmentDate")] Student student, int[] selectedCourses)
        {
            if (selectedCourses != null)
            {
                student.Enrollments = new List<Enrollment>();
                foreach (var courseId in selectedCourses)
                {
                    student.Enrollments.Add(new Enrollment
                    {
                        CourseID = courseId
                    });
                }
            }

            if (ModelState.IsValid)
            {
                _context.Add(student);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            PopulateCoursesDropDownList(selectedCourses);
            return View(student);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var student = await _context.Students
                .Include(s => s.Enrollments)
                .ThenInclude(e => e.Course)
                .FirstOrDefaultAsync(m => m.ID == id);

            if (student == null)
            {
                return NotFound();
            }

            PopulateCoursesDropDownList(student.Enrollments.Select(e => e.CourseID));
            return View(student);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ID,LastName,FirstMidName,EnrollmentDate")] Student student, int[] selectedCourses)
        {
            if (id != student.ID)
            {
                return NotFound();
            }

            var studentToUpdate = await _context.Students
                .Include(s => s.Enrollments)
                .ThenInclude(e => e.Course)
                .FirstOrDefaultAsync(s => s.ID == id);

            if (await TryUpdateModelAsync(studentToUpdate, "", s => s.FirstMidName, s => s.LastName, s => s.EnrollmentDate))
            {
                UpdateStudentCourses(selectedCourses, studentToUpdate);

                try
                {
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateException)
                {
                    ModelState.AddModelError("", "Unable to save changes.");
                }
            }

            PopulateCoursesDropDownList(selectedCourses);
            return View(studentToUpdate);
        }

        private void PopulateCoursesDropDownList(IEnumerable<int> selectedCourses = null)
        {
            var courses = _context.Courses.OrderBy(c => c.Title).ToList();
            ViewBag.Courses = new MultiSelectList(courses, "CourseID", "Title", selectedCourses);
        }

        private void UpdateStudentCourses(int[] selectedCourses, Student student)
        {
            if (selectedCourses == null)
            {
                student.Enrollments = new List<Enrollment>();
                return;
            }

            var selectedCoursesHS = new HashSet<int>(selectedCourses);
            var studentCourses = new HashSet<int>(student.Enrollments.Select(e => e.CourseID));

            foreach (var course in _context.Courses)
            {
                if (selectedCoursesHS.Contains(course.CourseID))
                {
                    if (!studentCourses.Contains(course.CourseID))
                    {
                        student.Enrollments.Add(new Enrollment { CourseID = course.CourseID, StudentID = student.ID });
                    }
                }
                else
                {
                    if (studentCourses.Contains(course.CourseID))
                    {
                        var enrollmentToRemove = student.Enrollments.FirstOrDefault(e => e.CourseID == course.CourseID);
                        if (enrollmentToRemove != null)
                            _context.Remove(enrollmentToRemove);
                    }
                }
            }
        }
    }
}

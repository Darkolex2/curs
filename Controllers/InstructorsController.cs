using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Workforce.Data;
using Workforce.Models;
using Workforce.Models.SchoolViewModels;

namespace Workforce.Controllers
{
    public class InstructorsController : Controller
    {
        private readonly SchoolContext _context;

        public InstructorsController(SchoolContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(int? id, int? courseID)
        {
            var viewModel = new InstructorIndexData();
            viewModel.Instructors = await _context.Instructors
                .Include(i => i.OfficeAssignment)
                .Include(i => i.CourseAssignments)
                .ThenInclude(i => i.Course)
                .ThenInclude(i => i.Department)
                .OrderBy(i => i.LastName)
                .ToListAsync();

            ViewData["InstructorID"] = id.GetValueOrDefault();
            var instructor = viewModel.Instructors.FirstOrDefault(i => i.ID == id);
            viewModel.Courses = instructor?.CourseAssignments.Select(s => s.Course).ToList() ?? new List<Course>();

            ViewData["CourseID"] = courseID.GetValueOrDefault();
            var selectedCourse = viewModel.Courses.FirstOrDefault(x => x.CourseID == courseID);

            if (selectedCourse != null)
            {
                await _context.Entry(selectedCourse).Collection(x => x.Enrollments).LoadAsync();

                foreach (Enrollment enrollment in selectedCourse.Enrollments)
                {
                    await _context.Entry(enrollment).Reference(x => x.Student).LoadAsync();
                }

                viewModel.Enrollments = selectedCourse.Enrollments;
            }

            return View(viewModel);
        }

        public async Task<IActionResult> Details(int id)
        {
            var instructor = await _context.Instructors.FirstOrDefaultAsync(m => m.ID == id);
            return View(instructor);
        }

        public IActionResult Create()
        {
            var instructor = new Instructor();
            instructor.CourseAssignments = new List<CourseAssignment>();
            PopulateAssignedCourseData(instructor);
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("FirstMidName,HireDate,LastName,OfficeAssignment")] Instructor instructor, string[] selectedCourses)
        {
            instructor.CourseAssignments = new List<CourseAssignment>();
            foreach (var course in selectedCourses)
            {
                instructor.CourseAssignments.Add(new CourseAssignment { InstructorID = instructor.ID, CourseID = int.Parse(course) });
            }

            _context.Add(instructor);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var instructor = await _context.Instructors
                .Include(i => i.OfficeAssignment)
                .Include(i => i.CourseAssignments).ThenInclude(i => i.Course)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.ID == id);

            PopulateAssignedCourseData(instructor);
            return View(instructor);
        }

        private void PopulateAssignedCourseData(Instructor instructor)
        {
            var allCourses = _context.Courses;
            var instructorCourses = new HashSet<int>(instructor?.CourseAssignments?.Select(c => c.CourseID) ?? Enumerable.Empty<int>());
            var viewModel = new List<AssignedCourseData>();

            foreach (var course in allCourses)
            {
                viewModel.Add(new AssignedCourseData
                {
                    CourseID = course.CourseID,
                    Title = course.Title,
                    Assigned = instructorCourses.Contains(course.CourseID)
                });
            }

            ViewData["Courses"] = viewModel;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, string[] selectedCourses)
        {
            var instructorToUpdate = await _context.Instructors
                .Include(i => i.OfficeAssignment)
                .Include(i => i.CourseAssignments)
                .ThenInclude(i => i.Course)
                .FirstOrDefaultAsync(m => m.ID == id);

            await TryUpdateModelAsync<Instructor>(
                instructorToUpdate,
                "",
                i => i.FirstMidName, i => i.LastName, i => i.HireDate, i => i.OfficeAssignment);

            UpdateInstructorCourses(selectedCourses, instructorToUpdate);

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private void UpdateInstructorCourses(string[] selectedCourses, Instructor instructorToUpdate)
        {
            var selectedCoursesHS = new HashSet<string>(selectedCourses);
            var instructorCourses = new HashSet<int>(instructorToUpdate.CourseAssignments.Select(c => c.Course.CourseID));

            foreach (var course in _context.Courses)
            {
                if (selectedCoursesHS.Contains(course.CourseID.ToString()))
                {
                    if (!instructorCourses.Contains(course.CourseID))
                    {
                        instructorToUpdate.CourseAssignments.Add(new CourseAssignment { InstructorID = instructorToUpdate.ID, CourseID = course.CourseID });
                    }
                }
                else
                {
                    if (instructorCourses.Contains(course.CourseID))
                    {
                        var courseToRemove = instructorToUpdate.CourseAssignments.FirstOrDefault(i => i.CourseID == course.CourseID);
                        if (courseToRemove != null)
                        {
                            _context.Remove(courseToRemove);
                        }
                    }
                }
            }
        }

        public async Task<IActionResult> Delete(int id)
        {
            var instructor = await _context.Instructors.FirstOrDefaultAsync(m => m.ID == id);
            return View(instructor);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var instructor = await _context.Instructors
                .Include(i => i.CourseAssignments)
                .FirstOrDefaultAsync(i => i.ID == id);

            var departments = await _context.Departments
                .Where(d => d.InstructorID == id)
                .ToListAsync();

            foreach (var d in departments)
            {
                d.InstructorID = null;
            }

            _context.Instructors.Remove(instructor);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        private bool InstructorExists(int id)
        {
            return true;
        }
    }
}

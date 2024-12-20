﻿
namespace DemoPresentationLayer.Controllers
{
    public class EmployeesController : Controller
    {
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        public EmployeesController(IMapper mapper, IUnitOfWork unitOfWork)
        {
            _mapper = mapper;
            _unitOfWork = unitOfWork;
        }

        public async Task<IActionResult> Index(string? searchValue)
        {
            var employees = Enumerable.Empty<Employee>();
            if (string.IsNullOrEmpty(searchValue))
                employees = await _unitOfWork.Employees.GetAllWithDepartmentsAsync();
            else
                employees = await _unitOfWork.Employees.GetAllAsync(searchValue);

            var employeesViewModel = _mapper.Map<IEnumerable<Employee>, IEnumerable<EmployeeViewModel>>(employees);

            return View(employeesViewModel);
        }

        public async Task<IActionResult> Create()
        {
            var departments = await _unitOfWork.Departments.GetAllAsync();
            SelectList listItems = new SelectList(departments , "Id", "Name");
            ViewBag.Departments = listItems;
            return View();
        }
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EmployeeViewModel employeeVM)
        {
            if (!ModelState.IsValid) return View(employeeVM);

            if (employeeVM.Image is not null)
                employeeVM.ImageName = await DocumentSettings.UploadFileAsync(employeeVM.Image, "Images");

            var employee = _mapper.Map<EmployeeViewModel, Employee>(employeeVM);
            await _unitOfWork.Employees.AddAsync(employee);
            await _unitOfWork.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Details([FromRoute] int? id)
            => await EmployeeControllerHandlerAsync(id, nameof(Details));

        public async Task<IActionResult> Edit([FromRoute] int? id)
            => await EmployeeControllerHandlerAsync(id , nameof(Edit));
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit([FromRoute]int? id, EmployeeViewModel employeeVM)
        {
            if (id != employeeVM.Id) return BadRequest();

            if (ModelState.IsValid)
            {
                try
                {
                    if (employeeVM.Image is not null)
                        employeeVM.ImageName = await DocumentSettings.UploadFileAsync(employeeVM.Image, "Images");

                    var employee = _mapper.Map<EmployeeViewModel, Employee>(employeeVM);
                    _unitOfWork.Employees.Update(employee);
                    if (await _unitOfWork.SaveChangesAsync() > 0)
                        TempData["Message"] = "Employee Updated Successfully";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    // log Exception
                    ModelState.AddModelError("", ex.Message);
                }
            }
            return View(employeeVM);

        }

        public async Task<IActionResult> Delete([FromRoute] int? id)
            => await EmployeeControllerHandlerAsync(id, nameof(Delete));
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete([FromRoute]int? id, Employee employee)
        {
            if (id != employee.Id) return BadRequest();
            if (ModelState.IsValid)
            {
                try
                {
                    _unitOfWork.Employees.Delete(employee);
                    if (await _unitOfWork.SaveChangesAsync() > 0 && employee.ImageName is not null)
                        DocumentSettings.DeleteFile("Images", employee.ImageName);
                    return RedirectToAction(nameof(Index));
                }
                catch(Exception ex)
                {
                    ModelState.AddModelError("", ex.Message);
                }
            }
            return View(employee);
        }

        public async Task<IActionResult> EmployeeControllerHandlerAsync(int? id, string viewName)
        {
            if(viewName == nameof(Edit))
            {
                var departments = await _unitOfWork.Departments.GetAllAsync();
                SelectList listItems = new SelectList(departments, "Id", "Name");
                ViewBag.Departments = listItems;
            }
            if (!id.HasValue) return BadRequest();
            var employee = await _unitOfWork.Employees.GetAsync(id.Value);
            if (employee is null) return NotFound();

            #region Manual Mapping
            /*            var employeeVM = new EmployeeViewModel()
                        {
                            Address = employee.Address,
                            Department = employee.Department,
                            DepartmentId = employee.DepartmentId,
                            Name = employee.Name,
                            Id = employee.Id,
                            IsActive = employee.IsActive,
                            Phone = employee.Phone,
                            Salary = employee.Salary,
                            Age = employee.Age,
                            Email = employee.Email,
                        };*/
            #endregion

            var employeeVM = _mapper.Map<EmployeeViewModel>(employee);

            return View(viewName, employeeVM);
        }

    }
}

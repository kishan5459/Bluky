using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Utility;
using BulkyWeb.Areas.Admin.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BulkyWeb.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = SD.Role_Admin)]
public class ProductController : Controller
{
	private readonly IUnitOfWork _unitOfWork;

	private readonly IWebHostEnvironment _webHostEnvironment;

	public ProductController(IUnitOfWork unitOfWork, IWebHostEnvironment webHostEnvironment) {
		_unitOfWork = unitOfWork;
		_webHostEnvironment = webHostEnvironment;
	}

	public IActionResult Index() {
		IList<Product> productLIst = _unitOfWork.ProductRepo.GetAll(includeProperties: "Category").ToList();

		return View(productLIst);
	}

	// UPSERT - CREATE - EDIT
	public IActionResult Upsert(int? id) {
		IEnumerable<SelectListItem> CategoryList = _unitOfWork.CategoryRepo.GetAll()
			.Select(c => new SelectListItem {
				Text = c.Name,
				Value = c.Id.ToString()
			});

		// ViewBag.CategoryList = CategoryList;
		// ViewData["CategoryList"] = CategoryList;

		var productVM = new ProductVM() {
			Product = new Product(),
			CategoryList = CategoryList
		};

		// CREATE (Insert)
		if (id == null || id == 0) {
			return View(productVM);
		}
		// EDIT (Update)
		else {
			productVM.Product = _unitOfWork.ProductRepo.Get(p => p.Id == id)!;
			return View(productVM);
		}
	}

	[HttpPost]
	// public IActionResult Create(ProductVM productVM)
	public IActionResult Upsert(ProductVM productVM, IFormFile? file) {
		if (ModelState.IsValid) {
			string wwwRootPath = _webHostEnvironment.WebRootPath;
			if (file != null) {
				// random file-name
				string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
				string productPath = Path.Combine(wwwRootPath, @"img/product");

				// Delete old image
				string imgUrl = productVM.Product.ImageUrl;
				if (string.IsNullOrEmpty(imgUrl) == false) {
					string oldImagePath = Path.Combine(wwwRootPath, imgUrl.TrimStart('/'));

					if (System.IO.File.Exists(oldImagePath)) {
						System.IO.File.Delete(oldImagePath);
					}
				}

				using (var fileStream = new FileStream(Path.Combine(productPath, fileName), FileMode.Create)) {
					file.CopyTo(fileStream);
				}

				productVM.Product.ImageUrl = @"/img/product/" + fileName;
			}

			if (productVM.Product.Id == 0) {
				_unitOfWork.ProductRepo.Add(productVM.Product);
			}
			else {
				_unitOfWork.ProductRepo.Update(productVM.Product);
			}

			_unitOfWork.Save();

			TempData["succes"] = "Product created succesfully";

			return RedirectToAction(nameof(Index));
		}
		// If Modelstate.IsValid == False
		else {
			IEnumerable<SelectListItem> CategoryList = _unitOfWork.CategoryRepo.GetAll()
				.Select(c => new SelectListItem {
					Text = c.Name,
					Value = c.Id.ToString()
				});

			productVM.CategoryList = CategoryList;
		}

		return View(productVM);
	}

	#region
	[HttpGet]
	public IActionResult GetAll() {
		IList<Product> productLIst = _unitOfWork.ProductRepo.GetAll(includeProperties: "Category").ToList();
		return Json(new { data = productLIst });
	}

	[HttpDelete]
	public IActionResult Delete(int? id) {
		Product? product = _unitOfWork.ProductRepo.Get(c => c.Id == id);

		if (product == null) {
			return Json(new { success = false, message = "Error while deleting" });
		}

		// Delete old image
		string wwwRootPath = _webHostEnvironment.WebRootPath;
		string imgUrl = product.ImageUrl;
		if (string.IsNullOrEmpty(imgUrl) == false) {
			string oldImagePath = Path.Combine(wwwRootPath, imgUrl.TrimStart('/'));

			if (System.IO.File.Exists(oldImagePath)) {
				System.IO.File.Delete(oldImagePath);
			}
		}

		_unitOfWork.ProductRepo.Remove(product);
		_unitOfWork.Save();

		return Json(new { success = true, message = "Delete Succesful" });
	}
	#endregion
}
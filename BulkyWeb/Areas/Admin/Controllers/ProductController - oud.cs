using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using BulkyWeb.Areas.Admin.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Routing.Constraints;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.IO;

namespace BulkyWeb.Areas.Admin.Controllers_OUD;

[Area("AdminOUD")]
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

				string imgUrl = productVM.Product.ImageUrl;
				if (!string.IsNullOrEmpty(imgUrl)) {
					// Delete old image
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

	public IActionResult Delete(int? id) {
		if (id == null || id == 0) {
			return NotFound();
		}

		Product? product = _unitOfWork.ProductRepo.Get(p => p.Id == id);

		if (product == null) {
			return NotFound();
		}

		return View(product);
	}

	[HttpPost, ActionName(nameof(Delete))]
	public IActionResult DeletePOST(int? id) {
		Product? obj = _unitOfWork.ProductRepo.Get(c => c.Id == id);

		if (obj == null) {
			return NotFound();
		}

		_unitOfWork.ProductRepo.Remove(obj);
		_unitOfWork.Save();

		TempData["succes"] = "Category deleted succesfully";

		return RedirectToAction(nameof(Index));
	}

	#region
	[HttpGet]
	public IActionResult GetAll() {
		IList<Product> productLIst = _unitOfWork.ProductRepo.GetAll(includeProperties: "Category").ToList();
		return Json(new { data = productLIst });
	}
	#endregion
}
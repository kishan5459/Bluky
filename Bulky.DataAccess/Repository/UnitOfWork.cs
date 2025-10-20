using Bulky.DataAccess.Data;
using Bulky.DataAccess.Repository.IRepository;

namespace Bulky.DataAccess.Repository;


public class UnitOfWork : IUnitOfWork
{
	// Variables
	private readonly ApplicationDbContext _db;

	// Properties
	public ICategoryRepository CategoryRepo { get; private set; }
	public IProductRepository ProductRepo { get; private set; }

	// Constructor
	public UnitOfWork(ApplicationDbContext db) {
		_db = db;

		// CategoryRepository manueel injecteren met DI
		CategoryRepo = new CategoryRepository(_db);
		ProductRepo = new ProductRepository(_db);
	}
	
	// Methodes

	public void Save() {
		_db.SaveChanges();
	}
}
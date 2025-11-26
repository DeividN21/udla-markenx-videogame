using MyProject.Domain.Enums;

namespace MyProject.Domain.Models
{
  public class Product
  {
    private readonly ProductCategory _category;
    private readonly float _price;

    public Product(ProductCategory category, float price)
    {
      _category = category;
      _price = price;
    }

    public ProductCategory Category => _category;
    public float Price => _price;
  }
}
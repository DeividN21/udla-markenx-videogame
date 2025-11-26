using System.Linq;
using MyProject.Domain.Models;

namespace MyProject.Domain.Services
{
  public class ConsumerInterestService
  {
    public bool IsInterestedIn(Consumer consumer, Product product)
    {
      return consumer.Interests.Contains(product.Category);
    }
  }
}

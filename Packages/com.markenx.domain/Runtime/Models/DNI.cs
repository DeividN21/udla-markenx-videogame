namespace MyProject.Domain.Models
{
  /// <summary>
  /// Simple demographic document.
  /// In a real system you might parse real IDs, payrolls, forms, etc.
  /// </summary>
  public class DNI
  {
    public string FirstName { get; }
    public string LastName { get; }
    public int Age { get; }
    public float MonthlyIncome { get; }

    public DNI(string firstName, string lastName, int age, float income)
    {
      FirstName = firstName;
      LastName = lastName;
      Age = age;
      MonthlyIncome = income;
    }
  }
}

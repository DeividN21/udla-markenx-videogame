using System;
using System.Collections.Generic;
using MyProject.Domain.Enums;
using MyProject.Domain.Records;

namespace MyProject.Domain.Models
{
  public class Consumer
  {
    private readonly NationalIdentityDocument _nationalID;
    private readonly HashSet<ProductCategory> _interests;
    private readonly decimal _monthlyIncome;

    public Consumer(
      NationalIdentityDocument nationalID,
      IEnumerable<ProductCategory> interests,
      decimal monthlyIncome)
    {
      _nationalID = RequireNationalIdentityDocument(nationalID);
      _interests = new HashSet<ProductCategory>(interests);
      _monthlyIncome = monthlyIncome;
      Age = new PersonAge(_nationalID.BirthDate);
    }

    public string FirstName => _nationalID.FirstName;
    public string LastName => _nationalID.LastName;
    public PersonAge Age { get; }
    public IReadOnlyCollection<ProductCategory> Interests => _interests;
    public decimal MonthlyIncome => _monthlyIncome;

    private NationalIdentityDocument RequireNationalIdentityDocument(NationalIdentityDocument nationalID)
    {
      return nationalID ?? throw new ArgumentNullException(nameof(nationalID));
    }
  }
}

using System;

namespace MyProject.Domain.Records
{
    public record NationalIdentityDocument(
        string FirstName,
        string LastName,
        DateTime BirthDate
    );
}

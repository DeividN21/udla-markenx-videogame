using System;

namespace MyProject.Domain.Enums
{
    public record PersonAge(DateTime BirthDate)
    {
        public int Years
        {
            get
            {
                var today = DateTime.Today;
                int age = today.Year - BirthDate.Year;
                if (BirthDate.Date > today.AddYears(-age))
                    age--;
                return age;
            }
        }

        public AgeGroup Group =>
            Years switch
            {
                < 13 => AgeGroup.Kid,
                < 18 => AgeGroup.Teen,
                < 60 => AgeGroup.Adult,
                _ => AgeGroup.Senior
            };
    }
}
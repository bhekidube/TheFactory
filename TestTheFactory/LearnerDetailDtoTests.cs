using System.Text.Json;
using TheFactory.Contracts;

namespace TestTheFactory;

public class LearnerDetailDtoTests
{
    [Test]
    public void LearnerDetailDto_IncludesParentGuardianData()
    {
        var dto = new LearnerDetailDto
        {
            LearnerId = 7,
            FirstName = "Maya",
            Surname = "Dube",
            Grade = "Grade 2",
            ParentGuardian = new ParentGuardianDto
            {
                FirstName = "Nomusa",
                Surname = "Dube",
                PhoneNumber = "0771234567",
                EmailAddress = "nomusa@example.com",
                RelationshipToLearner = "Mother"
            }
        };

        var json = JsonSerializer.Serialize(dto);
        var roundTripped = JsonSerializer.Deserialize<LearnerDetailDto>(json);

        Assert.That(roundTripped, Is.Not.Null);
        Assert.That(roundTripped!.ParentGuardian.FirstName, Is.EqualTo("Nomusa"));
        Assert.That(roundTripped.ParentGuardian.PhoneNumber, Is.EqualTo("0771234567"));
    }
}

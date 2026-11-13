namespace TestTheFactory;

using TheFactory.Controllers;
using NUnit.Framework;

public class TripAnalyticsRequestValidationTests
{
    [Test]
    public void IsValidRange_AcceptsRecentRangeForPositiveOperatorAndUser()
    {
        var today = DateTime.UtcNow.Date;

        var isValid = TripAnalyticsRequestValidation.IsValidRange(7, 42, today.AddDays(-30), today);

        Assert.That(isValid, Is.True);
    }

    [TestCase(0, 42)]
    [TestCase(7, 0)]
    public void IsValidRange_RejectsMissingOperatorOrUser(int operatorId, int userId)
    {
        var today = DateTime.UtcNow.Date;

        var isValid = TripAnalyticsRequestValidation.IsValidRange(operatorId, userId, today.AddDays(-1), today);

        Assert.That(isValid, Is.False);
    }

    [Test]
    public void IsValidRange_RejectsReversedOrOverlongRanges()
    {
        var today = DateTime.UtcNow.Date;

        Assert.That(TripAnalyticsRequestValidation.IsValidRange(7, 42, today, today.AddDays(-1)), Is.False);
        Assert.That(TripAnalyticsRequestValidation.IsValidRange(7, 42, today.AddDays(-3661), today), Is.False);
    }
}
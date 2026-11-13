namespace TestTheFactory;

using NUnit.Framework;
using TheFactory.Services;

public class TripHistoryArchiveServiceTests
{
    [Test]
    public void GetNextRunTime_Before1755_Returns1755Today()
    {
        var localNow = new DateTime(2026, 11, 13, 17, 54, 59);

        var nextRun = TripHistoryArchiveService.GetNextRunTime(localNow);

        Assert.That(nextRun, Is.EqualTo(new DateTime(2026, 11, 13, 17, 55, 0)));
    }

    [Test]
    public void GetNextRunTime_At1755OrLater_Returns1755Tomorrow()
    {
        var localNow = new DateTime(2026, 11, 13, 17, 55, 0);

        var nextRun = TripHistoryArchiveService.GetNextRunTime(localNow);

        Assert.That(nextRun, Is.EqualTo(new DateTime(2026, 11, 14, 17, 55, 0)));
    }
}
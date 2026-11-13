namespace TestTheFactory;

using NUnit.Framework;

public class OperatorScopeValidationTests
{
    [Test]
    public void ValidateOperatorMutationScope_RejectsDifferentOperator()
    {
        var allowed = OperatorScopeValidator.ValidateOperatorMutationScope(7, 9);

        Assert.That(allowed, Is.False);
    }

    [Test]
    public void ValidateOperatorMutationScope_AllowsMatchingOperator()
    {
        var allowed = OperatorScopeValidator.ValidateOperatorMutationScope(9, 9);

        Assert.That(allowed, Is.True);
    }
}

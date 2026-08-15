using FlooringManager.Application.Estimates;
using FlooringManager.Domain.Estimates;

namespace FlooringManager.UnitTests.Estimates;

public sealed class EstimateTransitionsTests
{
    [Theory]
    [InlineData(EstimateStatus.Draft, true)]
    [InlineData(EstimateStatus.Sent, false)]
    [InlineData(EstimateStatus.Accepted, false)]
    [InlineData(EstimateStatus.Rejected, false)]
    [InlineData(EstimateStatus.Expired, false)]
    public void CanSend_OnlyFromDraft(EstimateStatus status, bool expected) =>
        Assert.Equal(expected, EstimateTransitions.CanSend(status));

    [Theory]
    [InlineData(EstimateStatus.Draft, false)]
    [InlineData(EstimateStatus.Sent, true)]
    [InlineData(EstimateStatus.Accepted, false)]
    [InlineData(EstimateStatus.Rejected, false)]
    [InlineData(EstimateStatus.Expired, false)]
    public void CanAccept_OnlyFromSent(EstimateStatus status, bool expected) =>
        Assert.Equal(expected, EstimateTransitions.CanAccept(status));
}

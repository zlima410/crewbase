using FlooringManager.Domain.Estimates;

namespace FlooringManager.Application.Estimates;

/// <summary>
/// Allowed estimate status changes. Acceptance is the one that also creates a job;
/// that side effect lives in <c>IEstimateAcceptanceService</c>, not here.
/// </summary>
public static class EstimateTransitions
{
    /// <summary>Draft is the only status that may move to Sent.</summary>
    public static bool CanSend(EstimateStatus status) => status == EstimateStatus.Draft;

    /// <summary>
    /// Sent is the only status that may move to Accepted and create a job.
    /// Already-accepted is a no-op handled by the acceptance service, not a transition.
    /// </summary>
    public static bool CanAccept(EstimateStatus status) => status == EstimateStatus.Sent;
}

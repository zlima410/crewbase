namespace FlooringManager.Domain.Jobs;

public enum JobStatus
{
    Scheduled = 1,
    InProgress = 2,
    Waiting = 3,
    Completed = 4,
    Cancelled = 5
}

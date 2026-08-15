using FlooringManager.Domain.Estimates;
using FlooringManager.Domain.Jobs;

namespace FlooringManager.UnitTests.Jobs;

public sealed class JobFromEstimateTests
{
    [Fact]
    public void FromAcceptedEstimate_CopiesScopeAndStartsScheduled()
    {
        var estimateId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var propertyId = Guid.NewGuid();
        var createdAt = DateTimeOffset.Parse("2026-08-13T12:00:00Z");

        var living = Room("Living Room", 300m, 330m, 0, InstallationMethod.NailDown, FinishType.OilBased, "site finished");
        var hall = Room("Hallway", 64m, 67.2m, 1, null, null, null);

        var estimate = new Estimate
        {
            Id = estimateId,
            CompanyId = companyId,
            CustomerId = customerId,
            PropertyId = propertyId,
            EstimateNumber = "EST-0001",
            Status = EstimateStatus.Sent,
            Notes = "sand in place",
            Rooms = { living, hall }
        };

        var job = Job.FromAcceptedEstimate(estimate, "JOB-0001", createdAt);

        Assert.NotEqual(Guid.Empty, job.Id);
        Assert.Equal(companyId, job.CompanyId);
        Assert.Equal(customerId, job.CustomerId);
        Assert.Equal(propertyId, job.PropertyId);
        Assert.Equal(estimateId, job.EstimateId);
        Assert.Equal("JOB-0001", job.JobNumber);
        Assert.Equal(JobStatus.Scheduled, job.Status);
        Assert.Equal("sand in place", job.Description);
        Assert.Null(job.InternalNotes);
        Assert.Null(job.CustomerNotes);
        Assert.Null(job.ScheduledStart);
        Assert.Equal(createdAt, job.CreatedAt);

        Assert.Equal(2, job.Rooms.Count);
        Assert.All(job.Rooms, room =>
        {
            Assert.Equal(job.Id, room.JobId);
            Assert.NotEqual(living.Id, room.Id);
            Assert.NotEqual(hall.Id, room.Id);
        });

        var copiedLiving = job.Rooms.Single(r => r.Name == "Living Room");
        Assert.Equal(300m, copiedLiving.SquareFeet);
        Assert.Equal(330m, copiedLiving.BillableSquareFeet);
        Assert.Equal(FlooringType.SolidHardwood, copiedLiving.FlooringType);
        Assert.Equal(WorkType.NewInstallation, copiedLiving.WorkType);
        Assert.Equal(InstallationMethod.NailDown, copiedLiving.InstallationMethod);
        Assert.Equal(FinishType.OilBased, copiedLiving.FinishType);
        Assert.Equal("site finished", copiedLiving.Notes);
        Assert.Equal(0, copiedLiving.Position);

        var copiedHall = job.Rooms.Single(r => r.Name == "Hallway");
        Assert.Equal(1, copiedHall.Position);
        Assert.Null(copiedHall.InstallationMethod);
        Assert.Null(copiedHall.FinishType);
        Assert.Null(copiedHall.Notes);

        Assert.Equal(EstimateStatus.Sent, estimate.Status);
    }

    private static EstimateRoom Room(
        string name,
        decimal squareFeet,
        decimal billable,
        int position,
        InstallationMethod? installation,
        FinishType? finish,
        string? notes) =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            SquareFeet = squareFeet,
            BillableSquareFeet = billable,
            FlooringType = FlooringType.SolidHardwood,
            WorkType = WorkType.NewInstallation,
            InstallationMethod = installation,
            FinishType = finish,
            Notes = notes,
            Position = position
        };
}

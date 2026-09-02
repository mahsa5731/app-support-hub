using System.Reflection;
using AppSupportHub.Domain.WorkItems;

namespace AppSupportHub.UnitTests.Domain.WorkItems;

public sealed class WorkItemHistoryTests
{
    private static readonly Guid _applicationSystemId = Guid.Parse(
        "e13f883e-2c9f-44be-b89f-b50ba5006cee");
    private static readonly DateTimeOffset _createdAt = new(
        2026,
        2,
        4,
        17,
        0,
        0,
        TimeSpan.Zero);

    [Fact]
    public void HistoryCollectionCannotBeMutatedByCaller()
    {
        WorkItem workItem = CreateWorkItem();
        WorkItemHistoryEntry existingEntry = Assert.Single(workItem.History);
        ICollection<WorkItemHistoryEntry> mutableView = Assert.IsAssignableFrom<
            ICollection<WorkItemHistoryEntry>>(workItem.History);

        Assert.Throws<NotSupportedException>(() => mutableView.Add(existingEntry));
        Assert.Throws<NotSupportedException>(() => mutableView.Remove(existingEntry));
        Assert.Single(workItem.History);
    }

    [Fact]
    public void HistoryEntryPropertiesHaveNoSetters()
    {
        PropertyInfo[] properties = typeof(WorkItemHistoryEntry).GetProperties();

        Assert.NotEmpty(properties);
        Assert.All(
            properties,
            property => Assert.Null(property.GetSetMethod(nonPublic: true)));
    }

    [Fact]
    public void CreatedHistoryHasIdentityAndNormalizedTimestamp()
    {
        DateTimeOffset localTimestamp = new(2026, 2, 4, 11, 0, 0, TimeSpan.FromHours(-6));
        var workItem = WorkItem.Create(
            _applicationSystemId,
            WorkItemType.Incident,
            "Title",
            "Description",
            WorkItemPriority.Medium,
            null,
            " Actor ",
            localTimestamp);

        WorkItemHistoryEntry entry = Assert.Single(workItem.History);
        Assert.NotEqual(Guid.Empty, entry.Id);
        Assert.Equal(workItem.Id, entry.WorkItemId);
        Assert.Equal("Actor", entry.ActorIdentifier);
        Assert.Equal(localTimestamp.ToUniversalTime(), entry.OccurredAtUtc);
        Assert.Equal(TimeSpan.Zero, entry.OccurredAtUtc.Offset);
    }

    [Fact]
    public void AssignmentHistoryUsesAssigneePreviousAndNewValues()
    {
        WorkItem workItem = CreateWorkItem();
        workItem.Assign("First", "Actor", _createdAt.AddMinutes(1));
        workItem.Assign(" Second ", " Actor ", _createdAt.AddMinutes(2));
        workItem.Unassign(" Actor ", _createdAt.AddMinutes(3));

        WorkItemHistoryEntry firstAssignment = workItem.History.ElementAt(1);
        WorkItemHistoryEntry reassignment = workItem.History.ElementAt(2);
        WorkItemHistoryEntry unassignment = workItem.History.ElementAt(3);

        Assert.Null(firstAssignment.PreviousValue);
        Assert.Equal("First", firstAssignment.NewValue);
        Assert.Equal("First", reassignment.PreviousValue);
        Assert.Equal("Second", reassignment.NewValue);
        Assert.Equal("Second", unassignment.PreviousValue);
        Assert.Null(unassignment.NewValue);
        Assert.All(workItem.History.Skip(1), entry => Assert.Equal("Actor", entry.ActorIdentifier));
    }

    [Fact]
    public void DueDateHistoryUsesUtcRoundTripValuesAndNullWhenCleared()
    {
        WorkItem workItem = CreateWorkItem();
        DateTimeOffset localDueAt = new(2026, 2, 5, 11, 0, 0, TimeSpan.FromHours(-6));
        DateTimeOffset expectedUtc = localDueAt.ToUniversalTime();

        workItem.ChangeDueDate(localDueAt, "Actor", _createdAt.AddMinutes(1));
        workItem.ChangeDueDate(null, "Actor", _createdAt.AddMinutes(2));

        WorkItemHistoryEntry setEntry = workItem.History.ElementAt(1);
        WorkItemHistoryEntry clearEntry = workItem.History.ElementAt(2);
        Assert.Null(setEntry.PreviousValue);
        Assert.Equal(expectedUtc.ToString("O"), setEntry.NewValue);
        Assert.Equal(expectedUtc.ToString("O"), clearEntry.PreviousValue);
        Assert.Null(clearEntry.NewValue);
    }

    [Fact]
    public void DetailsHistoryNeverCopiesDescriptionPayload()
    {
        WorkItem workItem = CreateWorkItem();
        const string updatedDescription = "A changed description that should not enter history";

        workItem.UpdateDetails(
            workItem.Title,
            updatedDescription,
            "Actor",
            _createdAt.AddMinutes(1));

        WorkItemHistoryEntry entry = workItem.History.Last();
        Assert.Null(entry.PreviousValue);
        Assert.Null(entry.NewValue);
        Assert.Equal("Description changed.", entry.Comment);
        Assert.DoesNotContain(updatedDescription, entry.Comment, StringComparison.Ordinal);
    }

    [Fact]
    public void ResolutionReopenAndCancellationUseSemanticComments()
    {
        WorkItem resolvedWorkItem = CreateWorkItem();
        MoveToInProgress(resolvedWorkItem);
        resolvedWorkItem.TransitionTo(
            WorkItemStatus.Resolved,
            "Actor",
            _createdAt.AddMinutes(3),
            resolutionSummary: " Resolution details ");
        resolvedWorkItem.TransitionTo(
            WorkItemStatus.InProgress,
            "Actor",
            _createdAt.AddMinutes(4),
            " Reopened because issue returned ");

        WorkItem cancelledWorkItem = CreateWorkItem();
        cancelledWorkItem.TransitionTo(
            WorkItemStatus.Cancelled,
            "Actor",
            _createdAt.AddMinutes(1),
            " Duplicate request ");

        WorkItemHistoryEntry resolution = resolvedWorkItem.History
            .Single(entry => entry.EventType == WorkItemHistoryEventType.ResolutionRecorded);
        WorkItemHistoryEntry reopen = resolvedWorkItem.History
            .Single(entry => entry.EventType == WorkItemHistoryEventType.Reopened);
        WorkItemHistoryEntry cancellation = cancelledWorkItem.History
            .Single(entry => entry.EventType == WorkItemHistoryEventType.Cancelled);

        Assert.Equal("Resolution details", resolution.Comment);
        Assert.Equal("Reopened because issue returned", reopen.Comment);
        Assert.Equal("Duplicate request", cancellation.Comment);
        Assert.Null(resolution.PreviousValue);
        Assert.Null(resolution.NewValue);
    }

    [Fact]
    public void EveryMutationHistoryTimestampIsNormalizedToUtc()
    {
        WorkItem workItem = CreateWorkItem();
        DateTimeOffset localTimestamp = new(2026, 2, 4, 12, 0, 0, TimeSpan.FromHours(-6));

        workItem.Assign("Analyst", "Actor", localTimestamp);

        WorkItemHistoryEntry entry = workItem.History.Last();
        Assert.Equal(localTimestamp.ToUniversalTime(), entry.OccurredAtUtc);
        Assert.Equal(TimeSpan.Zero, entry.OccurredAtUtc.Offset);
        Assert.Equal(localTimestamp.ToUniversalTime(), workItem.UpdatedAtUtc);
    }

    private static WorkItem CreateWorkItem()
    {
        return WorkItem.Create(
            _applicationSystemId,
            WorkItemType.Incident,
            "Title",
            "Description",
            WorkItemPriority.Medium,
            null,
            "Creator",
            _createdAt);
    }

    private static void MoveToInProgress(WorkItem workItem)
    {
        workItem.TransitionTo(
            WorkItemStatus.UnderAnalysis,
            "Actor",
            _createdAt.AddMinutes(1));
        workItem.TransitionTo(
            WorkItemStatus.InProgress,
            "Actor",
            _createdAt.AddMinutes(2));
    }
}

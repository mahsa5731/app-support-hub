using AppSupportHub.Domain.WorkItems;

namespace AppSupportHub.UnitTests.Domain.WorkItems;

public sealed class WorkItemTransitionTests
{
    private static readonly Guid _applicationSystemId = Guid.Parse(
        "24f47f59-dfd0-40cb-b86a-9430ac6ec7d7");
    private static readonly DateTimeOffset _createdAt = new(
        2026,
        2,
        3,
        16,
        0,
        0,
        TimeSpan.Zero);

    public static TheoryData<WorkItemType, WorkItemStatus, WorkItemStatus>
        AllowedTransitions => new()
        {
            { WorkItemType.Incident, WorkItemStatus.New, WorkItemStatus.UnderAnalysis },
            { WorkItemType.Incident, WorkItemStatus.New, WorkItemStatus.Cancelled },
            { WorkItemType.Incident, WorkItemStatus.UnderAnalysis, WorkItemStatus.InProgress },
            { WorkItemType.Incident, WorkItemStatus.UnderAnalysis, WorkItemStatus.Blocked },
            { WorkItemType.Incident, WorkItemStatus.UnderAnalysis, WorkItemStatus.Cancelled },
            { WorkItemType.Incident, WorkItemStatus.InProgress, WorkItemStatus.Blocked },
            { WorkItemType.Incident, WorkItemStatus.InProgress, WorkItemStatus.Testing },
            { WorkItemType.Incident, WorkItemStatus.InProgress, WorkItemStatus.Resolved },
            { WorkItemType.Enhancement, WorkItemStatus.InProgress, WorkItemStatus.Blocked },
            { WorkItemType.Enhancement, WorkItemStatus.InProgress, WorkItemStatus.Testing },
            { WorkItemType.ChangeRequest, WorkItemStatus.InProgress, WorkItemStatus.Blocked },
            { WorkItemType.ChangeRequest, WorkItemStatus.InProgress, WorkItemStatus.Testing },
            { WorkItemType.Incident, WorkItemStatus.Blocked, WorkItemStatus.UnderAnalysis },
            { WorkItemType.Incident, WorkItemStatus.Blocked, WorkItemStatus.InProgress },
            { WorkItemType.Incident, WorkItemStatus.Blocked, WorkItemStatus.Cancelled },
            { WorkItemType.Incident, WorkItemStatus.Testing, WorkItemStatus.InProgress },
            { WorkItemType.Incident, WorkItemStatus.Testing, WorkItemStatus.Resolved },
            { WorkItemType.Enhancement, WorkItemStatus.Testing, WorkItemStatus.InProgress },
            { WorkItemType.Enhancement, WorkItemStatus.Testing, WorkItemStatus.Resolved },
            { WorkItemType.ChangeRequest, WorkItemStatus.Testing, WorkItemStatus.InProgress },
            { WorkItemType.ChangeRequest, WorkItemStatus.Testing, WorkItemStatus.Resolved },
            { WorkItemType.Incident, WorkItemStatus.Resolved, WorkItemStatus.InProgress },
            { WorkItemType.Incident, WorkItemStatus.Resolved, WorkItemStatus.Closed },
            { WorkItemType.Enhancement, WorkItemStatus.Resolved, WorkItemStatus.InProgress },
            { WorkItemType.Enhancement, WorkItemStatus.Resolved, WorkItemStatus.Closed },
            { WorkItemType.ChangeRequest, WorkItemStatus.Resolved, WorkItemStatus.InProgress },
            { WorkItemType.ChangeRequest, WorkItemStatus.Resolved, WorkItemStatus.Closed },
        };

    public static TheoryData<WorkItemType, WorkItemStatus, WorkItemStatus>
        ForbiddenTransitions => new()
        {
            { WorkItemType.Incident, WorkItemStatus.New, WorkItemStatus.InProgress },
            { WorkItemType.Incident, WorkItemStatus.UnderAnalysis, WorkItemStatus.Testing },
            { WorkItemType.Incident, WorkItemStatus.InProgress, WorkItemStatus.Closed },
            { WorkItemType.Enhancement, WorkItemStatus.InProgress, WorkItemStatus.Resolved },
            { WorkItemType.ChangeRequest, WorkItemStatus.InProgress, WorkItemStatus.Resolved },
            { WorkItemType.Incident, WorkItemStatus.Blocked, WorkItemStatus.Testing },
            { WorkItemType.Incident, WorkItemStatus.Testing, WorkItemStatus.Closed },
            { WorkItemType.Incident, WorkItemStatus.Resolved, WorkItemStatus.Testing },
            { WorkItemType.Incident, WorkItemStatus.Closed, WorkItemStatus.InProgress },
            { WorkItemType.Incident, WorkItemStatus.Cancelled, WorkItemStatus.New },
        };

    [Theory]
    [MemberData(nameof(AllowedTransitions))]
    public void TransitionAllowsEveryEntryInExactMatrix(
        WorkItemType type,
        WorkItemStatus currentStatus,
        WorkItemStatus targetStatus)
    {
        WorkItem workItem = CreateAtStatus(type, currentStatus);
        int originalHistoryCount = workItem.History.Count;
        DateTimeOffset transitionedAt = _createdAt.AddHours(10);

        Assert.True(workItem.CanTransitionTo(targetStatus));

        bool changed = workItem.TransitionTo(
            targetStatus,
            " Actor ",
            transitionedAt,
            targetStatus == WorkItemStatus.Cancelled ? " Cancelled for test " : " Transition note ",
            targetStatus == WorkItemStatus.Resolved ? " Resolved for test " : null);

        bool semanticEntryExpected = targetStatus is WorkItemStatus.Resolved or WorkItemStatus.Cancelled
            || currentStatus == WorkItemStatus.Resolved && targetStatus == WorkItemStatus.InProgress;
        int expectedEntryCount = semanticEntryExpected ? 2 : 1;

        Assert.True(changed);
        Assert.Equal(targetStatus, workItem.Status);
        Assert.Equal(transitionedAt, workItem.UpdatedAtUtc);
        Assert.Equal(originalHistoryCount + expectedEntryCount, workItem.History.Count);
        WorkItemHistoryEntry statusEntry = workItem.History.ElementAt(originalHistoryCount);
        Assert.Equal(WorkItemHistoryEventType.StatusChanged, statusEntry.EventType);
        Assert.Equal(currentStatus.ToString(), statusEntry.PreviousValue);
        Assert.Equal(targetStatus.ToString(), statusEntry.NewValue);
    }

    [Theory]
    [MemberData(nameof(ForbiddenTransitions))]
    public void TransitionRejectsRepresentativeForbiddenMoveFromEveryStatus(
        WorkItemType type,
        WorkItemStatus currentStatus,
        WorkItemStatus targetStatus)
    {
        WorkItem workItem = CreateAtStatus(type, currentStatus);
        int originalHistoryCount = workItem.History.Count;
        DateTimeOffset originalUpdatedAt = workItem.UpdatedAtUtc;
        string? originalResolution = workItem.ResolutionSummary;
        DateTimeOffset? originalResolvedAt = workItem.ResolvedAtUtc;

        Assert.False(workItem.CanTransitionTo(targetStatus));
        Assert.Throws<InvalidOperationException>(() => workItem.TransitionTo(
            targetStatus,
            "Actor",
            _createdAt.AddHours(10),
            resolutionSummary: targetStatus == WorkItemStatus.Resolved ? "Summary" : null));
        Assert.Equal(currentStatus, workItem.Status);
        Assert.Equal(originalUpdatedAt, workItem.UpdatedAtUtc);
        Assert.Equal(originalResolution, workItem.ResolutionSummary);
        Assert.Equal(originalResolvedAt, workItem.ResolvedAtUtc);
        Assert.Equal(originalHistoryCount, workItem.History.Count);
    }

    [Fact]
    public void IncidentCanResolveDirectlyFromInProgress()
    {
        WorkItem workItem = CreateAtStatus(WorkItemType.Incident, WorkItemStatus.InProgress);
        DateTimeOffset resolvedAt = _createdAt.AddHours(1);

        bool changed = workItem.TransitionTo(
            WorkItemStatus.Resolved,
            "Resolver",
            resolvedAt,
            resolutionSummary: " Service restored ");

        Assert.True(changed);
        Assert.Equal(WorkItemStatus.Resolved, workItem.Status);
        Assert.Equal("Service restored", workItem.ResolutionSummary);
        Assert.Equal(resolvedAt, workItem.ResolvedAtUtc);
        WorkItemHistoryEntry[] finalEntries = workItem.History.TakeLast(2).ToArray();
        Assert.Equal(WorkItemHistoryEventType.StatusChanged, finalEntries[0].EventType);
        Assert.Equal(WorkItemHistoryEventType.ResolutionRecorded, finalEntries[1].EventType);
        Assert.Equal("Service restored", finalEntries[1].Comment);
    }

    [Theory]
    [InlineData(WorkItemType.Enhancement)]
    [InlineData(WorkItemType.ChangeRequest)]
    public void NonIncidentCannotResolveDirectlyFromInProgress(WorkItemType type)
    {
        WorkItem workItem = CreateAtStatus(type, WorkItemStatus.InProgress);

        Assert.Throws<InvalidOperationException>(() => workItem.TransitionTo(
            WorkItemStatus.Resolved,
            "Resolver",
            _createdAt.AddHours(1),
            resolutionSummary: "Summary"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidResolutionTransitionRequiresSummary(string? resolutionSummary)
    {
        WorkItem workItem = CreateAtStatus(WorkItemType.Enhancement, WorkItemStatus.Testing);

        Assert.Throws<ArgumentException>(() => workItem.TransitionTo(
            WorkItemStatus.Resolved,
            "Resolver",
            _createdAt.AddHours(1),
            resolutionSummary: resolutionSummary));
    }

    [Fact]
    public void ResolutionRejectsSummaryBeyondMaximum()
    {
        WorkItem workItem = CreateAtStatus(WorkItemType.Incident, WorkItemStatus.Testing);
        string oversizedSummary = new('x', WorkItem.ResolutionSummaryMaxLength + 1);

        Assert.Throws<ArgumentException>(() => workItem.TransitionTo(
            WorkItemStatus.Resolved,
            "Resolver",
            _createdAt.AddHours(1),
            resolutionSummary: oversizedSummary));
    }

    [Theory]
    [InlineData(WorkItemType.Incident)]
    [InlineData(WorkItemType.Enhancement)]
    [InlineData(WorkItemType.ChangeRequest)]
    public void TestingCanResolveEveryWorkItemType(WorkItemType type)
    {
        WorkItem workItem = CreateAtStatus(type, WorkItemStatus.Testing);

        workItem.TransitionTo(
            WorkItemStatus.Resolved,
            "Resolver",
            _createdAt.AddHours(1),
            resolutionSummary: "Verified resolution");

        Assert.Equal(WorkItemStatus.Resolved, workItem.Status);
        Assert.Equal("Verified resolution", workItem.ResolutionSummary);
        Assert.NotNull(workItem.ResolvedAtUtc);
    }

    [Fact]
    public void ReopenClearsCurrentResolutionWithoutChangingEarlierHistory()
    {
        WorkItem workItem = CreateAtStatus(WorkItemType.Incident, WorkItemStatus.Resolved);
        WorkItemHistoryEntry resolutionEntry = workItem.History.Last();
        int originalHistoryCount = workItem.History.Count;

        bool changed = workItem.TransitionTo(
            WorkItemStatus.InProgress,
            "Analyst",
            _createdAt.AddHours(2),
            "Issue recurred");

        Assert.True(changed);
        Assert.Null(workItem.ResolutionSummary);
        Assert.Null(workItem.ResolvedAtUtc);
        Assert.Equal(originalHistoryCount + 2, workItem.History.Count);
        Assert.Same(resolutionEntry, workItem.History.ElementAt(originalHistoryCount - 1));
        WorkItemHistoryEntry[] finalEntries = workItem.History.TakeLast(2).ToArray();
        Assert.Equal(WorkItemHistoryEventType.StatusChanged, finalEntries[0].EventType);
        Assert.Equal(WorkItemHistoryEventType.Reopened, finalEntries[1].EventType);
        Assert.Equal("Issue recurred", finalEntries[1].Comment);
    }

    [Fact]
    public void ClosingRetainsResolutionFields()
    {
        WorkItem workItem = CreateAtStatus(WorkItemType.Incident, WorkItemStatus.Resolved);
        string? resolution = workItem.ResolutionSummary;
        DateTimeOffset? resolvedAt = workItem.ResolvedAtUtc;

        workItem.TransitionTo(
            WorkItemStatus.Closed,
            "Closer",
            _createdAt.AddHours(2));

        Assert.Equal(resolution, workItem.ResolutionSummary);
        Assert.Equal(resolvedAt, workItem.ResolvedAtUtc);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CancellationRequiresComment(string? comment)
    {
        WorkItem workItem = CreateAtStatus(WorkItemType.Incident, WorkItemStatus.New);

        Assert.Throws<ArgumentException>(() => workItem.TransitionTo(
            WorkItemStatus.Cancelled,
            "Actor",
            _createdAt.AddMinutes(1),
            comment));
    }

    [Fact]
    public void CancellationRejectsCommentBeyondMaximum()
    {
        WorkItem workItem = CreateAtStatus(WorkItemType.Incident, WorkItemStatus.New);
        string oversizedComment = new('x', WorkItem.HistoryCommentMaxLength + 1);

        Assert.Throws<ArgumentException>(() => workItem.TransitionTo(
            WorkItemStatus.Cancelled,
            "Actor",
            _createdAt.AddMinutes(1),
            oversizedComment));
    }

    [Fact]
    public void CancellationAppendsStatusThenSemanticHistory()
    {
        WorkItem workItem = CreateAtStatus(WorkItemType.Incident, WorkItemStatus.New);

        workItem.TransitionTo(
            WorkItemStatus.Cancelled,
            "Actor",
            _createdAt.AddMinutes(1),
            " Duplicate request ");

        WorkItemHistoryEntry[] finalEntries = workItem.History.TakeLast(2).ToArray();
        Assert.Equal(WorkItemHistoryEventType.StatusChanged, finalEntries[0].EventType);
        Assert.Null(finalEntries[0].Comment);
        Assert.Equal(WorkItemHistoryEventType.Cancelled, finalEntries[1].EventType);
        Assert.Equal("Duplicate request", finalEntries[1].Comment);
    }

    [Theory]
    [InlineData(WorkItemStatus.Closed)]
    [InlineData(WorkItemStatus.Cancelled)]
    public void TerminalStatusesRejectEveryChangedTransition(WorkItemStatus status)
    {
        WorkItem workItem = CreateAtStatus(WorkItemType.Incident, status);

        foreach (WorkItemStatus target in Enum.GetValues<WorkItemStatus>().Where(value => value != status))
        {
            Assert.False(workItem.CanTransitionTo(target));
            Assert.Throws<InvalidOperationException>(() => workItem.TransitionTo(
                target,
                "Actor",
                _createdAt.AddHours(10),
                target == WorkItemStatus.Cancelled ? "Reason" : null,
                target == WorkItemStatus.Resolved ? "Summary" : null));
        }
    }

    [Fact]
    public void SameStatusTransitionIsTrueNoOp()
    {
        WorkItem workItem = CreateAtStatus(WorkItemType.Incident, WorkItemStatus.UnderAnalysis);
        DateTimeOffset originalUpdatedAt = workItem.UpdatedAtUtc;
        int originalHistoryCount = workItem.History.Count;

        Assert.True(workItem.CanTransitionTo(WorkItemStatus.UnderAnalysis));
        bool changed = workItem.TransitionTo(
            WorkItemStatus.UnderAnalysis,
            "Actor",
            _createdAt.AddHours(10));

        Assert.False(changed);
        Assert.Equal(originalUpdatedAt, workItem.UpdatedAtUtc);
        Assert.Equal(originalHistoryCount, workItem.History.Count);
    }

    private static WorkItem CreateAtStatus(
        WorkItemType type,
        WorkItemStatus status)
    {
        var workItem = WorkItem.Create(
            _applicationSystemId,
            type,
            "Work item",
            "Description",
            WorkItemPriority.Medium,
            null,
            "Creator",
            _createdAt);

        switch (status)
        {
            case WorkItemStatus.New:
                return workItem;
            case WorkItemStatus.Cancelled:
                workItem.TransitionTo(
                    WorkItemStatus.Cancelled,
                    "Actor",
                    _createdAt.AddMinutes(1),
                    "Cancelled for setup");
                return workItem;
        }

        workItem.TransitionTo(
            WorkItemStatus.UnderAnalysis,
            "Actor",
            _createdAt.AddMinutes(1));

        if (status == WorkItemStatus.UnderAnalysis)
        {
            return workItem;
        }

        if (status == WorkItemStatus.Blocked)
        {
            workItem.TransitionTo(
                WorkItemStatus.Blocked,
                "Actor",
                _createdAt.AddMinutes(2));
            return workItem;
        }

        workItem.TransitionTo(
            WorkItemStatus.InProgress,
            "Actor",
            _createdAt.AddMinutes(2));

        if (status == WorkItemStatus.InProgress)
        {
            return workItem;
        }

        workItem.TransitionTo(
            WorkItemStatus.Testing,
            "Actor",
            _createdAt.AddMinutes(3));

        if (status == WorkItemStatus.Testing)
        {
            return workItem;
        }

        workItem.TransitionTo(
            WorkItemStatus.Resolved,
            "Actor",
            _createdAt.AddMinutes(4),
            resolutionSummary: "Resolved for setup");

        if (status == WorkItemStatus.Closed)
        {
            workItem.TransitionTo(
                WorkItemStatus.Closed,
                "Actor",
                _createdAt.AddMinutes(5));
        }

        return workItem;
    }
}

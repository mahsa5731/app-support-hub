using AppSupportHub.Domain.WorkItems;

namespace AppSupportHub.UnitTests.Domain.WorkItems;

public sealed class WorkItemTests
{
    private static readonly Guid _applicationSystemId = Guid.Parse(
        "4f6c77ef-4294-40d9-a1c2-181ffd675b4d");
    private static readonly DateTimeOffset _createdAt = new(
        2026,
        2,
        2,
        15,
        0,
        0,
        TimeSpan.Zero);

    [Theory]
    [InlineData(WorkItemType.Incident)]
    [InlineData(WorkItemType.Enhancement)]
    [InlineData(WorkItemType.ChangeRequest)]
    public void CreateBuildsValidWorkItemForEachType(WorkItemType type)
    {
        WorkItem workItem = CreateWorkItem(type: type);

        Assert.NotEqual(Guid.Empty, workItem.Id);
        Assert.Equal(_applicationSystemId, workItem.ApplicationSystemId);
        Assert.Equal(type, workItem.Type);
        Assert.Equal(WorkItemStatus.New, workItem.Status);
        Assert.Null(workItem.AssigneeIdentifier);
        Assert.Null(workItem.ResolutionSummary);
        Assert.Null(workItem.ResolvedAtUtc);
        Assert.Equal(workItem.CreatedAtUtc, workItem.UpdatedAtUtc);
    }

    [Fact]
    public void CreateRejectsEmptyApplicationSystemId()
    {
        Assert.Throws<ArgumentException>(() => WorkItem.Create(
            Guid.Empty,
            WorkItemType.Incident,
            "Title",
            "Description",
            WorkItemPriority.Medium,
            null,
            "actor",
            _createdAt));
    }

    [Theory]
    [InlineData("title")]
    [InlineData("description")]
    [InlineData("actor")]
    public void CreateRejectsWhitespaceRequiredText(string parameter)
    {
        Assert.Throws<ArgumentException>(() => CreateWithValue(parameter, "   "));
    }

    [Theory]
    [InlineData("title", WorkItem.TitleMaxLength)]
    [InlineData("description", WorkItem.DescriptionMaxLength)]
    [InlineData("actor", WorkItem.ActorIdentifierMaxLength)]
    public void CreateRejectsTextBeyondEachCreationMaximum(string parameter, int maximumLength)
    {
        string oversizedValue = new('x', maximumLength + 1);

        Assert.Throws<ArgumentException>(() => CreateWithValue(parameter, oversizedValue));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void CreateRejectsDueDateAtOrBeforeCreation(int minuteOffset)
    {
        Assert.Throws<ArgumentException>(() => CreateWorkItem(
            dueAt: _createdAt.AddMinutes(minuteOffset)));
    }

    [Fact]
    public void CreateTrimsTextAndNormalizesInstantsToUtc()
    {
        DateTimeOffset localCreatedAt = new(2026, 2, 2, 9, 0, 0, TimeSpan.FromHours(-6));
        DateTimeOffset localDueAt = localCreatedAt.AddHours(4);

        var workItem = WorkItem.Create(
            _applicationSystemId,
            WorkItemType.Incident,
            " Title ",
            " Description ",
            WorkItemPriority.High,
            localDueAt,
            " Actor ",
            localCreatedAt);

        Assert.Equal("Title", workItem.Title);
        Assert.Equal("Description", workItem.Description);
        Assert.Equal(localCreatedAt.ToUniversalTime(), workItem.CreatedAtUtc);
        Assert.Equal(localDueAt.ToUniversalTime(), workItem.DueAtUtc);
        Assert.Equal(TimeSpan.Zero, workItem.CreatedAtUtc.Offset);
        Assert.Equal("Actor", Assert.Single(workItem.History).ActorIdentifier);
    }

    [Fact]
    public void CreateAppendsExactlyOneCreatedHistoryEntry()
    {
        WorkItem workItem = CreateWorkItem();

        WorkItemHistoryEntry entry = Assert.Single(workItem.History);
        Assert.Equal(WorkItemHistoryEventType.Created, entry.EventType);
        Assert.Equal(workItem.Id, entry.WorkItemId);
        Assert.Null(entry.PreviousValue);
        Assert.Null(entry.NewValue);
        Assert.Null(entry.Comment);
    }

    [Fact]
    public void AssignReassignAndUnassignTrackChanges()
    {
        WorkItem workItem = CreateWorkItem();

        bool assigned = workItem.Assign(" First ", " Actor ", _createdAt.AddMinutes(1));
        bool reassigned = workItem.Assign("Second", "Actor", _createdAt.AddMinutes(2));
        bool unassigned = workItem.Unassign("Actor", _createdAt.AddMinutes(3));

        Assert.True(assigned);
        Assert.True(reassigned);
        Assert.True(unassigned);
        Assert.Null(workItem.AssigneeIdentifier);
        Assert.Equal(4, workItem.History.Count);
        Assert.Equal(WorkItemHistoryEventType.Assigned, workItem.History.ElementAt(1).EventType);
        Assert.Equal(WorkItemHistoryEventType.Assigned, workItem.History.ElementAt(2).EventType);
        Assert.Equal(WorkItemHistoryEventType.Unassigned, workItem.History.ElementAt(3).EventType);
    }

    [Fact]
    public void AssigningSameNormalizedAssigneeIsTrueNoOp()
    {
        WorkItem workItem = CreateWorkItem();
        workItem.Assign("Analyst", "Actor", _createdAt.AddMinutes(1));
        DateTimeOffset originalUpdatedAt = workItem.UpdatedAtUtc;
        int originalHistoryCount = workItem.History.Count;

        bool changed = workItem.Assign(" Analyst ", "Other actor", _createdAt.AddMinutes(2));

        Assert.False(changed);
        Assert.Equal(originalUpdatedAt, workItem.UpdatedAtUtc);
        Assert.Equal(originalHistoryCount, workItem.History.Count);
    }

    [Fact]
    public void UnassigningAlreadyUnassignedWorkItemIsTrueNoOp()
    {
        WorkItem workItem = CreateWorkItem();
        DateTimeOffset originalUpdatedAt = workItem.UpdatedAtUtc;

        bool changed = workItem.Unassign("Actor", _createdAt.AddMinutes(1));

        Assert.False(changed);
        Assert.Equal(originalUpdatedAt, workItem.UpdatedAtUtc);
        Assert.Single(workItem.History);
    }

    [Theory]
    [InlineData(WorkItemStatus.Closed)]
    [InlineData(WorkItemStatus.Cancelled)]
    public void AssignmentIsRejectedInTerminalState(WorkItemStatus terminalStatus)
    {
        WorkItem workItem = CreateAtStatus(terminalStatus);
        int historyCount = workItem.History.Count;

        Assert.Throws<InvalidOperationException>(() => workItem.Assign(
            "Analyst",
            "Actor",
            _createdAt.AddHours(10)));
        Assert.Equal(historyCount, workItem.History.Count);
        Assert.Null(workItem.AssigneeIdentifier);
    }

    [Fact]
    public void AssignmentRejectsIdentifiersBeyondMaximum()
    {
        WorkItem workItem = CreateWorkItem();
        string oversizedAssignee = new('x', WorkItem.AssigneeIdentifierMaxLength + 1);
        string oversizedActor = new('x', WorkItem.ActorIdentifierMaxLength + 1);

        Assert.Throws<ArgumentException>(() => workItem.Assign(
            oversizedAssignee,
            "Actor",
            _createdAt.AddMinutes(1)));
        Assert.Throws<ArgumentException>(() => workItem.Assign(
            "Analyst",
            oversizedActor,
            _createdAt.AddMinutes(1)));
    }

    [Theory]
    [InlineData(WorkItemStatus.Closed)]
    [InlineData(WorkItemStatus.Cancelled)]
    public void TerminalStateRejectsUnassignmentPriorityAndDueDateChanges(
        WorkItemStatus terminalStatus)
    {
        WorkItem workItem = CreateWorkItem();
        workItem.Assign("Analyst", "Actor", _createdAt.AddSeconds(1));
        MoveToTerminalStatus(workItem, terminalStatus);
        int historyCount = workItem.History.Count;

        Assert.Throws<InvalidOperationException>(() => workItem.Unassign(
            "Actor",
            _createdAt.AddHours(10)));
        Assert.Throws<InvalidOperationException>(() => workItem.ChangePriority(
            WorkItemPriority.Critical,
            "Actor",
            _createdAt.AddHours(10)));
        Assert.Throws<InvalidOperationException>(() => workItem.ChangeDueDate(
            _createdAt.AddDays(2),
            "Actor",
            _createdAt.AddHours(10)));
        Assert.Equal(historyCount, workItem.History.Count);
        Assert.Equal("Analyst", workItem.AssigneeIdentifier);
    }

    [Fact]
    public void ChangePriorityRecordsPreviousAndNewValues()
    {
        WorkItem workItem = CreateWorkItem();

        bool changed = workItem.ChangePriority(
            WorkItemPriority.Critical,
            "Actor",
            _createdAt.AddMinutes(1));

        Assert.True(changed);
        Assert.Equal(WorkItemPriority.Critical, workItem.Priority);
        WorkItemHistoryEntry entry = workItem.History.Last();
        Assert.Equal(WorkItemHistoryEventType.PriorityChanged, entry.EventType);
        Assert.Equal(nameof(WorkItemPriority.Medium), entry.PreviousValue);
        Assert.Equal(nameof(WorkItemPriority.Critical), entry.NewValue);
    }

    [Fact]
    public void SamePriorityIsTrueNoOp()
    {
        WorkItem workItem = CreateWorkItem();
        DateTimeOffset originalUpdatedAt = workItem.UpdatedAtUtc;

        bool changed = workItem.ChangePriority(
            WorkItemPriority.Medium,
            "Actor",
            _createdAt.AddMinutes(1));

        Assert.False(changed);
        Assert.Equal(originalUpdatedAt, workItem.UpdatedAtUtc);
        Assert.Single(workItem.History);
    }

    [Fact]
    public void ChangeDueDateCanSetReplaceAndClearDate()
    {
        WorkItem workItem = CreateWorkItem();
        DateTimeOffset firstDueAt = _createdAt.AddDays(1);
        DateTimeOffset secondDueAt = _createdAt.AddDays(2);

        bool set = workItem.ChangeDueDate(firstDueAt, "Actor", _createdAt.AddMinutes(1));
        bool replaced = workItem.ChangeDueDate(secondDueAt, "Actor", _createdAt.AddMinutes(2));
        bool cleared = workItem.ChangeDueDate(null, "Actor", _createdAt.AddMinutes(3));

        Assert.True(set);
        Assert.True(replaced);
        Assert.True(cleared);
        Assert.Null(workItem.DueAtUtc);
        Assert.Equal(4, workItem.History.Count);
        Assert.All(
            workItem.History.Skip(1),
            entry => Assert.Equal(WorkItemHistoryEventType.DueDateChanged, entry.EventType));
    }

    [Fact]
    public void ChangeDueDateRejectsInvalidDateWithoutMutation()
    {
        WorkItem workItem = CreateWorkItem();
        DateTimeOffset originalUpdatedAt = workItem.UpdatedAtUtc;

        Assert.Throws<ArgumentException>(() => workItem.ChangeDueDate(
            _createdAt,
            "Actor",
            _createdAt.AddMinutes(1)));
        Assert.Null(workItem.DueAtUtc);
        Assert.Equal(originalUpdatedAt, workItem.UpdatedAtUtc);
        Assert.Single(workItem.History);
    }

    [Fact]
    public void EquivalentNormalizedDueDateIsTrueNoOp()
    {
        DateTimeOffset dueAtUtc = _createdAt.AddDays(1);
        WorkItem workItem = CreateWorkItem(dueAt: dueAtUtc);
        DateTimeOffset equivalentLocalDueAt = dueAtUtc.ToOffset(TimeSpan.FromHours(-6));
        DateTimeOffset originalUpdatedAt = workItem.UpdatedAtUtc;

        bool changed = workItem.ChangeDueDate(
            equivalentLocalDueAt,
            "Actor",
            _createdAt.AddMinutes(1));

        Assert.False(changed);
        Assert.Equal(originalUpdatedAt, workItem.UpdatedAtUtc);
        Assert.Single(workItem.History);
    }

    [Fact]
    public void UpdateDetailsRecordsChangedFieldsWithoutDescriptionPayload()
    {
        WorkItem workItem = CreateWorkItem();

        bool changed = workItem.UpdateDetails(
            " Updated title ",
            " Updated description ",
            " Actor ",
            _createdAt.AddMinutes(1));

        Assert.True(changed);
        Assert.Equal("Updated title", workItem.Title);
        Assert.Equal("Updated description", workItem.Description);
        WorkItemHistoryEntry entry = workItem.History.Last();
        Assert.Equal(WorkItemHistoryEventType.DetailsUpdated, entry.EventType);
        Assert.Equal("Work item", entry.PreviousValue);
        Assert.Equal("Updated title", entry.NewValue);
        Assert.Equal("Title and description changed.", entry.Comment);
        Assert.DoesNotContain("Updated description", entry.Comment, StringComparison.Ordinal);
    }

    [Fact]
    public void UnchangedDetailsAreTrueNoOp()
    {
        WorkItem workItem = CreateWorkItem();
        DateTimeOffset originalUpdatedAt = workItem.UpdatedAtUtc;

        bool changed = workItem.UpdateDetails(
            " Work item ",
            " Description ",
            "Actor",
            _createdAt.AddMinutes(1));

        Assert.False(changed);
        Assert.Equal(originalUpdatedAt, workItem.UpdatedAtUtc);
        Assert.Single(workItem.History);
    }

    [Theory]
    [InlineData("title", "   ")]
    [InlineData("description", "   ")]
    public void UpdateDetailsRejectsWhitespaceRequiredText(string parameter, string value)
    {
        WorkItem workItem = CreateWorkItem();

        Assert.Throws<ArgumentException>(() => workItem.UpdateDetails(
            parameter == "title" ? value : workItem.Title,
            parameter == "description" ? value : workItem.Description,
            "Actor",
            _createdAt.AddMinutes(1)));
        Assert.Single(workItem.History);
    }

    [Theory]
    [InlineData("title", WorkItem.TitleMaxLength)]
    [InlineData("description", WorkItem.DescriptionMaxLength)]
    public void UpdateDetailsRejectsTextBeyondMaximum(string parameter, int maximumLength)
    {
        WorkItem workItem = CreateWorkItem();
        string oversizedValue = new('x', maximumLength + 1);

        Assert.Throws<ArgumentException>(() => workItem.UpdateDetails(
            parameter == "title" ? oversizedValue : workItem.Title,
            parameter == "description" ? oversizedValue : workItem.Description,
            "Actor",
            _createdAt.AddMinutes(1)));
        Assert.Single(workItem.History);
    }

    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, false)]
    [InlineData(1, true)]
    public void IsOverdueUsesStrictDueInstantBoundary(int secondOffset, bool expected)
    {
        DateTimeOffset dueAt = _createdAt.AddHours(1);
        WorkItem workItem = CreateWorkItem(dueAt: dueAt);

        bool isOverdue = workItem.IsOverdue(dueAt.AddSeconds(secondOffset));

        Assert.Equal(expected, isOverdue);
    }

    [Fact]
    public void WorkItemWithoutDueDateIsNotOverdue()
    {
        WorkItem workItem = CreateWorkItem();

        Assert.False(workItem.IsOverdue(_createdAt.AddYears(1)));
    }

    [Theory]
    [InlineData(WorkItemStatus.Resolved)]
    [InlineData(WorkItemStatus.Closed)]
    [InlineData(WorkItemStatus.Cancelled)]
    public void CompletedOrCancelledWorkItemIsNotOverdue(WorkItemStatus status)
    {
        WorkItem workItem = CreateAtStatus(status, _createdAt.AddMinutes(30));

        Assert.False(workItem.IsOverdue(_createdAt.AddHours(1)));
    }

    private static WorkItem CreateWorkItem(
        WorkItemType type = WorkItemType.Incident,
        DateTimeOffset? dueAt = null)
    {
        return WorkItem.Create(
            _applicationSystemId,
            type,
            "Work item",
            "Description",
            WorkItemPriority.Medium,
            dueAt,
            "Creator",
            _createdAt);
    }

    private static WorkItem CreateAtStatus(
        WorkItemStatus status,
        DateTimeOffset? dueAt = null)
    {
        WorkItem workItem = CreateWorkItem(dueAt: dueAt);

        if (status == WorkItemStatus.Cancelled)
        {
            workItem.TransitionTo(
                WorkItemStatus.Cancelled,
                "Actor",
                _createdAt.AddMinutes(1),
                "Cancelled for test");
            return workItem;
        }

        workItem.TransitionTo(
            WorkItemStatus.UnderAnalysis,
            "Actor",
            _createdAt.AddMinutes(1));
        workItem.TransitionTo(
            WorkItemStatus.InProgress,
            "Actor",
            _createdAt.AddMinutes(2));
        workItem.TransitionTo(
            WorkItemStatus.Testing,
            "Actor",
            _createdAt.AddMinutes(3));
        workItem.TransitionTo(
            WorkItemStatus.Resolved,
            "Actor",
            _createdAt.AddMinutes(4),
            resolutionSummary: "Resolved for test");

        if (status == WorkItemStatus.Closed)
        {
            workItem.TransitionTo(
                WorkItemStatus.Closed,
                "Actor",
                _createdAt.AddMinutes(5));
        }

        return workItem;
    }

    private static void MoveToTerminalStatus(WorkItem workItem, WorkItemStatus terminalStatus)
    {
        if (terminalStatus == WorkItemStatus.Cancelled)
        {
            workItem.TransitionTo(
                WorkItemStatus.Cancelled,
                "Actor",
                _createdAt.AddMinutes(1),
                "Cancelled for test");
            return;
        }

        workItem.TransitionTo(
            WorkItemStatus.UnderAnalysis,
            "Actor",
            _createdAt.AddMinutes(1));
        workItem.TransitionTo(
            WorkItemStatus.InProgress,
            "Actor",
            _createdAt.AddMinutes(2));
        workItem.TransitionTo(
            WorkItemStatus.Testing,
            "Actor",
            _createdAt.AddMinutes(3));
        workItem.TransitionTo(
            WorkItemStatus.Resolved,
            "Actor",
            _createdAt.AddMinutes(4),
            resolutionSummary: "Resolved for test");
        workItem.TransitionTo(
            WorkItemStatus.Closed,
            "Actor",
            _createdAt.AddMinutes(5));
    }

    private static WorkItem CreateWithValue(string parameter, string value)
    {
        return WorkItem.Create(
            _applicationSystemId,
            WorkItemType.Incident,
            parameter == "title" ? value : "Title",
            parameter == "description" ? value : "Description",
            WorkItemPriority.Medium,
            null,
            parameter == "actor" ? value : "Actor",
            _createdAt);
    }
}

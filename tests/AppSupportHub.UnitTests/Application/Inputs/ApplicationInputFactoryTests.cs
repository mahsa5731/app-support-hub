using AppSupportHub.Application.Abstractions.Results;
using AppSupportHub.Application.Systems.CreateApplicationSystem;
using AppSupportHub.Application.Systems.Inputs;
using AppSupportHub.Application.Systems.ListApplicationSystems;
using AppSupportHub.Application.WorkItems.CreateWorkItem;
using AppSupportHub.Application.WorkItems.Inputs;
using AppSupportHub.Application.WorkItems.TransitionWorkItemStatus;
using AppSupportHub.Domain.Systems;
using AppSupportHub.Domain.WorkItems;

namespace AppSupportHub.UnitTests.Application.Inputs;

public sealed class ApplicationInputFactoryTests
{
    [Fact]
    public void SystemFactoryParsesCaseInsensitiveCreateVocabulary()
    {
        var factory = new ApplicationSystemInputFactory();

        ApplicationResult<CreateApplicationSystemCommand> result = factory.CreateCreateCommand(
            "Example",
            "Description",
            "commercial",
            "CRITICAL",
            "active",
            "Business",
            "Technical",
            "Support",
            "Vendor");

        Assert.True(result.IsSuccess);
        Assert.Equal(ApplicationSystemType.Commercial, result.Value.Type);
        Assert.Equal(ApplicationCriticality.Critical, result.Value.Criticality);
        Assert.Equal(ApplicationLifecycleStatus.Active, result.Value.InitialLifecycleStatus);
    }

    [Theory]
    [InlineData("unknown", "High", "Active")]
    [InlineData("Commercial", "999", "Active")]
    [InlineData("Commercial", "High", "3")]
    public void SystemFactoryRejectsUnknownAndNumericCreateVocabulary(
        string type,
        string criticality,
        string lifecycle)
    {
        var factory = new ApplicationSystemInputFactory();

        ApplicationResult<CreateApplicationSystemCommand> result = factory.CreateCreateCommand(
            "Example",
            "Description",
            type,
            criticality,
            lifecycle,
            "Business",
            "Technical",
            "Support",
            "Vendor");

        Assert.False(result.IsSuccess);
        Assert.Equal("validation.invalid_input", result.Error!.Code);
    }

    [Fact]
    public void SystemFactoryAllowsBlankOptionalListChoices()
    {
        ApplicationResult<ListApplicationSystemsQuery> result =
            new ApplicationSystemInputFactory().CreateListQuery(
            "sample",
            null,
            " ",
            string.Empty,
            25);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.Type);
        Assert.Null(result.Value.Criticality);
        Assert.Null(result.Value.LifecycleStatus);
    }

    [Fact]
    public void WorkItemFactoryParsesCaseInsensitiveCreateVocabulary()
    {
        ApplicationResult<CreateWorkItemCommand> result =
            new WorkItemInputFactory().CreateCreateCommand(
            Guid.NewGuid(),
            "changeREQUEST",
            "Title",
            "Description",
            "high",
            null,
            "actor");

        Assert.True(result.IsSuccess);
        Assert.Equal(WorkItemType.ChangeRequest, result.Value.Type);
        Assert.Equal(WorkItemPriority.High, result.Value.Priority);
    }

    [Theory]
    [InlineData("1", "High")]
    [InlineData("Incident", "invalid")]
    public void WorkItemFactoryRejectsUnknownAndNumericCreateVocabulary(
        string type,
        string priority)
    {
        ApplicationResult<CreateWorkItemCommand> result =
            new WorkItemInputFactory().CreateCreateCommand(
            Guid.NewGuid(),
            type,
            "Title",
            "Description",
            priority,
            null,
            "actor");

        Assert.False(result.IsSuccess);
        Assert.Equal("validation.invalid_input", result.Error!.Code);
    }

    [Theory]
    [InlineData("underanalysis", WorkItemStatus.UnderAnalysis)]
    [InlineData("INPROGRESS", WorkItemStatus.InProgress)]
    [InlineData("Resolved", WorkItemStatus.Resolved)]
    public void WorkItemFactoryParsesTransitionVocabulary(
        string value,
        WorkItemStatus expected)
    {
        ApplicationResult<TransitionWorkItemStatusCommand> result =
            new WorkItemInputFactory().CreateTransitionCommand(
            Guid.NewGuid(),
            value,
            "actor",
            null,
            null);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value.TargetStatus);
    }

    [Fact]
    public void WorkItemFactoryRejectsNumericTransitionVocabulary()
    {
        ApplicationResult<TransitionWorkItemStatusCommand> result =
            new WorkItemInputFactory().CreateTransitionCommand(
            Guid.NewGuid(),
            "2",
            "actor",
            null,
            null);

        Assert.False(result.IsSuccess);
        Assert.Equal("validation.invalid_input", result.Error!.Code);
    }

    [Fact]
    public void ChoiceContractsExposeStableStringVocabulary()
    {
        var systems = new ApplicationSystemInputFactory();
        var workItems = new WorkItemInputFactory();

        Assert.Equal(["Commercial", "Custom"], systems.Types);
        Assert.Equal(["Planned", "Active"], systems.InitialLifecycleStatuses);
        Assert.Equal(["Incident", "Enhancement", "ChangeRequest"], workItems.Types);
        Assert.Contains("Cancelled", workItems.Statuses);
    }
}

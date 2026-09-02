using System.ComponentModel.DataAnnotations;
using AppSupportHub.Application.Abstractions.Results;
using AppSupportHub.Application.ChangeAssessments;
using AppSupportHub.Web.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AppSupportHub.Web.Pages.WorkItems;

public sealed class AssessmentModel(
    ChangeAssessmentInputFactory inputFactory,
    GetChangeAssessmentHandler getHandler,
    SaveChangeAssessmentHandler saveHandler) : PageModel
{
    [BindProperty]
    public AssessmentInput Input { get; set; } = new();

    public Guid WorkItemId { get; private set; }

    public string WorkItemTitle { get; private set; } = "Change assessment";

    public ChangeAssessmentReadModel? ExistingAssessment { get; private set; }

    public IReadOnlyList<string> Risks => inputFactory.Risks;

    public async Task<IActionResult> OnGetAsync(
        Guid workItemId,
        CancellationToken cancellationToken)
    {
        return await LoadAsync(workItemId, true, cancellationToken);
    }

    public async Task<IActionResult> OnPostAsync(
        Guid workItemId,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            await LoadAsync(workItemId, false, cancellationToken);
            return Page();
        }

        ApplicationResult<SaveChangeAssessmentCommand> command =
            inputFactory.CreateSaveCommand(
                workItemId,
                Input.BusinessNeed,
                Input.TechnicalImpact,
                Input.SecurityImpact,
                Input.Risk,
                Input.AcceptanceCriteria,
                Input.TestPlan,
                Input.RollbackPlan,
                DemoActor.Identifier);
        if (!command.IsSuccess)
        {
            await LoadAsync(workItemId, false, cancellationToken);
            return ApplicationErrorMapper.ToPageResult(this, command.Error!, "Input.Risk");
        }

        ApplicationResult<MutationOutcome> result = await saveHandler.ExecuteAsync(
            command.Value,
            cancellationToken);
        if (!result.IsSuccess)
        {
            await LoadAsync(workItemId, false, cancellationToken);
            return ApplicationErrorMapper.ToPageResult(this, result.Error!);
        }

        TempData["StatusMessage"] = result.Value.Changed
            ? "Change assessment saved."
            : "The change assessment was already up to date.";
        return RedirectToPage(new { workItemId });
    }

    private async Task<IActionResult> LoadAsync(
        Guid workItemId,
        bool populateInput,
        CancellationToken cancellationToken)
    {
        WorkItemId = workItemId;
        ApplicationResult<GetChangeAssessmentResult> result = await getHandler.ExecuteAsync(
            new GetChangeAssessmentQuery(workItemId),
            cancellationToken);
        if (!result.IsSuccess)
        {
            return ApplicationErrorMapper.ToPageResult(this, result.Error!);
        }

        WorkItemTitle = result.Value.WorkItemTitle;
        ExistingAssessment = result.Value.Assessment;
        if (populateInput && ExistingAssessment is { } assessment)
        {
            Input = new AssessmentInput
            {
                BusinessNeed = assessment.BusinessNeed,
                TechnicalImpact = assessment.TechnicalImpact,
                SecurityImpact = assessment.SecurityImpact,
                Risk = assessment.Risk,
                AcceptanceCriteria = assessment.AcceptanceCriteria,
                TestPlan = assessment.TestPlan,
                RollbackPlan = assessment.RollbackPlan,
            };
        }

        return Page();
    }

    public sealed class AssessmentInput
    {
        [Required, StringLength(2000)]
        [Display(Name = "Business need")]
        public string BusinessNeed { get; set; } = string.Empty;

        [Required, StringLength(2000)]
        [Display(Name = "Technical impact")]
        public string TechnicalImpact { get; set; } = string.Empty;

        [Required, StringLength(2000)]
        [Display(Name = "Security impact")]
        public string SecurityImpact { get; set; } = string.Empty;

        [Required]
        public string Risk { get; set; } = "Medium";

        [Required, StringLength(2000)]
        [Display(Name = "Acceptance criteria")]
        public string AcceptanceCriteria { get; set; } = string.Empty;

        [Required, StringLength(2000)]
        [Display(Name = "Test plan")]
        public string TestPlan { get; set; } = string.Empty;

        [Required, StringLength(2000)]
        [Display(Name = "Rollback plan")]
        public string RollbackPlan { get; set; } = string.Empty;
    }
}

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YourRhythmStudio.Application.Root;
using YourRhythmStudio.Domain;
using YourRhythmStudio.ViewModels.Root;

namespace YourRhythmStudio.Controllers;

[Authorize(AuthenticationSchemes = "YourRhythmCookie", Roles = YourRhythmRoles.RootAdmin)]
[Route("Root")]
public sealed class RootController : Controller
{
    private readonly RootAdminService _svc;

    public RootController(RootAdminService svc) => _svc = svc;

    private Guid ActorId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private string ActorEmail => User.FindFirstValue(ClaimTypes.Email) ?? User.Identity?.Name ?? "root";

    // ──── Dashboard ────────────────────────────────────────────────────────────

    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<IActionResult> Index()
    {
        ViewData["RootSection"] = "dashboard";
        return View(await _svc.GetDashboardAsync());
    }

    // ──── Requests ─────────────────────────────────────────────────────────────

    [HttpGet("Requests")]
    public async Task<IActionResult> Requests(string? status)
    {
        ViewData["RootSection"] = "requests";
        var items = await _svc.GetRequestsAsync(status);
        return View(new RequestListViewModel { Items = items, StatusFilter = status });
    }

    [HttpGet("Requests/{id:guid}")]
    public async Task<IActionResult> RequestDetail(Guid id)
    {
        ViewData["RootSection"] = "requests";
        var vm = await _svc.GetRequestAsync(id);
        if (vm is null) return NotFound();
        return View(vm);
    }

    [HttpPost("Requests/{id:guid}/Approve")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveRequest(Guid id)
    {
        var (ok, err) = await _svc.ApproveRequestAsync(id, ActorId, ActorEmail);
        TempData[ok ? "SuccessMessage" : "ErrorMessage"] = ok ? "Solicitacao aprovada. E-mail enviado ao usuario." : err;
        return RedirectToAction(nameof(RequestDetail), new { id });
    }

    [HttpPost("Requests/{id:guid}/Reject")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectRequest(Guid id, RejectRequestViewModel vm)
    {
        var (ok, err) = await _svc.RejectRequestAsync(id, ActorId, ActorEmail, vm.Note);
        TempData[ok ? "SuccessMessage" : "ErrorMessage"] = ok ? "Solicitacao rejeitada." : err;
        return RedirectToAction(nameof(RequestDetail), new { id });
    }

    // ──── Accounts ─────────────────────────────────────────────────────────────

    [HttpGet("Accounts")]
    public async Task<IActionResult> Accounts(string? search, string? status, string? plan)
    {
        ViewData["RootSection"] = "accounts";
        var items = await _svc.GetAccountsAsync(search, status, plan);
        return View(new AccountListViewModel { Items = items, Search = search, StatusFilter = status, PlanFilter = plan });
    }

    [HttpGet("Accounts/{id:guid}")]
    public async Task<IActionResult> AccountDetail(Guid id)
    {
        ViewData["RootSection"] = "accounts";
        var vm = await _svc.GetAccountDetailAsync(id);
        if (vm is null) return NotFound();
        return View(vm);
    }

    [HttpGet("Accounts/Create")]
    public IActionResult CreateAccount()
    {
        ViewData["RootSection"] = "accounts";
        return View(new CreateAccountViewModel());
    }

    [HttpPost("Accounts/Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateAccount(CreateAccountViewModel vm)
    {
        if (!ModelState.IsValid) { ViewData["RootSection"] = "accounts"; return View(vm); }

        var (ok, err) = await _svc.CreateAccountAsync(vm, ActorId, ActorEmail);
        if (!ok) { ModelState.AddModelError(string.Empty, err!); ViewData["RootSection"] = "accounts"; return View(vm); }

        TempData["SuccessMessage"] = "Conta criada com sucesso.";
        return RedirectToAction(nameof(Accounts));
    }

    [HttpGet("Accounts/{id:guid}/Edit")]
    public async Task<IActionResult> EditAccount(Guid id)
    {
        ViewData["RootSection"] = "accounts";
        var detail = await _svc.GetAccountDetailAsync(id);
        if (detail is null) return NotFound();

        return View(new EditAccountViewModel
        {
            AccountId = id,
            DisplayName = detail.DisplayName,
            SchoolName = detail.SchoolName,
            PlanCode = detail.PlanCode
        });
    }

    [HttpPost("Accounts/{id:guid}/Edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditAccount(Guid id, EditAccountViewModel vm)
    {
        vm.AccountId = id;
        if (!ModelState.IsValid) { ViewData["RootSection"] = "accounts"; return View(vm); }

        var (ok, err) = await _svc.EditAccountAsync(vm, ActorId, ActorEmail);
        if (!ok) { ModelState.AddModelError(string.Empty, err!); ViewData["RootSection"] = "accounts"; return View(vm); }

        TempData["SuccessMessage"] = "Conta atualizada.";
        return RedirectToAction(nameof(AccountDetail), new { id });
    }

    [HttpPost("Accounts/{id:guid}/Block")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BlockAccount(Guid id)
    {
        var (ok, err) = await _svc.BlockAccountAsync(id, ActorId, ActorEmail);
        TempData[ok ? "SuccessMessage" : "ErrorMessage"] = ok ? "Conta bloqueada." : err;
        return RedirectToAction(nameof(AccountDetail), new { id });
    }

    [HttpPost("Accounts/{id:guid}/Unblock")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UnblockAccount(Guid id)
    {
        var (ok, err) = await _svc.UnblockAccountAsync(id, ActorId, ActorEmail);
        TempData[ok ? "SuccessMessage" : "ErrorMessage"] = ok ? "Conta reativada." : err;
        return RedirectToAction(nameof(AccountDetail), new { id });
    }

    [HttpPost("Accounts/{id:guid}/Cancel")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelAccount(Guid id)
    {
        var (ok, err) = await _svc.CancelAccountAsync(id, ActorId, ActorEmail);
        TempData[ok ? "SuccessMessage" : "ErrorMessage"] = ok ? "Conta cancelada." : err;
        return RedirectToAction(nameof(AccountDetail), new { id });
    }

    // ──── Plans ────────────────────────────────────────────────────────────────

    [HttpGet("Plans")]
    public async Task<IActionResult> Plans()
    {
        ViewData["RootSection"] = "plans";
        return View(new PlansViewModel { Plans = await _svc.GetPlansAsync() });
    }

    [HttpPost("Plans")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpsertPlan(UpsertPlanViewModel vm)
    {
        if (!ModelState.IsValid) return RedirectToAction(nameof(Plans));
        var (ok, err) = await _svc.UpsertPlanAsync(vm, ActorId, ActorEmail);
        TempData[ok ? "SuccessMessage" : "ErrorMessage"] = ok ? "Plano salvo." : err;
        return RedirectToAction(nameof(Plans));
    }

    // ──── Storage ──────────────────────────────────────────────────────────────

    [HttpGet("Storage")]
    public async Task<IActionResult> Storage()
    {
        ViewData["RootSection"] = "storage";
        return View(new StorageOverviewViewModel { Items = await _svc.GetStorageOverviewAsync() });
    }

    [HttpPost("Storage/{schoolId:guid}/Quota")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStorageQuota(Guid schoolId, EditStorageQuotaViewModel vm)
    {
        vm.SchoolId = schoolId;
        var (ok, err) = await _svc.UpdateStorageQuotaAsync(schoolId, vm.QuotaGb, ActorId, ActorEmail);
        TempData[ok ? "SuccessMessage" : "ErrorMessage"] = ok ? "Cota atualizada." : err;
        return RedirectToAction(nameof(Storage));
    }
}

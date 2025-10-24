using Microsoft.AspNetCore.Mvc;

namespace Bioteca.Prism.Core.Security.Authorization
{
    /// <summary>
    /// An action result to customize the failure result from AuthorizeActionFilter
    /// </summary>
    public class UnauthorizedActionResult : ActionResult, IActionResult
    {
    }

}

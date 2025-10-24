using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bioteca.Prism.Core.Security.Authorization
{
    /// <summary>
    /// Authorization attribute
    /// </summary>
    /// <remarks>
    /// More details here:
    /// https://stackoverflow.com/questions/31464359/how-do-you-create-a-custom-authorizeattribute-in-asp-net-core
    /// </remarks>
    public class AuthorizeAttribute : TypeFilterAttribute
    {
        /// <summary>
        /// A string identifying roles/claims required to run an action. When empty or null the authorization filter will only check for a valid JWT, ignoring the token payload.
        /// </summary>
        public string RequiredAuthorization { get; } = null;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="requiredAuthorization">A string identifying roles/claims required to run an action. When empty or null the authorization filter will only check for a valid JWT, ignoring the token payload.</param>
        public AuthorizeAttribute(string requiredAuthorization) : base(typeof(AuthorizeActionFilter))
        {
            Arguments = new object[] { requiredAuthorization };
            RequiredAuthorization = requiredAuthorization;
        }
    }
}

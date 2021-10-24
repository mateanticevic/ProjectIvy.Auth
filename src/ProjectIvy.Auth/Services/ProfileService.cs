using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using IdentityServer4.Models;
using IdentityServer4.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using ProjectIvy.Auth.Models;

namespace ProjectIvy.Auth.Services
{
    public class ProfileService : IProfileService
    {
        private ILogger _logger;
        protected UserManager<ApplicationUser> _userManager;
        private readonly IUserClaimsPrincipalFactory<ApplicationUser> _claimsFactory;

        public ProfileService(ILogger<ProfileService> logger,
                              UserManager<ApplicationUser> userManager,
                              IUserClaimsPrincipalFactory<ApplicationUser> claimsFactory)
        {
            _claimsFactory = claimsFactory;
            _logger = logger;
            _userManager = userManager;
        }

        public async Task GetProfileDataAsync(ProfileDataRequestContext context)
        {
            var user = await _userManager.GetUserAsync(context.Subject);
            var principal = await _claimsFactory.CreateAsync(user);
            
            context.IssuedClaims.AddRange(principal.Claims);
        }

        public async Task IsActiveAsync(IsActiveContext context)
        {
            var user = await _userManager.GetUserAsync(context.Subject);

            context.IsActive = (user != null);
        }
    }
}

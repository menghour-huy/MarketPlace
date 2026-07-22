using Market.API.Application.Common.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Market.API.Application.Auth.Commands.Login
{
    public record LoginCommand(string Email, string Password) : IRequest<AuthResult?>;

    public class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResult?>
    {
        private readonly IIdentityService _identityService;
        public LoginCommandHandler(IIdentityService identityService) => _identityService = identityService;

        public async Task<AuthResult?> Handle(LoginCommand request, CancellationToken ct)
        {
            var (succeeded, result) = await _identityService.LoginAsync(request.Email, request.Password);

            if (!succeeded || result is null)
                return null;

            return result;
        }
    }
}

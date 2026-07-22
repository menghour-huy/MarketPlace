using FluentValidation;
using Market.API.Application.Common.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Market.API.Application.Auth.Commands.Register
{
    public record RegisterCommand(string FullName, string Email, string Password, Guid? StoreId) : IRequest<Guid>;

    public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
    {
        public RegisterCommandValidator()
        {
            RuleFor(x => x.Email).NotEmpty().EmailAddress();
            RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
            RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        }
    }

    public class RegisterCommandHandler : IRequestHandler<RegisterCommand, Guid>
    {
        private readonly IIdentityService _identityService;
        public RegisterCommandHandler(IIdentityService identityService) => _identityService = identityService;

        public async Task<Guid> Handle(RegisterCommand request, CancellationToken ct)
        {
            var (succeeded, userId, errors) = await _identityService.CreateUserAsync(
                request.FullName, request.Email, request.Password, request.StoreId);

            if (!succeeded)
                throw new FluentValidation.ValidationException(string.Join("; ", errors));
            return Guid.Parse(userId!);
        }
    }
}

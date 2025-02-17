﻿using FluentValidation;
using Microsoft.AspNetCore.Authentication;
using Ocelot.Configuration.File;
using Ocelot.Configuration.Creator;

namespace Ocelot.Configuration.Validator
{
    public class RouteFluentValidator : AbstractValidator<FileRoute>
    {
        private readonly IAuthenticationSchemeProvider _authenticationSchemeProvider;

        public RouteFluentValidator(IAuthenticationSchemeProvider authenticationSchemeProvider, HostAndPortValidator hostAndPortValidator, FileQoSOptionsFluentValidator fileQoSOptionsFluentValidator)
        {
            _authenticationSchemeProvider = authenticationSchemeProvider;

            RuleFor(route => route.QoSOptions)
                .SetValidator(fileQoSOptionsFluentValidator);

            RuleFor(route => route.DownstreamPathTemplate)
                .NotEmpty()
                .WithMessage("{PropertyName} cannot be empty");

            RuleFor(route => route.UpstreamPathTemplate)
                .NotEmpty()
                .WithMessage("{PropertyName} cannot be empty");

            When(route => !string.IsNullOrEmpty(route.DownstreamPathTemplate), () =>
            {
                RuleFor(route => route.DownstreamPathTemplate)
                    .Must(path => path.StartsWith("/"))
                    .WithMessage("{PropertyName} {PropertyValue} doesnt start with forward slash");

                RuleFor(route => route.DownstreamPathTemplate)
                    .Must(path => !path.Contains("//"))
                    .WithMessage("{PropertyName} {PropertyValue} contains double forward slash, Ocelot does not support this at the moment. Please raise an issue in GitHib if you need this feature.");

                RuleFor(route => route.DownstreamPathTemplate)
                    .Must(path => !path.Contains("https://") && !path.Contains("http://"))
                    .WithMessage("{PropertyName} {PropertyValue} contains scheme");
            });

            When(route => !string.IsNullOrEmpty(route.UpstreamPathTemplate), () =>
            {
                RuleFor(route => route.UpstreamPathTemplate)
                    .Must(path => !path.Contains("//"))
                    .WithMessage("{PropertyName} {PropertyValue} contains double forward slash, Ocelot does not support this at the moment. Please raise an issue in GitHib if you need this feature.");

                RuleFor(route => route.UpstreamPathTemplate)
                    .Must(path => path.StartsWith("/"))
                    .WithMessage("{PropertyName} {PropertyValue} doesnt start with forward slash");

                RuleFor(route => route.UpstreamPathTemplate)
                    .Must(path => !path.Contains("https://") && !path.Contains("http://"))
                    .WithMessage("{PropertyName} {PropertyValue} contains scheme");
            });

            When(route => route.RateLimitOptions.EnableRateLimiting, () =>
            {
                RuleFor(route => route.RateLimitOptions.Period)
                    .NotEmpty()
                    .WithMessage("RateLimitOptions.Period is empty");

                RuleFor(route => route.RateLimitOptions)
                    .Must(IsValidPeriod)
                    .WithMessage("RateLimitOptions.Period does not contain integer then s (second), m (minute), h (hour), d (day) e.g. 1m for 1 minute period");
            });

            RuleFor(route => route.AuthenticationOptions)
                .MustAsync(IsSupportedAuthenticationProviders)
                .WithMessage("{PropertyName} {PropertyValue} is unsupported authentication provider");

            When(route => string.IsNullOrEmpty(route.ServiceName), () =>
            {
                RuleFor(r => r.DownstreamHostAndPorts).NotEmpty()
                    .WithMessage("When not using service discovery DownstreamHostAndPorts must be set and not empty or Ocelot cannot find your service!");
            });

            When(route => string.IsNullOrEmpty(route.ServiceName), () =>
            {
                RuleForEach(route => route.DownstreamHostAndPorts)
                    .SetValidator(hostAndPortValidator);
            });

            When(route => !string.IsNullOrEmpty(route.DownstreamHttpVersion), () =>
            {
                RuleFor(r => r.DownstreamHttpVersion).Matches("^[0-9]([.,][0-9]{1,1})?$");
            });

            When(route => !string.IsNullOrEmpty(route.DownstreamHttpVersionPolicy), () =>
            {
                RuleFor(r => r.DownstreamHttpVersionPolicy).Matches($@"^({VersionPolicies.RequestVersionExact}|{VersionPolicies.RequestVersionOrHigher}|{VersionPolicies.RequestVersionOrLower})$");
            });
        }

        private async Task<bool> IsSupportedAuthenticationProviders(FileAuthenticationOptions options, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(options.AuthenticationProviderKey)
                && options.AuthenticationProviderKeys.Length == 0)
            {
                return true;
            }

            var schemes = await _authenticationSchemeProvider.GetAllSchemesAsync();
            var supportedSchemes = schemes.Select(scheme => scheme.Name).ToList();
            var primary = options.AuthenticationProviderKey;
            return !string.IsNullOrEmpty(primary) && supportedSchemes.Contains(primary)
                || (string.IsNullOrEmpty(primary) && options.AuthenticationProviderKeys.All(supportedSchemes.Contains));
        }

        private static bool IsValidPeriod(FileRateLimitRule rateLimitOptions)
        {
            if (string.IsNullOrEmpty(rateLimitOptions.Period))
            {
                return false;
            }

            var period = rateLimitOptions.Period.Trim();

            var secondsRegEx = new Regex("^[0-9]+s");
            var minutesRegEx = new Regex("^[0-9]+m");
            var hoursRegEx = new Regex("^[0-9]+h");
            var daysRegEx = new Regex("^[0-9]+d");

            return secondsRegEx.Match(period).Success
                || minutesRegEx.Match(period).Success
                || hoursRegEx.Match(period).Success
                || daysRegEx.Match(period).Success;
        }
    }
}

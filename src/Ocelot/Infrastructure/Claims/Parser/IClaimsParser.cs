﻿using Ocelot.Responses;
using System.Security.Claims;

namespace Ocelot.Infrastructure.Claims.Parser
{
    public interface IClaimsParser
    {
        Response<string> GetValue(IEnumerable<Claim> claims, string key, string delimiter, int index);

        Response<List<string>> GetValuesByClaimType(IEnumerable<Claim> claims, string claimType);
    }
}

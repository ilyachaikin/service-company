using Microsoft.AspNetCore.Identity;
using ServiceCompany.Domain.Common;

namespace ServiceCompany.Infrastructure.Identity;

public static class IdentityResultExtensions
{
    public static Result ToApplicationResult(this IdentityResult result)
    {
        return result.Succeeded
            ? Result.Success()
            : Result.Failure(string.Join(", ", result.Errors.Select(e => e.Description)));
    }
}

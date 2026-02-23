using System;
using System.Security.Claims;
using Parental.Backend.Models.Enum;

namespace Parental.Backend.Extensions;

public static class ClaimsPrincipalExtension
{
    public static string GetUsername(this ClaimsPrincipal principal)
    {
        return principal?.FindFirst(ClaimTypes.Name)?.Value;
    }

    public static UserRoleType GetUserRoleType(this ClaimsPrincipal principal)
    {
        var itemStr = principal?.FindFirst(ClaimTypes.Role)?.Value;
        
        if (itemStr != null && Enum.TryParse<UserRoleType>(itemStr, out var item))
        {
            return item;
        }

        return UserRoleType.NOT_SET;
    }
}

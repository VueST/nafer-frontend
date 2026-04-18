using Nafer.Core.Domain.Models;

namespace Nafer.Core.Domain.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class AuthorizeAttribute : Attribute
{
    public UserRole RequiredRole { get; }

    public AuthorizeAttribute(UserRole requiredRole)
    {
        RequiredRole = requiredRole;
    }
}

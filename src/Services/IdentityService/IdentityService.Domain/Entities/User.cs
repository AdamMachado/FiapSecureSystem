using IdentityService.Domain.ValueObjects;
using Shared.Kernel.Primitives;

namespace IdentityService.Domain.Entities;

public sealed class User : Entity<Guid>
{
    private readonly List<string> _roles = [];
    private readonly List<string> _scopes = [];

    private User(
        Guid id,
        EmailAddress email,
        string displayName,
        IEnumerable<string> roles,
        IEnumerable<string> scopes,
        bool isActive)
        : base(id)
    {
        Email = email;
        DisplayName = displayName;
        _roles.AddRange(roles);
        _scopes.AddRange(scopes);
        IsActive = isActive;
    }

    private User()
    {
    }

    public EmailAddress Email { get; private set; } = default!;
    public string DisplayName { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public IReadOnlyCollection<string> Roles => _roles.AsReadOnly();
    public IReadOnlyCollection<string> Scopes => _scopes.AsReadOnly();

    public static User Create(
        Guid id,
        EmailAddress email,
        string displayName,
        IEnumerable<string> roles,
        IEnumerable<string> scopes,
        bool isActive)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("User id cannot be empty.", nameof(id));

        ArgumentNullException.ThrowIfNull(email);

        if (string.IsNullOrWhiteSpace(displayName))
            throw new ArgumentException("Display name cannot be empty.", nameof(displayName));

        var normalizedRoles = NormalizeValues(roles, nameof(roles));
        var normalizedScopes = NormalizeValues(scopes, nameof(scopes));

        return new User(
            id,
            email,
            displayName.Trim(),
            normalizedRoles,
            normalizedScopes,
            isActive);
    }

    private static IReadOnlyCollection<string> NormalizeValues(
        IEnumerable<string> values,
        string paramName)
    {
        ArgumentNullException.ThrowIfNull(values);

        var normalized = values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (normalized.Length == 0)
            throw new ArgumentException("At least one value must be provided.", paramName);

        return normalized;
    }
}

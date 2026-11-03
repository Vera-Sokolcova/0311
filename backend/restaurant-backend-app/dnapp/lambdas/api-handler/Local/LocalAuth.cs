using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace APIHandler.Local;

public sealed class LocalUser
{
    public required string Sub { get; init; }
    public required string Email { get; init; }
    public string PasswordHash { get; set; } = "";
    public Dictionary<string, string> Attributes { get; } = new();
    public HashSet<string> Groups { get; } = new(StringComparer.OrdinalIgnoreCase);
}

/// <summary>User store + JWT issuing. Implements the small part of Cognito that the handlers use.</summary>
public sealed class LocalAuth
{
    private const string Issuer = "local-restaurant-api";
    private const string Audience = "local-client";

    private readonly ConcurrentDictionary<string, LocalUser> _byEmail = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _groups = new(StringComparer.OrdinalIgnoreCase) { "Customer", "Waiter" };
    private readonly SymmetricSecurityKey _key;
    private readonly JsonWebTokenHandler _jwt = new();

    public LocalAuth()
    {
        var secret = Environment.GetEnvironmentVariable("JWT_SECRET");
        if (string.IsNullOrWhiteSpace(secret) || secret.Length < 32)
            secret = "local-dev-secret-change-me-0123456789abcdef";
        _key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));

        Seed(LocalDb.OwnerSub, "owner@example.com", "Owner123!", "Olena", "Owner", "Customer");
        Seed(LocalDb.WaiterSub, "waiter@example.com", "Waiter123!", "Wiktor", "Waiter", "Waiter");
    }

    private void Seed(string sub, string email, string password, string first, string last, string group)
    {
        var u = new LocalUser { Sub = sub, Email = email };
        u.PasswordHash = Hash(sub, password);
        u.Attributes["sub"] = sub;
        u.Attributes["email"] = email;
        u.Attributes["given_name"] = first;
        u.Attributes["family_name"] = last;
        u.Groups.Add(group);
        _byEmail[email] = u;
    }

    private static string Hash(string sub, string password) =>
        Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(sub + ":" + password)));

    // ---------- lookups used by repositories ----------
    public LocalUser? FindByEmail(string email) => _byEmail.TryGetValue(email ?? "", out var u) ? u : null;
    public LocalUser? FindBySub(string sub) => _byEmail.Values.FirstOrDefault(u => u.Sub == sub);
    public IEnumerable<LocalUser> All() => _byEmail.Values;
    public void EnsureGroup(string group) { lock (_groups) _groups.Add(group); }
    public List<string> AllGroups() { lock (_groups) return _groups.ToList(); }

    public static UserType ToUserType(LocalUser u) => new()
    {
        Username = u.Email,
        Enabled = true,
        UserStatus = UserStatusType.CONFIRMED,
        Attributes = u.Attributes.Select(kv => new AttributeType { Name = kv.Key, Value = kv.Value }).ToList()
    };

    // ---------- tokens ----------
    private string CreateToken(LocalUser u, string use)
    {
        var claims = new Dictionary<string, object>
        {
            ["sub"] = u.Sub,
            ["email"] = u.Email,
            ["cognito:username"] = u.Email,
            ["token_use"] = use,
            ["cognito:groups"] = u.Groups.ToArray()
        };
        if (u.Attributes.TryGetValue("given_name", out var fn)) claims["given_name"] = fn;
        if (u.Attributes.TryGetValue("family_name", out var ln)) claims["family_name"] = ln;

        return _jwt.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = Issuer,
            Audience = Audience,
            Expires = DateTime.UtcNow.AddHours(12),
            Claims = claims,
            SigningCredentials = new SigningCredentials(_key, SecurityAlgorithms.HmacSha256)
        });
    }

    /// <summary>Validates a Bearer token; returns authorizer-style claims built from the current user state.</summary>
    public async Task<Dictionary<string, string>?> ValidateAsync(string token)
    {
        var result = await _jwt.ValidateTokenAsync(token, new TokenValidationParameters
        {
            ValidIssuer = Issuer,
            ValidAudience = Audience,
            IssuerSigningKey = _key,
            ValidateLifetime = true
        });
        if (!result.IsValid) return null;

        var sub = result.Claims.TryGetValue("sub", out var s) ? s?.ToString() : null;
        var user = sub is null ? null : FindBySub(sub);
        if (user is null) return null;

        var claims = new Dictionary<string, string>
        {
            ["sub"] = user.Sub,
            ["email"] = user.Email,
            ["cognito:username"] = user.Email,
            ["cognito:groups"] = string.Join(",", user.Groups)
        };
        foreach (var k in new[] { "given_name", "family_name" })
            if (user.Attributes.TryGetValue(k, out var v)) claims[k] = v;
        return claims;
    }

    // ---------- Cognito operations ----------
    public InitiateAuthResponse InitiateAuth(InitiateAuthRequest r)
    {
        r.AuthParameters.TryGetValue("USERNAME", out var email);
        r.AuthParameters.TryGetValue("PASSWORD", out var password);
        var user = FindByEmail(email ?? "");
        if (user is null || user.PasswordHash != Hash(user.Sub, password ?? ""))
            throw new NotAuthorizedException("Incorrect username or password.");

        return new InitiateAuthResponse
        {
            HttpStatusCode = HttpStatusCode.OK,
            AuthenticationResult = new AuthenticationResultType
            {
                IdToken = CreateToken(user, "id"),
                AccessToken = CreateToken(user, "access"),
                RefreshToken = "local-refresh",
                TokenType = "Bearer",
                ExpiresIn = 12 * 3600
            }
        };
    }

    public SignUpResponse SignUp(SignUpRequest r)
    {
        if (_byEmail.ContainsKey(r.Username))
            throw new UsernameExistsException("User already exists");

        var sub = Guid.NewGuid().ToString();
        var u = new LocalUser { Sub = sub, Email = r.Username, PasswordHash = Hash(sub, r.Password) };
        u.Attributes["sub"] = sub;
        foreach (var a in r.UserAttributes) u.Attributes[a.Name] = a.Value;
        u.Attributes["email"] = r.Username;
        _byEmail[r.Username] = u;
        return new SignUpResponse { HttpStatusCode = HttpStatusCode.OK, UserConfirmed = true, UserSub = sub };
    }

    public ListGroupsResponse ListGroups() => new()
    {
        HttpStatusCode = HttpStatusCode.OK,
        Groups = AllGroups().Select(g => new GroupType { GroupName = g }).ToList()
    };

    public CreateGroupResponse CreateGroup(CreateGroupRequest r)
    {
        EnsureGroup(r.GroupName);
        return new CreateGroupResponse { HttpStatusCode = HttpStatusCode.OK, Group = new GroupType { GroupName = r.GroupName } };
    }

    public AdminAddUserToGroupResponse AddToGroup(AdminAddUserToGroupRequest r)
    {
        var u = FindByEmail(r.Username) ?? throw new UserNotFoundException("User not found");
        EnsureGroup(r.GroupName);
        u.Groups.Add(r.GroupName);
        return new AdminAddUserToGroupResponse { HttpStatusCode = HttpStatusCode.OK };
    }

    public ChangePasswordResponse ChangePassword(ChangePasswordRequest r)
    {
        var claims = ValidateAsync(r.AccessToken).GetAwaiter().GetResult()
                     ?? throw new NotAuthorizedException("Invalid access token");
        var u = FindByEmail(claims["email"]) ?? throw new UserNotFoundException("User not found");
        if (u.PasswordHash != Hash(u.Sub, r.PreviousPassword))
            throw new NotAuthorizedException("Incorrect username or password.");
        u.PasswordHash = Hash(u.Sub, r.ProposedPassword);
        return new ChangePasswordResponse { HttpStatusCode = HttpStatusCode.OK };
    }

    public AdminUpdateUserAttributesResponse UpdateAttributes(AdminUpdateUserAttributesRequest r)
    {
        var u = FindByEmail(r.Username) ?? throw new UserNotFoundException("User not found");
        foreach (var a in r.UserAttributes) u.Attributes[a.Name] = a.Value;
        return new AdminUpdateUserAttributesResponse { HttpStatusCode = HttpStatusCode.OK };
    }

    public AdminGetUserResponse GetUser(string username)
    {
        var u = FindByEmail(username) ?? FindBySub(username) ?? throw new UserNotFoundException("User not found");
        return new AdminGetUserResponse
        {
            HttpStatusCode = HttpStatusCode.OK,
            Username = u.Email,
            Enabled = true,
            UserStatus = UserStatusType.CONFIRMED,
            UserAttributes = u.Attributes.Select(kv => new AttributeType { Name = kv.Key, Value = kv.Value }).ToList()
        };
    }

    public AdminListGroupsForUserResponse ListGroupsForUser(string username)
    {
        var u = FindByEmail(username) ?? throw new UserNotFoundException("User not found");
        return new AdminListGroupsForUserResponse
        {
            HttpStatusCode = HttpStatusCode.OK,
            Groups = u.Groups.Select(g => new GroupType { GroupName = g }).ToList()
        };
    }

    public ListUsersResponse ListUsers(ListUsersRequest r)
    {
        IEnumerable<LocalUser> users = All();
        var m = Regex.Match(r.Filter ?? "", "given_name \\^= \"(.*)\"");
        if (m.Success)
            users = users.Where(u => (u.Attributes.GetValueOrDefault("given_name") ?? "")
                .StartsWith(m.Groups[1].Value, StringComparison.OrdinalIgnoreCase));
        return new ListUsersResponse
        {
            HttpStatusCode = HttpStatusCode.OK,
            Users = users.Select(ToUserType).ToList(),
            PaginationToken = null
        };
    }
}

using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;

namespace APIHandler.Local;

internal static class ProxyHelper
{
    public static object? Success(Type returnType, object? value)
    {
        if (returnType == typeof(void)) return null;
        if (returnType == typeof(Task)) return Task.CompletedTask;
        if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            var t = returnType.GetGenericArguments()[0];
            return typeof(Task).GetMethod(nameof(Task.FromResult))!.MakeGenericMethod(t).Invoke(null, [value]);
        }
        return value;
    }

    public static object? Fail(Type returnType, Exception ex)
    {
        if (returnType == typeof(Task)) return Task.FromException(ex);
        if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            var t = returnType.GetGenericArguments()[0];
            var m = typeof(Task).GetMethods()
                .First(x => x.Name == nameof(Task.FromException) && x.IsGenericMethodDefinition);
            return m.MakeGenericMethod(t).Invoke(null, [ex]);
        }
        throw ex;
    }
}

/// <summary>Stands in for IAmazonCognitoIdentityProvider; implements only what the handlers call.</summary>
public class LocalCognitoProxy : DispatchProxy
{
    private LocalAuth _auth = null!;

    public static IAmazonCognitoIdentityProvider Create(LocalAuth auth)
    {
        var proxy = Create<IAmazonCognitoIdentityProvider, LocalCognitoProxy>();
        ((LocalCognitoProxy)(object)proxy)._auth = auth;
        return proxy;
    }

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        var method = targetMethod!;
        if (method.Name == "Dispose") return null;

        try
        {
            var req = args is { Length: > 0 } ? args[0] : null;
            object? result = (method.Name, req) switch
            {
                ("InitiateAuthAsync", InitiateAuthRequest r) => _auth.InitiateAuth(r),
                ("SignUpAsync", SignUpRequest r) => _auth.SignUp(r),
                ("AdminConfirmSignUpAsync", AdminConfirmSignUpRequest) =>
                    new AdminConfirmSignUpResponse { HttpStatusCode = System.Net.HttpStatusCode.OK },
                ("ListGroupsAsync", ListGroupsRequest) => _auth.ListGroups(),
                ("CreateGroupAsync", CreateGroupRequest r) => _auth.CreateGroup(r),
                ("AdminAddUserToGroupAsync", AdminAddUserToGroupRequest r) => _auth.AddToGroup(r),
                ("ChangePasswordAsync", ChangePasswordRequest r) => _auth.ChangePassword(r),
                ("AdminUpdateUserAttributesAsync", AdminUpdateUserAttributesRequest r) => _auth.UpdateAttributes(r),
                ("AdminGetUserAsync", AdminGetUserRequest r) => _auth.GetUser(r.Username),
                ("AdminListGroupsForUserAsync", AdminListGroupsForUserRequest r) => _auth.ListGroupsForUser(r.Username),
                ("ListUsersAsync", ListUsersRequest r) => _auth.ListUsers(r),
                _ => throw new NotSupportedException($"Cognito.{method.Name} is not available in local mode")
            };
            return ProxyHelper.Success(method.ReturnType, result);
        }
        catch (Exception ex)
        {
            return ProxyHelper.Fail(method.ReturnType, ex);
        }
    }
}

/// <summary>Placeholder for AWS clients that local mode does not use: any call fails with NotSupported.</summary>
public class NotSupportedProxy : DispatchProxy
{
    private string _name = "";

    public static T Create<T>() where T : class
    {
        var proxy = Create<T, NotSupportedProxy>();
        ((NotSupportedProxy)(object)proxy)._name = typeof(T).Name;
        return proxy;
    }

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        var method = targetMethod!;
        if (method.Name == "Dispose") return null;
        return ProxyHelper.Fail(method.ReturnType,
            new NotSupportedException($"{_name}.{method.Name} is not available in local mode"));
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Amazon.DynamoDBv2.DataModel;
using APIHandler.Entities;

namespace APIHandler.Local;

/// <summary>
/// In-memory stand-in for IDynamoDBContext. Some handlers use the context directly (not via repositories),
/// so the few operations they call (Load/Save/Delete/Scan/Query/BatchGet) are served from LocalDb by entity type.
/// </summary>
public class InMemoryDynamoContextProxy : DispatchProxy
{
    private LocalDb _db = null!;

    public static IDynamoDBContext Create(LocalDb db)
    {
        var proxy = Create<IDynamoDBContext, InMemoryDynamoContextProxy>();
        ((InMemoryDynamoContextProxy)(object)proxy)._db = db;
        return proxy;
    }

    internal static object CreateProxy(Type iface, Type proxyType)
    {
        var create = typeof(DispatchProxy).GetMethods()
            .First(m => m.Name == nameof(DispatchProxy.Create) && m.GetGenericArguments().Length == 2);
        return create.MakeGenericMethod(iface, proxyType).Invoke(null, null)!;
    }

    internal static IList NewList(Type itemType) =>
        (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(itemType))!;

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        var method = targetMethod!;
        try
        {
            switch (method.Name)
            {
                case "Dispose":
                    return null;

                case "LoadAsync":
                {
                    var type = method.GetGenericArguments()[0];
                    return ProxyHelper.Success(method.ReturnType, Load(type, args![0]));
                }
                case "SaveAsync":
                    Save(args![0]!);
                    return ProxyHelper.Success(method.ReturnType, null);

                case "DeleteAsync":
                    Delete(args![0]!);
                    return ProxyHelper.Success(method.ReturnType, null);

                case "ScanAsync":
                {
                    var type = method.GetGenericArguments()[0];
                    return NewSearch(method.ReturnType, type, All(type));
                }
                case "QueryAsync":
                {
                    var type = method.GetGenericArguments()[0];
                    return NewSearch(method.ReturnType, type, Query(type, args![0]));
                }
                case "CreateBatchGet":
                {
                    var type = method.GetGenericArguments()[0];
                    var batch = (BatchGetProxy)CreateProxy(method.ReturnType, typeof(BatchGetProxy));
                    batch.Init(this, type);
                    return batch;
                }
                default:
                    throw new NotSupportedException($"IDynamoDBContext.{method.Name} is not available in local mode");
            }
        }
        catch (Exception ex)
        {
            return ProxyHelper.Fail(method.ReturnType, ex);
        }
    }

    private static object NewSearch(Type returnType, Type itemType, IList items)
    {
        var search = (SearchProxy)CreateProxy(returnType, typeof(SearchProxy));
        search.Init(itemType, items);
        return search;
    }

    internal object? Load(Type type, object? key)
    {
        var id = key as string;
        if (id is null) return null;
        if (type == typeof(Dish)) return _db.Dishes.GetValueOrDefault(id);
        if (type == typeof(Location)) return _db.Locations.GetValueOrDefault(id);
        if (type == typeof(Table)) return _db.Tables.GetValueOrDefault(id);
        if (type == typeof(Reservation)) return _db.Reservations.GetValueOrDefault(id);
        if (type == typeof(Order)) return _db.Orders.GetValueOrDefault(id);
        if (type == typeof(ProfileImage)) return _db.ProfileImages.GetValueOrDefault(id);
        throw new NotSupportedException($"Load<{type.Name}> is not available in local mode");
    }

    private void Save(object value)
    {
        switch (value)
        {
            case Dish d: _db.Dishes[d.Id] = d; break;
            case Location l: _db.Locations[l.Id] = l; break;
            case Table t: _db.Tables[t.Id] = t; break;
            case Reservation r: _db.Reservations[r.Id] = r; break;
            case Order o: _db.Orders[o.OrderId] = o; break;
            case ProfileImage p: _db.ProfileImages[p.UserId] = p; break;
            default: throw new NotSupportedException($"Save<{value.GetType().Name}> is not available in local mode");
        }
    }

    private void Delete(object value)
    {
        switch (value)
        {
            case Dish d: _db.Dishes.TryRemove(d.Id, out _); break;
            case Location l: _db.Locations.TryRemove(l.Id, out _); break;
            case Table t: _db.Tables.TryRemove(t.Id, out _); break;
            case Reservation r: _db.Reservations.TryRemove(r.Id, out _); break;
            case Order o: _db.Orders.TryRemove(o.OrderId, out _); break;
            case ProfileImage p: _db.ProfileImages.TryRemove(p.UserId, out _); break;
            default: throw new NotSupportedException($"Delete<{value.GetType().Name}> is not available in local mode");
        }
    }

    private IList All(Type type)
    {
        var list = NewList(type);
        IEnumerable<object> items;
        if (type == typeof(Dish)) items = _db.Dishes.Values;
        else if (type == typeof(Location)) items = _db.Locations.Values;
        else if (type == typeof(Table)) items = _db.Tables.Values;
        else if (type == typeof(Reservation)) items = _db.Reservations.Values;
        else if (type == typeof(Order)) items = _db.Orders.Values;
        else if (type == typeof(ProfileImage)) items = _db.ProfileImages.Values;
        else throw new NotSupportedException($"Scan<{type.Name}> is not available in local mode");
        foreach (var item in items) list.Add(item);
        return list;
    }

    private IList Query(Type type, object? hashKey)
    {
        var list = NewList(type);
        if (type == typeof(Reservation) && hashKey is string tableId)
            foreach (var r in _db.Reservations.Values.Where(r => r.TableId == tableId)) list.Add(r);
        return list;
    }
}

/// <summary>Result of Scan/Query: serves GetRemainingAsync / GetNextSetAsync.</summary>
public class SearchProxy : DispatchProxy
{
    private Type _itemType = typeof(object);
    private IList _items = new List<object>();
    private bool _done;

    internal void Init(Type itemType, IList items) { _itemType = itemType; _items = items; }

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        var method = targetMethod!;
        try
        {
            switch (method.Name)
            {
                case "GetRemainingAsync":
                    _done = true;
                    return ProxyHelper.Success(method.ReturnType, _items);
                case "GetNextSetAsync":
                {
                    var set = _done ? InMemoryDynamoContextProxy.NewList(_itemType) : _items;
                    _done = true;
                    return ProxyHelper.Success(method.ReturnType, set);
                }
                case "get_IsDone":
                    return _done;
                default:
                    throw new NotSupportedException($"AsyncSearch.{method.Name} is not available in local mode");
            }
        }
        catch (Exception ex)
        {
            return ProxyHelper.Fail(method.ReturnType, ex);
        }
    }
}

/// <summary>Result of CreateBatchGet: AddKey, ExecuteAsync, Results.</summary>
public class BatchGetProxy : DispatchProxy
{
    private InMemoryDynamoContextProxy? _context;
    private Type _itemType = typeof(object);
    private readonly List<object?> _keys = new();
    private IList? _results;

    internal void Init(InMemoryDynamoContextProxy context, Type itemType) { _context = context; _itemType = itemType; }

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        var method = targetMethod!;
        try
        {
            switch (method.Name)
            {
                case "AddKey":
                    _keys.Add(args is { Length: > 0 } ? args[0] : null);
                    return null;
                case "Execute":
                case "ExecuteAsync":
                    Run();
                    return ProxyHelper.Success(method.ReturnType, null);
                case "get_Results":
                    return _results ?? InMemoryDynamoContextProxy.NewList(_itemType);
                default:
                    throw new NotSupportedException($"BatchGet.{method.Name} is not available in local mode");
            }
        }
        catch (Exception ex)
        {
            return ProxyHelper.Fail(method.ReturnType, ex);
        }
    }

    private void Run()
    {
        _results = InMemoryDynamoContextProxy.NewList(_itemType);
        foreach (var key in _keys)
        {
            var item = _context!.Load(_itemType, key);
            if (item is not null) _results.Add(item);
        }
    }
}

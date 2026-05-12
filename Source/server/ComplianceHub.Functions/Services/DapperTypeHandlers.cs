using System.Data;
using Dapper;

namespace ComplianceHub.Functions.Services;

// Dapper does not natively support DateOnly / TimeOnly parameters or readers.
// Without these handlers, every `new { date = DateOnly.X }` parameter throws
// NotSupportedException at execute time. Postgres maps DateOnly ↔ `date` and
// TimeOnly ↔ `time` via DateTime / TimeSpan respectively.

public class DateOnlyTypeHandler : SqlMapper.TypeHandler<DateOnly>
{
    public override DateOnly Parse(object value) => value switch
    {
        DateTime dt => DateOnly.FromDateTime(dt),
        DateOnly d => d,
        string s => DateOnly.Parse(s),
        _ => DateOnly.FromDateTime(Convert.ToDateTime(value)),
    };

    public override void SetValue(IDbDataParameter parameter, DateOnly value)
    {
        parameter.DbType = DbType.Date;
        parameter.Value = value.ToDateTime(TimeOnly.MinValue);
    }
}

public class NullableDateOnlyTypeHandler : SqlMapper.TypeHandler<DateOnly?>
{
    public override DateOnly? Parse(object? value) => value switch
    {
        null => null,
        DBNull => null,
        DateTime dt => DateOnly.FromDateTime(dt),
        DateOnly d => d,
        string s => DateOnly.Parse(s),
        _ => DateOnly.FromDateTime(Convert.ToDateTime(value)),
    };

    public override void SetValue(IDbDataParameter parameter, DateOnly? value)
    {
        parameter.DbType = DbType.Date;
        parameter.Value = value.HasValue ? value.Value.ToDateTime(TimeOnly.MinValue) : DBNull.Value;
    }
}

public static class DapperConfig
{
    private static bool _registered;
    private static readonly object _lock = new();

    public static void RegisterTypeHandlers()
    {
        if (_registered) return;
        lock (_lock)
        {
            if (_registered) return;
            SqlMapper.AddTypeHandler(new DateOnlyTypeHandler());
            SqlMapper.AddTypeHandler(new NullableDateOnlyTypeHandler());
            _registered = true;
        }
    }
}

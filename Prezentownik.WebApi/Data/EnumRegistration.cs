using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;
using Prezentownik.WebApi.Models.Enums;

namespace Prezentownik.WebApi.Data;

public static class EnumRegistration
{
    public static NpgsqlDbContextOptionsBuilder MapApplicationEnums(this NpgsqlDbContextOptionsBuilder builder, string schema)
    {
        builder.MapEnum<ItemType>("itemType", schema);

        return builder;
    }
}

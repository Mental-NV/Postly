namespace Postly.Api.Features.Shared.Pagination;

public readonly record struct OpaqueCursor(DateTimeOffset CreatedAtUtc, long Id)
{
    public static OpaqueCursor Initial { get; } =
        new(DateTimeOffset.MaxValue, long.MaxValue);

    public static bool TryParse(string? value, out OpaqueCursor cursor)
    {
        cursor = Initial;

        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        var parts = value.Split('_');
        if (parts.Length != 2)
        {
            return false;
        }

        if (!DateTimeOffset.TryParse(parts[0], out var createdAtUtc)
            || !long.TryParse(parts[1], out var id))
        {
            return false;
        }

        cursor = new OpaqueCursor(createdAtUtc, id);
        return true;
    }

    public static string Encode(DateTimeOffset createdAtUtc, long id)
        => $"{createdAtUtc:O}_{id}";

    public bool Includes(DateTimeOffset createdAtUtc, long id)
        => CreatedAtUtc == DateTimeOffset.MaxValue
           || createdAtUtc < CreatedAtUtc
           || (createdAtUtc == CreatedAtUtc && id < Id);
}

public sealed record CursorPage<T>(
    IReadOnlyList<T> Items,
    string? NextCursor
);

public static class OpaqueCursorPagination
{
    public static CursorPage<T> Paginate<T>(
        IEnumerable<T> source,
        OpaqueCursor cursor,
        int pageSize,
        Func<T, DateTimeOffset> createdAtSelector,
        Func<T, long> idSelector)
    {
        var ordered = source
            .Where(item => cursor.Includes(createdAtSelector(item), idSelector(item)))
            .OrderByDescending(createdAtSelector)
            .ThenByDescending(idSelector)
            .Take(pageSize + 1)
            .ToList();

        var visibleItems = ordered.Take(pageSize).ToArray();
        string? nextCursor = null;

        if (ordered.Count > pageSize)
        {
            var lastVisibleItem = visibleItems[^1];
            nextCursor = OpaqueCursor.Encode(
                createdAtSelector(lastVisibleItem),
                idSelector(lastVisibleItem));
        }

        return new CursorPage<T>(visibleItems, nextCursor);
    }
}

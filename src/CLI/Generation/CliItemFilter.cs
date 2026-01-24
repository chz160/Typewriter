using Typewriter.CodeModel;

namespace Typewriter.CLI.Generation;

/// <summary>
/// Applies filter patterns to collections of items.
/// This is a port of the VS extension's ItemFilter class.
/// </summary>
internal static class CliItemFilter
{
    /// <summary>
    /// Applies a filter pattern to a collection of items.
    /// </summary>
    /// <param name="items">The items to filter.</param>
    /// <param name="filter">The filter pattern (e.g., "*Model", "[Serializable]", ":BaseClass").</param>
    /// <param name="matchFound">Updated to true if any items match the filter.</param>
    /// <returns>The filtered collection of items.</returns>
    internal static IEnumerable<Item> Apply(IEnumerable<Item> items, string? filter, ref bool matchFound)
    {
        if (string.IsNullOrWhiteSpace(filter))
        {
            // No filter - set matchFound if there are any items
            var itemList = items.ToList();
            matchFound = matchFound || itemList.Any();
            return itemList;
        }

        if (items is not IFilterable filterable)
        {
            // Not filterable - set matchFound if there are any items
            var itemList = items.ToList();
            matchFound = matchFound || itemList.Any();
            return itemList;
        }

        Func<Item, IEnumerable<string>> selector;

        filter = filter.Trim();

        if (filter.StartsWith("[", StringComparison.OrdinalIgnoreCase) && filter.EndsWith("]", StringComparison.OrdinalIgnoreCase))
        {
            // Attribute filter: [AttributeName]
            filter = filter.Trim('[', ']', ' ');
            selector = filterable.AttributeFilterSelector;
        }
        else if (filter.StartsWith(":", StringComparison.OrdinalIgnoreCase))
        {
            // Inheritance filter: :BaseClass
            filter = filter.Remove(0, 1).Trim();
            selector = filterable.InheritanceFilterSelector;
        }
        else
        {
            // Name filter: *Model
            selector = filterable.ItemFilterSelector;
        }

        var filtered = ApplyFilter(items, filter, selector);

        matchFound = matchFound || filtered.Any();

        return filtered;
    }

    private static ICollection<Item> ApplyFilter(
        IEnumerable<Item> items,
        string filter,
        Func<Item, IEnumerable<string>> selector)
    {
        var parts = filter.Split('*');

        for (var i = 0; i < parts.Length; i++)
        {
            var part = parts[i];

            if (parts.Length == 1)
            {
                // Exact match
                items = items.Where(item => selector(item).Any(p =>
                    string.Equals(p, part, StringComparison.OrdinalIgnoreCase)));
            }
            else if (i == 0 && !string.IsNullOrWhiteSpace(part))
            {
                // StartsWith match
                items = items.Where(item => selector(item).Any(p =>
                    p.StartsWith(part, StringComparison.OrdinalIgnoreCase)));
            }
            else if (i == parts.Length - 1 && !string.IsNullOrWhiteSpace(part))
            {
                // EndsWith match
                items = items.Where(item => selector(item).Any(p =>
                    p.EndsWith(part, StringComparison.OrdinalIgnoreCase)));
            }
            else if (i > 0 && i < parts.Length - 1 && !string.IsNullOrWhiteSpace(part))
            {
                // Contains match
                items = items.Where(item => selector(item).Any(p =>
                    p.Contains(part)));
            }
        }

        return items.ToList();
    }
}

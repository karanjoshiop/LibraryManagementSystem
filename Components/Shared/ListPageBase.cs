using Microsoft.AspNetCore.Components;

namespace LibraryManagementSystem.Components.Shared
{
    /// <summary>
    /// Common search / filter / tab / paging behaviour for the list pages.
    /// Pages only load the full list into <see cref="AllItems"/> and describe
    /// which fields are searchable and how the filter/tab predicates work.
    /// </summary>
    public abstract class ListPageBase<T> : ComponentBase
    {
        protected List<T> AllItems { get; set; } = new();

        protected string SearchText { get; set; } = "";
        protected string SelectedFilter { get; set; } = "All";
        protected string SelectedTab { get; set; } = "";
        protected int PageNumber { get; set; } = 1;
        protected int PageSize { get; set; } = 10;

        protected virtual IReadOnlyList<string> FilterOptions => Array.Empty<string>();
        protected virtual IReadOnlyList<string> TabOptions => Array.Empty<string>();

        protected abstract IEnumerable<string?> GetSearchValues(T item);

        protected virtual bool MatchesFilter(T item) => true;
        protected virtual bool MatchesTab(T item) => true;

        protected IReadOnlyList<T> FilteredItems => AllItems
            .Where(MatchesTab)
            .Where(MatchesFilter)
            .Where(MatchesSearch)
            .ToList();

        protected IReadOnlyList<T> PageItems => FilteredItems
            .Skip((PageNumber - 1) * PageSize)
            .Take(PageSize)
            .ToList();

        protected int TotalCount => FilteredItems.Count;

        protected int TotalPages =>
            Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));

        private bool MatchesSearch(T item)
        {
            var term = SearchText.Trim();
            if (term.Length == 0) return true;

            return GetSearchValues(item)
                .Any(value => value is not null
                           && value.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        protected Task OnSearchChanged(string value)
        {
            SearchText = value;
            PageNumber = 1;
            return Task.CompletedTask;
        }

        protected Task OnFilterChanged(string value)
        {
            SelectedFilter = value;
            PageNumber = 1;
            return Task.CompletedTask;
        }

        protected Task OnTabChanged(string value)
        {
            SelectedTab = value;
            PageNumber = 1;
            return Task.CompletedTask;
        }

        protected Task OnPageChanged(int value)
        {
            PageNumber = Math.Clamp(value, 1, TotalPages);
            return Task.CompletedTask;
        }

        protected Task OnPageSizeChanged(int value)
        {
            PageSize = value;
            PageNumber = 1;
            return Task.CompletedTask;
        }

        /// <summary>Call after reloading data in case a delete shrank the list.</summary>
        protected void ClampPage()
        {
            if (PageNumber > TotalPages) PageNumber = TotalPages;
        }
    }
}

using System;
using System.Collections.Generic;

namespace CustomerNotificationService.Application.Common;

/// <summary>
/// Represents a paginated result set with metadata
/// </summary>
/// <typeparam name="T">The type of items in the result set</typeparam>
public class PagedResult<T>
{
    /// <summary>
    /// The items in the current page
    /// </summary>
    public IReadOnlyList<T> Items { get; set; } = Array.Empty<T>();

    /// <summary>
    /// Current page number (1-based indexing)
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// Number of items per page
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Total number of items across all pages
    /// </summary>
    public int TotalItems { get; set; }

    /// <summary>
    /// Total number of pages
    /// </summary>
    public int TotalPages { get; set; }

    /// <summary>
    /// Indicates whether there is a next page
    /// </summary>
    public bool HasNext { get; set; }

    /// <summary>
    /// Indicates whether there is a previous page
    /// </summary>
    public bool HasPrevious { get; set; }

    /// <summary>
    /// Initializes a new instance of PagedResult
    /// </summary>
    public PagedResult()
    {
    }

    /// <summary>
    /// Initializes a new instance of PagedResult with the specified parameters
    /// </summary>
    /// <param name="items">The items in the current page</param>
    /// <param name="page">Current page number</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="totalItems">Total number of items</param>
    public PagedResult(IReadOnlyList<T> items, int page, int pageSize, int totalItems)
    {
        Items = items;
        Page = page;
        PageSize = pageSize;
        TotalItems = totalItems;
        TotalPages = (int)Math.Ceiling((double)totalItems / pageSize);
        HasNext = page < TotalPages;
        HasPrevious = page > 1;
    }
}
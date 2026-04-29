namespace ChatApplication.Shared.Requests;

public class BaseSearchRequest : PaginationRequest
{
    public string? SearchTerm { get; set; }
    public string? SortBy { get; set; }
    public bool SortDescending { get; set; }
}

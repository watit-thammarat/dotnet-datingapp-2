using System.Text.Json;
using API.Helpers;
using Microsoft.AspNetCore.Http;

namespace API.Extensions
{
    public static class HttpExtensions
    {
        public static void AddPaginationHeader(this HttpResponse res, int currentPage, int itemsPerPage, int totalItems, int totalPages)
        {
            var header = new PaginationHeader(currentPage, itemsPerPage, totalItems, totalPages);
            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            var json = JsonSerializer.Serialize(header, options);

            res.Headers.Add("Pagination", json);
            res.Headers.Add("Access-Control-Expose-Headers", "Pagination");
        }
    }
}
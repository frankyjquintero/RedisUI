using Microsoft.AspNetCore.Http;
using System;

namespace RedisUI.Models
{
    /// <summary>
    /// Represents query parameters extracted from an HTTP request for Redis UI pagination and filtering.
    /// </summary>
    public readonly struct RequestQueryParamsModel : IEquatable<RequestQueryParamsModel>
    {
        public int Db { get; }
        public long Cursor { get; } // antes era Page
        public int PageSize { get; }
        public string SearchKey { get; }

        public RequestQueryParamsModel(HttpRequest request)
        {
            var query = request.Query;

            Db = query.TryGetValue("db", out var dbVal) && int.TryParse(dbVal, out var dbParsed) ? dbParsed : 0;
            Cursor = query.TryGetValue("cursor", out var cursorVal) && long.TryParse(cursorVal, out var cursorParsed) ? cursorParsed : 0;
            PageSize = query.TryGetValue("size", out var sizeVal) && int.TryParse(sizeVal, out var sizeParsed) ? sizeParsed : 10;
            SearchKey = query.TryGetValue("key", out var keyVal) ? keyVal.ToString() : string.Empty;
        }

        public bool Equals(RequestQueryParamsModel other)
        {
            return Db == other.Db
                && Cursor == other.Cursor
                && PageSize == other.PageSize
                && string.Equals(SearchKey, other.SearchKey, StringComparison.Ordinal);
        }

        public override bool Equals(object? obj)
        {
            return obj is RequestQueryParamsModel other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Db, Cursor, PageSize, SearchKey);
        }

        public static bool operator ==(RequestQueryParamsModel left, RequestQueryParamsModel right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(RequestQueryParamsModel left, RequestQueryParamsModel right)
        {
            return !(left == right);
        }
    }
}

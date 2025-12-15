public void CleanupOldBuckets(PROVISIONAPI_LIMIT data, int days)
{
    var threshold = DateTime.Now.AddDays(-days);

    Action<Dictionary<string, int>> cleanup = (dic) =>
    {
        foreach (var key in dic.Keys.ToList())
        {
            if (DateTime.TryParseExact(
                key,
                "yyyyMMddHHmm",
                null,
                System.Globalization.DateTimeStyles.None,
                out var bucketTime))
            {
                // 기준 날짜보다 이전이면 삭제
                if (bucketTime < threshold)
                {
                    dic.Remove(key);
                }
            }
        }
    };

    cleanup(data.Group);
    cleanup(data.Member);
    cleanup(data.Search);
}
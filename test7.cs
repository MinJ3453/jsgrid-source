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

string input = val.GroupLimitType; // 예: "D", "W", "M"

LimitType type = (LimitType)Enum.Parse(typeof(LimitType), input, true);

int days = (int)type; // D=1, W=7, M=30
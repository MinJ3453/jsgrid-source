public string GetResetTime(PROVISIONAPI_LIMIT data, string category, int days)
{
    Dictionary<string, int> bucket = null;

    switch (category.ToUpper())
    {
        case "GROUP": bucket = data.Group; break;
        case "MEMBER": bucket = data.Member; break;
        case "SEARCH": bucket = data.Search; break;
        default: return null;
    }

    if (bucket == null || bucket.Count == 0)
        return null;

    // 1) 가장 초기 시간
    string earliestKey = bucket.Keys.OrderBy(x => x).First();

    // 2) DateTime 변환
    DateTime earliestTime =
        DateTime.ParseExact(earliestKey, "yyyyMMddHHmm", null);

    // 3) days를 더해 초기화 예정 시간 계산
    DateTime resetTime = earliestTime.AddDays(days);

    // 4) 다시 yyyyMMddHHmm 문자열로 반환
    return resetTime.ToString("yyyyMMddHHmm");
}
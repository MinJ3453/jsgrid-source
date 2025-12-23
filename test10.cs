public PROVISIONAPI_LIMIT IncreaseCurrentBucket(
    PROVISIONAPI_LIMIT data,
    string category,
    int amount = 1
)
{
    // 기존 데이터가 없으면 새로 생성
    if (data == null)
        data = new PROVISIONAPI_LIMIT();

    var now = DateTime.Now;
    var rounded = now.AddMinutes(-(now.Minute % 5));
    string bucketKey = rounded.ToString("yyyyMMddHHmm");

    Dictionary<string, int> bucket;

    switch (category.ToUpper())
    {
        case "GROUP": bucket = data.Group; break;
        case "MEMBER": bucket = data.Member; break;
        case "SEARCH": bucket = data.Search; break;
        default: throw new Exception("지원하지 않는 카테고리입니다: " + category);
    }

    // ✅ 기존 데이터는 그대로 두고
    // ✅ 현재 시간 버킷만 없으면 추가
    if (!bucket.ContainsKey(bucketKey))
        bucket[bucketKey] = 0;

    // ✅ 누적 증가
    bucket[bucketKey] += amount;

    // ✅ 전체 데이터 그대로 리턴
    return data;
}
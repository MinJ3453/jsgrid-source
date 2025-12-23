public void IncreaseCurrentBucket(PROVISIONAPI_LIMIT data, string category)
{
    var now = DateTime.Now;
    var rounded = now.AddMinutes(-(now.Minute % 5));
    string bucketKey = rounded.ToString("yyyyMMddHHmm");

    Dictionary<string, int> bucket;

    switch (category.ToUpper())
    {
        case "GROUP": bucket = data.Group; break;
        case "MEMBER": bucket = data.Member; break;
        case "SEARCH": bucket = data.Search; break;
        default: throw new Exception("지원하지 않는 카테고리입니다.");
    }

    // ✅ 여기서 "버킷 없으면 추가"
    if (!bucket.ContainsKey(bucketKey))
        bucket[bucketKey] = 0;

    bucket[bucketKey] += 1;
}
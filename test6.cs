using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

public class ProvisionLimitService
{
    // JSON → 객체 변환
    public PROVISIONAPI_LIMIT Deserialize(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new PROVISIONAPI_LIMIT();

        return JsonConvert.DeserializeObject<PROVISIONAPI_LIMIT>(json) 
               ?? new PROVISIONAPI_LIMIT();
    }

    // 현재 시간을 5분 단위 버킷으로 변환
    public string GetCurrentBucketKey()
    {
        var now = DateTime.Now;
        var rounded = now.AddMinutes(-(now.Minute % 5));
        return rounded.ToString("yyyyMMddHHmm");
    }

    // 카테고리 Dictionary 가져오기
    private Dictionary<string, int> GetCategory(PROVISIONAPI_LIMIT data, string category)
    {
        switch (category.ToUpper())
        {
            case "GROUP": return data.Group;
            case "MEMBER": return data.Member;
            case "SEARCH": return data.Search;
            default: throw new Exception("지원하지 않는 카테고리입니다: " + category);
        }
    }

    // 없으면 생성 후 값 반환
    public int GetOrCreate(PROVISIONAPI_LIMIT data, string category)
    {
        var bucketKey = GetCurrentBucketKey();
        var bucket = GetCategory(data, category);

        if (!bucket.ContainsKey(bucketKey))
            bucket[bucketKey] = 0;

        return bucket[bucketKey];
    }

    // 증가
    public void Increase(PROVISIONAPI_LIMIT data, string category, int amount)
    {
        var bucketKey = GetCurrentBucketKey();
        var bucket = GetCategory(data, category);

        if (!bucket.ContainsKey(bucketKey))
            bucket[bucketKey] = 0;

        bucket[bucketKey] += amount;
    }

    // 특정 카테고리 총합
    public int Sum(PROVISIONAPI_LIMIT data, string category)
    {
        var bucket = GetCategory(data, category);
        return bucket.Values.Sum();
    }

    // 24시간 지난 데이터 삭제
    public void CleanupOldBuckets(PROVISIONAPI_LIMIT data)
    {
        var now = DateTime.Now;

        Action<Dictionary<string, int>> cleanup = (dic) =>
        {
            foreach (var key in dic.Keys.ToList())
            {
                DateTime bucketTime;
                if (DateTime.TryParseExact(
                    key,
                    "yyyyMMddHHmm",
                    null,
                    System.Globalization.DateTimeStyles.None,
                    out bucketTime))
                {
                    if ((now - bucketTime).TotalHours > 24)
                        dic.Remove(key);
                }
            }
        };

        cleanup(data.Group);
        cleanup(data.Member);
        cleanup(data.Search);
    }

    // 객체 → JSON 변환
    public string Serialize(PROVISIONAPI_LIMIT data)
    {
        return JsonConvert.SerializeObject(data);
    }
}
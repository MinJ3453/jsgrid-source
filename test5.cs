using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

public class BucketCategoryMap : Dictionary<string, Dictionary<string, int>>
{
}

public class BucketService
{
    // JSON → 객체 변환
    public BucketCategoryMap Deserialize(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new BucketCategoryMap();

        return JsonConvert.DeserializeObject<BucketCategoryMap>(json) 
               ?? new BucketCategoryMap();
    }

    // 현재 시간을 5분 단위로 반올림하여 키 생성
    public string GetCurrentBucketKey()
    {
        var now = DateTime.Now;
        var rounded = now.AddMinutes(-(now.Minute % 5));
        return rounded.ToString("yyyyMMddHHmm");
    }

    // 특정 카테고리에서 버킷 가져오기 (없으면 생성)
    public int GetOrCreate(BucketCategoryMap map, string category)
    {
        string bucketKey = GetCurrentBucketKey();

        if (!map.ContainsKey(category))
            map[category] = new Dictionary<string, int>();

        if (!map[category].ContainsKey(bucketKey))
            map[category][bucketKey] = 0;

        return map[category][bucketKey];
    }

    // 증가시키기
    public void Increase(BucketCategoryMap map, string category, int amount)
    {
        string bucketKey = GetCurrentBucketKey();

        if (!map.ContainsKey(category))
            map[category] = new Dictionary<string, int>();

        if (!map[category].ContainsKey(bucketKey))
            map[category][bucketKey] = 0;

        map[category][bucketKey] += amount;
    }

    // 카테고리 전체 합계 (예: MEMBER 의 M 전체 합산)
    public int SumCategory(BucketCategoryMap map, string category)
    {
        if (!map.ContainsKey(category))
            return 0;

        return map[category].Values.Sum();
    }

    // 24시간 지난 버킷 삭제
    public void CleanupOldBuckets(BucketCategoryMap map)
    {
        var now = DateTime.Now;

        foreach (var category in map.Keys.ToList())
        {
            foreach (var bucketKey in map[category].Keys.ToList())
            {
                if (DateTime.TryParseExact(
                    bucketKey,
                    "yyyyMMddHHmm",
                    null,
                    System.Globalization.DateTimeStyles.None,
                    out var bucketTime))
                {
                    if ((now - bucketTime).TotalHours > 24)
                    {
                        map[category].Remove(bucketKey);
                    }
                }
            }
        }
    }

    // 객체 → JSON 직렬화 (Redis 저장용)
    public string Serialize(BucketCategoryMap map)
    {
        return JsonConvert.SerializeObject(map);
    }
}

// -------------------------
// 사용 예시
// -------------------------
public class Program
{
    public static void Main()
    {
        string json = @"{
          ""GROUP"": { ""202512101510"": 1, ""202512101520"": 2 },
          ""MEMBER"": { ""202512101510"": 1 }
        }";

        var service = new BucketService();

        // JSON → 객체
        var map = service.Deserialize(json);

        // 현재 버킷 데이터 가져오기 + 생성
        service.GetOrCreate(map, "GROUP");

        // 값 증가
        service.Increase(map, "MEMBER", 1);

        // 전체 합계
        int memberTotal = service.SumCategory(map, "MEMBER");

        // 24시간 지난 데이터 삭제
        service.CleanupOldBuckets(map);

        // 다시 JSON으로 변환
        string finalJson = service.Serialize(map);

        Console.WriteLine("총 MEMBER 합계: " + memberTotal);
        Console.WriteLine("최종 JSON: " + finalJson);
    }
}
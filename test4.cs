using System;
using System.Collections.Generic;
using System.Text.Json;

public class A
{
    public long Time { get; set; } // yyyyMMddHHmm 형태
}

public class B : A
{
    public int G { get; set; }
    public int M { get; set; }
}

public class TimeBucketMap : Dictionary<string, B> { }

public static class TimeBucketService
{
    // yyyyMMddHHmm 포맷 시간 얻기
    private static long GetCurrentBucketKey()
    {
        return long.Parse(DateTime.Now.ToString("yyyyMMddHHmm"));
    }

    // 24시간 지난 데이터 삭제
    private static void CleanOldBuckets(TimeBucketMap map)
    {
        long now = GetCurrentBucketKey();

        var keysToRemove = new List<string>();

        foreach (var kv in map)
        {
            long bucketTime = kv.Value.Time;

            DateTime bucketDate = DateTime.ParseExact(bucketTime.ToString(), "yyyyMMddHHmm", null);

            if (bucketDate < DateTime.Now.AddHours(-24))
            {
                keysToRemove.Add(kv.Key);
            }
        }

        foreach (var key in keysToRemove)
        {
            map.Remove(key);
        }
    }

    // 버킷 조회 + 없으면 생성
    public static B GetOrCreateBucket(TimeBucketMap map)
    {
        long key = GetCurrentBucketKey();
        string keyStr = key.ToString();

        // 24시간 지난 데이터 제거
        CleanOldBuckets(map);

        if (map.TryGetValue(keyStr, out var bucket))
        {
            return bucket;
        }

        // 없으면 신규 생성
        var newBucket = new B
        {
            Time = key,
            G = 0,
            M = 0
        };

        map[keyStr] = newBucket;
        return newBucket;
    }

    // JSON → TimeBucketMap 파싱
    public static TimeBucketMap Deserialize(string raw)
    {
        return JsonSerializer.Deserialize<TimeBucketMap>(raw) ??
               new TimeBucketMap();
    }
}
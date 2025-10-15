using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

public static class JsonHelpers
{
    /// <summary>
    /// 檢查 JSON 是否有效，並嘗試在 Rtn_Value 陣列中找出 Col1 == targetKey 的那筆；
    /// 若找到且 Col9/Col10 皆有值，回傳 true 並輸出其值。
    /// </summary>
    public static bool TryGetPartAndLot(
        string json,
        out string part,      // 對應 Col9: "MHUN12AD05SMFH A_RDL-2"
        out string lot,       // 對應 Col10: "2P25331UPT081-1-02062"
        string targetKey = "001#",
        bool requireRtnResultTrue = false)
    {
        part = string.Empty;
        lot = string.Empty;

        if (string.IsNullOrWhiteSpace(json)) return false;

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object) return false;

            // 可選：先檢查 Rtn_Result 必須為 "True"
            if (requireRtnResultTrue &&
                (!root.TryGetProperty("Rtn_Result", out var rr) ||
                 !string.Equals(rr.GetString(), "True", StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }

            if (!root.TryGetProperty("Rtn_Value", out var arr) ||
                arr.ValueKind != JsonValueKind.Array)
                return false;

            foreach (var item in arr.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object) continue;

                if (item.TryGetProperty("Col1", out var col1) &&
                    string.Equals(col1.GetString()?.Trim(), targetKey, StringComparison.Ordinal))
                {
                    string? c9 = item.TryGetProperty("Col9", out var p9) ? p9.GetString() : null;
                    string? c10 = item.TryGetProperty("Col10", out var p10) ? p10.GetString() : null;

                    if (!string.IsNullOrWhiteSpace(c9) && !string.IsNullOrWhiteSpace(c10))
                    {
                        part = c9!;
                        lot = c10!;
                        return true;
                    }
                    // 找到列但缺欄位就視為失敗
                    return false;
                }
            }

            // 走完整個陣列都沒找到 targetKey
            return false;
        }
        catch
        {
            // 解析失敗/結構不符
            return false;
        }
    }
}
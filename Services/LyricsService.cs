using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DailyPlan.Services;

// ===== 单行歌词 =====
internal sealed class LyricLine
{
    public TimeSpan Time;
    public string Text = "";
}

// ===== 一首歌的歌词 =====
internal sealed class Lyrics
{
    public string TrackName = "";
    public string ArtistName = "";
    public bool IsSynced;              // 是否带时间标签（可滚动同步）
    public List<LyricLine> Lines = new();

    // 根据播放位置定位当前行（二分查找最后一个 Time <= position 的行），无匹配返回 -1
    public int IndexAt(TimeSpan position)
    {
        int lo = 0, hi = Lines.Count - 1, ans = -1;
        while (lo <= hi)
        {
            int mid = (lo + hi) / 2;
            if (Lines[mid].Time <= position) { ans = mid; lo = mid + 1; }
            else hi = mid - 1;
        }
        return ans;
    }
}

// ===== 歌词服务：LRCLIB 匹配 + LRC 解析 + 本地缓存 =====
internal sealed class LyricsService
{
    static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(10) };

    public event Action? Changed;

    Lyrics? _current;
    string _currentKey = "";

    public Lyrics? Current => _current;

    static string CacheDir => Path.Combine(AppContext.BaseDirectory, "lyrics_cache");

    public void Clear()
    {
        _current = null;
        _currentKey = "";
    }

    // 歌曲变化时调用；同一首歌（artist|title 相同）不重复请求
    public async Task FetchAsync(string title, string artist, TimeSpan duration)
    {
        title = (title ?? "").Trim();
        artist = (artist ?? "").Trim();
        if (title.Length == 0)
        {
            Clear();
            Changed?.Invoke();
            return;
        }

        var key = artist + "|" + title;
        if (key == _currentKey) return;

        _currentKey = key;
        _current = null;
        Changed?.Invoke(); // 先清空，让界面回落为「歌名 - 歌手」

        var cached = TryLoadCache(key, title, artist);
        if (cached != null) { _current = cached; Changed?.Invoke(); return; }

        var fetched = await FetchFromWebAsync(title, artist, duration);
        if (fetched != null)
        {
            _current = fetched;
            SaveCache(key, fetched);
            Changed?.Invoke();
        }
    }

    async Task<Lyrics?> FetchFromWebAsync(string title, string artist, TimeSpan duration)
    {
        try
        {
            var url = $"https://lrclib.net/api/search?track_name={Uri.EscapeDataString(title)}&artist_name={Uri.EscapeDataString(artist)}";
            var json = await Http.GetStringAsync(url);
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Array) return null;

            JsonElement? best = null;
            int bestScore = -1;
            foreach (var e in doc.RootElement.EnumerateArray())
            {
                int s = Score(e, title, artist);
                if (s > bestScore) { bestScore = s; best = e; }
            }
            if (best == null) return null;

            var elem = best.Value;
            string lrc = "";
            if (elem.TryGetProperty("syncedLyrics", out var sl) && sl.ValueKind == JsonValueKind.String)
                lrc = sl.GetString() ?? "";
            if (lrc.Length == 0 && elem.TryGetProperty("plainLyrics", out var pl) && pl.ValueKind == JsonValueKind.String)
                lrc = pl.GetString() ?? "";

            if (lrc.Length == 0) return null;
            return ParseLrc(lrc, title, artist);
        }
        catch { return null; }
    }

    static int Score(JsonElement e, string title, string artist)
    {
        int s = 0;
        string t = e.TryGetProperty("trackName", out var te) && te.ValueKind == JsonValueKind.String ? te.GetString() ?? "" : "";
        string a = e.TryGetProperty("artistName", out var ae) && ae.ValueKind == JsonValueKind.String ? ae.GetString() ?? "" : "";

        if (string.Equals(t, title, StringComparison.OrdinalIgnoreCase)) s += 3;
        else if (t.Length > 0 && (title.Contains(t, StringComparison.OrdinalIgnoreCase) || t.Contains(title, StringComparison.OrdinalIgnoreCase))) s += 1;

        if (string.Equals(a, artist, StringComparison.OrdinalIgnoreCase)) s += 3;
        else if (a.Length > 0 && (artist.Contains(a, StringComparison.OrdinalIgnoreCase) || a.Contains(artist, StringComparison.OrdinalIgnoreCase))) s += 1;

        if (e.TryGetProperty("syncedLyrics", out var sl) && sl.ValueKind == JsonValueKind.String && !string.IsNullOrEmpty(sl.GetString()))
            s += 2;

        return s;
    }

    // ===== 本地缓存 =====
    Lyrics? TryLoadCache(string key, string title, string artist)
    {
        try
        {
            var path = CachePath(key);
            if (!File.Exists(path)) return null;
            var text = File.ReadAllText(path);
            if (text.Trim().Length == 0) return null;
            return ParseLrc(text, title, artist);
        }
        catch { return null; }
    }

    void SaveCache(string key, Lyrics lyrics)
    {
        try
        {
            Directory.CreateDirectory(CacheDir);
            var sb = new StringBuilder();
            foreach (var l in lyrics.Lines)
                sb.Append('[').Append((int)l.Time.TotalMinutes).Append(':')
                  .Append((l.Time.Seconds).ToString("D2")).Append('.')
                  .Append((l.Time.Milliseconds / 10).ToString("D2")).Append(']')
                  .Append(l.Text).Append('\n');
            File.WriteAllText(CachePath(key), sb.ToString());
        }
        catch { /* 缓存失败忽略 */ }
    }

    static string CachePath(string key)
    {
        var hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(key)));
        return Path.Combine(CacheDir, hash + ".lrc");
    }

    // ===== LRC 解析 =====
    static readonly Regex TimeTag = new(@"\[(\d{1,3}):(\d{1,2})(?:[.:](\d{1,3}))?\]", RegexOptions.Compiled);

    static Lyrics ParseLrc(string lrcText, string title, string artist)
    {
        var result = new Lyrics { TrackName = title, ArtistName = artist };
        foreach (var raw in lrcText.Replace("\r", "").Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length == 0) continue;

            var matches = TimeTag.Matches(line);
            if (matches.Count == 0) continue; // 元数据行 [ar:]/[ti:]/[offset:] 等跳过

            int lastIdx = line.LastIndexOf(']');
            string text = lastIdx >= 0 && lastIdx + 1 < line.Length ? line.Substring(lastIdx + 1).Trim() : "";

            foreach (Match m in matches)
            {
                int mm = int.Parse(m.Groups[1].Value);
                int ss = int.Parse(m.Groups[2].Value);
                int ms = m.Groups[3].Success ? ParseMs(m.Groups[3].Value) : 0;
                result.Lines.Add(new LyricLine
                {
                    Time = TimeSpan.FromSeconds(mm * 60 + ss) + TimeSpan.FromMilliseconds(ms),
                    Text = text,
                });
            }
        }
        result.Lines.Sort((a, b) => a.Time.CompareTo(b.Time));
        result.IsSynced = result.Lines.Count > 0;
        return result;
    }

    static int ParseMs(string s)
        => s.Length switch { 1 => int.Parse(s) * 100, 2 => int.Parse(s) * 10, _ => int.Parse(s) };
}

using System.Text.RegularExpressions;

namespace Calendar.Core.Text;

/// <summary>Result of parsing free text for a date/time hint.</summary>
public sealed record ParsedSchedule(string CleanTitle, DateTime? When, bool HasTime);

/// <summary>
/// Lightweight Russian natural-language date/time extractor for quick-add.
/// Recognises: сегодня/завтра/послезавтра, "через N мин/час/дн/недель",
/// дни недели ("в среду"), "5 июня", и время "в 18" / "в 18:30" / "14:30".
/// Returns the remaining text as the clean title.
/// </summary>
public static class NaturalDateParser
{
    public static ParsedSchedule Parse(string input, DateTime now)
    {
        var original = (input ?? string.Empty).Trim();
        if (original.Length == 0) return new ParsedSchedule(original, null, false);

        var title = original;
        var date = now.Date;
        bool hasDate = false, hasTime = false;
        int hour = 9, minute = 0;

        void Take(string pattern, Action<Match> onMatch)
        {
            var m = Regex.Match(title, pattern, RegexOptions.IgnoreCase);
            if (!m.Success) return;
            onMatch(m);
            title = title.Remove(m.Index, m.Length).Insert(m.Index, " ");
        }

        Take(@"\bпослезавтра\b", _ => { date = now.Date.AddDays(2); hasDate = true; });
        Take(@"\bзавтра\b",      _ => { date = now.Date.AddDays(1); hasDate = true; });
        Take(@"\bсегодня\b",     _ => { date = now.Date;           hasDate = true; });

        Take(@"\bчерез\s+(\d+)\s*(минут\w*|мин|час\w*|ч|дн\w*|день|недел\w*)\b", m =>
        {
            var n = int.Parse(m.Groups[1].Value);
            var u = m.Groups[2].Value.ToLowerInvariant();
            DateTime dt;
            if (u.StartsWith("мин")) { dt = now.AddMinutes(n); hasTime = true; }
            else if (u == "ч" || u.StartsWith("час")) { dt = now.AddHours(n); hasTime = true; }
            else if (u.StartsWith("недел")) { dt = now.AddDays(7 * n); }
            else { dt = now.AddDays(n); }
            date = dt.Date;
            if (hasTime) { hour = dt.Hour; minute = dt.Minute; }
            hasDate = true;
        });

        Take(@"\bв(?:о)?\s+(понедельник\w*|вторник\w*|сред\w+|четверг\w*|пятниц\w+|суббот\w+|воскресень\w+)\b", m =>
        {
            var wd = WeekdayFromRu(m.Groups[1].Value);
            if (wd.HasValue) { date = NextWeekday(now.Date, wd.Value); hasDate = true; }
        });

        Take(@"\b(\d{1,2})\s+(январ\w+|феврал\w+|март\w*|апрел\w+|ма[йя]|июн\w+|июл\w+|август\w*|сентябр\w+|октябр\w+|ноябр\w+|декабр\w+)\b", m =>
        {
            var d = int.Parse(m.Groups[1].Value);
            var mon = MonthFromRu(m.Groups[2].Value);
            if (mon > 0 && d is >= 1 and <= 31)
            {
                try
                {
                    var dt = new DateTime(now.Year, mon, d);
                    if (dt.Date < now.Date) dt = dt.AddYears(1);
                    date = dt;
                    hasDate = true;
                }
                catch { /* invalid day for month */ }
            }
        });

        Take(@"\b(?:в|к)\s*(\d{1,2})(?::(\d{2}))?\s*(?:ч\w*)?\b", m =>
        {
            var h = int.Parse(m.Groups[1].Value);
            var mi = m.Groups[2].Success ? int.Parse(m.Groups[2].Value) : 0;
            if (h < 24 && mi < 60) { hour = h; minute = mi; hasTime = true; }
        });

        if (!hasTime)
        {
            Take(@"\b(\d{1,2}):(\d{2})\b", m =>
            {
                var h = int.Parse(m.Groups[1].Value);
                var mi = int.Parse(m.Groups[2].Value);
                if (h < 24 && mi < 60) { hour = h; minute = mi; hasTime = true; }
            });
        }

        DateTime? when = null;
        if (hasDate || hasTime)
        {
            var d = hasDate ? date : now.Date;
            when = new DateTime(d.Year, d.Month, d.Day, hour, minute, 0);
            if (!hasDate && hasTime && when < now) when = when.Value.AddDays(1);
        }

        var clean = Regex.Replace(title, @"\s+", " ").Trim();
        clean = Regex.Replace(clean, @"^(в|к|на|через)\s+|\s+(в|к|на|через)$", " ", RegexOptions.IgnoreCase).Trim();
        clean = Regex.Replace(clean, @"\s+", " ").Trim();
        if (clean.Length == 0) clean = original;

        return new ParsedSchedule(clean, when, hasTime);
    }

    private static DayOfWeek? WeekdayFromRu(string s)
    {
        s = s.ToLowerInvariant();
        if (s.StartsWith("пон")) return DayOfWeek.Monday;
        if (s.StartsWith("вто")) return DayOfWeek.Tuesday;
        if (s.StartsWith("сре")) return DayOfWeek.Wednesday;
        if (s.StartsWith("чет")) return DayOfWeek.Thursday;
        if (s.StartsWith("пят")) return DayOfWeek.Friday;
        if (s.StartsWith("суб")) return DayOfWeek.Saturday;
        if (s.StartsWith("вос")) return DayOfWeek.Sunday;
        return null;
    }

    private static int MonthFromRu(string s)
    {
        s = s.ToLowerInvariant();
        if (s.StartsWith("янв")) return 1;
        if (s.StartsWith("фев")) return 2;
        if (s.StartsWith("мар")) return 3;
        if (s.StartsWith("апр")) return 4;
        if (s.StartsWith("ма"))  return 5;
        if (s.StartsWith("июн")) return 6;
        if (s.StartsWith("июл")) return 7;
        if (s.StartsWith("авг")) return 8;
        if (s.StartsWith("сен")) return 9;
        if (s.StartsWith("окт")) return 10;
        if (s.StartsWith("ноя")) return 11;
        if (s.StartsWith("дек")) return 12;
        return 0;
    }

    private static DateTime NextWeekday(DateTime from, DayOfWeek target)
    {
        var diff = ((int)target - (int)from.DayOfWeek + 7) % 7;
        return from.AddDays(diff);
    }
}

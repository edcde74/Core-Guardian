using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using UnityEngine;

namespace CoreGuardian
{
    // CombatData: CSV parsing helpers.
    public sealed partial class CombatData
    {
        static float F(string s)
        {
            float v;
            if (
                !float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out v)
                || float.IsNaN(v)
                || float.IsInfinity(v)
                || v < 0
            )
            {
                throw new ArgumentException("Invalid non-negative number: " + s);
            }
            return v;
        }

        // Double parse: 8-digit wave IDs exceed float's exact integer range (2^24).
        static int I(string s)
        {
            double v;
            if (
                !double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out v)
                || double.IsNaN(v)
                || double.IsInfinity(v)
                || v < 0
                || v > int.MaxValue
            )
            {
                throw new ArgumentException("Invalid non-negative number: " + s);
            }
            if (v != Math.Floor(v))
            {
                throw new ArgumentException("Expected integer: " + s);
            }
            return (int)v;
        }

        static IEnumerable<string[]> Rows(string text, int columns, int alternate = 0)
        {
            var lines = text.Replace("\r", "")
                .Split('\n')
                .Where(x => !string.IsNullOrWhiteSpace(x) && !x.TrimStart().StartsWith("#"))
                .ToArray();
            if (lines.Length < 2)
            {
                throw new ArgumentException("CSV has no data rows");
            }

            var ids = new HashSet<string>();
            foreach (var l in lines.Skip(1))
            {
                var r = l.Split(',').Select(x => x.Trim()).ToArray();
                if ((r.Length != columns && r.Length != alternate) || r.Any(x => x.Length == 0) || !ids.Add(r[0]))
                {
                    throw new ArgumentException("Invalid/duplicate CSV row: " + l);
                }
                yield return r;
            }
        }

        static T Kind<T>(string value)
            where T : struct
        {
            T result;
            if (!Enum.TryParse<T>(value, true, out result) || !Enum.IsDefined(typeof(T), result))
            {
                throw new ArgumentException("Unknown " + typeof(T).Name + ": " + value);
            }

            return result;
        }

        static bool IsNumericTable(string text)
        {
            foreach (var raw in text.Replace("\r", "").Split('\n'))
            {
                var line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("#"))
                    continue;

                var header = SplitCsv(line);
                return header.Length > 1 && header[1] == "key";
            }
            return false;
        }

        // ---------- Numeric-ID tables ----------
        internal sealed class Table
        {
            public string Name;
            public string[] Header;
            public readonly List<string[]> Rows = new List<string[]>();
            readonly Dictionary<string, int> columns = new Dictionary<string, int>();

            public Table(string name, string text, params string[] required)
            {
                Name = name;
                if (text == null)
                {
                    throw new ArgumentException("Missing CSV: " + name);
                }

                foreach (var raw in text.Replace("\r", "").Split('\n'))
                {
                    var line = raw.Trim();
                    if (line.Length == 0 || line.StartsWith("#"))
                        continue;

                    var cells = SplitCsv(line);
                    if (Header == null)
                    {
                        Header = cells;
                        for (int i = 0; i < cells.Length; i++)
                        {
                            columns[cells[i]] = i;
                        }
                        continue;
                    }
                    if (cells.Length != Header.Length)
                    {
                        throw new ArgumentException(name + ": wrong column count: " + line);
                    }

                    Rows.Add(cells);
                }
                if (Header == null || Rows.Count == 0)
                {
                    throw new ArgumentException(name + ": CSV has no data rows");
                }

                foreach (var column in required)
                {
                    if (!columns.ContainsKey(column))
                    {
                        throw new ArgumentException(name + ": missing column " + column);
                    }
                }
            }

            public bool Has(string column) => columns.ContainsKey(column);

            public string S(string[] row, string column) => columns.TryGetValue(column, out int i) ? row[i] : "";

            public string Required(string[] row, string column)
            {
                var v = S(row, column);
                if (v.Length == 0)
                {
                    throw new ArgumentException(Name + ": empty " + column + " in row " + row[0]);
                }
                return v;
            }

            public float F(string[] row, string column) => CombatData.F(Required(row, column));

            public float F(string[] row, string column, float fallback)
            {
                var v = S(row, column);
                return v.Length == 0 ? fallback : CombatData.F(v);
            }

            public int I(string[] row, string column) => CombatData.I(Required(row, column));

            public int I(string[] row, string column, int fallback)
            {
                var v = S(row, column);
                return v.Length == 0 ? fallback : CombatData.I(v);
            }

            public bool B(string[] row, string column)
            {
                int v = I(row, column, 0);
                if (v > 1)
                {
                    throw new ArgumentException(Name + ": expected 0/1 in " + column);
                }
                return v == 1;
            }

            public int Id(string[] row, int min, int max)
            {
                int id = I(row, "id");
                if (id < min || id > max)
                {
                    throw new ArgumentException(Name + ": ID " + id + " outside " + min + "~" + max);
                }

                return id;
            }
        }

        internal static string[] SplitCsv(string line)
        {
            var cells = new List<string>();
            var cell = new StringBuilder();
            bool quoted = false;
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (quoted)
                {
                    if (c == '"')
                    {
                        if (i + 1 < line.Length && line[i + 1] == '"')
                        {
                            cell.Append('"');
                            i++;
                        }
                        else
                        {
                            quoted = false;
                        }
                    }
                    else
                    {
                        cell.Append(c);
                    }
                }
                else if (c == '"')
                {
                    quoted = true;
                }
                else if (c == ',')
                {
                    cells.Add(cell.ToString().Trim());
                    cell.Clear();
                }
                else
                {
                    cell.Append(c);
                }
            }
            if (quoted)
            {
                throw new ArgumentException("Unterminated quote: " + line);
            }

            cells.Add(cell.ToString().Trim());
            return cells.ToArray();
        }

        int TowerMask(string list)
        {
            int mask = 0;
            if (list.Length == 0)
                return 0;

            foreach (var part in list.Split('|'))
            {
                int id = I(part.Trim());
                if (!TowersById.TryGetValue(id, out var t))
                {
                    throw new ArgumentException("enemies: unknown tower " + id);
                }
                mask |= 1 << (int)t.Kind;
            }
            return mask;
        }

        static void Unique(HashSet<int> ids, int id, string table)
        {
            if (!ids.Add(id))
            {
                throw new ArgumentException(table + ": duplicate ID " + id);
            }
        }

        static (int id, float value) Reference(string text, string table)
        {
            var parts = text.Split('*');
            if (parts.Length != 2)
            {
                throw new ArgumentException(table + ": reference must be ID*value: " + text);
            }

            return (I(parts[0].Trim()), F(parts[1].Trim()));
        }
    }
}

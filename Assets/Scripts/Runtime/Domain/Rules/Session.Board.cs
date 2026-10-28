using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CoreGuardian
{
    // Session: board, route and structure cells; enemy positioning on the route.
    public sealed partial class Session
    {
        public void SetBoard(int width, int height, Vector2Int core, IEnumerable<Vector2Int> route)
        {
            if (width <= 0 || height <= 0)
            {
                throw new ArgumentException("Invalid board");
            }

            this.width = width;
            this.height = height;
            this.core = core;
            structureCells.Clear(); // 새 보드 = 건물 없음 (SetStructures로 다시 지정)
            path.Clear();
            lengths.Clear();
            pathCells.Clear();
            segmentEnds.Clear();
            exactPathIndex = false;
            pathLength = 0;
            foreach (var p in route)
            {
                if (p.x < 0 || p.y < 0 || p.x >= width || p.y >= height)
                {
                    throw new ArgumentException("Path outside board");
                }
                path.Add(p);
            }
            if (path.Count < 2 || path[path.Count - 1] != (Vector2)core)
            {
                throw new ArgumentException("Path must terminate at core");
            }

            for (int i = 1; i < path.Count; i++)
            {
                var a = Vector2Int.RoundToInt(path[i - 1]);
                var b = Vector2Int.RoundToInt(path[i]);
                if (a.x != b.x && a.y != b.y)
                {
                    throw new ArgumentException("Path must be axial");
                }
                float len = Vector2.Distance(a, b);
                if (len <= 0)
                {
                    throw new ArgumentException("Zero length segment");
                }
                lengths.Add(len);
                pathLength += len;
                var dir = new Vector2Int(Math.Sign(b.x - a.x), Math.Sign(b.y - a.y));
                for (var p = a; p != b; p += dir)
                {
                    pathCells.Add(p);
                }
                pathCells.Add(b);
            }
            // Small integer axial lengths make subtracting a segment start exactly equal
            // to the old repeated subtraction for nonnegative travelled distances.
            exactPathIndex = pathLength <= 1048576;
            float end = 0;
            for (int i = 0; i < lengths.Count; i++)
            {
                double axis = Math.Abs((double)path[i + 1].x - path[i].x) + Math.Abs((double)path[i + 1].y - path[i].y);
                if (lengths[i] != axis || axis != Math.Floor(axis))
                {
                    exactPathIndex = false;
                }

                end += lengths[i];
                segmentEnds.Add(end);
            }
        }

        public bool IsBuildable(Vector2Int cell)
        {
            return cell.x >= 0
                && cell.y >= 0
                && cell.x < width
                && cell.y < height
                && !pathCells.Contains(cell)
                && cell != core
                && !structureCells.Contains(cell)
                && !Towers.Any(t => t.Position == cell);
        }

        public void SetStructures(IEnumerable<Vector2Int> cells)
        {
            structureCells.Clear();
            if (cells != null)
            {
                foreach (var c in cells)
                {
                    structureCells.Add(c);
                }
            }
        }

        void PositionEnemy(EnemyHandle e)
        {
            if (e.Slot.Route == 1)
            {
                enemyRevision++;
                PositionFlying(e.Slot);
                return;
            }
            float d = e.PathTravelled;
            if (exactPathIndex && d >= 0 && d <= pathLength)
            {
                int low = 0,
                    high = segmentEnds.Count - 1;
                while (low < high)
                {
                    int mid = low + (high - low) / 2;
                    if (d <= segmentEnds[mid])
                    {
                        high = mid;
                    }
                    else
                    {
                        low = mid + 1;
                    }
                }
                float local = d - (low == 0 ? 0 : segmentEnds[low - 1]);
                e.Position = Vector2.Lerp(path[low], path[low + 1], local / lengths[low]);
                e.PathRemaining = Mathf.Max(0, pathLength - e.PathTravelled);
                return;
            }
            for (int i = 0; i < lengths.Count; i++)
            {
                if (d <= lengths[i])
                {
                    e.Position = Vector2.Lerp(path[i], path[i + 1], d / lengths[i]);
                    e.PathRemaining = Mathf.Max(0, pathLength - e.PathTravelled);
                    return;
                }
                d -= lengths[i];
            }
            e.Position = path[path.Count - 1];
            e.PathRemaining = 0;
        }
    }
}

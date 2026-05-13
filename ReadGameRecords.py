#!/usr/bin/env python3
import argparse
import sys
from dataclasses import dataclass
from datetime import datetime
from typing import Optional


DIFF_ORDER = ("绿", "黄", "红", "紫", "白")


def parse_time_bound(s: str, *, is_end: bool) -> datetime:
    """解析 --from / --to 时间界。支持 / 与 . 作为日期分隔；可只写月-日（缺省为当前年）；可省略时分秒。"""
    raw = s.strip()
    if not raw:
        raise argparse.ArgumentTypeError("时间不能为空")

    last_err: Optional[Exception] = None
    text = raw.replace("T", " ").strip()

    if " " in text:
        head, tail = text.split(None, 1)
        date_part, time_part = (head, tail) if ":" in tail else (text, "")
    else:
        date_part, time_part = text, ""

    date_norm = date_part.replace("/", "-").replace(".", "-")
    try:
        nums = [int(x) for x in date_norm.split("-") if x != ""]
    except ValueError as e:
        last_err = e
        nums = []

    year: Optional[int] = None
    month: Optional[int] = None
    day: Optional[int] = None
    if len(nums) == 3:
        year, month, day = nums[0], nums[1], nums[2]
    elif len(nums) == 2:
        year = datetime.now().year
        month, day = nums[0], nums[1]

    if year is not None and month is not None and day is not None:
        h, mi, sec = 0, 0, 0
        tp = time_part.strip()
        time_ok = True
        if tp:
            try:
                tsegs = [int(x) for x in tp.split(":")]
            except ValueError:
                time_ok = False
            else:
                if len(tsegs) == 3:
                    h, mi, sec = tsegs[0], tsegs[1], tsegs[2]
                elif len(tsegs) == 2:
                    h, mi = tsegs[0], tsegs[1]
                    sec = 59 if is_end else 0
                elif len(tsegs) == 1:
                    h = tsegs[0]
                    mi, sec = (59, 59) if is_end else (0, 0)
                else:
                    time_ok = False
        if time_ok:
            try:
                dt = datetime(year, month, day, h, mi, sec)
            except ValueError as e:
                last_err = e
            else:
                if not tp and is_end:
                    return dt.replace(hour=23, minute=59, second=59)
                return dt

    fmts = ("%Y-%m-%d %H:%M:%S", "%Y-%m-%d %H:%M", "%Y-%m-%d")
    for fmt in fmts:
        try:
            dt = datetime.strptime(raw, fmt)
            if fmt == "%Y-%m-%d" and is_end:
                return dt.replace(hour=23, minute=59, second=59)
            return dt
        except ValueError as e:
            last_err = e
    raise argparse.ArgumentTypeError(f"无法解析时间: {s!r} ({last_err})") from last_err


def parse_line(line: str) -> Optional[tuple[datetime, str, int, str, list[str]]]:
    line = line.strip()
    # 任意行首可能出现的 UTF-8 BOM（U+FEFF）；无 BOM 时为空操作
    line = line.lstrip("\ufeff").strip()
    if not line:
        return None
    parts = line.split("\t")
    if len(parts) < 5:
        return None
    try:
        ts = datetime.strptime(parts[0], "%Y-%m-%d %H:%M:%S")
    except ValueError:
        return None
    kind = parts[1].strip()
    try:
        mid = int(parts[2].strip())
    except ValueError:
        return None
    title = parts[3]
    diff = parts[4].strip()
    return ts, kind, mid, diff, parts, title


@dataclass
class Agg:
    title: str
    select_song: int = 0
    game_start: int = 0
    finish: int = 0
    skip: int = 0
    ach_sum: float = 0.0
    ach_n: int = 0
    fc_plus: int = 0  # FC / FC+ / AP / AP+

    def __init__(self, title: str):
        self.title = title

    def add_achievement(self, ach: float, combo: str) -> None:
        self.ach_sum += ach
        self.ach_n += 1
        c = combo.strip()
        if c in ("FC", "FC+", "AP", "AP+"):
            self.fc_plus += 1


def main() -> int:
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except (AttributeError, OSError, ValueError):
        pass

    p = argparse.ArgumentParser(description="按歌曲与难度聚合 GameRecords 日志统计")
    p.add_argument("path", help="GameRecords.txt 的路径")
    p.add_argument("--from", dest="time_from", type=lambda s: parse_time_bound(s, is_end=False), help="参与统计的最早时间（含当天）")
    p.add_argument("--to", dest="time_to", type=lambda s: parse_time_bound(s, is_end=True), help="参与统计的最晚时间（含当天）")
    p.add_argument("--minId", dest="min_id", type=int, help="参与统计的最小歌曲 id")
    p.add_argument("--maxId", dest="max_id", type=int, help="参与统计的最大歌曲 id")
    args = p.parse_args()

    try:
        f = open(args.path, encoding="utf-8", errors="replace")
    except OSError as e:
        print(f"无法打开文件: {args.path}: {e}", file=sys.stderr)
        return 1

    buckets: dict[tuple[int, str], Agg] = {}
    with f:
        for raw in f:
            parsed = parse_line(raw)
            if parsed is None:
                continue
            ts, kind, mid, diff, parts, title = parsed
            nonDxId = mid % 10000
            if args.time_from is not None and ts < args.time_from:
                continue
            if args.time_to is not None and ts > args.time_to:
                continue
            if args.min_id is not None and nonDxId < args.min_id:
                continue
            if args.max_id is not None and nonDxId > args.max_id:
                continue

            key = (mid, diff)
            a = buckets.setdefault(key, Agg(title))
            if kind == "选歌":
                a.select_song += 1
            elif kind == "开局":
                a.game_start += 1
            elif kind == "结束":
                a.finish += 1
                if len(parts) >= 7:
                    try:
                        ach = float(parts[5].strip())
                    except ValueError:
                        pass
                    else:
                        a.add_achievement(ach, parts[6])
            elif kind in ("跳关", "跳歌"):
                a.skip += 1

    # (歌曲id, 难度序) 升序
    ordered = sorted(buckets.keys(), key=lambda k: (k[0] % 10000, k[0] // 10000, DIFF_ORDER.index(k[1])))

    for mid, diff in ordered:
        a = buckets[(mid, diff)]
        avg_ach = a.ach_sum / a.ach_n if a.ach_n else 0.0
        fc_rate = (100.0 * a.fc_plus / a.finish) if a.finish else 0.0
        restart = a.game_start - a.select_song

        lines_out = [
            f"{a.title} ({mid})-{diff}",
            f"选歌次数:{a.select_song}",
            f"正常打完次数:{a.finish}",
            f"平均达成率:{avg_ach:.4f}%",
            f"FC率:{fc_rate:.1f}%",
            f"跳关次数:{a.skip}",
        ]
        if restart != 0: lines_out.append(f"重开次数:{restart}")
        print("\t".join(lines_out))

    return 0


if __name__ == "__main__":
    main()

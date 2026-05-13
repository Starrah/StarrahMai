using System.Globalization;
using System.Text;
using AquaMai.Mods.UX;
using HarmonyLib;
using MAI2.Util;
using Manager;
using MelonLoader;
using Process;

namespace StarrahMai;

/**
 * 记录每首歌的游玩次数、达成率等信息。
 * 格式：（每条事件一行，相邻两个字段间以 \t 作为分隔）
 * 时间 (选歌|开局|跳关|结束) 歌曲id 歌曲名字 (绿|黄|红|紫|白) [达成率] [None|FC|FC+|AP|AP+] [None|Sync|FS|FS+|FDX|FDX+] [DXScore]
 * 可配套项目中附带的“ReadGameRecords.py”来分析
 */
public static class GameRecords
{
    public static readonly string SavePath = "GameRecords.txt";

    private static readonly string[] DifficultyLabel = ["绿", "黄", "红", "紫", "白", "?"];
    private static readonly string[] ComboLabel = ["None", "FC", "FC+", "AP", "AP+"];
    private static readonly string[] SyncLabel = ["None", "FS", "FS+", "FDX", "FDX+", "Sync"];
    
    private static string BuildLineForMonitor(int monitorIndex, string eventKind, bool includeScores)
    {
        int musicId = GameManager.SelectMusicID[monitorIndex];
        int difficulty = GameManager.SelectDifficultyID[monitorIndex];
        var music = Singleton<DataManager>.Instance.GetMusic(musicId);
        string title = music != null ? music.name.str : "";

        if (includeScores && Singleton<GamePlayManager>.Instance.GetGameScore(monitorIndex).IsTrackSkip)
        {
            eventKind = "跳关";
            includeScores = false;
        }

        var sb = new StringBuilder(128);
        sb.Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
        sb.Append('\t').Append(eventKind);
        sb.Append('\t').Append(musicId);
        sb.Append('\t').Append(title);
        sb.Append('\t').Append(DifficultyLabel[difficulty]);

        if (includeScores)
        {
            var scoreObj = Singleton<GamePlayManager>.Instance.GetGameScore(monitorIndex);
            int ach = GameManager.ConvAchiveDecimalToInt(scoreObj.GetAchivement());
            sb.Append('\t').Append($"{ach / 10000}.{ach%10000:D4}");
            sb.Append('\t').Append(ComboLabel[(int)scoreObj.ComboType]);
            sb.Append('\t').Append(SyncLabel[(int)scoreObj.SyncType]);
            sb.Append('\t').Append(scoreObj.DxScore);
        }

        sb.AppendLine();
        return sb.ToString();
    }

    private static void WriteLog(string eventKind, bool includeScores = false)
    {
        var result = "";
        for (int m = 0; m < 2; m++)
        {
            MelonLogger.Msg($"{eventKind}, {m}, {Singleton<UserDataManager>.Instance.GetUserData(m)}, {Singleton<UserDataManager>.Instance.GetUserData(m)?.IsEntry}"); // TODO
            if (!Singleton<UserDataManager>.Instance.GetUserData(m).IsEntry) continue;
            var line = BuildLineForMonitor(m, eventKind, includeScores);
            if (line != null) result += line;
        }
        try
        {
            File.AppendAllText(SavePath, result, Encoding.UTF8);
        }
        catch (Exception ex)
        {
            MelonLogger.Warning($"[GameRecord] Failed to write log: {ex.Message}");
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(TrackStartProcess), nameof(TrackStartProcess.OnStart))]
    public static void AfterTrackStartProcessOnStart()
    {
        WriteLog("选歌");
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(GameProcess), nameof(GameProcess.OnStart))]
    public static void AfterGameProcessOnStart()
    {
        WriteLog("开局");
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ResultProcess), nameof(ResultProcess.OnStart))]
    public static void BeforeResultProcessOnStart()
    {
        if (DontRuinMyAccount.ignoreScore)
        {
            MelonLogger.Msg("由于本局触发了DontRuinMyAccount，本局将不记录游戏结束的日志。");
            return;
        }
        WriteLog("结束", includeScores: true);
    }
}
using System.Diagnostics.CodeAnalysis;
using System.Net.WebSockets;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using AquaMai.Config.Types;
using AquaMai.Core.Helpers;
using HarmonyLib;
using Main;
using MelonLoader;
using MelonLoader.TinyJSON;
using Monitor;
using Process;
using UnityEngine;
using Random = System.Random;

namespace StarrahMai;

public static class OBSSave
{
    // 一键保存录像回放的按钮。
    // 如果配合ADXHIDIOMod和本Mod的MaimollerCoin功能使用，则应设为F3。
    // 如果配合AquaMai的Maimoller IO使用，则应设为AquaMai提供的控制器“自定义功能键”。
    public static readonly (KeyCodeOrName, bool) Key = (KeyCodeOrName.CustomFn1, false);

    private static OBSWebsocket obs = new();

    private static string configFile = Path.Combine(Environment.CurrentDirectory, "StarrahMai.OBSSave.config.json");

    public static bool autoSave = false; // 是否每局结束（退出成绩展示界面时）自动保存录像。可以在配置文件中进行配置。

    [HarmonyPrepare]
    public static bool Prepare(MethodBase original)
    {
        if (original != null) return true; // 只对类prepare进行处理，如果是具体patch method的prepare，不做处理

        // 尝试读取配置文件（其中保存了URL和密码）。如果没有的话，就生成该文件为随机内容。
        if (!File.Exists(configFile))
        {
            var randomConfig = new Dictionary<string, object>
            {
                ["URL"] = "ws://localhost:4455",
                ["password"] = GenerateRandomPassword(),
                ["autoSave"] = false
            };
            File.WriteAllText(configFile, JSON.Dump(randomConfig, EncodeOptions.PrettyPrint));
            MelonLogger.Warning(
                $"[OBSSave] 未检测到用于配置OBS Websocket的URL和密码的配置文件，故生成了随机密码的新配置文件，位于：{Path.GetFullPath(configFile)}");
            MelonLogger.Warning($"[OBSSave] 请修改该配置文件、填入正确的URL和密码，或在OBS侧将密码修改为与配置文件中随机生成的新密码一致！");
        }

        var config = JSON.Load(File.ReadAllText(configFile)) as ProxyObject;
        string URL = config["URL"], password = config["password"];
        if (config.TryGetValue("autoSave", out var item)) autoSave = item;

        Task.Run(async () =>
        {
            try
            {
                await obs.Connect(URL, password);
            }
            catch (Exception e)
            {
                MelonLogger.Error($"[OBSSave] 连接失败！ {e}");
                MelonLogger.Warning($"[OBSSave] 请确保已在配置文件中配置了正确的URL和密码，配置文件位于：{configFile}");
            }
        });

        return true;
    }

    public static async Task StopThenStartReplayBuffer()
    {
        try
        {
            await obs.SendRequest("StopReplayBuffer");
            await Task.Delay(500);
        }
        catch
        {
            // 关闭操作即使发生异常也无所谓，直接忽略
        }
        
        // 检查确认回放缓存已关闭完成，避免还在关闭中状态时就调用开启接口、导致失败
        const int MAX_RETRY = 10; // 每隔300ms重复检查，最多重试10次
        for (int _ = 0; _ < MAX_RETRY; _++)
        {
            var res = await obs.SendRequest("GetReplayBufferStatus");
            if (res["outputActive"]) await Task.Delay(300); // 返回开启中状态、说明还未完成关闭，继续等待
            else break; // 已完成关闭
        }

        try
        {
            await obs.SendRequest("StartReplayBuffer");
            MelonLogger.Msg("[OBSSave] 歌曲开始，已自动重置OBS回放缓存");
        }
        catch (Exception e)
        {
            MelonLogger.Error($"[OBSSave] 重置OBS回放缓存失败！{e}");
        }
    }

    public static async Task SaveReplayBuffer(GameObject monitorForDisplayToast = null)
    {
        try
        {
            await obs.SendRequest("SaveReplayBuffer");
            MelonLogger.Msg("[OBSSave] 本局录像已保存");
            if (monitorForDisplayToast != null) Toast.ShowToast(monitorForDisplayToast, "本局录像已保存");
        }
        catch (Exception e)
        {
            MelonLogger.Error($"[OBSSave] 录像保存失败！ {e}`");
            if (monitorForDisplayToast != null) Toast.ShowToast(monitorForDisplayToast, $"录像保存失败！{e.Message}");
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(TrackStartProcess), "OnStart")]
    public static void TrackStartProcessOnStart()
    {
        Task.Run(StopThenStartReplayBuffer);
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(GameMainObject), "Update")]
    public static void OnUpdate(Transform ___leftMonitor)
    {
        if (!KeyListener.GetKeyDownOrLongPress(Key.Item1, Key.Item2)) return;
        Task.Run(() => SaveReplayBuffer(___leftMonitor.gameObject));
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ResultProcess), "ToNextProcess")]
    public static void OnResultProcessEnd(ResultMonitor[] ____monitors)
    {
        if (!autoSave) return;
        // 如果autoSave已被开启，则自动保存录像
        Task.Run(() => SaveReplayBuffer(____monitors[0].gameObject));
    }

    // 生成随机密码的辅助方法，AI写的
    private static string GenerateRandomPassword(int length = 16)
    {
        const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }
}

/**
 * 官方文档： https://github.com/obsproject/obs-websocket/blob/master/docs/generated/protocol.md
 * 纯手工无AI（其实是AI写的太烂了，异步包装包不明白甚至有些地方协议实现都是错的）
 */
[SuppressMessage("ReSharper", "ParameterHidesMember")]
class OBSWebsocket
{
    private ClientWebSocket ws;

    private string url;
    private string password;

    public async Task Connect(string url, string password)
    {
        this.url = url;
        this.password = password;
        await Reconnect();
    }

    private async Task Reconnect()
    {
        if (ws != null) ws.Dispose();
        ws = new ClientWebSocket();
        await ws.ConnectAsync(new Uri(this.url), CancellationToken.None);
        await Authenticate(this.password);
        MelonLogger.Msg("[OBSSave] 已连接到OBS Websocket服务器");
    }

    private async Task<string> ReceiveAsync(CancellationToken? ct = null)
    {
        var buffer = new byte[4096];
        var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), ct ?? CancellationToken.None);
        if (result.MessageType == WebSocketMessageType.Close)
            throw new Exception($"Websocket被对端请求关闭！({result.CloseStatus}) {result.CloseStatusDescription}");
        return Encoding.UTF8.GetString(buffer, 0, result.Count);
    }

    private Task SendAsync(string text, CancellationToken? ct = null)
    {
        return ws.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(text)), WebSocketMessageType.Text, true,
            ct ?? CancellationToken.None);
    }

    /**
     * @param strict 如果为true，则要求必须第一个包就是符合expectOp的，否则直接抛异常；如果为false，则会一直循环收包、直到收到符合expectOp的为止。
     */
    public async Task<ProxyObject> ReceiveData(int? expectOp, bool strict = true)
    {
        while (true)
        {
            var msg = await ReceiveAsync();
            ProxyObject obj = JSON.Load(msg) as ProxyObject;
            // ReSharper disable once PossibleNullReferenceException
            int op = obj["op"];
            if (expectOp == null || op == expectOp)
            {
                return obj["d"] as ProxyObject;
            }
            else if (strict)
            {
                throw new Exception($"收到的包不符合预期！预期op={expectOp}，实际却收到了{op}");
            }
        }
    }

    public async Task SendData(int op, object data)
    {
        var packet = new Dictionary<string, object>
        {
            ["op"] = op,
            ["d"] = data
        };
        await SendAsync(JSON.Dump(packet));
    }

    private async Task Authenticate(string password)
    {
        // 根据协议，首先会收到一个op=0 Hello包
        var helloMsg = await ReceiveData(0);

        var identifyMsg = new Dictionary<string, object>
        {
            ["rpcVersion"] = 1, // 写死RPC版本，
            ["eventSubscriptions"] = 0 // 不订阅任何事件，防止收到Event
        };

        // 根据密码计算authencitation
        Variant authObj;
        if (helloMsg.TryGetValue("authentication", out authObj))
        {
            // hello中有authentication时才需要密码校验
            string salt = authObj["salt"];
            string challenge = authObj["challenge"];

            // 具体计算是AI写的
            // 步骤 1: 连接密码和盐值，计算SHA256哈希，然后Base64编码
            string secretString = password + salt;
            byte[] secretHash;
            using (SHA256 sha256 = SHA256.Create())
            {
                secretHash = sha256.ComputeHash(Encoding.UTF8.GetBytes(secretString));
            }

            string base64Secret = Convert.ToBase64String(secretHash);
            // 步骤 2: 连接base64Secret和挑战码，计算SHA256哈希，然后Base64编码
            string challengeString = base64Secret + challenge;
            byte[] challengeHash;
            using (SHA256 sha256 = SHA256.Create())
            {
                challengeHash = sha256.ComputeHash(Encoding.UTF8.GetBytes(challengeString));
            }

            string authenticationString = Convert.ToBase64String(challengeHash);

            identifyMsg["authentication"] = authenticationString;
        }

        await SendData(1, identifyMsg); // 发送op=1 Identify包

        var identifiedMsg = await ReceiveData(2); // 如果连接成功会收到op=2 Identified包
        if (identifiedMsg["negotiatedRpcVersion"] != 1) throw new Exception("连接错误，服务器使用了不支持的RpcVersion！");
    }

    public class RPCRequestException : Exception
    {
        public int code;
        public string comment = "";

        public RPCRequestException(ProxyObject requestStatus)
        {
            code = requestStatus["code"];
            if (requestStatus.TryGetValue("comment", out var item)) comment = item;
        }

        public override string Message => $"调用OBS RPC操作失败！({code}) {comment}";
    }

    public async Task<ProxyObject> SendRequest(string name, object data = null, bool allowRetry = true)
    {
        string requestId = Guid.NewGuid().ToString();
        var packet = new Dictionary<string, object>
        {
            ["requestType"] = name,
            ["requestId"] = requestId
        };
        if (data != null) packet["requestData"] = data;

        try
        {
            await SendData(6, packet);
        }
        catch (Exception e)
        {
            if (!allowRetry) throw;
            MelonLogger.Warning(e);
            MelonLogger.Warning("[OBSSave] 连接断开，正在尝试重连...");
            await Reconnect();
            return await SendRequest(name, data, false);
        }

        ProxyObject response;
        while (true)
        {
            try
            {
                response = await ReceiveData(7);
            }
            catch (Exception e)
            {
                if (!allowRetry) throw;
                MelonLogger.Warning(e);
                MelonLogger.Warning("[OBSSave] 连接断开，正在尝试重连");
                await Reconnect();
                return await SendRequest(name, data, false);
            }

            if (response["requestId"] == requestId)
            {
                if (response["requestStatus"]["result"])
                {
                    Variant responseData;
                    if (response.TryGetValue("responseData", out responseData)) return responseData as ProxyObject;
                    else return null;
                }
                else
                {
                    throw new RPCRequestException(response["requestStatus"] as ProxyObject);
                }
            }
        }
    }
}
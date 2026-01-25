# [StarrahMai](https://github.com/Starrah/StarrahMai)
> 个人自用Sinmai Mod，作为AquaMai等的的补充。

### 当前功能列表
- Autoplay：对AquaMai的Autoplay支持的增强。
  - Service键短按：完全开启或完全关闭Autoplay（而不是按一次切换一个模式）
  - Service键长按：开启Autoplay后马上关闭（用于手动强制触发DontRuinMyAccount）
  - 以上两个功能都是在歌曲开始前的封面等信息显示的页面起就可以使用了，而无需像AquaMai一样必须等到进谱面。
- MaimollerCoin：Maimoller的Coin键可以用于AquaMai的功能中了。
  - 适用范围：所有能够指定为Test/Service键的地方，比如练习模式/快速重试切歌等等。
  - 方法：在AquaMai里将相应功能的按键指定为F3，即可实现用Maimoller的Coin键（FN3）控制该功能。
    - 键盘上原有的F3键会失效，不能在游戏中使用。
- LinuxPatch：纯个人自用，用于使在游戏在Linux+Wine环境下能够正常运行与游玩的一些功能（除了我应该不会有什么人这么闲到在Wine下面折腾HDD吧......）
  - 不过你**不必刻意关闭此模块**，这个模块内置了检查当前是否为Wine环境、如果不是的话自动不生效，因此在一般Windows环境下也不会有问题。
- OBSSave：调用OBS的API，实现一键或每首歌自动，保存OBS的回放缓存。
  - 如何启用：
    1. 首先你要有一个OBS（本机或者局域网内其他机器都可以），打开好“回放缓存”功能。
    2. **重要！** 设置-快捷键-回放缓存-保存回放，**设置一个快捷键**，设什么都行。这是调用回放缓存相关API时可以成功的前提（为什么OBS要有这种奇怪的设定我也不知道）。
    3. 在OBS中，工具-Websocket服务器设置-开启Websocket服务器，然后“查看连接信息”把密码复制下来，一会会用到。
    4. 打好Mod后运行一次游戏，会生成一个名为`StarrahMai.OBSSave.config.json`的配置文件，在其中填入OBS服务器的地址（本机运行情况下保持默认生成的默认值即可，如果是其他机则需要修改），以及OBS侧设置的Websocket服务器密码。
    5. 运行游戏，Log中会出现`[OBSSave] 已连接到OBS Websocket服务器`的话，就说明功能起来了。
  - 工作机制&使用方法：
    - 每个Track开始时，会重置回放缓存中的内容。这确保了每次保存出来的视频，都是刚好从这首歌的开头开始的。
    - 按下绑定的按键（只能在代码中设置，见`OBSSave.cs:OBSSave.Key`），即可保存回放缓存到硬盘。
      - 默认按键为F3，以便和MaimollerCoin联动，使用机台上的按键控制。
    - 此外，`StarrahMai.OBSSave.config.json`配置文件中有一个字段`autoSave`。
      - 如果设为true，则在每首歌之后、退出成绩显示页面时，会自动触发一次保存。（也就是连同打歌的过程和成绩页面都保存下来）

### 编译和使用
1. Libs里放入`AMDaemon.NET.dll`、`Assembly-CSharp.dll`、`Assembly-CSharp-firstpass.dll`。
2. 然后用IDE打开并编译（对不起我真懒得写脚本了），基本上用任何IDE都能直接编译，Visual Studio和Jetbrains Rider，都是直接导入项目就可以了。
3. 编译出的`StarrahMai.dll`文件放到`Package/Mods`目录下。

### 如需开关功能/配置
- 需要直接在源码中做调整来进行配置。个人自用，懒得写文本配置模块了。
- 开关功能：`Core.cs`:`OnInitializeMelon`，注释掉相应的模块的`loadModule`即可。
- 调整配置：
  - 可配置项一般以`public static readonly`的形式放在相应模块的类的开头，直接修改赋值即可。

### 验证Mod已经正确运行
- 游戏启动后，MelonLoader的Log窗口中会有Log：`[StarrahMai] 已加载`，同时也会打印出加载的模块的列表。

### PS：为什么会有这个Mod，而不是全都PR进AquaMai
- 这个Mod里放的大多是一些，我很喜欢，但是想了想又觉得不是人人都需要/大部分人都可能没那么感兴趣的东西，这种东西拿去AquaMai提PR可能也没那么合适。
  - 我评估觉得很重要的、大部分人都需要，也大概率值得被merge的东西，还是会像原来一样PR给AquaMai的。
- 也有可能是时，我没时间/懒得写各种不同的配置了，因为要给大众使用总要有完善的可配置属性。我自己用的话写死直接改代码重编就完事了x）
- 所以如果相关人员认为合适的话，非常欢迎AquaMai随时收编这里面的功能。

### License
- [MIT License](https://starrah2.mit-license.org) © 2025 Starrah
  - 允许任意自由编译、使用、修改、派生
  - 如果重分发源代码（发给他人等），应带着本README文件一起发过去；如果重分发DLL，不得删除DLL里MelonInfo编码进去的作者信息。
  - 允许和欢迎包括但不限于AquaMai在内的任何Mod收编全部或部分功能
    - 若如此做，请把`Starrah<starrah@foxmail.com>`列进相关commit的author。
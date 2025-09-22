# [StarrahMai](https://github.com/Starrah/StarrahMai)
> 个人自用Sinmai Mod，作为AquaMai等的的补充。

### 当前功能列表
- Autoplay相关：
  - Service键短按：完全开启或完全关闭Autoplay（而不是按一次切换一个模式）
  - Service键长按：开启Autoplay后马上关闭（用于手动强制触发DontRuinMyAccount）
  - 以上两个功能都是在歌曲开始前的歌名显示页面起就可以使用了，而无需像AquaMai一样必须等到进谱面。

### 编译和使用
1. Libs里放入AMDaemon.NET.dll、Assembly-CSharp.dll、Assembly-CSharp-firstpass.dll。
2. 然后用IDE打开并编译（对不起我真懒得写脚本了），基本上用任何IDE都能直接编译，Visual Studio和Jetbrains Rider，都是直接导入项目就可以了。
3. 编译出的DLL文件放到Package/Mods目录下。

### 如需配置
- 建议直接在源码中开启/关闭相应代码来进行配置。个人自用，懒得写文本配置模块了。

### 验证Mod已经正确运行
- 游戏启动后，MelonLoader的Log窗口中会有Log：`[StarrahMai] Initialized.`

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
# Honor Tune

给💊笔记本用的性能调节桌面应用。单文件 WinForms，免安装、免运行库、不联网。

在 **HONOR MagicBook DRA-XX（DarwinA / Core Ultra 5 125H + RTX 4060）** 上开发并逐项实测。

![概览](docs/overview.png)

---

## 一、它能做什么

### 档位（直接走 EC）

点一下切换💊自己的性能模式，和电脑管家是同一套接口：

| 档位 | 💊模式值 | 电源计划 |
|---|---|---|
| 智能档 | `0` | Windows 平衡 |
| 性能档 | `1` | Honor Performance |
| 高能档 | `3` | Honor Performance |

切换成功后会自动把 Windows 电源方案一起切过去，不用再去设置里手动改。

### 风扇转速（仅在 高能档 生效）

五个档位：静音 / 偏静 / 标准 / 偏冷 / 强冷。

这不是写注册表做样子——是真的下发到 EC。实测同样 8 核满载 40 秒：

| 档位 | 风扇转速 |
|---|---|
| 静音 | 2460 / 2220 RPM |
| 强冷 | 3060 / 2820 RPM |

**性能档下指令虽然返回成功，但风扇不跟随**，所以非高能档时按钮是禁用的。

### GPU 超频（侧栏第二项）

可以改独显的核心、显存频率偏移，走的是和猎人营地同一条 NVAPI：

- `nvapi64.dll` → `NvAPI_GPU_SetPstates20`
- 只抬 P0，核心 domain `0`，显存 domain `4`
- 界面是 MHz，下发前乘 1000 变成 kHz

这张 4060 的滑条收在核心 0–200 MHz、显存 0–400 MHz。拖完要点「应用偏移」才写入驱动，这时会申请一次 UAC。没提权的话驱动会拒绝，滑条上的数不会进显卡。还原为 0 会把偏移清掉。

偏移不进 BIOS，重启就没了。开了后台服务的话，服务会按上次成功的数值再写一次。

页面上还有两个荣耀预设：`荣耀 150/300`、`一键 200/400`，点一下把数值填进滑条，仍然要点「应用偏移」才写显卡。

### 顺带下发的功耗墙

点「应用偏移」写完频率后，会和营地「应用一次预设」一样，把荣耀那组功耗墙一起写一次：

| 项 | 值 | 来源 |
| --- | --- | --- |
| TGP | 100 W | `HunterCampCustomMode.xml` 的 `5060/5070` 档 `MaxTGP` |
| 温度墙 | 87 ℃ | 同档 `MaxTemperature` |
| CPU 功耗 | 140 W | 同档 `CPUWattage` |

TGP 和温度墙本来就是 EC 按档位管的：切智能/性能/高能时，荣耀那条 `04/0F` 模式指令一下去，EC 自己就套用对应档位。这里写同一组数，只是为了被营地/管家改过之后能回到荣耀的状态。

写入走 `root\wmi` → `OemWMIMethod` → `OemWMIfun`，group `0x03` / cmd `0x17`，负载 `[3]=TGP-60`、`[4]=温度墙`、`[5]=CPU 瓦数`，不用管理员。

营地反汇编出来的写法是 `[2]=0xFF`，但这台机器上 EC 会回 `1`（拒绝）。所以程序按 A/B/C/D 四种排布依次试，成功的那个记进 `config.ini` 的 `[power] variant`，之后直接用。四种排布的对照可以用 `HonorTune.exe --power-diag` 打印，输出在 `%APPDATA%\HonorTune\power-diag.txt`。

EC 里存的值**读不回来**。要确认是否生效，开个负载看 `nvidia-smi --query-gpu=power.limit` 有没有变成 100 W。

「还原为 0」只清频率，不动功耗墙；后台服务重设时也照营地服务的样子只重设超频，不碰 TGP。

### 实时监控

CPU 占用（带曲线）、内存、CPU 实时频率、GPU 温度、电池、刷新率，以及两颗风扇的实际转速。

> CPU 频率读的是 `Processor Information\Actual Frequency`，和任务管理器一致。
> 这颗混合架构 CPU 上 `Processor Frequency` 会一直显示 0.92 GHz，是错的。

### 细节调节

- **刷新率** —— 列出当前分辨率支持的全部刷新率，点了立即生效；**15 秒内不确认自动回滚**，防止改黑了没法救。
- **界面动画** —— 关闭最小化/菜单淡入/任务栏动画，可调菜单显示延迟 0–400 ms。
- **后台应用** —— 限制后台运行（对应「设置 › 隐私和安全性 › 后台应用」）。
- **当前生效设置** —— 只读，直接读系统真实值。
- **恢复到初始状态** —— 把改动全部还原。
- **后台服务** —— 记住上次选的性能档和风扇转速。开机后由 Windows 服务重写，不弹出窗口；档位被管家或固件打掉时会再写回去。安装或卸载服务时要一次 UAC，平时开界面不用管理员。

### 电池保护（侧栏第三项）

改的是荣耀 EC 里那组充电阈值，和荣耀电脑管家改的是同一组值：

- 读 `group 3 / cmd 0x11`（参数 `0xFF`），回包 `[1]=恢复%`、`[2]=上限%`
- 写 `group 3 / cmd 0x10`，`[2]=恢复%`、`[3]=上限%`
- 写完立刻读回来核对，值不一致就算失败

三档预设：家用 70/40、办公 90/70、差旅 100/95；右上角开关关掉就是一直充到 100%。

**插电自动切档**：不插电时强制智能档，并禁用概览页的性能档/高能档；插上电源且电量高于 20% 时，切回之前手动选的档位。这个逻辑主界面和后台服务都会执行。

阈值存在 EC 里，重启、换系统都还在，不需要服务重设。

![细节调节](docs/detail.png)

---

## 二、怎么和💊的接口打通的

💊没有公开接口。这里的参数来自对已安装程序的静态分析和实测，不是猜的。

| 项目 | 实现 |
|---|---|
| 读💊模式 | WMI `root\wmi` → `OemWMIMethod::OemWMIfun`，输入 64 字节，`04 / 0E` |
| 写💊模式 | 同上，`04 / 0F`，第 3 字节为模式值，写完回读校验 |
| 读风扇转速 | 同上，`02 / 08`，第 3 字节为风扇序号，返回低字节在前 |
| 写风扇档位 | 同上，`07 / <input1> / <input2>` |
| 风扇参数 | 运行期解析 `config\FanSpeedConfig.dat`，按机型取 |

### 风扇参数是按机型读的，没有写死

`FanSpeedConfig.dat` 是「前 256 字节明文 + 加密段」的格式，每个字节都加了 `0x80`。减去 `0x80` 就能读到本机那一条：

```xml
<Product MachineType="DarwinA" input1="0x08">
  <Fan index="0" input2="0xAA"/>
  <Fan index="1" input2="0xAB"/>
  <Fan index="2" input2="0xAC"/>
  <Fan index="3" input2="0xAD"/>
  <Fan index="4" input2="0xAE"/>
```

机型名从 `HKLM\SOFTWARE\HONOR\PCManager\ProductName` 读，所以在别的💊机型上跑，取到的是那台机器自己的参数。

明文段可能正好截在最后一档中间（本机就是），这时若前四档是等差，会按同样步长补出第五档；补不出来或找不到机型时按钮直接禁用，不会乱发指令。

### Honor Performance 的 GUID

`b8a2c9f4-7d3e-4a1b-9c2f-5e8d6a3b1c4f` 不是本机随机生成的，它写死在 `PerfCommonPlugin.dll` 的 `HonorPowerPlan::EnterHighPerfMode` 里，各机型一致。

程序仍然按「先这个固定 GUID → 再按名字找 → 再试模板 GUID → 最后退 Windows 高性能」的顺序解析，免得某个机型上方案被改名或没建时切错。

### 其它

| 功能 | 实现 |
|---|---|
| 切换电源方案 | `PowerSetActiveScheme` |
| 刷新率 | `EnumDisplaySettings` 枚举 + `ChangeDisplaySettingsEx` |
| 界面动画 | HKCU `MinAnimate` / `MenuShowDelay` / `TaskbarAnimations` + `WM_SETTINGCHANGE` |
| 后台限制 | HKCU `BackgroundAccessApplications\GlobalUserDisabled` |
| GPU 温度 | `nvidia-smi` |
| GPU 频率偏移 | `nvapi64` → `NvAPI_GPU_SetPstates20`，核心/显存 P0 偏移，MHz×1000 |

---

## 三、编译

不需要装 SDK，用系统自带的 .NET Framework 编译器：

```
build.cmd
```

或者手动：

```
%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe /codepage:65001 /target:winexe ^
  /win32icon:app.ico /out:HonorTune.exe HonorTune.cs ^
  /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.Management.dll /r:System.ServiceProcess.dll
```

C# 5 语法，任何 Windows 10/11 自带的 .NET Framework 4.x 都能跑。

---

## 四、运行

**不要**从带 Low 完整性标签的目录（例如某些沙箱工作区）直接启动，那样写不了 `%APPDATA%`，也建不了电源方案。复制到一般目录运行：

```
%LOCALAPPDATA%\Programs\HonorTune\HonorTune.exe
```

不需要管理员权限。

### 命令行

```
HonorTune.exe --apply eco|balanced|perf   切换 Windows 侧预设模式
HonorTune.exe --restore                   恢复到初始状态
HonorTune.exe --status                    打印当前状态
HonorTune.exe --refresh 120               改刷新率
HonorTune.exe --list-refresh              列出可用刷新率
HonorTune.exe --diag <输出文件>           环境诊断
HonorTune.exe --gpu-oc 150 300            核心 +150 MHz、显存 +300 MHz
HonorTune.exe --gpu-reset                 清掉 GPU 频率偏移
```

---

## 五、已知限制

- 风扇档位只在**高能档**生效，这是硬件/固件行为，不是本程序的限制。
- 实时转速和温度依赖💊 WMI 接口，非💊机型上相关区域会显示 `—`。
- 温度只有 **GPU**；CPU 核心温度要读 MSR，本机 ACPI 热区读数不可信，已过滤。
- 刷新率以**主显示器**为准。
- 界面按 96 DPI 设计，高缩放下系统整体位图缩放，文字会略软。

## 六、说明

第三方工具，与荣耀官方无关。所有接口均为本机逆向分析与实测所得，换机型、换管家版本都可能失效。改动硬件性能模式与风扇转速有其固有风险，请自行判断使用。

代码以 MIT 协议发布。

## 七、致谢

本项目在 [DeepSeek](https://www.deepseek.com) 模型辅助下完成：💊 WMI 接口的定位、`PerfCommonPlugin.dll` 的反汇编分析、`FanSpeedConfig.dat` 的结构还原、界面实现与文档整理，均为与模型结对完成。

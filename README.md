# IL2CPP Symbol Reader

IL2CPP usym 符号文件解析工具，用于将 Unity IL2CPP 原生地址转换为 C# 源代码位置。

## 功能特性

- ✅ 读取和解析 Unity 6+ 的 usym 符号文件
- ✅ 将原生地址转换为 C# 源文件和行号
- ✅ 支持批量地址查询
- ✅ 支持解析完整的堆栈跟踪
- ✅ 跨平台支持（macOS、Linux、Windows）
- ✅ 单文件可执行程序，无需依赖

## 快速开始

### 1. 编译工具

```bash
# 运行一键编译脚本
./build.sh
```

编译完成后，可执行文件位于 `publish/Il2CppSymbolReader`（约 74MB）

### 2. 使用工具

#### 查看帮助信息

```bash
./publish/Il2CppSymbolReader --help
```

#### 读取 usym 文件信息

```bash
./publish/Il2CppSymbolReader read il2cpp.usym
```

输出示例：
```
IL2CPP Symbol Reader
====================
Reading usym file: il2cpp.usym
Magic: 0x2D6D7973 (valid)
Version: 1.0
Line Count: 6109445
Strings Size: 50000000 bytes
Symbol file is valid and ready to use.
```

#### 查找单个地址

```bash
./publish/Il2CppSymbolReader lookup il2cpp.usym 0xc3eeb4
```

输出示例：
```
IL2CPP Symbol Reader
====================
Looking up address: 0xc3eeb4
Result:
  File: Assets/Main/LoadDll.cs
  Line: 25
  Method Index: 1234
  Address: 0xc3eeb4
```

#### 导出所有符号

```bash
./publish/Il2CppSymbolReader dump il2cpp.usym
```

将所有符号导出到 `dump.txt` 文件中。

#### 解析堆栈跟踪

创建堆栈跟踪文件 `stacktrace.txt`：
```
at LoadDll.Start () [0xc3eeb4] in Assets/Main/LoadDll.cs:25
at Entry.Start () [0xd4f5c6] in Assets/HotUpdate/Entry.cs:15
```

然后运行：
```bash
./publish/Il2CppSymbolReader stacktrace il2cpp.usym stacktrace.txt
```

输出示例：
```
IL2CPP Symbol Reader
====================
Processing stack trace from: stacktrace.txt

Resolved Stack Trace:
--------------------
at LoadDll.Start () [0xc3eeb4]
   → Assets/Main/LoadDll.cs:25

at Entry.Start () [0xd4f5c6]
   → Assets/HotUpdate/Entry.cs:15
```

#### 解析多个地址

```bash
./publish/Il2CppSymbolReader resolve il2cpp.usym 0xc3eeb4 0xd4f5c6 0xe5a7d8
```

## 工作流程

### 完整的符号解析流程

1. **修改 IL2CPP 源码** - 在异常堆栈中输出原生地址
   - 修改 `icalls/mscorlib/System.Diagnostics/StackTrace.cpp`
   - 设置 `stackFrame->il_offset = (int32_t)nativeAddr;`

2. **Unity 导出时生成符号映射**
   - 勾选 Development Build
   - 启用 Source Map 生成
   - 确保生成 LineNumberMappings.json

3. **使用 Unity 6 usym 工具生成 il2cpp.usym**
   - Unity 6 提供的符号生成工具
   - 输入：IL2CPP 编译产物 + LineNumberMappings.json
   - 输出：il2cpp.usym 符号文件

4. **使用本工具解析地址** ✅
   - 输入：il2cpp.usym + 原生地址
   - 输出：C# 源文件位置和行号

### usym 文件格式

usym 文件记录了以下映射关系：
```
原生地址 → C# 源文件 + 行号
```

文件结构：
- **Header**: 魔术数字(sym-), 版本, 行数, 字符串表大小
- **Line Table**: 每个地址对应的源代码位置信息
- **String Table**: 存储文件路径的字符串池

## 安装到系统（可选）

### 安装到用户目录

```bash
mkdir -p ~/bin
cp publish/Il2CppSymbolReader ~/bin/
echo 'export PATH="$HOME/bin:$PATH"' >> ~/.zshrc  # macOS
source ~/.zshrc

# 现在可以在任何目录直接使用
Il2CppSymbolReader --help
```

### 安装到系统目录

```bash
sudo cp publish/Il2CppSymbolReader /usr/local/bin/
sudo chmod +x /usr/local/bin/Il2CppSymbolReader

# 现在可以在任何目录直接使用
Il2CppSymbolReader --help
```

## 开发说明

### 项目结构

```
il2cpp_usym_resolver/
├── build.sh                    # 一键编译脚本
├── Il2CppSymbolReader.csproj   # 项目文件
├── Program.cs                  # 程序入口
├── CommandLineInterface.cs     # 命令行界面
├── UsymReader.cs              # usym 文件读取器
├── UsymStructures.cs          # usym 数据结构
├── Il2CppAddressResolver.cs   # 地址解析器
├── UnityStackTraceParser.cs   # Unity 堆栈跟踪解析器
└── MemoryMappedFile.cs        # 跨平台内存映射文件
```

### 技术实现

- **语言**: C# (.NET 9.0)
- **关键技术**:
  - Memory-Mapped File - 高效读取大文件
  - Binary Serialization - 解析二进制格式
  - Binary Search - 快速地址查找
  - Cross-platform APIs - 支持多平台

### 重要修改记录

- **2024-12**: 修复 macOS 兼容性
  - 替换 Windows-only P/Invoke API
  - 使用 .NET 标准 `System.IO.MemoryMappedFiles`
  - 移除命名内存映射（macOS 不支持）

## 故障排除

### 错误: "Named maps are not supported"

**原因**: macOS 不支持命名的内存映射文件

**解决**: 已在最新版本中修复，使用 `null` 作为 mapName

### 错误: "Invalid usym file magic"

**原因**: 文件不是有效的 usym 格式或已损坏

**解决**:
1. 确认文件是由 Unity 6 usym 工具生成的
2. 检查文件大小是否正常（通常几十 MB）
3. 重新生成 usym 文件

### 错误: "Symbol file not found"

**原因**: 文件路径不正确

**解决**: 使用绝对路径或确保相对路径正确

## 许可证

本工具基于开源项目修改，用于 Unity IL2CPP 符号解析。

## 参考资料

- [Unity IL2CPP 文档](https://docs.unity3d.com/Manual/IL2CPP.html)
- [Unity Symbol Files](https://docs.unity3d.com/Manual/debugging-il2cpp.html)
- [HybridCLR 文档](https://hybridclr.doc.code-philosophy.com/)

## 更新日志

### v1.0.0 (2024-12)
- ✅ 初始版本
- ✅ 支持 usym 文件读取和解析
- ✅ 支持地址查找和堆栈跟踪解析
- ✅ 修复 macOS 兼容性问题
- ✅ 单文件发布支持
- ✅ 一键编译脚本

## 联系方式

如有问题或建议，欢迎反馈！

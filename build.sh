#!/bin/bash
# IL2CPP Symbol Reader - 一键编译和发布脚本

set -e  # 遇到错误立即退出

# 颜色输出
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${BLUE}========================================${NC}"
echo -e "${BLUE}IL2CPP Symbol Reader - 编译发布工具${NC}"
echo -e "${BLUE}========================================${NC}"
echo ""

# 获取脚本所在目录
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
cd "$SCRIPT_DIR"

echo -e "${YELLOW}[1/4] 清理旧的编译文件...${NC}"
if [ -d "bin" ]; then
    rm -rf bin
    echo "✓ 已删除 bin 目录"
fi
if [ -d "obj" ]; then
    rm -rf obj
    echo "✓ 已删除 obj 目录"
fi
if [ -d "publish" ]; then
    rm -rf publish
    echo "✓ 已删除 publish 目录"
fi
echo ""

echo -e "${YELLOW}[2/4] 还原 NuGet 包...${NC}"
dotnet restore
echo ""

echo -e "${YELLOW}[3/4] 编译项目 (Release 配置)...${NC}"
dotnet build -c Release
if [ $? -eq 0 ]; then
    echo -e "${GREEN}✓ 编译成功${NC}"
else
    echo -e "${RED}✗ 编译失败${NC}"
    exit 1
fi
echo ""

echo -e "${YELLOW}[4/4] 发布单文件可执行程序...${NC}"
echo "平台: macOS ARM64"
echo "目标: publish/Il2CppSymbolReader"
dotnet publish \
    -c Release \
    -r osx-arm64 \
    --self-contained \
    -p:PublishSingleFile=true \
    -p:IncludeNativeLibrariesForSelfExtract=true \
    -o publish

if [ $? -eq 0 ]; then
    echo -e "${GREEN}✓ 发布成功${NC}"
else
    echo -e "${RED}✗ 发布失败${NC}"
    exit 1
fi
echo ""

# 设置可执行权限
chmod +x publish/Il2CppSymbolReader

# 显示文件信息
echo -e "${BLUE}========================================${NC}"
echo -e "${GREEN}编译发布完成！${NC}"
echo -e "${BLUE}========================================${NC}"
echo ""
echo -e "可执行文件: ${GREEN}$SCRIPT_DIR/publish/Il2CppSymbolReader${NC}"
echo -e "文件大小: $(du -h publish/Il2CppSymbolReader | cut -f1)"
echo ""
echo -e "${YELLOW}使用方法:${NC}"
echo ""
echo -e "1. 查看帮助:"
echo -e "   ${GREEN}./publish/Il2CppSymbolReader --help${NC}"
echo ""
echo -e "2. 读取 usym 文件信息:"
echo -e "   ${GREEN}./publish/Il2CppSymbolReader read il2cpp.usym${NC}"
echo ""
echo -e "3. 查找地址:"
echo -e "   ${GREEN}./publish/Il2CppSymbolReader lookup il2cpp.usym 0xc3eeb4${NC}"
echo ""
echo -e "4. 导出所有符号:"
echo -e "   ${GREEN}./publish/Il2CppSymbolReader dump il2cpp.usym${NC}"
echo ""
echo -e "5. 解析堆栈跟踪:"
echo -e "   ${GREEN}./publish/Il2CppSymbolReader stacktrace il2cpp.usym stacktrace.txt${NC}"
echo ""
echo -e "${YELLOW}安装到系统 (可选):${NC}"
echo -e "   ${GREEN}cp publish/Il2CppSymbolReader ~/bin/${NC}"
echo -e "   或"
echo -e "   ${GREEN}sudo cp publish/Il2CppSymbolReader /usr/local/bin/${NC}"
echo ""

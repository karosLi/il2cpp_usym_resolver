using System.IO.MemoryMappedFiles;

namespace Il2CppSymbolReader;

/// <summary>
/// 跨平台内存映射文件实现（包装.NET标准库）
/// </summary>
public class MemoryMappedFile : IDisposable
{
    private readonly System.IO.MemoryMappedFiles.MemoryMappedFile _mmf;
    private readonly long _capacity;

    private MemoryMappedFile(System.IO.MemoryMappedFiles.MemoryMappedFile mmf, long capacity)
    {
        _mmf = mmf;
        _capacity = capacity;
    }

    /// <summary>
    /// 从文件创建内存映射文件
    /// </summary>
    public static MemoryMappedFile CreateFromFile(string path, FileMode mode, string? mapName, long capacity, MemoryMappedFileAccess access)
    {
        var fileInfo = new FileInfo(path);
        if (!fileInfo.Exists && mode == FileMode.Open)
        {
            throw new FileNotFoundException("File not found", path);
        }

        var fileLength = fileInfo.Exists ? fileInfo.Length : 0;
        if (capacity == 0 && fileLength > 0)
        {
            capacity = fileLength;
        }
        else if (capacity == 0)
        {
            capacity = 1024; // 默认大小
        }

        var mmfAccess = access switch
        {
            MemoryMappedFileAccess.Read => System.IO.MemoryMappedFiles.MemoryMappedFileAccess.Read,
            MemoryMappedFileAccess.Write => System.IO.MemoryMappedFiles.MemoryMappedFileAccess.Write,
            MemoryMappedFileAccess.ReadWrite => System.IO.MemoryMappedFiles.MemoryMappedFileAccess.ReadWrite,
            _ => System.IO.MemoryMappedFiles.MemoryMappedFileAccess.Read
        };

        System.IO.MemoryMappedFiles.MemoryMappedFile mmf;

        if (mode == FileMode.Open && fileInfo.Exists)
        {
            // 打开现有文件
            mmf = System.IO.MemoryMappedFiles.MemoryMappedFile.CreateFromFile(
                path,
                mode,
                mapName,
                capacity,
                mmfAccess);
        }
        else
        {
            // 创建新文件或打开现有文件
            using var fs = new FileStream(path, mode,
                access == MemoryMappedFileAccess.Read ? FileAccess.Read : FileAccess.ReadWrite,
                FileShare.Read);

            mmf = System.IO.MemoryMappedFiles.MemoryMappedFile.CreateFromFile(
                fs,
                mapName,
                capacity,
                mmfAccess,
                HandleInheritability.None,
                leaveOpen: false);
        }

        return new MemoryMappedFile(mmf, capacity);
    }

    /// <summary>
    /// 创建视图访问器
    /// </summary>
    public MemoryMappedViewAccessor CreateViewAccessor(long offset, long size, MemoryMappedFileAccess access)
    {
        if (size == 0)
        {
            size = _capacity - offset;
        }

        var mmfAccess = access switch
        {
            MemoryMappedFileAccess.Read => System.IO.MemoryMappedFiles.MemoryMappedFileAccess.Read,
            MemoryMappedFileAccess.Write => System.IO.MemoryMappedFiles.MemoryMappedFileAccess.Write,
            MemoryMappedFileAccess.ReadWrite => System.IO.MemoryMappedFiles.MemoryMappedFileAccess.ReadWrite,
            _ => System.IO.MemoryMappedFiles.MemoryMappedFileAccess.Read
        };

        var accessor = _mmf.CreateViewAccessor(offset, size, mmfAccess);
        return new MemoryMappedViewAccessor(accessor);
    }

    public void Dispose()
    {
        _mmf?.Dispose();
    }
}

/// <summary>
/// 内存映射文件访问模式
/// </summary>
public enum MemoryMappedFileAccess
{
    Read,
    Write,
    ReadWrite
}

/// <summary>
/// 内存映射视图访问器（包装.NET标准库）
/// </summary>
public class MemoryMappedViewAccessor : IDisposable
{
    private readonly System.IO.MemoryMappedFiles.MemoryMappedViewAccessor _accessor;
    private bool _disposed;

    internal MemoryMappedViewAccessor(System.IO.MemoryMappedFiles.MemoryMappedViewAccessor accessor)
    {
        _accessor = accessor;
    }

    public long Capacity => _accessor.Capacity;

    public void ReadArray<T>(long position, T[] array, int offset, int count) where T : struct
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(MemoryMappedViewAccessor));

        _accessor.ReadArray(position, array, offset, count);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _accessor?.Dispose();
            _disposed = true;
        }
    }
}

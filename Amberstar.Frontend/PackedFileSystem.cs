using Amber.IO.Common.FileSystem;
using Amber.Serialization;

namespace Amberstar.Frontend;

/// <summary>
/// Read-only files in memory without sub folders (the game data of a packed game version).
/// </summary>
// The virtual file system of Amber.IO.FileSystem 0.1.0 returns readers over all files instead of one file.
internal sealed class PackedFileSystem : IReadOnlyFileSystem
{
	readonly Folder root = new();

	public void AddFile(string name, byte[] data) => root.AddFile(new File(name, data, root));

	public bool MemoryFileSystem => true;

	public IReadOnlyNode? GetNode(string path) => path.Length == 0 ? root : GetFile(path);

	public IReadOnlyFolder? GetFolder(string path) => path.Length == 0 ? root : null;

	public IReadOnlyFile? GetFile(string path) => root.Files.GetValueOrDefault(path);

	public IEnumerable<IReadOnlyFile> GetAllFiles() => root.Files.Values;

	public IReadOnlyFileStream? GetFileReader(string path) => GetFile(path)?.Stream;

	sealed class Folder : IReadOnlyFolder
	{
		readonly Dictionary<string, IReadOnlyFile> files = [];

		public string Name => "";
		public string Path => "";
		public IReadOnlyFolder? Parent => null;
		public IReadOnlyDictionary<string, IReadOnlyFolder> Folders { get; } = new Dictionary<string, IReadOnlyFolder>();
		public IReadOnlyDictionary<string, IReadOnlyFile> Files => files;

		public void AddFile(File file) => files.Add(file.Name, file);
	}

	sealed class File(string name, byte[] data, IReadOnlyFolder parent) : IReadOnlyFile, IReadOnlyFileStream
	{
		public string Name => name;
		public string Path => name;
		public IReadOnlyFolder? Parent => parent;
		public IReadOnlyFileStream Stream => this;
		public int Size => data.Length;

		public IDisposableDataReader GetReader() => new Reader(data);
	}

	sealed class Reader(byte[] data) : DataReader(data), IDisposableDataReader
	{
		public void Dispose()
		{
		}
	}
}

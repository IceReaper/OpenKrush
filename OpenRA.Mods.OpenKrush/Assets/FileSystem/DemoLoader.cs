namespace OpenRA.Mods.OpenKrush.Assets.FileSystem;

using Common.FileFormats;
using ICSharpCode.SharpZipLib.Zip;
using JetBrains.Annotations;
using OpenRA.FileSystem;

[UsedImplicitly]
public class DemoLoader : IPackageLoader
{
	private class DemoPackage : IReadOnlyPackage
	{
		private readonly InstallShieldCABCompression? data;

		public string Name { get; }
		public IEnumerable<string> Contents { get; }

		public DemoPackage(Stream stream, string name)
		{
			stream.Position = 0x000B7B95;

			var zipBytes = new byte[6984869];

			for (var i = 0; i < zipBytes.Length;)
				i += stream.Read(zipBytes, i, zipBytes.Length - i);

			using var zipFile = new ZipFile(new MemoryStream(zipBytes));
			var data1Entry = zipFile.GetEntry("data1.cab");
			using var data1Stream = zipFile.GetInputStream(data1Entry);

			var data1Bytes = new byte[data1Entry.Size];

			for (var i = 0; i < data1Bytes.Length;)
				i += data1Stream.Read(data1Bytes, i, data1Bytes.Length - i);

			var volume = new MemoryStream(data1Bytes);
			this.data = new(volume, new() { [0] = volume });

			this.Name = name;
			this.Contents = this.data.Contents.SelectMany(e => e.Value);
		}

		public bool Contains(string entry)
		{
			return this.data?.Contents.Any(e => e.Value.Any(e => e == entry)) ?? false;
		}

		public IReadOnlyPackage OpenPackage(string filename, FileSystem context)
		{
			throw new NotImplementedException();
		}

		public Stream GetStream(string entry)
		{
			var file = new MemoryStream();
			this.data?.ExtractFile(entry, file);
			file.Position = 0;

			return file;
		}

		public void Dispose()
		{
		}
	}

	public bool TryParsePackage(Stream s, string filename, FileSystem context, out IReadOnlyPackage package)
	{
		package = new DemoPackage(s, filename);

		return true;
	}
}

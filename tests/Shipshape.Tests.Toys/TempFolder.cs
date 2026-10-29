namespace Shipshape.Tests.Toys;

/// <summary>A fresh folder under the system temp folder, deleted on dispose.</summary>
public sealed class TempFolder : IDisposable
{
    public string Path { get; } = Directory.CreateTempSubdirectory("shipshape-tests-").FullName;

    public string File(string name) => System.IO.Path.Combine(Path, name);

    public void Dispose() => Directory.Delete(Path, recursive: true);
}

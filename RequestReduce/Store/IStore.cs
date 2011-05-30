using System.IO;

namespace RequestReduce.Store
{
    public interface IStore
    {
        void Save(byte[] content, string url);
        byte[] GetContent(string url);
        Stream OpenStream(string url);
    }
}
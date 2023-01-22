namespace Nabu.Network;

public interface IRetroNetFileHandler
{
    byte FileHandleOpen(string filename, FileOpenFlags flags, byte handle);
    void FileHandleClose(byte handle);
    int FileSize(string filename);
    int FileHandleSize(byte handle);
    FileDetails FileDetails(string filename);
    FileDetails FileHandleDetails(byte handle);
    byte[] FileHandleRead(byte handle, short bufferOffset, int readOffset, short readLength);
    void FileHandleAppend(byte handle, short fileOffset, int dataOffset, byte[] data);
    void FileHandleInsert(byte handle, short fileOffset, int dataOffset, byte[] data);
    void FileHandleDeleteRange(byte handle, short fileOffset, short deleteLength);
    void FileHandleEmptyFile(byte handle);
    void FileHandleReplace(byte handle, short fileOffset, int dataOffset, byte[] data);
    void FileHandleDelete(string filename);
    void FileHandleCopy(string source, string destination, CopyMoveFlags flags);
    void FileHandleMove(string source, string destination, CopyMoveFlags flags);
    short FileList(string path, string wildcard, FileListFlags flags);
    FileDetails FileListItem(short index);
    byte[] FileHandleReadSequence(byte handle, short bufferOffset, int readOffset, short readLength);
    int FileHandleSeek(byte handle, int offset, FileSeekFlags flags);
}


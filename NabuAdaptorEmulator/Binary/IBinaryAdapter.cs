namespace Nabu.Binary;

public interface IBinaryAdapter
{

    void Open();
    void Close();
    byte Recv();
    (bool, byte) Recv(byte byt);
    byte[] Recv(int length = 1);
    (bool, byte[]) Recv(params byte[] bytes);
    void Send(params byte[] bytes);
    void Send(byte[] buffer, int bytes);
}
using System.Collections;
using System.Collections.Generic;

public interface IPacketReceiver
{
    int OperationCode { get; }
    void Receive(object packet);
}
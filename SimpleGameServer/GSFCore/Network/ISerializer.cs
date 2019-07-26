﻿using System.Collections;
using System.Collections.Generic;

namespace GameSystem.GameCore.Network
{
    public interface ISerializer
    {
        byte[] Serialize(object obj);
        object Deserialize(byte[] bytes);
        T Deserialize<T>(byte[] bytes);
    }

    public interface ISerializer<T>
    {
        byte[] Serialize(T item);
        T Deserialize(byte[] bytes);
    }
}
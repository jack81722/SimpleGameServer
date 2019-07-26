using GameSystem.GameCore.SerializableMath;
using System;
using System.Collections.Generic;
using System.Text;

[Serializable]
public class BulletInfo
{
    public int id;
    public float[] pos;

    public BulletInfo(int id, Vector3 pos)
    {
        this.id = id;
        this.pos = new float[] { pos.x, pos.y, pos.z };
    }
}


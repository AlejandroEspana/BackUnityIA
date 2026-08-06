using System.IO;
using System;

public static class SaveSystem
{
    public static byte[] Serialize(SaveData data)
    {
        using (MemoryStream ms = new MemoryStream())
        using (BinaryWriter writer = new BinaryWriter(ms))
        {
            writer.Write(data.SceneName ?? "");
            writer.Write(data.PosX);
            writer.Write(data.PosY);
            writer.Write(data.PosZ);
            writer.Write(data.WorldStateJson ?? "{}");
            writer.Write(data.LastConversation ?? "");

            return ms.ToArray();
        }
    }

    public static SaveData Deserialize(byte[] bytes)
    {
        if (bytes == null || bytes.Length == 0) return null;

        using (MemoryStream ms = new MemoryStream(bytes))
        using (BinaryReader reader = new BinaryReader(ms))
        {
            SaveData data = new SaveData();
            data.SceneName = reader.ReadString();
            data.PosX = reader.ReadSingle();
            data.PosY = reader.ReadSingle();
            data.PosZ = reader.ReadSingle();
            data.WorldStateJson = reader.ReadString();
            data.LastConversation = reader.ReadString();

            return data;
        }
    }
}

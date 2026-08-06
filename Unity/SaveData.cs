using UnityEngine;

[System.Serializable]
public class SaveData
{
    public string SceneName;
    public float PosX;
    public float PosY;
    public float PosZ;
    public string WorldStateJson;
    public string LastConversation;

    public SaveData() {}

    public SaveData(string sceneName, Vector3 position, string worldStateJson, string lastConversation)
    {
        SceneName = sceneName;
        PosX = position.x;
        PosY = position.y;
        PosZ = position.z;
        WorldStateJson = worldStateJson;
        LastConversation = lastConversation;
    }

    public Vector3 GetPosition()
    {
        return new Vector3(PosX, PosY, PosZ);
    }
}

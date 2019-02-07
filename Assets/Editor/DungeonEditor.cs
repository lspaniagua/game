using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Dungeon))]
public class DungeonEditor : Editor
{
    public override void OnInspectorGUI()
    {
        Dungeon dungeon = target as Dungeon;

        if (DrawDefaultInspector())
        {
            //dungeon.GenerateDungeon();
        }

        if (GUILayout.Button("Generate Dungeon"))
        {
            dungeon.GenerateDungeon();
        }
    }
}

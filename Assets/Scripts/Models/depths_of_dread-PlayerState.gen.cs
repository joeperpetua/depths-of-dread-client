// Generated by dojo-bindgen on Sun, 24 Nov 2024 19:07:25 +0000. Do not modify this file manually.
using System;
using Dojo;
using Dojo.Starknet;
using UnityEngine;

// Type definition for `depths_of_dread::models::Vec2` struct
[Serializable]
public struct Vec2
{
    public uint x;
    public uint y;

    public void FromVector3(Vector3 pos)
    {
        x = (uint)pos.x;
        y = (uint)pos.y;
    }
    public void MoveTo(Direction dir)
    {
        x = (uint)MoveCheck(dir).x;
        y = (uint)MoveCheck(dir).y;
    }

    public readonly Vector3 ToVector3() => new(x, y, 0);
    public readonly Vector3 MoveCheck(Direction dir) => ToVector3() + dir.ToVector3();

    public static bool IsInBounds(Vec2 pos, Vec2 size) => pos.x >= 0 && pos.x < size.x && pos.y >= 0 && pos.y < size.y;
    public static bool IsInBounds(Vector3 pos, Vec2 size) => pos.x >= 0 && pos.x <= size.x && pos.y >= 0 && pos.y <= size.y;
    public static bool AreEqual(Vec2 a, Vec2 b) => a.x == b.x && a.y == b.y;
}


// Model definition for `depths_of_dread::models::PlayerState` model
public class depths_of_dread_PlayerState : ModelInstance
{
    [ModelField("player")]
    public FieldElement player;

    [ModelField("game_id")]
    public uint game_id;

    [ModelField("current_floor")]
    public ushort current_floor;

    [ModelField("position")]
    public Vec2 position;

    [ModelField("coins")]
    public ushort coins;

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
    }
}


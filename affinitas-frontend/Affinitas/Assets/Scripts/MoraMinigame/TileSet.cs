using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TileSet", menuName = "Minigame/Tile Set", order = 1)]
public class TileSet : ScriptableObject
{
    public List<Sprite> tiles = new List<Sprite>(9);
}
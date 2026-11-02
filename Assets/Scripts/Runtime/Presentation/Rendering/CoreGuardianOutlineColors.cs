using UnityEngine;

// 10-31 확정: 선택(누름)·마우스 올림 아웃라인 색. 같은 대상은 누를 때와 올릴 때 같은 색, 종류마다 다른 색.
// 누름은 씬의 선택 아웃라인 굵기(2픽셀), 마우스 올림은 HoverWidth(1픽셀). 강화 건물은 그림 테두리(금색)로 따로.
public enum OutlineKind
{
    None,
    Player,
    Tower,
    Enemy,
    Core,
}

public static class CoreGuardianOutlineColors
{
    public const int HoverWidth = 1;
    public static readonly Color Tower = Hex(0x6f, 0xd9, 0xff);
    public static readonly Color Player = Hex(0x15, 0xff, 0x00);
    public static readonly Color Core = Hex(0xc9, 0xa6, 0xff);
    public static readonly Color Enemy = Hex(0xff, 0x5a, 0x4e);

    public static Color For(OutlineKind kind)
    {
        switch (kind)
        {
            case OutlineKind.Player:
                return Player;
            case OutlineKind.Tower:
                return Tower;
            case OutlineKind.Enemy:
                return Enemy;
            case OutlineKind.Core:
                return Core;
            default:
                return Color.clear;
        }
    }

    static Color Hex(int r, int g, int b) => new Color(r / 255f, g / 255f, b / 255f, 1);
}

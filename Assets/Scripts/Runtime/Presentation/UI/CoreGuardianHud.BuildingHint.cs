using UnityEngine;
using UnityEngine.UI;

// 11-01 사용자 요청: 강화 건물(포탑 연구소·기술병 작업대)에 마우스를 올리면 건물 바로 위에
// 이름(금색)과 설명, '클릭 또는 [1]/[2]' 키 안내를 띄운다. 키 그림은 Hud/KeyHint1·KeyHint2(A안 밝은 회색).
public sealed partial class CoreGuardianHud
{
    const float BuildingHintPad = 16,
        BuildingHintKeySize = 26;

    RectTransform buildingHint;
    Text buildingHintTitle;
    Text buildingHintBody;
    Text buildingHintKeyLabel;
    Image buildingHintKey;
    int shownBuildingHint;

    void BuildBuildingHint()
    {
        buildingHint = Box("BuildingHint", canvas, Vector2.zero, Vector2.zero, new Vector2(320, 80));
        buildingHint.pivot = new Vector2(.5f, 0);
        buildingHint.GetComponent<Image>().raycastTarget = false;
        buildingHintTitle = Text("Title", buildingHint, new Vector2(BuildingHintPad, -10), new Vector2(288, 28), 19);
        buildingHintTitle.color = new Color32(255, 212, 90, 255);
        buildingHintBody = Text("Body", buildingHint, new Vector2(BuildingHintPad, -42), new Vector2(200, 28), 16);
        buildingHintBody.horizontalOverflow = HorizontalWrapMode.Overflow;
        buildingHintKeyLabel = Text("KeyLabel", buildingHint, new Vector2(0, -42), new Vector2(80, 28), 16);
        buildingHintKeyLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
        buildingHintKeyLabel.color = new Color32(170, 180, 180, 255);
        buildingHintKey = CoreGuardianHudSkin.Icon(
            buildingHint,
            "KeyHint1",
            new Vector2(0, -42 + (BuildingHintKeySize - 28) / 2f),
            new Vector2(BuildingHintKeySize, BuildingHintKeySize)
        );
        SetHudActive(buildingHint.gameObject, false);
    }

    void ShowBuildingHintText(int which)
    {
        bool bench = which == 2;
        SetHudText(buildingHintTitle, bench ? "기술병 작업대" : "포탑 연구소");
        SetHudText(buildingHintBody, bench ? "기술병을 강화할 수 있습니다." : "포탑을 강화할 수 있습니다.");
        SetHudText(buildingHintKeyLabel, "클릭 또는");
        buildingHintKey.sprite = CoreGuardianHudSkin.Sprite(bench ? "KeyHint2" : "KeyHint1");
        float bodyWidth = Mathf.Ceil(buildingHintBody.preferredWidth);
        float labelWidth = Mathf.Ceil(buildingHintKeyLabel.preferredWidth);
        float labelX = BuildingHintPad + bodyWidth + 14;
        float keyX = labelX + labelWidth + 6;
        float width = keyX + BuildingHintKeySize + BuildingHintPad;
        SetHudSize(buildingHintBody.rectTransform, new Vector2(bodyWidth, 28));
        SetHudSize(buildingHintKeyLabel.rectTransform, new Vector2(labelWidth, 28));
        SetHudPosition(buildingHintKeyLabel.rectTransform, new Vector2(labelX, -42));
        SetHudPosition(buildingHintKey.rectTransform, new Vector2(keyX, -42 + (28 - BuildingHintKeySize) / 2f));
        SetHudSize(buildingHintTitle.rectTransform, new Vector2(width - BuildingHintPad * 2, 28));
        SetHudSize(buildingHint, new Vector2(width, 80));
    }

    void UpdateBuildingHint(Camera camera)
    {
        int which = game != null && game.Ready ? game.HoverBuilding : 0;
        if (which == 0 || camera == null)
        {
            if (buildingHint != null)
            {
                SetHudActive(buildingHint.gameObject, false);
            }
            shownBuildingHint = 0;
            return;
        }
        if (buildingHint == null)
        {
            BuildBuildingHint();
        }
        if (which != shownBuildingHint)
        {
            ShowBuildingHintText(which);
            buildingHint.SetAsLastSibling();
            shownBuildingHint = which;
        }

        var screen = camera.WorldToScreenPoint(game.World(game.BuildingTop(which == 2)));
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas, screen, null, out Vector2 local))
            return;

        var area = canvas.rect.size;
        var size = buildingHint.sizeDelta;
        var p = local + area * .5f;
        p.x = Mathf.Clamp(p.x, size.x * .5f + 8, Mathf.Max(size.x * .5f + 8, area.x - size.x * .5f - 8));
        p.y = Mathf.Clamp(p.y, 8, Mathf.Max(8, area.y - size.y - 8));
        SetHudPosition(buildingHint, new Vector2(Mathf.Round(p.x), Mathf.Round(p.y)));
        SetHudActive(buildingHint.gameObject, true);
    }
}

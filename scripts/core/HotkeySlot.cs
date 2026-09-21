// A,S,D,F,Q,W,E 핫키 슬롯. 순서가 곧 기본 물리 키(Key.A ~ Key.E)에 대응한다.
public enum HotkeySlot
{
    A,
    S,
    D,
    F,
    Q,
    W,
    E,
    R,
    Num1,
    Num2,
    Num3,
    Num4,
    I
}

// 슬롯에 무엇이 들어있는지 구분 (스킬 / 소비 아이템 / UI 토글).
public enum SlotContentType
{
    None,
    Skill,
    Item,
    UI
}

// 슬롯 하나에 배치된 콘텐츠(스킬 또는 아이템 Id)를 표현하는 값 객체.
public class SlotBinding
{
    public SlotContentType ContentType { get; }
    public string ContentId { get; }

    public bool IsEmpty => ContentType == SlotContentType.None || string.IsNullOrEmpty(ContentId);

    public SlotBinding()
    {
        ContentType = SlotContentType.None;
        ContentId = string.Empty;
    }

    public SlotBinding(SlotContentType contentType, string contentId)
    {
        ContentType = contentType;
        ContentId = contentId;
    }
}

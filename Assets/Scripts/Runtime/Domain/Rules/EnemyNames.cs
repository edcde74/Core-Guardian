namespace CoreGuardian
{
    // The one Korean name table for enemies (B18): the HUD and the developer menu both read it.
    public static class EnemyNames
    {
        public static string Kind(EnemyKind kind)
        {
            switch (kind)
            {
                case EnemyKind.Attack:
                    return "공격형";
                case EnemyKind.Defense:
                    return "수비형";
                case EnemyKind.Buff:
                    return "버프형";
                case EnemyKind.Charger:
                    return "돌격병";
                case EnemyKind.Splitter:
                    return "분열체";
                case EnemyKind.SplitChild:
                    return "분열 작은 개체";
                case EnemyKind.Healer:
                    return "치유사";
                case EnemyKind.Armored:
                    return "장갑병";
                case EnemyKind.Flyer:
                    return "비행체";
                default:
                    return "보스";
            }
        }

        public static string Boss(EnemySpec spec) =>
            spec == null ? "보스"
            : spec.Key == "Boss" ? "파성장군" // 11-04 확정: 10라운드 보스 이름
            : spec.Key == "IronKnight" ? "철벽 기사"
            : spec.Key == "CrimsonLord" ? "진홍의 군주"
            : "보스";

        public static string Of(EnemySpec spec) =>
            spec == null ? "?"
            : spec.Kind == EnemyKind.Boss ? Boss(spec)
            : Kind(spec.Kind);
    }
}

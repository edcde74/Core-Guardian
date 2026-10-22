using System;
using System.Collections.Generic;
using System.Linq;

namespace CoreGuardian
{
    public enum AugmentationGrade { Silver, Gold, Platinum }
    public enum AugmentationFamily { Gold, Crystals, Flawless, Player, Towers, Core }
    public sealed class AugmentationCard
    {
        public readonly AugmentationGrade Grade;
        public readonly AugmentationFamily Family;
        public string Id => Grade + "_" + Family;
        public string GradeName => Grade == AugmentationGrade.Silver ? "실버" : Grade == AugmentationGrade.Gold ? "골드" : "플래티넘";
        public string Title => new[] { "골드 보급", "수정 보급", "완벽한 방어", "기술병 전투", "포탑 전투", "코어 증축" }[(int)Family];
        public int Value(int silver, int gold, int platinum) => Grade == AugmentationGrade.Silver ? silver : Grade == AugmentationGrade.Gold ? gold : platinum;
        public string Description {
            get {
                switch (Family) {
                    case AugmentationFamily.Gold: return "즉시 " + Value(100,300,600) + "골드 지급";
                    case AugmentationFamily.Crystals: return "즉시 " + Value(80,240,480) + "수정 지급";
                    case AugmentationFamily.Flawless: return "이후 코어 피해 없이 라운드 완료 시\n" + Value(10,30,60) + "골드 + " + Value(5,15,30) + "수정";
                    case AugmentationFamily.Player: return Grade == AugmentationGrade.Platinum ? "매 공격마다 현재 대상에게\n현재 피해로 투사체 1발 추가" : "기술병 피해 입력값 +" + Value(2,4,0) + (Grade == AugmentationGrade.Gold ? "\n명중 시 1초간 이동속도 20% 감소" : "");
                    case AugmentationFamily.Towers: return "현재·이후 설치하는 모든 공격 포탑\n공격력 버프 +" + Value(10,30,60) + "%\n지원 포탑 버프와 합산";
                    default: return "코어 최대·현재 에너지\n각각 +" + Value(2,6,12);
                }
            }
        }
        public AugmentationCard(AugmentationGrade grade, AugmentationFamily family) { Grade=grade; Family=family; }
    }
    public static class AugmentationDeck
    {
        public static readonly IReadOnlyList<AugmentationCard> Cards = Array.AsReadOnly(
            (from AugmentationGrade grade in Enum.GetValues(typeof(AugmentationGrade))
             from AugmentationFamily family in Enum.GetValues(typeof(AugmentationFamily))
             select new AugmentationCard(grade,family)).ToArray());
        public static AugmentationGrade RollGrade(double roll) {
            if (roll < 0 || roll >= 1 || double.IsNaN(roll)) throw new ArgumentOutOfRangeException(nameof(roll));
            return roll < .70 ? AugmentationGrade.Silver : roll < .95 ? AugmentationGrade.Gold : AugmentationGrade.Platinum;
        }
        public static AugmentationCard[] Draw(Random random, IEnumerable<string> selected)
        {
            var history=new HashSet<string>(selected); var result=new List<AugmentationCard>();
            for (int i=0;i<3;i++) {
                var grade=RollGrade(random.NextDouble());
                var candidates=Cards.Where(c=>c.Grade==grade&&!result.Any(r=>r.Id==c.Id)).ToArray();
                var fresh=candidates.Where(c=>!history.Contains(c.Id)).ToArray();
                if (fresh.Length>0) candidates=fresh;
                result.Add(candidates[random.Next(candidates.Length)]);
            }
            return result.ToArray();
        }
        public static int BestIndex(IReadOnlyList<AugmentationCard> cards) {
            if (cards.Count==0) throw new ArgumentException("No cards");
            int best=0;for(int i=1;i<cards.Count;i++)if(cards[i].Grade>cards[best].Grade)best=i;return best;
        }
    }
    public struct AugmentationGrant { public int Gold, Crystals, Core, PlayerDamage; }
    public sealed class AugmentationEffects
    {
        readonly List<string> selected=new List<string>();
        public IReadOnlyList<string> Selected => selected.AsReadOnly();
        public int ExtraPlayerProjectiles { get; private set; }
        public bool PlayerSlow { get; private set; }
        public int TowerBuffPercent { get; private set; }
        public int FlawlessGold { get; private set; }
        public int FlawlessCrystals { get; private set; }
        public AugmentationGrant Apply(AugmentationCard card) {
            var grant=new AugmentationGrant();selected.Add(card.Id);
            switch(card.Family) {
                case AugmentationFamily.Gold: grant.Gold=card.Value(100,300,600);break;
                case AugmentationFamily.Crystals: grant.Crystals=card.Value(80,240,480);break;
                case AugmentationFamily.Flawless: FlawlessGold+=card.Value(10,30,60);FlawlessCrystals+=card.Value(5,15,30);break;
                case AugmentationFamily.Player: if(card.Grade==AugmentationGrade.Platinum)ExtraPlayerProjectiles++;else{grant.PlayerDamage=card.Value(2,4,0);if(card.Grade==AugmentationGrade.Gold)PlayerSlow=true;}break;
                case AugmentationFamily.Towers: TowerBuffPercent+=card.Value(10,30,60);break;
                case AugmentationFamily.Core: grant.Core=card.Value(2,6,12);break;
            }
            return grant;
        }
    }
}

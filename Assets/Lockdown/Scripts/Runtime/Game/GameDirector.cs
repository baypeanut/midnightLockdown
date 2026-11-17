using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Lockdown
{
    // owns the timer, puzzle progress, score and the win or lose outcome
    public class GameDirector : MonoBehaviour
    {
        public static GameDirector I { get; private set; }

        [Header("timer")]
        [Tooltip("used when the settings menu does not override it")]
        public float timeLimitSeconds = 600f;
        public bool useMenuTimer = true;
        public float chipBonusSeconds = 20f;
        public float hintPenaltySeconds = 20f;

        [Header("score")]
        public int totalChips = 5;
        public int baseScore = 1000;
        public int pointsPerSecondLeft = 5;
        public int pointsPerChip = 250;
        public int hintCost = 150;
        public int mistakeCost = 25;

        public GameState State { get; private set; } = GameState.Briefing;
        public float Remaining { get; private set; }
        public float Elapsed { get; private set; }
        public int HintsUsed { get; private set; }
        public int Mistakes { get; private set; }
        public int Chips { get; private set; }
        public int SafeResets { get; set; }
        public int MirrorSteps { get; set; }
        public bool VisitedOffice { get; private set; }
        public RunSecrets Secrets { get; private set; }
        public int SolvedCount => solved.Count;
        public int PuzzleCount => Enum.GetValues(typeof(PuzzleId)).Length;
        public int LastScore { get; private set; }
        public int LastRank { get; private set; }
        public readonly List<string> unlockedThisRun = new List<string>();

        public event Action<PuzzleId> PuzzleSolved;
        public event Action<GameState> StateChanged;
        public event Action<Stage> StageChanged;

        readonly HashSet<PuzzleId> solved = new HashSet<PuzzleId>();
        readonly int[] hintTier = new int[6];
        bool warnedFive, warnedOne;
        Stage lastStage = (Stage)(-1);
        float briefingLeft = 11f;

        void Awake()
        {
            I = this;
            UiFocus.Active = false;
            GameEvents.Clear();
            Secrets = new RunSecrets(Environment.TickCount);
            if (useMenuTimer) timeLimitSeconds = GameSettings.TimerMinutes * 60f;
            Remaining = timeLimitSeconds;
            GameSettings.Apply();
            Achievements.Unlocked += OnAchievement;
            GameEvents.OnMistake += () => Mistakes++;
            GameEvents.OnZoneEntered += OnZone;
        }

        void OnDestroy()
        {
            Achievements.Unlocked -= OnAchievement;
            if (I == this) I = null;
        }

        void Start()
        {
            if (Narrator.I)
            {
                Narrator.I.Say("warden_intro_a");
                Narrator.I.Say("warden_intro_b");
                Narrator.I.Say("dee_intro");
            }
            Hud.I?.Toast("Midnight Lockdown  |  " + Leaderboard.Format(timeLimitSeconds) + " on the clock", 4f);
        }

        void Update()
        {
            if (State == GameState.Briefing)
            {
                briefingLeft -= Time.deltaTime;
                if (briefingLeft <= 0f) SetState(GameState.Playing);
            }
            if (State == GameState.Playing)
            {
                Remaining -= Time.deltaTime;
                Elapsed += Time.deltaTime;
                if (!warnedFive && Remaining <= 300f && timeLimitSeconds > 330f) { warnedFive = true; Narrator.I?.SayNow("warden_five"); }
                if (!warnedOne && Remaining <= 60f) { warnedOne = true; Narrator.I?.SayNow("warden_one"); }
                if (Remaining <= 0f) { Remaining = 0f; Lose(); }
            }
            var st = CurrentStage;
            if (st != lastStage)
            {
                lastStage = st;
                StageChanged?.Invoke(st);
            }
        }

        // timer starts early if the player gets to work during the briefing
        public void Engage()
        {
            if (State == GameState.Briefing) SetState(GameState.Playing);
        }

        void SetState(GameState s)
        {
            if (State == s) return;
            State = s;
            StateChanged?.Invoke(s);
        }

        public bool IsSolved(PuzzleId p) => solved.Contains(p);
        public bool Active => State == GameState.Playing || State == GameState.Briefing;

        public Stage CurrentStage
        {
            get
            {
                if (!IsSolved(PuzzleId.Power)) return Stage.Power;
                if (!VisitedOffice && !IsSolved(PuzzleId.Clock) && !IsSolved(PuzzleId.Safe)) return Stage.Catwalk;
                if (!IsSolved(PuzzleId.Clock) || !IsSolved(PuzzleId.Safe)) return Stage.Office;
                if (!IsSolved(PuzzleId.Laser)) return Stage.Laser;
                if (!IsSolved(PuzzleId.Reboot)) return Stage.Reboot;
                return Stage.Done;
            }
        }

        public void Solve(PuzzleId p)
        {
            if (!Active || solved.Contains(p)) return;
            Engage();
            solved.Add(p);
            Sfx.Play2D("beep_ok", 0.6f);
            Hud.I?.Toast("Puzzle solved  " + solved.Count + " / " + PuzzleCount, 2.5f, UiKit.Good);
            switch (p)
            {
                case PuzzleId.Power:
                    Achievements.Unlock("lights");
                    Narrator.I?.Say("dee_power");
                    break;
                case PuzzleId.Safe:
                    if (SafeResets == 0) Achievements.Unlock("safecracker");
                    break;
                case PuzzleId.Laser:
                    if (MirrorSteps <= 24) Achievements.Unlock("mirrors");
                    Narrator.I?.Say("dee_final");
                    break;
            }
            if ((p == PuzzleId.Clock || p == PuzzleId.Safe) && IsSolved(PuzzleId.Clock) && IsSolved(PuzzleId.Safe))
                Narrator.I?.Say("dee_vault");
            PuzzleSolved?.Invoke(p);
            if (solved.Count == PuzzleCount) Win();
        }

        void OnZone(string id)
        {
            switch (id)
            {
                case "ladder": if (IsSolved(PuzzleId.Power)) Narrator.I?.Say("dee_catwalk"); break;
                case "catwalk": Achievements.Unlock("climber"); break;
                case "office":
                    if (!VisitedOffice) { VisitedOffice = true; Narrator.I?.Say("dee_office"); }
                    break;
                case "clock": if (VisitedOffice) Narrator.I?.Say("dee_clock"); break;
                case "safe": if (VisitedOffice) Narrator.I?.Say("dee_safe"); break;
            }
        }

        public string UseHint()
        {
            var st = CurrentStage;
            int i = (int)st;
            int tier = Mathf.Min(hintTier[i], 2);
            string text = GameText.Hint(st, tier, Secrets);
            if (Active && hintTier[i] < 3)
            {
                Engage();
                hintTier[i]++;
                HintsUsed++;
                Remaining = Mathf.Max(5f, Remaining - hintPenaltySeconds);
            }
            Sfx.Play2D("hint_chime", 0.6f);
            return text;
        }

        public void CollectChip()
        {
            Chips++;
            if (State == GameState.Playing || State == GameState.Briefing) Remaining += chipBonusSeconds;
            Hud.I?.Toast("Override chip " + Chips + " / " + totalChips + "   +" + Mathf.RoundToInt(chipBonusSeconds) + "s", 2.5f, UiKit.Good);
            if (Chips >= totalChips) Achievements.Unlock("collector");
        }

        public int ComputeScore()
        {
            int s = baseScore + Mathf.RoundToInt(Remaining) * pointsPerSecondLeft + Chips * pointsPerChip - HintsUsed * hintCost - Mistakes * mistakeCost;
            return Mathf.Max(0, s);
        }

        public void Pause(bool on)
        {
            if (State == GameState.Won || State == GameState.Lost) return;
            if (on && State != GameState.Paused) { pausedFrom = State; SetState(GameState.Paused); }
            else if (!on && State == GameState.Paused) SetState(pausedFrom);
        }
        GameState pausedFrom = GameState.Playing;

        void Win()
        {
            SetState(GameState.Won);
            Achievements.Unlock("escape");
            if (HintsUsed == 0) Achievements.Unlock("unassisted");
            if (Elapsed < 300f) Achievements.Unlock("nightowl");
            LastScore = ComputeScore();
            LastRank = Leaderboard.Submit(new Leaderboard.Entry
            {
                score = LastScore,
                seconds = Elapsed,
                chips = Chips,
                hints = HintsUsed,
                date = DateTime.Now.ToString("MMM d HH:mm")
            });
            Narrator.I?.SayNow("warden_win");
            Sfx.Play2D("win_stinger", 0.9f);
        }

        void Lose()
        {
            SetState(GameState.Lost);
            LastScore = 0;
            Narrator.I?.SayNow("warden_lose");
            Sfx.Play2D("lose_stinger", 0.9f);
            Haptics.Both(0.6f, 0.5f);
        }

        void OnAchievement(Achievements.Def d)
        {
            unlockedThisRun.Add(d.title);
            Hud.I?.Toast("Achievement: " + d.title, 3.5f, new Color(1f, 0.85f, 0.3f));
            Sfx.Play2D("chip_pickup", 0.5f);
        }

        public void Restart()
        {
            Haptics.StopAll();
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void ToMenu()
        {
            Haptics.StopAll();
            SceneManager.LoadScene(0);
        }

        // used by editor checks
        public void DebugSolve(PuzzleId p) => Solve(p);
        public void DebugVisitOffice() => OnZone("office");
        public void DebugSetRemaining(float s) => Remaining = s;
    }
}

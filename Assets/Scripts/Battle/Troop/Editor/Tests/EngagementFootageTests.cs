using System.Collections.Generic;
using System.Globalization;
using System.Text;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

// Footage of three engagements (tooling only): one simulation on a copy of the project's Battle Tuning with three
// experiments far enough apart (ExperimentSpacing, well beyond S_Distance) that they never sense each other:
//   1 Aligned:  a troop attacks (Pursue) an idle troop standing square in front of it.
//   2 Angled:   the same, starting AngledBearingDeg off the idle troop's front.
//   3 Mutual:   two troops attack each other, both marching.
// Every frame (30 a second) it writes each soldier's place, facing and state, and each troop's footprint, line and state,
// as CSV in an "engagement_footage" folder beside the test results. Tools/Replay/render_experiments.py draws them.
// Explicit: -testFilter EngagementFootageTests
[Category("Measurement")]
public sealed class EngagementFootageTests
{
    const float FrameTime = 1f / 30f;
    const float Seconds = 20f;
    const float ExperimentSpacing = 150f;
    const float StartDistance = 16f;
    const float AngledBearingDeg = 45f;
    const string Spearman = "Troop and Character Profiles/Characters/Generic/Spearman";

    readonly List<Object> _created = new();
    TroopTuning _tuning;

    [SetUp]
    public void SetUp()
    {
        TroopTuning.OverrideForTests(null);
        TroopTuning project = TroopTuning.Active;
        _tuning = TroopTuning.CreateDefaults();
        for (int i = 0; i < project.Sections.Count; i++) EditorUtility.CopySerialized(project.Sections[i], _tuning.Sections[i]);
        TroopTuning.OverrideForTests(_tuning);
    }

    [TearDown]
    public void TearDown()
    {
        TroopObjects.FlushPending();
        TestScene.Destroy(_created);
        TroopTuning.OverrideForTests(null);
        _tuning?.DestroySections();
        _tuning = null;
    }

    Troop Spawn(CharacterProfile character, string name, int team, Vector2 position, Vector2 facing)
    {
        var root = new GameObject(name);
        root.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, 0f, Mathf.Atan2(facing.y, facing.x) * Mathf.Rad2Deg));
        _created.Add(root);
        var troop = root.AddComponent<Troop>();
        troop.Configure(team, new List<CharacterProfile> { character });
        return troop;
    }

    static string Folder()
    {
        string[] args = System.Environment.GetCommandLineArgs();
        string root = System.IO.Path.GetFullPath("Temp");
        for (int i = 0; i + 1 < args.Length; i++)
            if (args[i] == "-testResults") root = System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(args[i + 1]));
        return System.IO.Path.Combine(root, "engagement_footage");
    }

    static string F(float v) => float.IsNaN(v) ? "nan" : v.ToString("0.###", CultureInfo.InvariantCulture);

    [Test, Explicit]
    public void RecordThreeEngagements()
    {
        var world = new GameObject("Test Combat World").AddComponent<TroopCombatWorld>();
        world.SetSeed(2025);
        world.Initialize();
        _created.Add(world.gameObject);
        var spear = Resources.Load<CharacterProfile>(Spearman);
        Assert.IsNotNull(spear, "Spearman profile");

        var troops = new List<(Troop troop, int experiment, string role)>();
        // 1 Aligned: the idle troop faces south; the attacker stands square in front of it, facing north.
        Vector2 o1 = new(0f, 0f);
        Troop idle1 = Spawn(spear, "Idle", 1, o1, Vector2.down);
        Troop attacker1 = Spawn(spear, "Attacker", 0, o1 + Vector2.down * StartDistance, Vector2.up);
        attacker1.Pursue(idle1);
        troops.Add((attacker1, 1, "attacker"));
        troops.Add((idle1, 1, "idle"));
        // 2 Angled: the attacker starts AngledBearingDeg off the idle troop's front, facing it.
        Vector2 o2 = new(ExperimentSpacing, 0f);
        Troop idle2 = Spawn(spear, "Idle", 1, o2, Vector2.down);
        float bearing = AngledBearingDeg * Mathf.Deg2Rad;
        Vector2 from = new(Mathf.Sin(bearing), -Mathf.Cos(bearing));
        Troop attacker2 = Spawn(spear, "Attacker", 0, o2 + from * StartDistance, -from);
        attacker2.Pursue(idle2);
        troops.Add((attacker2, 2, "attacker"));
        troops.Add((idle2, 2, "idle"));
        // 3 Mutual: two troops facing each other, each attacking the other.
        Vector2 o3 = new(2f * ExperimentSpacing, 0f);
        Troop south = Spawn(spear, "South", 0, o3 + Vector2.down * (StartDistance * 0.5f + 1f), Vector2.up);
        Troop north = Spawn(spear, "North", 1, o3 + Vector2.up * (StartDistance * 0.5f + 1f), Vector2.down);
        south.Pursue(north);
        north.Pursue(south);
        troops.Add((south, 3, "attacker"));
        troops.Add((north, 3, "attacker"));

        string folder = Folder();
        System.IO.Directory.CreateDirectory(folder);
        var table = new StringBuilder("index,name,team,experiment,role,character\n");
        for (int i = 0; i < troops.Count; i++)
            table.AppendLine($"{i},{troops[i].troop.name},{troops[i].troop.TeamId},{troops[i].experiment},{troops[i].role}," +
                $"{troops[i].troop.FirstCharacter.CharacterName}");
        var soldiers = new StringBuilder("frame,time,troop,soldier,x,y,fx,fy,flags\n");
        var states = new StringBuilder("frame,time,troop,px,py,kind,cx,cy,ax,ay,ffx,ffy,hl,hw,rr,lsx,lsy,lex,ley," +
            "engaged,assault,wheeling,assaultcharges,routed,retreating,dazed,disordered,alive,gap,shock\n");

        int frames = Mathf.CeilToInt(Seconds / FrameTime);
        for (int frame = 0; frame <= frames; frame++)
        {
            if (frame > 0) world.SimulateFrame(FrameTime);
            float time = frame * FrameTime;
            for (int i = 0; i < troops.Count; i++)
            {
                Troop t = troops[i].troop;
                if (t == null) continue;
                TroopFootprint fp = t.Footprint;
                TroopFormationData line = t.Formation;
                float gap = float.NaN;
                foreach (var record in t.Engagements)
                    if (record.Opponent != null && (float.IsNaN(gap) || record.Gap < gap)) gap = record.Gap;
                Vector2 pivot = t.transform.position;
                states.Append(frame).Append(',').Append(F(time)).Append(',').Append(i).Append(',')
                    .Append(F(pivot.x)).Append(',').Append(F(pivot.y)).Append(',').Append((int)fp.Kind).Append(',')
                    .Append(F(fp.Center.x)).Append(',').Append(F(fp.Center.y)).Append(',')
                    .Append(F(fp.Axis.x)).Append(',').Append(F(fp.Axis.y)).Append(',')
                    .Append(F(fp.Facing.x)).Append(',').Append(F(fp.Facing.y)).Append(',')
                    .Append(F(fp.HalfLength)).Append(',').Append(F(fp.HalfWidth)).Append(',').Append(F(fp.Rounding)).Append(',')
                    .Append(line != null ? F(line.Start.x) : "nan").Append(',').Append(line != null ? F(line.Start.y) : "nan").Append(',')
                    .Append(line != null ? F(line.End.x) : "nan").Append(',').Append(line != null ? F(line.End.y) : "nan").Append(',')
                    .Append(t.IsEngaged ? 1 : 0).Append(',').Append(t.IsAssaulting ? 1 : 0).Append(',')
                    .Append(t.IsAssaultWheeling ? 1 : 0).Append(',').Append(t.AssaultChargesMade).Append(',')
                    .Append(t.IsRouted ? 1 : 0).Append(',').Append(t.IsRetreating ? 1 : 0).Append(',')
                    .Append(t.IsDazed ? 1 : 0).Append(',').Append(t.IsDisordered ? 1 : 0).Append(',')
                    .Append(t.LivingSoldierCount).Append(',').Append(F(gap)).Append(',')
                    .Append(F(t.Shock)).Append('\n');
                for (int k = 0; k < t.Soldiers.Count; k++)
                {
                    SoldierAgentState s = t.Soldiers[k];
                    if (!s.IsAlive) continue;
                    Vector2 p = t.WorldPosition(s);
                    Vector2 f = s.Facing;
                    int flags = (s.IsCharging && !s.IsFeinting ? 1 : 0) | (s.IsClashColorPulsing ? 2 : 0) |
                                (s.IsStunned ? 4 : 0) | (s.IsFeinting ? 8 : 0) | (s.IsAssaultCharging ? 16 : 0) |
                                (s.IsFollowing ? 32 : 0) | (s.IsReturning ? 64 : 0);
                    soldiers.Append(frame).Append(',').Append(F(time)).Append(',').Append(i).Append(',').Append(k).Append(',')
                        .Append(F(p.x)).Append(',').Append(F(p.y)).Append(',').Append(F(f.x)).Append(',').Append(F(f.y))
                        .Append(',').Append(flags).Append('\n');
                }
            }
        }
        System.IO.File.WriteAllText(System.IO.Path.Combine(folder, "troops.csv"), table.ToString());
        System.IO.File.WriteAllText(System.IO.Path.Combine(folder, "soldiers.csv"), soldiers.ToString());
        System.IO.File.WriteAllText(System.IO.Path.Combine(folder, "troop_states.csv"), states.ToString());
        Assert.Pass($"{frames + 1} frames written to {folder}");
    }
}

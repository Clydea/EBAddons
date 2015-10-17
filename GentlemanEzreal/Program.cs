using System;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using EloBuddy.SDK.Rendering;
using SharpDX;
using Color = System.Drawing.Color;

namespace GentlemanEzreal
{
    internal class Program
    {
        public static Spell.Skillshot Q, W, R;
        public static Spell.Active E;
        public static Menu EzrealMenu, DrawMenu, ComboMenu, HarassMenu, LaneClearMenu, KSMenu, PredMenu;
        public static AIHeroClient Me = ObjectManager.Player;
        public static HitChance QHitChance;
        public static HitChance WHitChance;

        public static void Main(string[] args)
        {
            Loading.OnLoadingComplete += OnLoaded;
        }

        public static bool HasSpell(string s)
        {
            return Player.Spells.FirstOrDefault(o => o.SData.Name.Contains(s)) != null;
        }
        // Skills
        private static void OnLoaded(EventArgs args)
        {
            if (Player.Instance.ChampionName != "Ezreal")
                return;
            Bootstrap.Init(null);

            Q = new Spell.Skillshot(SpellSlot.Q, 1190, SkillShotType.Linear, 250, int.MaxValue, 65);
            W = new Spell.Skillshot(SpellSlot.W, 990, SkillShotType.Linear, 250, int.MaxValue, 80);
            E = new Spell.Active(SpellSlot.E, 700);
            R = new Spell.Skillshot(SpellSlot.R, 2000, SkillShotType.Linear, 1, int.MaxValue, 160);

            // Menu Settings
            EzrealMenu = MainMenu.AddMenu("Gentleman Ezreal", "gentlemanezreal");
            EzrealMenu.AddGroupLabel("Gentleman Ezreal");
            EzrealMenu.AddSeparator();
            EzrealMenu.AddLabel("This addon made by Clyde for EloBuddy users!");

            ComboMenu = EzrealMenu.AddSubMenu("Combo", "sbtw");
            ComboMenu.AddGroupLabel("Combo Settings");
            ComboMenu.AddSeparator();
            ComboMenu.Add("usecomboq", new CheckBox("Use Q"));
            ComboMenu.Add("usecombow", new CheckBox("Use W"));
            ComboMenu.AddSeparator();
            ComboMenu.Add("usecomboe", new CheckBox("Use E"));
            ComboMenu.AddSeparator();
            ComboMenu.Add("rkill", new CheckBox("R if Killable"));

            HarassMenu = EzrealMenu.AddSubMenu("HarassMenu", "Harass");
            HarassMenu.Add("useQHarass", new CheckBox("Use Q"));
            HarassMenu.Add("useWHarass", new CheckBox("Use W"));
            HarassMenu.Add("waitAA", new CheckBox("wait for AA to finish", false));

            KSMenu = EzrealMenu.AddSubMenu("KSMenu", "ksmenu");
            KSMenu.AddGroupLabel("Kill Steal");
            KSMenu.AddSeparator();
            KSMenu.Add("ksq", new CheckBox("KS with Q"));
            KSMenu.Add("ksr", new CheckBox("KS with R"));

            PredMenu = EzrealMenu.AddSubMenu("Prediction", "pred");
            PredMenu.AddGroupLabel("Prediction");
            PredMenu.AddSeparator();
            PredMenu.Add("predq", new CheckBox("Q Hit Chance [CHECK FOR MEDIUM | NO CHECK FOR HIGH]"));
            PredMenu.AddSeparator();
            PredMenu.Add("predw", new CheckBox("W Hit Chance [ CHECK FOR MEDIUM | NO CHECK FOR HIGH]"));

            DrawMenu = EzrealMenu.AddSubMenu("Drawings", "drawings");
            DrawMenu.AddGroupLabel("Drawings");
            DrawMenu.AddSeparator();
            DrawMenu.Add("drawq", new CheckBox("Draw Q"));
            DrawMenu.Add("draww", new CheckBox("Draw W"));
            DrawMenu.Add("drawe", new CheckBox("Draw E"));
            DrawMenu.AddSeparator();
            DrawMenu.Add("drawc", new CheckBox("Draw Combo Damage"));
            DrawMenu.Add("disable", new CheckBox("Disable Draw Combo Damage"));

            LaneClearMenu = EzrealMenu.AddSubMenu("Lane Clear", "laneclear");
            LaneClearMenu.AddGroupLabel("Lane Clear Settings");
            LaneClearMenu.Add("ClearQ", new CheckBox("Use Q"));

            Game.OnTick += Tick;
            Drawing.OnDraw += OnDraw;
            Drawing.OnDraw += OnDamageDraw;

        }


        //Drawings
        private static void OnDraw(EventArgs args)
        {
            if (!Me.IsDead)
            {
                if (DrawMenu["drawq"].Cast<CheckBox>().CurrentValue && Q.IsLearned)
                {
                    Drawing.DrawCircle(Me.Position, Q.Range, Color.Navy);
                }
                if (DrawMenu["drawe"].Cast<CheckBox>().CurrentValue && E.IsLearned)
                {
                    Drawing.DrawCircle(Me.Position, E.Range, Color.OrangeRed);
                }
                if (DrawMenu["draww"].Cast<CheckBox>().CurrentValue && W.IsLearned)
                {
                    Drawing.DrawCircle(Me.Position, W.Range, Color.DarkBlue);
                }
            }

        }

        // Combo Draw Damage
        public static void OnDamageDraw(EventArgs args)
        {
            var killableText = new Text("",
            new System.Drawing.Font(System.Drawing.FontFamily.GenericSansSerif, 9, System.Drawing.FontStyle.Bold));
            var disable = DrawMenu["disable"].Cast<CheckBox>().CurrentValue;
            var drawDamage = DrawMenu["drawc"].Cast<CheckBox>().CurrentValue;
            if (disable) return;

            if (drawDamage)

            {
                foreach (var ai in EntityManager.Heroes.Enemies)
                {
                    if (ai.IsValidTarget())
                    {
                        var drawn = 0;
                        if (ComboDamage(ai) >= ai.Health && drawn == 0)
                        {
                            killableText.Position = Drawing.WorldToScreen(ai.Position) - new Vector2(40, -40);
                            killableText.Color = Color.Red;
                            killableText.TextValue = "FULL COMBO TO KILL";
                            killableText.Draw();

                        }

                    }
                }
            }
        }

        private static void Tick(EventArgs args)
        {
            QHitChance = PredMenu["predq"].Cast<CheckBox>().CurrentValue ? HitChance.Medium : HitChance.High;
            WHitChance = PredMenu["predw"].Cast<CheckBox>().CurrentValue ? HitChance.Medium : HitChance.High;
            Killsteal();
            // AutoCast();

            {
                if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
                    Combo();
            }

            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear) ||
                Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.JungleClear))
            {
                LaneClear.LaneClear();
            }

            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Harass))
            {
                Harass();
            }

        }

        private static void Killsteal()
        {
            if (KSMenu["ksq"].Cast<CheckBox>().CurrentValue && Q.IsReady())
            {
                try
                {
                    foreach (
                        var etarget in
                            EntityManager.Heroes.Enemies.Where(
                                hero => hero.IsValidTarget(E.Range) && !hero.IsDead && !hero.IsZombie))
                    {
                        if (Me.GetSpellDamage(etarget, SpellSlot.Q) >= etarget.Health)
                        {
                            var poutput = Q.GetPrediction(etarget);
                            if (poutput.HitChance >= HitChance.Medium)
                            {
                                Q.Cast(poutput.CastPosition);
                            }
                        }
                    }
                }
                catch
                {
                    // ignored
                }
            }
            if (KSMenu["ksr"].Cast<CheckBox>().CurrentValue && R.IsReady())
            {
                try
                {
                    foreach (var rtarget in EntityManager.Heroes.Enemies.Where(hero => hero.IsValidTarget(R.Range)))
                    {
                        if (Me.GetSpellDamage(rtarget, SpellSlot.R) >= rtarget.Health)
                        {
                            var poutput = R.GetPrediction(rtarget);
                            if (poutput.HitChance >= HitChance.Medium)
                            {
                                R.Cast(poutput.CastPosition);
                            }
                        }
                    }
                }
                catch
                {
                    // ignored
                }
            }
        }

        //Combo Settings
        private static void Combo()
        {
            var useQ = ComboMenu["usecomboq"].Cast<CheckBox>().CurrentValue;
            var useW = ComboMenu["usecombow"].Cast<CheckBox>().CurrentValue;
            var useE = ComboMenu["usecomboe"].Cast<CheckBox>().CurrentValue;
            var useR = ComboMenu["rkill"].Cast<CheckBox>().CurrentValue;


            if (useQ && Q.IsReady())
            {
                var target = TargetSelector.GetTarget(Q.Range, DamageType.Magical);
                if (target.IsValidTarget(Q.Range))
                {
                    if (Q.GetPrediction(target).HitChance >= QHitChance)
                    {

                        Q.Cast(target);

                    }
                }
            }

            if (useW && W.IsReady())
            {
                var target = TargetSelector.GetTarget(W.Range, DamageType.Magical);
                if (target.IsValidTarget(W.Range))
                {
                    if (W.GetPrediction(target).HitChance >= WHitChance)
                    {
                        W.Cast(target);
                    }
                }

            }



            if (useR && R.IsReady())
            {
                var target = TargetSelector.GetTarget(R.Range, DamageType.Magical);
                if (target.IsValidTarget(R.Range))
                {
                    if (Me.GetSpellDamage(target, SpellSlot.R) > target.Health)
                    {
                        R.Cast(target);

                    }
                }

            }

        }

        public static void Harass()
        {
            var target = TargetSelector.GetTarget(800, DamageType.Magical);
            Orbwalker.OrbwalkTo(Game.CursorPos);
            if (Orbwalker.IsAutoAttacking && HarassMenu["waitAA"].Cast<CheckBox>().CurrentValue)
                return;
            if (HarassMenu["useQHarass"].Cast<CheckBox>().CurrentValue && Q.IsReady())
            {
                if (target.Distance(Me) <= Q.Range)
                {
                    var predQ = Q.GetPrediction(target).CastPosition;
                    Q.Cast(predQ);
                    return;
                }
            }

            if (HarassMenu["useWHarass"].Cast<CheckBox>().CurrentValue && W.IsReady())
            {
                if (target.Distance(Me) <= W.Range)
                {
                    var predW = W.GetPrediction(target).CastPosition;
                    W.Cast(predW);
                }
            }
        }

        // Calculate Combo Damage
        public static float ComboDamage(Obj_AI_Base target)
        {
            var damage = 0d;

            if (Q.IsReady(3))
            {
                damage += Me.GetSpellDamage(target, SpellSlot.Q);
            }

            if (E.IsReady(2))
            {
                damage += Me.GetSpellDamage(target, SpellSlot.E);
            }

            if (W.IsReady(4))
            {
                damage += Me.GetSpellDamage(target, SpellSlot.W);
            }

            if (R.IsReady(5))
            {
                damage += Me.GetSpellDamage(target, SpellSlot.R);
            }

            damage += Me.GetAutoAttackDamage(target) * 3;
            return (float)damage;
        }
    }
}
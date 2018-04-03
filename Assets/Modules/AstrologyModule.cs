﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using UnityEngine;
using Random = UnityEngine.Random;

public class AstrologyModule : MonoBehaviour
{
    public KMBombInfo BombInfo;
    public KMBombModule BombModule;
    public KMAudio KMAudio;
    public KMSelectable ButtonPO;
    public KMSelectable ButtonNO;
    public KMSelectable ButtonGO;

    public Material[] Elements;
    public Material[] Planets;
    public Material[] Zodiacs;
    public Material Zilch;

    public MeshRenderer Symbol1;
    public MeshRenderer Symbol2;
    public MeshRenderer Symbol3;

    private int PoorButton = 0;
    private int NoButton = 1;
    private int GoodButton = 2;

    private int NumElements = 4;
    private int NumPlanets = 10;
    private int NumZodiacs = 12;

    private bool isActive = false;
    private bool isComplete = false;
    private int OmenScore = 0;
    private int E = 0;
    private int P = 0;
    private int Z = 0;

    private int moduleId;
    private static int moduleIdCounter = 1;

    private int[] EvP = {
        0,0,1,-1,0,1,-2,2,0,-1,
        -2,0,-1,0,2,0,-2,2,0,1,
        -1,-1,0,-1,1,2,0,2,1,-2,
        -1,2,-1,0,-2,-1,0,2,-2,2,
    };

    private int[] EvZ = {
        1,0,-1,0,0,2,2,0,1,0,1,0,
        2,2,-1,2,-1,-1,-2,1,2,0,0,2,
        -2,-1,0,0,1,0,1,2,-1,-2,1,1,
        1,1,-2,-2,2,0,-1,1,0,0,-1,-1,
    };

    private int[] PvZ = {
        -1,-1,2,0,-1,0,-1,1,0,0,-2,-2,
        -2,0,1,0,2,0,-1,1,2,0,1,0,
        -2,-2,-1,-1,1,-1,0,-2,0,0,-1,1,
        -2,2,-2,0,0,1,-1,0,2,-2,-1,1,
        -2,0,-1,-2,-2,-2,-1,1,1,1,0,-1,
        -1,-2,1,-1,0,0,0,1,0,-1,2,0,
        -1,-1,0,0,1,1,0,0,0,0,-1,-1,
        -1,2,0,0,1,-2,1,0,2,-1,1,0,
        1,0,2,1,-1,1,1,1,0,-2,2,0,
        -1,0,0,-1,-2,1,2,1,1,0,0,-1,
    };

    protected int Lookup(int y, int x, int width)
    {
        return y * width + x;
    }

    protected string NameFromSymbol(MeshRenderer symbol)
    {
        string name = symbol.material.name.ToLower();
        if (name.Contains("instance"))
        {
            name = name.Substring(0, name.Length - " (instance)".Length);
        }
        return name;
    }

    protected int GetOmenDigit()
    {
        int n = OmenScore;
        if (n < 0) { n = -n; }
        return n % 10;
    }

    protected void Start()
    {
        // In Unity, we assigned all possible symbol materials to the game objects because otherwise they do not show up on Mac.
        // Here we reduce the number of materials back to one.
        Symbol1.materials = new[] { Zilch };
        Symbol2.materials = new[] { Zilch };
        Symbol3.materials = new[] { Zilch };

        moduleId = moduleIdCounter++;
        GetComponent<KMBombModule>().OnActivate += OnActivate;
        ButtonPO.OnInteract += delegate () { HandlePress(PoorButton); return false; };
        ButtonNO.OnInteract += delegate () { HandlePress(NoButton); return false; };
        ButtonGO.OnInteract += delegate () { HandlePress(GoodButton); return false; };
    }

    protected void OnActivate()
    {
        isActive = true;

        E = Random.Range(0, NumElements);
        P = Random.Range(0, NumPlanets);
        Z = Random.Range(0, NumZodiacs);

        OmenScore += EvP[Lookup(E, P, NumPlanets)];
        OmenScore += EvZ[Lookup(E, Z, NumZodiacs)];
        OmenScore += PvZ[Lookup(P, Z, NumZodiacs)];

        Symbol1.material = Elements[E];
        Symbol2.material = Planets[P];
        Symbol3.material = Zodiacs[Z];

        string s1 = NameFromSymbol(Symbol1);
        string s2 = NameFromSymbol(Symbol2);
        string s3 = NameFromSymbol(Symbol3);

        bool isIn1 = false;
        bool isIn2 = false;
        bool isIn3 = false;

        string serialNum = "";
        List<string> data = BombInfo.QueryWidgets(KMBombInfo.QUERYKEY_GET_SERIAL_NUMBER, null);
        foreach (string response in data)
        {
            Dictionary<string, string> responseDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(response);
            serialNum = responseDict["serial"].ToLower();
            break;
        }

        foreach (char c in serialNum)
        {
            if (s1.Contains("" + c)) { isIn1 = true; }
            if (s2.Contains("" + c)) { isIn2 = true; }
            if (s3.Contains("" + c)) { isIn3 = true; }
        }

        if (isIn1) { OmenScore++; } else { OmenScore--; }
        if (isIn2) { OmenScore++; } else { OmenScore--; }
        if (isIn3) { OmenScore++; } else { OmenScore--; }

        Debug.LogFormat("[Astrology #{0}] Element: {1} ({4}); Planet: {2} ({5}); Zodiac: {3} ({6})", moduleId, s1.ToUpper(), s2.ToUpper(), s3.ToUpper(), isIn1 ? "+1" : "−1", isIn2 ? "+1" : "−1", isIn3 ? "+1" : "−1");
        Debug.LogFormat("[Astrology #{0}] Table lookups: {1}/{2}={4}; {1}/{3}={5}; {2}/{3}={6}", moduleId, s1.ToUpper(), s2.ToUpper(), s3.ToUpper(), sgn(EvP[Lookup(E, P, NumPlanets)]), sgn(EvZ[Lookup(E, Z, NumZodiacs)]), sgn(PvZ[Lookup(P, Z, NumZodiacs)]));
        Debug.LogFormat("[Astrology #{0}] Total omen score: {1}", moduleId, OmenScore);
    }

    private string sgn(int number)
    {
        return (number < 0 ? "−" : number > 0 ? "+" : "") + System.Math.Abs(number);
    }

    protected bool HandlePress(int button)
    {
        KMAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, this.transform);
        GetComponent<KMSelectable>().AddInteractionPunch();

        if (!isActive || isComplete)
        {
            BombModule.HandleStrike();
        }
        else
        {
            bool complete = false;
            string timerText = BombInfo.GetFormattedTime();

            if (button == NoButton && OmenScore == 0)
            {
                complete = true;
            }
            if (button == PoorButton && OmenScore < 0 && timerText.Contains("" + GetOmenDigit()))
            {
                complete = true;
            }
            if (button == GoodButton && OmenScore > 0 && timerText.Contains("" + GetOmenDigit()))
            {
                complete = true;
            }

            Debug.LogFormat("[Astrology #{0}] You pressed {1} Omen at {2}.", moduleId, button == NoButton ? "No" : button == PoorButton ? "Poor" : "Good", timerText);
            if (complete)
            {
                Debug.LogFormat("[Astrology #{0}] Module solved.", moduleId);
                BombModule.HandlePass();
                isComplete = true;
            }
            else
            {
                BombModule.HandleStrike();
            }
        }

        return false;
    }

    void TwitchHandleForcedSolve()
    {
        KMAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, this.transform);
        GetComponent<KMSelectable>().AddInteractionPunch();
        Debug.LogFormat("[Astrology #{0}] Module forcibly solved.", moduleId);
        BombModule.HandlePass();
        isComplete = true;
    }

    private string TwitchHelpMessage = "Press good on 3 with !{0} press good on 3. Press bad on 2 with !{0} press bad on 2. No Omen is !{0} press no";

    Dictionary<string, KMSelectable> _twitchCommands = null;
    IEnumerator ProcessTwitchCommand(string command)
    {
        if (command.StartsWith("press ", StringComparison.InvariantCultureIgnoreCase))
            command = command.Substring(6).ToLowerInvariant();

        if (_twitchCommands == null)
            _twitchCommands = new Dictionary<string, KMSelectable>
            {
                { "good", ButtonGO },
                { "good on", ButtonGO },
                { "goodomen", ButtonGO },
                { "good omen", ButtonGO },
                { "goodomenon", ButtonGO },
                { "good omen on", ButtonGO },
                { "bad", ButtonPO },
                { "bad on", ButtonPO },
                { "badomen", ButtonPO },
                { "bad omen", ButtonPO },
                { "badomenon", ButtonPO },
                { "bad omen on", ButtonPO },
                { "poor", ButtonPO },
                { "poor on", ButtonPO },
                { "pooromen", ButtonPO },
                { "poor omen", ButtonPO },
                { "pooromenon", ButtonPO },
                { "poor omen on", ButtonPO },
                { "no", ButtonNO },
                { "no on", ButtonNO },
                { "noomen", ButtonNO },
                { "no omen", ButtonNO },
                { "noomenon", ButtonNO },
                { "no omen on", ButtonNO },
            };

        KMSelectable btn;
        if (_twitchCommands.TryGetValue(command.Trim().ToLowerInvariant(), out btn))
        {
            yield return null;
            yield return btn;
            yield return new WaitForSeconds(.1f);
            yield return btn;
            yield break;
        }

        var m = Regex.Match(command, @"^(.*)(\d)\s*$");
        if (m.Success && _twitchCommands.TryGetValue(m.Groups[1].Value.Trim().ToLowerInvariant(), out btn))
        {
            yield return null;
            if (btn == ButtonNO)
                yield return "waiting music";

            while (!BombInfo.GetFormattedTime().Contains(m.Groups[2].Value))
                yield return null;

            yield return btn;
            yield return new WaitForSeconds(.1f);
            yield return btn;
            yield break;
        }
    }
}

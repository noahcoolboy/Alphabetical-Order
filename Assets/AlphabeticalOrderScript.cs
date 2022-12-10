using System;
using System.Collections;
using System.Text.RegularExpressions;
using UnityEngine;

using Rnd = UnityEngine.Random;

public class AlphabeticalOrderScript : MonoBehaviour
{
    public KMAudio Audio;
    public KMNeedyModule Needy;

    public KMSelectable[] Keypad;
    public MeshRenderer[] LedsOff;
    public MeshRenderer[] LedsCorrect;
    public TextMesh[] Labels;

    private char[] letters = new char[4];
    private int currentProgress;
    private bool active;

    static int moduleIdCounter = 1;
    int moduleId;

    void Awake()
    {
        moduleId = moduleIdCounter++;

        Needy.OnNeedyActivation += OnNeedyActivation;
        Needy.OnNeedyDeactivation += OnNeedyDeactivation;
        Needy.OnTimerExpired += OnTimerExpired;

        for (int i = 0; i < Labels.Length; i++)
            Labels[i].text = "X";

        for (var i = 0; i < Keypad.Length; i++)
            AssignKeyHandler(i);
    }

    private void AssignKeyHandler(int keyIx)
    {
        Keypad[keyIx].OnInteract += delegate ()
        {
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, Keypad[keyIx].transform);
            Keypad[keyIx].AddInteractionPunch();

            if (!active)
                return false;

            if (Labels[keyIx].text[0] == letters[currentProgress] && !LedsCorrect[keyIx].gameObject.activeSelf)
            {
                LedsOff[keyIx].gameObject.SetActive(false);
                LedsCorrect[keyIx].gameObject.SetActive(true);
                currentProgress++;
            }
            else
            {
                if (LedsCorrect[keyIx].gameObject.activeSelf)
                    Debug.LogFormat("[Alphabetical Order #{0}] Incorrect, received a button that was already pressed", moduleId);
                else
                    Debug.LogFormat("[Alphabetical Order #{0}] Incorrect, received {2} but expected {3}", moduleId, currentProgress + 1, Labels[keyIx].text[0], letters[currentProgress]);
                Needy.OnStrike();
                currentProgress = 0;
                for (int i = 0; i < 4; i++)
                {
                    LedsOff[i].gameObject.SetActive(true);
                    LedsCorrect[i].gameObject.SetActive(false);
                }
                Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.Strike, transform);
            }

            if (currentProgress == 4)
            {
                for (int i = 0; i < 4; i++)
                {
                    LedsOff[i].gameObject.SetActive(true);
                    LedsCorrect[i].gameObject.SetActive(false);
                }

                currentProgress = 0;
                Solve();
                for (int i = 0; i < Labels.Length; i++)
                    Labels[i].text = "X";
            }
            return false;
        };
    }

    protected bool Solve()
    {
        active = false;
        Debug.LogFormat("[Alphabetical Order #{0}] Buttons pressed in alphabetical order", moduleId);
        Needy.OnPass();
        return false;
    }

    protected void OnNeedyActivation()
    {
        for (int i = 0; i < 4; i++)
        {
            letters[i] = (char) ('A' + Rnd.Range(0, 26));
            Labels[i].text = letters[i].ToString();
        }
        Debug.LogFormat("[Alphabetical Order #{0}] Buttons: {1}", moduleId, letters.Join());
        Array.Sort(letters);
        active = true;
    }

    protected void OnNeedyDeactivation()
    {
        active = false;
        currentProgress = 0;
        for (int i = 0; i < Labels.Length; i++)
            Labels[i].text = "X";
        for (int i = 0; i < 4; i++)
        {
            LedsOff[i].gameObject.SetActive(true);
            LedsCorrect[i].gameObject.SetActive(false);
        }
    }

    protected void OnTimerExpired()
    {
        Debug.LogFormat("[Alphabetical Order #{0}] The timer ran out before the buttons could be pressed alphabetically", moduleId);
        Needy.OnStrike();
        OnNeedyDeactivation();
    }

    //twitch plays
    #pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} TR BL 4 [Presses the top right, bottom left, and 4th button in reading order]";
    #pragma warning restore 414
    IEnumerator ProcessTwitchCommand(string command)
    {
        string[] parameters = Regex.Replace(command, @"\s+", " ").Split(' ');
        for (int i = 0; i < parameters.Length; i++)
        {
            if (!parameters[i].ToLowerInvariant().EqualsAny("1", "2", "3", "4", "tl", "tr", "bl", "br"))
            {
                yield return "sendtochaterror!f The specified button '" + parameters[i] + "' is invalid!";
                yield break;
            }
        }
        if (!active)
        {
            yield return "sendtochaterror Buttons cannot be pressed right now!";
            yield break;
        }
        yield return null;
        for (int i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].ToLowerInvariant().EqualsAny("1", "2", "3", "4"))
                Keypad[int.Parse(parameters[i]) - 1].OnInteract();
            else
                Keypad[Array.IndexOf(new string[] { "tl", "tr", "bl", "br" }, parameters[i].ToLowerInvariant())].OnInteract();
            yield return new WaitForSeconds(.1f);
        }
    }

    void TwitchHandleForcedSolve()
    {
        StartCoroutine(DealWithNeedy());
    }

    private IEnumerator DealWithNeedy()
    {
        while (true)
        {
            if (active)
            {
                for (int i = 0; i < 4; i++)
                {
                    if (Labels[i].text[0] == letters[currentProgress] && !LedsCorrect[i].gameObject.activeSelf)
                    {
                        Keypad[i].OnInteract();
                        yield return new WaitForSeconds(.1f);
                        break;
                    }
                }
            }
            else
                yield return null;
        }
    }
}
using System;
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

    void Awake()
    {
        GetComponent<KMNeedyModule>().OnNeedyActivation += OnNeedyActivation;
        GetComponent<KMNeedyModule>().OnNeedyDeactivation += OnNeedyDeactivation;
        GetComponent<KMNeedyModule>().OnTimerExpired += OnTimerExpired;

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

            if (Labels[keyIx].text[0] == letters[currentProgress])
            {
                LedsOff[keyIx].gameObject.SetActive(false);
                LedsCorrect[keyIx].gameObject.SetActive(true);
                currentProgress++;
            }
            else
            {
                OnTimerExpired();
                currentProgress = 0;
                Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.Strike, transform);
                for (int i = 0; i < Labels.Length; i++)
                    Labels[i].text = "X";
            }

            if (currentProgress == 4)
            {
                for (int i = 0; i < 4; i++)
                {
                    LedsOff[i].gameObject.SetActive(true);
                    LedsCorrect[i].gameObject.SetActive(false);
                }

                currentProgress = 0;
                Needy.HandlePass();
                for (int i = 0; i < Labels.Length; i++)
                    Labels[i].text = "X";
            }
            return false;
        };
    }

    protected bool Solve()
    {
        active = false;
        GetComponent<KMNeedyModule>().OnPass();
        return false;
    }

    protected void OnNeedyActivation()
    {
        for (int i = 0; i < 4; i++)
        {
            letters[i] = (char) ('A' + Rnd.Range(0, 26));
            Labels[i].text = letters[i].ToString();
        }
        Array.Sort(letters);
        active = true;
    }

    protected void OnNeedyDeactivation()
    {
        active = false;
        for (int i = 0; i < Labels.Length; i++)
            Labels[i].text = "X";
    }

    protected void OnTimerExpired()
    {
        GetComponent<KMNeedyModule>().OnStrike();
    }
}
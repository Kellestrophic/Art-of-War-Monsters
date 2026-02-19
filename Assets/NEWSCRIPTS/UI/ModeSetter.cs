using UnityEngine;

public class ModeSetter : MonoBehaviour
{
    const string Key = "MODE_VSAI";
    public void SetVsAI()   { PlayerPrefs.SetInt(Key, 1); PlayerPrefs.Save(); }
    public void SetDirect() { PlayerPrefs.SetInt(Key, 0); PlayerPrefs.Save(); }
}

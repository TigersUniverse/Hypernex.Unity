using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Hypernex.Game;
using UnityEngine;
using Logger = Hypernex.CCK.Logger;

namespace Hypernex.Tools.Debug
{
    [RequireComponent(typeof(DontDestroyMe))]
    public class MessagePackListener : MonoBehaviour
    {
        public static MessagePackListener Instance;
        
        private Dictionary<string, int> mps = new ();
        private Dictionary<string, int> msps = new ();

        public void AddMessage(string msgName, int size)
        {
            if (mps.ContainsKey(msgName))
                mps[msgName]++;
            else
                mps.Add(msgName, 1);
            if (msps.ContainsKey(msgName))
                msps[msgName] += size;
            else
                msps.Add(msgName, size);
        }

        private string mpsTxt;
        private string listTxt;
        
        private IEnumerator c()
        {
            while (true)
            {
                if (GameInstance.FocusedInstance == null)
                {
                    mpsTxt = "Join an instance";
                    listTxt = "for information!";
                }
                else
                {
                    int m = 0;
                    foreach (KeyValuePair<string,int> keyValuePair in new Dictionary<string, int>(mps))
                        m += keyValuePair.Value;
                    mpsTxt = $"Messages Per Second: {m}";
                    listTxt = String.Empty;
                    // TODO: Might throw Exception for Dictionary having different lengths
                    try
                    {
                        msps = msps.OrderByDescending(x => x.Value).ToDictionary(x => x.Key, x => x.Value);
                        foreach (KeyValuePair<string, int> keyValuePair in new Dictionary<string, int>(msps))
                            listTxt += $"{keyValuePair.Key} : {Math.Round((decimal) keyValuePair.Value / 1024, 2)} KB " +
                                       $"({mps[keyValuePair.Key]})\n";
                    }
                    catch(Exception){}
                }
                mps.Clear();
                msps.Clear();
                yield return new WaitForSeconds(1);
            }
        }

        private void OnEnable()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            if(Instance == this || Instance == null)
                StartCoroutine(c());
            Instance = this;
        }

        private void OnDisable() => StopCoroutine(c());

        private void OnDestroy()
        {
            if(Instance == this)
                Instance = null;
            StopCoroutine(c());
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 500, Screen.height));
            GUILayout.Label(mpsTxt);
            GUILayout.Label(listTxt);
            GUILayout.EndArea();
        }
    }
}
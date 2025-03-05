using System.Collections.Generic;
using UnityEngine;

namespace Hypernex.UI
{
    public class UIPage : MonoBehaviour
    {
        private static List<UIPage> pages = new List<UIPage>();

        public static void HideAll() => pages.ForEach(x => x.Hide());

        public GameObject PageToShow;

        public virtual void Show() => PageToShow.SetActive(true);
        public virtual void Hide() => PageToShow.SetActive(false);

        public void Start() => pages.Add(this);
        public void OnDestroy() => pages.Remove(this);
    }
}
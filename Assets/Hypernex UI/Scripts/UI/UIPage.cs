using System;
using System.Collections.Generic;
using UnityEngine;
using Logger = Hypernex.CCK.Logger;

namespace Hypernex.UI
{
    public class UIPage : MonoBehaviour
    {
        private static List<UIPage> pages = new List<UIPage>();
        private static UIPage LastPage;

        public static T GetPage<T>() where T : UIPage
        {
            Type tType = typeof(T);
            foreach (UIPage uiPage in pages)
            {
                if (uiPage.GetType() == tType)
                    return (T) uiPage;
            }
            Logger.CurrentLogger.Error("Could not find UIPage with Type " + tType.FullName);
            return null;
        }

        public static void HideAll() => pages.ForEach(x => x.Hide());

        public static void CancelReturn() => LastPage = null;
        
        public static void Return()
        {
            if(LastPage == null) return;
            LastPage.Show();
            LastPage = LastPage.PreviousPage;
        }

        public bool IsHidden => !PageToShow.activeSelf;
        protected bool HasInitialized { get; private set; }
        public GameObject PageToShow;
        public UIPage PreviousPage;
        public GameObject[] SubPageContainers;

        public virtual void Show(bool hideAll = true)
        {
            if(hideAll) HideAll();
            Initialize();
            PageToShow.SetActive(true);
            LastPage = this;
        }
        public virtual void Hide() => PageToShow.SetActive(false);
        public void Previous()
        {
            if(PreviousPage != null)
                LastPage = PreviousPage;
            Return();
        }

        public void ShowSubPage(int index)
        {
            if(IsHidden) Show();
            HideSubPages();
            SubPageContainers[index].SetActive(true);
        }
        public void ShowSubPage(GameObject container)
        {
            for (int i = 0; i < SubPageContainers.Length; i++)
            {
                GameObject compare = SubPageContainers[i];
                if(compare != container) continue;
                ShowSubPage(i);
                break;
            }
        }
        public void HideSubPages()
        {
            foreach (GameObject subPageContainer in SubPageContainers)
                subPageContainer.SetActive(false);
        }

        internal virtual void Initialize()
        {
            if(HasInitialized) return;
            HasInitialized = true;
            pages.Add(this);
        }

        private void OnDestroy() => pages.Remove(this);
    }
}
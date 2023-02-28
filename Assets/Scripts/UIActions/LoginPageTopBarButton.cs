﻿using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LoginPageTopBarButton : MonoBehaviour
{
    private static readonly List<LoginPageTopBarButton> Pages = new();

    public string PageName;
    public GameObject CorrespondingPage;
    public Button SelectionButton;
    public GameObject ButtonHighlight;

    public static void Show(string pageName)
    {
        foreach (LoginPageTopBarButton page in Pages)
        {
            bool match = page.PageName == pageName;
            page.CorrespondingPage.SetActive(match);
            if(page.ButtonHighlight != null)
                page.ButtonHighlight.SetActive(match);
        }
    }

    public void Show()
    {
        foreach (LoginPageTopBarButton page in Pages)
        {
            page.CorrespondingPage.SetActive(false);
            if(page.ButtonHighlight != null)
                page.ButtonHighlight.SetActive(false);
        }
        CorrespondingPage.SetActive(true);
        if(ButtonHighlight != null)
            ButtonHighlight.SetActive(true);
    }

    private void SanityCheck()
    {
        foreach (LoginPageTopBarButton page in Pages)
        {
            if (page.PageName == PageName)
            {
                DestroyImmediate(this);
                throw new Exception("Cannot have multiple page names!");
            }
        }
    }

    private void Start()
    {
        SanityCheck();
        Pages.Add(this);
        if(SelectionButton != null)
            SelectionButton.onClick.AddListener(Show);
    }

    private void OnDestroy()
    {
        if (Pages.Contains(this))
            Pages.Remove(this);
    }
}
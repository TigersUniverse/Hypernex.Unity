using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TopBarManager : MonoBehaviour
{
    public GameObject LoggedInObject;
    public TMP_Text WelcomeText;
    public Button SignoutButton;

    private readonly string[] greetings =
        {"Howdy", "Hello", "Greetings", "Welcome", "G'day", "Hey", "Howdy-do", "Shalom"};

    private bool isLoggingOut;
    
    void Start()
    {
        APIPlayer.OnUser += user =>
        {
            int i = new System.Random().Next(greetings.Length);
            WelcomeText.text = greetings[i] + ", " + user.Username;
        };
        APIPlayer.OnLogout += () => LoggedInObject.SetActive(false);
        SignoutButton.onClick.AddListener(() =>
        {
            if (!isLoggingOut)
            {
                isLoggingOut = true;
                APIPlayer.Logout(r => isLoggingOut = false);
            }
        });
    }
}

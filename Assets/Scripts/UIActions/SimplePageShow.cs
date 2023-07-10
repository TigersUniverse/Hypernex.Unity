using UnityEngine;

namespace Hypernex.UIActions
{
    public class SimplePageShow : MonoBehaviour
    {
        public void ShowPage(string n) => LoginPageTopBarButton.Show(n);
    }
}
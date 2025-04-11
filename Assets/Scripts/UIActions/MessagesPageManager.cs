using Hypernex.UI;
using Hypernex.UI.Templates;
using Hypernex.UIActions.Data;
using UnityEngine;

namespace Hypernex.UIActions
{
    public class MessagesPageManager : MonoBehaviour
    {
        public LoginPageTopBarButton Page;
        public DynamicScroll MessagesScroll;

        private TopTopLoginManager last;

        public void Show(TopTopLoginManager topTopLoginManager)
        {
            last = topTopLoginManager;
            Page.Show();
            for (int _ = 0; _ < TopTopLoginManager.UnreadMessages.Count; _++)
            {
                MessageMeta messageMeta = TopTopLoginManager.UnreadMessages.Dequeue();
                CreateMessageTemplate(messageMeta);
            }
        }

        public void Clear()
        {
            MessagesScroll.Clear();
            last.HideNotification();
            TopTopLoginManager.UnreadMessages.Clear();
            //OverlayManager.ClearNonImportantMessages();
        }
        
        private void CreateMessageTemplate(MessageMeta messageMeta)
        {
            GameObject message = DontDestroyMe.GetNotDestroyedObject("UITemplates").transform
                .Find("MessageTemplate").gameObject;
            GameObject newMessage = Instantiate(message);
            RectTransform c = newMessage.GetComponent<RectTransform>();
            newMessage.GetComponent<MessageTemplate>().Render(messageMeta);
            MessagesScroll.AddItem(c);
        }
    }
}
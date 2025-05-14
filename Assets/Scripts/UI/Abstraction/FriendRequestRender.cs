using Hypernex.Player;
using HypernexSharp.API;
using HypernexSharp.API.APIResults;

namespace Hypernex.UI.Abstraction
{
    public class FriendRequestRender : UserRender
    {
        private void OnResult(CallbackResult<EmptyResult> _) => Destroy(gameObject);

        public void OnAccept() =>
            APIPlayer.APIObject.AcceptFriendRequest(OnResult, APIPlayer.APIUser, APIPlayer.CurrentToken, u.Id);
        public void OnDeny() =>
            APIPlayer.APIObject.DeclineFriendRequest(OnResult, APIPlayer.APIUser, APIPlayer.CurrentToken, u.Id);
    }
}
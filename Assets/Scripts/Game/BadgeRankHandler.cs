using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Hypernex.UI.Pages;
using Hypernex.UI.Templates;
using HypernexSharp.APIObjects;
using UnityEngine;

namespace Hypernex.Game
{
    public abstract class BadgeRankHandler
    {
        private static List<BadgeRankHandler> badgeRankHandlers = new();
        
        public static BadgeRankHandler GetBadgeHandlerByName(string badgeName)
        {
            foreach (BadgeRankHandler badgeHandler in badgeRankHandlers)
            {
                if(badgeHandler.IsRank) continue;
                if (badgeHandler.Name == badgeName)
                    return badgeHandler;
            }
            return null;
        }

        public static List<BadgeRankHandler> GetRankHandlersByRank(Rank rank)
        {
            List<BadgeRankHandler> temp = new List<BadgeRankHandler>();
            foreach (BadgeRankHandler badgeRankHandler in badgeRankHandlers)
            {
                if(!badgeRankHandler.IsRank) continue;
                if(badgeRankHandler.TargetRanks.Contains(rank)) temp.Add(badgeRankHandler);
            }
            return temp;
        }

        static BadgeRankHandler()
        {
            List<Type> badgesRanksTypes = Assembly.GetExecutingAssembly().GetTypes()
                .Where(x => x.BaseType == typeof(BadgeRankHandler)).ToList();
            badgesRanksTypes.ForEach(x => Activator.CreateInstance(x));
        }
        
        /// <summary>
        /// If the current handler is for a Rank
        /// </summary>
        public abstract bool IsRank { get; }
        
        /// <summary>
        /// The Rank that this will be applied to
        /// </summary>
        public virtual Rank[] TargetRanks { get; }

        /// <summary>
        /// The name of the badge given to a user
        /// </summary>
        public virtual string Name { get; }

        /// <summary>
        /// How to apply the badge to a Profile
        /// </summary>
        /// <param name="profilePage">The Profile Page itself</param>
        /// <param name="targetUser">The User that the badge is being applied to</param>
        public abstract void ApplyToProfile(ProfilePage profilePage, User targetUser);

        /// <summary>
        /// How to apply the badge to a Nameplate
        /// </summary>
        /// <param name="nameplateTemplate">The NameplateTemplate itself</param>
        /// <param name="targetUser">The User that the badge is being applied to</param>
        public abstract void ApplyToNameplate(NameplateTemplate nameplateTemplate, User targetUser);

        /// <summary>
        /// BadgeRankHandler Utility to cast a Texture2D to a Sprite
        /// </summary>
        /// <param name="texture2D">The texture to make a Sprite</param>
        /// <returns>The new Sprite</returns>
        protected Sprite Texture2DToSprite(Texture2D texture2D)
        {
            Rect r = new Rect(0, 0, texture2D.width, texture2D.height);
            Vector2 p = new Vector2(texture2D.width / 2f, texture2D.height / 2f);
            Sprite sprite = Sprite.Create(texture2D, r, p);
            return sprite;
        }
        
        protected BadgeRankHandler() => badgeRankHandlers.Add(this);
        ~BadgeRankHandler() => badgeRankHandlers.Remove(this);
    }
}
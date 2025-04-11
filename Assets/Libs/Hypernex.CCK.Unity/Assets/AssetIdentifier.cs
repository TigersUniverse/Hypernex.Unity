using System;
using System.Collections;
using Hypernex.CCK.Auth;
using Hypernex.CCK.Unity.Auth;
using UnityEngine;
#if UNITY_EDITOR
using TriInspector;
using UnityEditor;
#endif

namespace Hypernex.CCK.Unity.Assets
{
    [Serializable]
#if UNITY_EDITOR
    [HideMonoScript]
    [ExecuteInEditMode]
#endif
    public class AssetIdentifier : MonoBehaviour
    {
        [SerializeField]
#if UNITY_EDITOR
        [HideInInspector]
#endif
        public string Id;
        
#if UNITY_EDITOR
        [ShowInInspector]
        [Title("Asset Identifier")]
        [HideLabel]
        [ValidateInput(nameof(ValidateIdInput))]
        [PropertySpace(SpaceAfter = 10)]
#endif
        public string UserInputId;
        
        private bool instanceExists;

#if UNITY_EDITOR
        [Button("Update Identifier")]
        [DisableIf(nameof(instanceExists), false)]
        [ValidateInput(nameof(ValidateLogin))]
        public async void SetIdentifier()
        {
            bool valid = await UserAuth.Instance.IsValidAsset(UserInputId);
            if(!valid)
            {
                EditorUtility.DisplayDialog("Hypernex.CCK.Unity", "Id is not valid!", "OK");
                return;
            }
            Id = UserInputId;
        }

        [Button("Remove Identifier")]
        public void RemoveIdentifier()
        {
            Id = String.Empty;
            UserInputId = String.Empty;
        }

        private TriValidationResult ValidateIdInput()
        {
            if (string.IsNullOrEmpty(Id))
                return TriValidationResult.Info(
                    "Asset Id is empty/uninitialized. This means a new asset will be created upon uploading.");
            return TriValidationResult.Valid;
        }
        
        private TriValidationResult ValidateLogin()
        {
            if (UserAuth.Instance == null || !UserAuth.Instance.IsAuth)
                return TriValidationResult.Error("Please login to update asset identifiers!");
            return TriValidationResult.Valid;
        }
        public static Action<AssetIdentifier> OnRequestAlign = identifier => { };

        private IEnumerator ForceIcons()
        {
            yield return new WaitForSeconds(0.01f);
            OnRequestAlign.Invoke(this);
        }

        private void OnEnable()
        {
            UserInputId = Id;
            StartCoroutine(ForceIcons());
        }

        private void Update()
        {
            instanceExists = UserAuth.Instance != null && UserAuth.Instance.IsAuth;
        }
#endif
    }
}
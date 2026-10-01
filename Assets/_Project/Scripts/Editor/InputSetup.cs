using UnityEditor;

namespace GodTower.Editor
{
    /// <summary>
    /// The game defines its input actions in code (<c>PointerClimbInput</c>), so the template project-wide
    /// actions asset is removed and unregistered.
    /// </summary>
    public static class InputSetup
    {
        private const string ProjectWideActionsKey = "com.unity.input.settings.actions";
        private const string TemplateActions = "Assets/InputSystem_Actions.inputactions";

        public static void Apply()
        {
            if (EditorBuildSettings.TryGetConfigObject(ProjectWideActionsKey, out UnityEngine.Object _))
                EditorBuildSettings.RemoveConfigObject(ProjectWideActionsKey);

            if (AssetDatabase.LoadMainAssetAtPath(TemplateActions) != null)
                AssetDatabase.DeleteAsset(TemplateActions);
        }
    }
}

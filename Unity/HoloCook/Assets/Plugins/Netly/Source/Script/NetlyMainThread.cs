namespace Netly
{
    using Netly.Core;
    using UnityEditor;
    using UnityEngine;
#if UNITY_EDITOR
    using layout = UnityEngine.GUILayout;
    using editor = UnityEditor.EditorGUILayout;
#endif


    public class NetlyMainThread : MonoBehaviour
    {
        private static NetlyMainThread instance;

        private void Awake()
        {
            if (instance == null || instance == this)
            {
                instance = this;
                MainThread.Automatic = true;
                return;
            }

            Destroy(gameObject);
            return;
        }

        private void Update()
        {
            MainThread.Clean();
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(NetlyMainThread))]
    public class NetlyMainThreadEditor : Editor
    {
        private NetlyMainThread m_mainThread;

        private void OnEnable()
        {
            m_mainThread = (NetlyMainThread)target;
        }

        public override void OnInspectorGUI()
        {
            editor.Space(5);
            editor.LabelField("Netly MainThread");
            editor.Space(20);
            editor.HelpBox
            (
                "This component needs to be always active for unity to receive events from netly.\n\n" +
                "If this component is inactive, the following errors may occur:\n" +
                "\n1. You will not receive any messages" +
                "\n2. It will not accept handling of (e.g: GameObject) in netly events" +
                "\n\n WARNING: Make sure there is an instance of the object in the scene and active",
                MessageType.None
            );
        }
    }
#endif
}